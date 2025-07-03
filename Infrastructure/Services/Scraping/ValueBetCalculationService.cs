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
    private readonly IGotoConversionService _gotoConversionService;

    public ValueBetCalculationService(
        IApplicationDbContext context,
        ILogger<ValueBetCalculationService> logger,
        IGotoConversionService gotoConversionService)
    {
        _context = context;
        _logger = logger;
        _gotoConversionService = gotoConversionService;
    }

    public async Task<List<ValueBetDto>> CalculateValueBetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting value bet calculation...");

            var betbyEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Betby)
                .ToListAsync(cancellationToken);

            var pinnacleEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Pinnacle)
                .ToListAsync(cancellationToken);

            var valueBets = new List<ValueBetDto>();
            var successfulMatches = 0;

            foreach (var betbyEvent in betbyEvents)
            {
                var betbyOdds = betbyEvent.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                if (betbyOdds == null) continue;

                Event? bestMatch = null;
                decimal bestAvgScore = 0;
                const decimal teamSimilarityThreshold = 80m;

                var normBetbyTeam1 = NormalizeStringPython(betbyEvent.Team1);
                var normBetbyTeam2 = NormalizeStringPython(betbyEvent.Team2);
                var betbyDateTime = FormatDateTimePython(betbyEvent.EventDateTime);

                foreach (var pinnacleEvent in pinnacleEvents)
                {
                    var pinnacleDateTime = FormatDateTimePython(pinnacleEvent.EventDateTime);

                    if (betbyDateTime != pinnacleDateTime)
                        continue;

                    var normPinnacleTeam1 = NormalizeStringPython(pinnacleEvent.Team1);
                    var normPinnacleTeam2 = NormalizeStringPython(pinnacleEvent.Team2);

                    var team1Score = CalculateTokenSetRatio(normBetbyTeam1, normPinnacleTeam1);
                    var team2Score = CalculateTokenSetRatio(normBetbyTeam2, normPinnacleTeam2);
                    var avgScore = (team1Score + team2Score) / 2;

                    if (team1Score >= teamSimilarityThreshold && team2Score >= teamSimilarityThreshold)
                    {
                        if (avgScore > bestAvgScore)
                        {
                            bestAvgScore = avgScore;
                            bestMatch = pinnacleEvent;
                        }
                    }
                }

                if (bestMatch != null)
                {
                    successfulMatches++;

                    var pinnacleOdds = bestMatch.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                    if (pinnacleOdds == null) continue;

                    try
                    {
                        var (valueBet, maxEv) = CalculateEVWithGotoConversion(
                            betbyEvent, betbyOdds, pinnacleOdds, bestAvgScore);

                        if (maxEv >= 0.01m)
                        {
                            _context.ValueBets.Add(valueBet);
                            valueBets.Add(new ValueBetDto
                            {
                                Id = valueBet.Id,
                                League = betbyEvent.League,
                                EventDateTime = betbyEvent.EventDateTime,
                                Team1 = betbyEvent.Team1,
                                Team2 = betbyEvent.Team2,
                                Link = betbyEvent.ExternalLink,
                                BestOutcome = valueBet.OutcomeType,
                                BetbyOdd = valueBet.BetbyOdd,
                                PinnacleOdd = valueBet.PinnacleOdd,
                                ImpliedProbability = valueBet.ImpliedProbability,
                                ExpectedValue = valueBet.ExpectedValue,
                                ConfidenceScore = valueBet.ConfidenceScore,
                                CalculatedAt = valueBet.CalculatedAt
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Skipping match due to invalid odds: {Error}", ex.Message);
                    }
                }
            }

            _logger.LogInformation("Found {Successful} successful matches, {ValueBets} value bets",
                successfulMatches, valueBets.Count);

            var oldValueBets = await _context.ValueBets
                .Where(vb => vb.CalculatedAt < DateTime.UtcNow.AddHours(-24))
                .ToListAsync(cancellationToken);
            _context.ValueBets.RemoveRange(oldValueBets);

            await _context.SaveChangesAsync(cancellationToken);

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

    private (ValueBet valueBet, decimal maxEv) CalculateEVWithGotoConversion(
        Event betbyEvent, Odd betbyOdds, Odd pinnacleOdds, decimal confidenceScore)
    {
        var pinnacleOddsArray = new decimal[] { pinnacleOdds.Team1Odd, pinnacleOdds.DrawOdd, pinnacleOdds.Team2Odd };
        var impliedProbs = _gotoConversionService.ConvertOddsToProbabilities(pinnacleOddsArray);

        var evTeam1 = CalculateEVPython(impliedProbs[0], betbyOdds.Team1Odd - 1, 1);
        var evDraw = CalculateEVPython(impliedProbs[1], betbyOdds.DrawOdd - 1, 1);
        var evTeam2 = CalculateEVPython(impliedProbs[2], betbyOdds.Team2Odd - 1, 1);

        var maxEv = Math.Max(Math.Max(evTeam1, evDraw), evTeam2);

        OutcomeType bestOutcome;
        decimal bestEv;
        decimal bestBetbyOdd;
        decimal bestPinnacleOdd;
        decimal bestImpliedProb;

        if (evTeam1 == maxEv)
        {
            bestOutcome = OutcomeType.Team1Win;
            bestEv = evTeam1;
            bestBetbyOdd = betbyOdds.Team1Odd;
            bestPinnacleOdd = pinnacleOdds.Team1Odd;
            bestImpliedProb = impliedProbs[0];
        }
        else if (evDraw == maxEv)
        {
            bestOutcome = OutcomeType.Draw;
            bestEv = evDraw;
            bestBetbyOdd = betbyOdds.DrawOdd;
            bestPinnacleOdd = pinnacleOdds.DrawOdd;
            bestImpliedProb = impliedProbs[1];
        }
        else
        {
            bestOutcome = OutcomeType.Team2Win;
            bestEv = evTeam2;
            bestBetbyOdd = betbyOdds.Team2Odd;
            bestPinnacleOdd = pinnacleOdds.Team2Odd;
            bestImpliedProb = impliedProbs[2];
        }

        var valueBet = new ValueBet(
            betbyEvent.Id,
            MarketType.Match1X2,
            bestOutcome,
            bestBetbyOdd,
            bestPinnacleOdd,
            bestImpliedProb,
            bestEv,
            confidenceScore
        );

        return (valueBet, maxEv);
    }

    private string NormalizeStringPython(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        var normalized = s.Normalize(NormalizationForm.FormKD);
        var asciiBytes = Encoding.ASCII.GetBytes(normalized);
        var asciiString = Encoding.ASCII.GetString(asciiBytes);

        var commonWords = new HashSet<string> { "fc", "cf", "club", "united", "city", "town", "athletic", "sport", "association", "al" };
        var cleaned = Regex.Replace(asciiString.ToLower(), @"[^a-zA-Z0-9 ]", "");

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Where(word => !commonWords.Contains(word))
                          .ToArray();

        return string.Join(" ", words);
    }

    private string FormatDateTimePython(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        return utc.ToString("yyyy-MM-dd, HH:mm");
    }

    private decimal CalculateTokenSetRatio(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 100m;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0m;

        var tokens1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokens2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (tokens1.Count == 0 && tokens2.Count == 0) return 100m;
        if (tokens1.Count == 0 || tokens2.Count == 0) return 0m;

        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();

        var similarity = union > 0 ? (decimal)intersection / union : 0m;
        return similarity * 100m;
    }

    private decimal CalculateEVPython(decimal prob, decimal gain, decimal loss)
    {
        return (prob * gain) - ((1 - prob) * loss);
    }

    public decimal CalculateExpectedValue(decimal probability, decimal odd)
    {
        return CalculateEVPython(probability, odd - 1, 1);
    }

    public decimal[] CalculateImpliedProbabilities(decimal[] odds)
    {
        return _gotoConversionService.ConvertOddsToProbabilities(odds);
    }
}