using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Application.DTOs.Scraping;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace OddScout.Infrastructure.Services.Scraping;

public class ValueBetCalculationService : IValueBetCalculationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ValueBetCalculationService> _logger;

    public ValueBetCalculationService(IApplicationDbContext context, ILogger<ValueBetCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ValueBetDto>> CalculateValueBetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting value bet calculation...");

            // Buscar eventos das duas fontes com suas odds
            var betbyEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Betby)
                .Where(e => e.EventDateTime >= DateTime.UtcNow.AddHours(-2))
                .ToListAsync(cancellationToken);

            var pinnacleEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Pinnacle)
                .Where(e => e.EventDateTime >= DateTime.UtcNow.AddHours(-2))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {BetbyCount} Betby events and {PinnacleCount} Pinnacle events",
                betbyEvents.Count, pinnacleEvents.Count);

            var valueBets = new List<ValueBetDto>();
            var matchAttempts = 0;
            var successfulMatches = 0;

            foreach (var betbyEvent in betbyEvents.Take(20)) // Processar mais eventos
            {
                matchAttempts++;

                var betbyOdds = betbyEvent.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                if (betbyOdds == null)
                {
                    _logger.LogDebug("Skipping Betby event {EventId} - No 1X2 odds found", betbyEvent.Id);
                    continue;
                }

                _logger.LogInformation("Processing Betby event {Count}: {Team1} vs {Team2} at {DateTime}",
                    matchAttempts, betbyEvent.Team1, betbyEvent.Team2, betbyEvent.EventDateTime);

                // Procurar match no Pinnacle
                var pinnacleMatch = FindBestMatch(betbyEvent, pinnacleEvents);

                if (pinnacleMatch == null)
                {
                    _logger.LogDebug("No match found for: {Team1} vs {Team2} at {DateTime}",
                        betbyEvent.Team1, betbyEvent.Team2, betbyEvent.EventDateTime);
                    continue;
                }

                // Desempacotar a tupla corretamente
                var (matchedEvent, confidenceScore) = pinnacleMatch.Value;

                successfulMatches++;
                _logger.LogInformation("✅ Match found! Betby: {BTeam1} vs {BTeam2} <-> Pinnacle: {PTeam1} vs {PTeam2} (Score: {Score:F1}%)",
                    betbyEvent.Team1, betbyEvent.Team2, matchedEvent.Team1, matchedEvent.Team2, confidenceScore);

                var pinnacleOdds = matchedEvent.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                if (pinnacleOdds == null)
                {
                    _logger.LogWarning("Pinnacle match found but no 1X2 odds available");
                    continue;
                }

                // Calcular probabilidades implícitas baseadas no Pinnacle
                var pinnacleOddsArray = new decimal[]
                {
                    pinnacleOdds.Team1Odd,
                    pinnacleOdds.DrawOdd,
                    pinnacleOdds.Team2Odd
                };

                var impliedProbs = CalculateImpliedProbabilities(pinnacleOddsArray);

                // Calcular EV para cada outcome
                var evTeam1 = CalculateExpectedValue(impliedProbs[0], betbyOdds.Team1Odd);
                var evDraw = CalculateExpectedValue(impliedProbs[1], betbyOdds.DrawOdd);
                var evTeam2 = CalculateExpectedValue(impliedProbs[2], betbyOdds.Team2Odd);

                var outcomes = new[]
                {
                    new { EV = evTeam1, Outcome = OutcomeType.Team1Win, BetbyOdd = betbyOdds.Team1Odd, PinnacleOdd = pinnacleOdds.Team1Odd, Prob = impliedProbs[0] },
                    new { EV = evDraw, Outcome = OutcomeType.Draw, BetbyOdd = betbyOdds.DrawOdd, PinnacleOdd = pinnacleOdds.DrawOdd, Prob = impliedProbs[1] },
                    new { EV = evTeam2, Outcome = OutcomeType.Team2Win, BetbyOdd = betbyOdds.Team2Odd, PinnacleOdd = pinnacleOdds.Team2Odd, Prob = impliedProbs[2] }
                };

                var bestOutcome = outcomes.OrderByDescending(o => o.EV).First();

                // Threshold de 1% para value bets
                if (bestOutcome.EV >= 0.01m)
                {
                    _logger.LogInformation("🎯 VALUE BET FOUND! EV={EV:F4} for {Outcome}", bestOutcome.EV, bestOutcome.Outcome);

                    // Salvar no banco de dados
                    var valueBet = new ValueBet(
                        betbyEvent.Id,
                        MarketType.Match1X2,
                        bestOutcome.Outcome,
                        bestOutcome.BetbyOdd,
                        bestOutcome.PinnacleOdd,
                        bestOutcome.Prob,
                        bestOutcome.EV,
                        confidenceScore
                    );

                    _context.ValueBets.Add(valueBet);

                    // Adicionar ao resultado
                    valueBets.Add(new ValueBetDto
                    {
                        Id = valueBet.Id,
                        League = betbyEvent.League,
                        EventDateTime = betbyEvent.EventDateTime,
                        Team1 = betbyEvent.Team1,
                        Team2 = betbyEvent.Team2,
                        Link = betbyEvent.ExternalLink,
                        BestOutcome = bestOutcome.Outcome,
                        BetbyOdd = bestOutcome.BetbyOdd,
                        PinnacleOdd = bestOutcome.PinnacleOdd,
                        ImpliedProbability = bestOutcome.Prob,
                        ExpectedValue = bestOutcome.EV,
                        ConfidenceScore = confidenceScore,
                        CalculatedAt = valueBet.CalculatedAt
                    });
                }
            }

            _logger.LogInformation("Match Summary: {Attempts} attempts, {Successful} successful matches, {ValueBets} value bets found",
                matchAttempts, successfulMatches, valueBets.Count);

            // Limpar value bets antigos antes de salvar os novos
            var oldValueBets = await _context.ValueBets
                .Where(vb => vb.CalculatedAt < DateTime.UtcNow.AddHours(-2))
                .ToListAsync(cancellationToken);

            _context.ValueBets.RemoveRange(oldValueBets);

            // Salvar mudanças
            await _context.SaveChangesAsync(cancellationToken);

            // Ordenar por EV decrescente
            valueBets = valueBets.OrderByDescending(vb => vb.ExpectedValue).ToList();

            _logger.LogInformation("Value bet calculation completed. Found {Count} value bets", valueBets.Count);
            return valueBets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during value bet calculation");
            return new List<ValueBetDto>();
        }
    }

    public decimal CalculateExpectedValue(decimal probability, decimal odd)
    {
        var gain = odd - 1;
        var loss = 1m;
        return (probability * gain) - ((1 - probability) * loss);
    }

    public decimal[] CalculateImpliedProbabilities(decimal[] odds)
    {
        var impliedProbs = odds.Select(odd => 1m / odd).ToArray();
        var total = impliedProbs.Sum();
        return impliedProbs.Select(prob => prob / total).ToArray();
    }

    private (Event match, decimal confidenceScore)? FindBestMatch(Event betbyEvent, List<Event> pinnacleEvents)
    {
        Event? bestMatch = null;
        decimal bestScore = 0;

        // Threshold rigoroso para evitar matches falsos
        const decimal minThreshold = 75m;

        var candidatesInTimeWindow = pinnacleEvents
            .Where(p => AreDateTimesEqual(betbyEvent.EventDateTime, p.EventDateTime))
            .Where(p => AreLeaguesSimilar(betbyEvent.League, p.League))
            .ToList();

        foreach (var pinnacleEvent in candidatesInTimeWindow)
        {
            // Calcular similaridade dos times
            var team1Score = CalculateStringSimilarity(betbyEvent.Team1, pinnacleEvent.Team1);
            var team2Score = CalculateStringSimilarity(betbyEvent.Team2, pinnacleEvent.Team2);

            // Ambos os times devem ter similaridade mínima de 60%
            if (team1Score < 60m || team2Score < 60m)
                continue;

            var avgScore = (team1Score + team2Score) / 2;

            if (avgScore >= minThreshold && avgScore > bestScore)
            {
                bestScore = avgScore;
                bestMatch = pinnacleEvent;
            }
        }

        return bestMatch != null ? (bestMatch, bestScore) : null;
    }

    private bool AreDateTimesEqual(DateTime dt1, DateTime dt2)
    {
        // Converter ambos para UTC para comparação
        var utc1 = dt1.Kind == DateTimeKind.Utc ? dt1 : dt1.ToUniversalTime();
        var utc2 = dt2.Kind == DateTimeKind.Utc ? dt2 : dt2.ToUniversalTime();

        // Verificar se são do mesmo dia
        if (utc1.Date != utc2.Date)
            return false;

        // Tolerância de 4 horas para problemas de timezone
        var differenceHours = Math.Abs((utc1 - utc2).TotalHours);
        return differenceHours <= 4;
    }

    private bool AreLeaguesSimilar(string league1, string league2)
    {
        var normalized1 = league1.ToLowerInvariant();
        var normalized2 = league2.ToLowerInvariant();

        // Se uma é da Argentina e outra da Estonia, claramente diferentes
        var country1 = ExtractCountry(normalized1);
        var country2 = ExtractCountry(normalized2);

        return country1 == country2 || string.IsNullOrEmpty(country1) || string.IsNullOrEmpty(country2);
    }

    private string ExtractCountry(string league)
    {
        var countries = new[] { "argentina", "brazil", "italy", "england", "spain", "germany", "france", "estonia", "ireland", "usa", "australia", "china" };
        return countries.FirstOrDefault(country => league.Contains(country)) ?? "";
    }

    private decimal CalculateStringSimilarity(string s1, string s2)
    {
        var normalized1 = NormalizeString(s1);
        var normalized2 = NormalizeString(s2);

        if (normalized1 == normalized2) return 100;

        var distance = LevenshteinDistance(normalized1, normalized2);
        var maxLength = Math.Max(normalized1.Length, normalized2.Length);

        if (maxLength == 0) return 100;

        return Math.Max(0, 100 - (decimal)(distance * 100) / maxLength);
    }

    private string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // Remove acentos
        var normalized = input.Normalize(NormalizationForm.FormKD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        result = Regex.Replace(result, @"[^a-zA-Z0-9\s]", "").ToLowerInvariant();

        var commonWords = new[] {
            "fc", "cf", "club", "united", "city", "town", "athletic", "sport", "association", "al",
            "de", "vs", "v", "and", "&", "sc", "ac", "real", "cd", "ca", "rc",
            "football", "soccer", "futbol", "ii", "2", "u23", "u21", "reserves", "b", "youth"
        };

        var words = result.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Where(word => !commonWords.Contains(word) && word.Length > 1)
                          .ToArray();

        return string.Join(" ", words);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }
}