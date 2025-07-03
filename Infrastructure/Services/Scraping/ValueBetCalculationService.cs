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
            _logger.LogInformation("Starting value bet calculation (Python-accurate)...");

            // 🔧 Load ALL events (no time filtering like Python)
            var betbyEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Betby)
                .ToListAsync(cancellationToken);

            var pinnacleEvents = await _context.Events
                .Include(e => e.Odds)
                .Where(e => e.Source == OddsSource.Pinnacle)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {BetbyCount} Betby events and {PinnacleCount} Pinnacle events",
                betbyEvents.Count, pinnacleEvents.Count);

            var valueBets = new List<ValueBetDto>();
            var matchAttempts = 0;
            var successfulMatches = 0;

            // 🔧 EXACT Python logic: iterate through betby_data
            foreach (var betbyEvent in betbyEvents)
            {
                matchAttempts++;

                var betbyOdds = betbyEvent.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                if (betbyOdds == null) continue;

                _logger.LogDebug("Processing Betby event {Count}: {Team1} vs {Team2} at {DateTime}",
                    matchAttempts, betbyEvent.Team1, betbyEvent.Team2, betbyEvent.EventDateTime);

                // 🔧 Python matching logic
                Event? bestMatch = null;
                decimal bestAvgScore = 0;
                const decimal teamSimilarityThreshold = 80m; // Exact Python value

                // Normalize betby teams (Python style)
                var normBetbyTeam1 = NormalizeStringPython(betbyEvent.Team1);
                var normBetbyTeam2 = NormalizeStringPython(betbyEvent.Team2);
                var betbyDateTime = FormatDateTimePython(betbyEvent.EventDateTime);

                // 🔧 EXACT Python loop: iterate over pinnacle matches
                foreach (var pinnacleEvent in pinnacleEvents)
                {
                    var pinnacleDateTime = FormatDateTimePython(pinnacleEvent.EventDateTime);

                    // 🔧 EXACT datetime match requirement (Python: betby_datetime != pinnacle_datetime)
                    if (betbyDateTime != pinnacleDateTime)
                        continue;

                    // Normalize pinnacle teams
                    var normPinnacleTeam1 = NormalizeStringPython(pinnacleEvent.Team1);
                    var normPinnacleTeam2 = NormalizeStringPython(pinnacleEvent.Team2);

                    // 🔧 Python fuzzy matching (approximating fuzz.token_set_ratio)
                    var team1Score = CalculateTokenSetRatio(normBetbyTeam1, normPinnacleTeam1);
                    var team2Score = CalculateTokenSetRatio(normBetbyTeam2, normPinnacleTeam2);
                    var avgScore = (team1Score + team2Score) / 2;

                    _logger.LogDebug("Team similarity: B({BT1}|{BT2}) vs P({PT1}|{PT2}) = {T1Score:F1}%/{T2Score:F1}% = {AvgScore:F1}%",
                        normBetbyTeam1, normBetbyTeam2, normPinnacleTeam1, normPinnacleTeam2, team1Score, team2Score, avgScore);

                    // 🔧 EXACT Python condition
                    if (team1Score >= teamSimilarityThreshold && team2Score >= teamSimilarityThreshold)
                    {
                        if (avgScore > bestAvgScore)
                        {
                            bestAvgScore = avgScore;
                            bestMatch = pinnacleEvent;
                        }
                    }
                }

                // 🔧 If match found, calculate EV (Python style)
                if (bestMatch != null)
                {
                    successfulMatches++;
                    _logger.LogInformation("✅ Match found! Betby: {BTeam1} vs {BTeam2} <-> Pinnacle: {PTeam1} vs {PTeam2} (Score: {Score:F1}%)",
                        betbyEvent.Team1, betbyEvent.Team2, bestMatch.Team1, bestMatch.Team2, bestAvgScore);

                    var pinnacleOdds = bestMatch.Odds.FirstOrDefault(o => o.MarketType == MarketType.Match1X2);
                    if (pinnacleOdds == null) continue;

                    try
                    {
                        // 🔧 Python EV calculation
                        var (valueBet, maxEv) = CalculateEVPythonStyle(
                            betbyEvent, betbyOdds, pinnacleOdds, bestAvgScore);

                        // 🔧 EXACT Python filter: max_ev >= 0.01
                        if (maxEv >= 0.01m)
                        {
                            _logger.LogInformation("🎯 VALUE BET FOUND! Max EV={EV:F6}", maxEv);

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

            _logger.LogInformation("Match Summary: {Attempts} attempts, {Successful} successful matches, {ValueBets} value bets found",
                matchAttempts, successfulMatches, valueBets.Count);

            // Clean old value bets
            var oldValueBets = await _context.ValueBets
                .Where(vb => vb.CalculatedAt < DateTime.UtcNow.AddHours(-24))
                .ToListAsync(cancellationToken);
            _context.ValueBets.RemoveRange(oldValueBets);

            await _context.SaveChangesAsync(cancellationToken);

            // Sort by Max EV descending (Python: matched_results.sort(key=lambda x: x['Max EV'], reverse=True))
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

    // 🔧 EXACT Python normalize_string function
    private string NormalizeStringPython(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        // Python: unicodedata.normalize('NFKD', s)
        var normalized = s.Normalize(NormalizationForm.FormKD);

        // Python: s.encode('ascii', 'ignore').decode('utf-8')
        var asciiBytes = Encoding.ASCII.GetBytes(normalized);
        var asciiString = Encoding.ASCII.GetString(asciiBytes);

        // Python: common_words = {'fc', 'cf', 'club', 'united', 'city', 'town', 'athletic', 'sport', 'association', 'al'}
        var commonWords = new HashSet<string> { "fc", "cf", "club", "united", "city", "town", "athletic", "sport", "association", "al" };

        // Python: re.sub(r'[^a-zA-Z0-9 ]', '', s.lower())
        var cleaned = Regex.Replace(asciiString.ToLower(), @"[^a-zA-Z0-9 ]", "");

        // Python: words = s.split() + filter common words
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Where(word => !commonWords.Contains(word))
                          .ToArray();

        return string.Join(" ", words);
    }

    // 🔧 Format DateTime to match Python string format
    private string FormatDateTimePython(DateTime dateTime)
    {
        // Ensure UTC and format consistently
        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        return utc.ToString("yyyy-MM-dd, HH:mm");
    }

    // 🔧 Approximate Python's fuzz.token_set_ratio
    private decimal CalculateTokenSetRatio(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 100m;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0m;

        // Token set approach: split into words and compare sets
        var tokens1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokens2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (tokens1.Count == 0 && tokens2.Count == 0) return 100m;
        if (tokens1.Count == 0 || tokens2.Count == 0) return 0m;

        // Calculate Jaccard similarity (intersection over union)
        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();

        var similarity = union > 0 ? (decimal)intersection / union : 0m;
        return similarity * 100m;
    }

    // 🔧 Python-style EV calculation
    private (ValueBet valueBet, decimal maxEv) CalculateEVPythonStyle(
        Event betbyEvent, Odd betbyOdds, Odd pinnacleOdds, decimal confidenceScore)
    {
        // Python: odds = [pinnacle_odd_team1, pinnacle_odd_draw, pinnacle_odd_team2]
        var pinnacleOddsArray = new decimal[] { pinnacleOdds.Team1Odd, pinnacleOdds.DrawOdd, pinnacleOdds.Team2Odd };

        // 🔧 Python: implied_probs = goto_conversion.goto_conversion(odds)
        // Since we don't have the exact goto_conversion function, use raw probabilities (no normalization)
        var impliedProbs = pinnacleOddsArray.Select(odd => 1m / odd).ToArray();

        // Python: calculate_ev for each outcome
        var evTeam1 = CalculateEVPython(impliedProbs[0], betbyOdds.Team1Odd - 1, 1);
        var evDraw = CalculateEVPython(impliedProbs[1], betbyOdds.DrawOdd - 1, 1);
        var evTeam2 = CalculateEVPython(impliedProbs[2], betbyOdds.Team2Odd - 1, 1);

        // Python: max_ev = max(ev_team1, ev_draw, ev_team2)
        var maxEv = Math.Max(Math.Max(evTeam1, evDraw), evTeam2);

        // Determine best outcome
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

    // 🔧 EXACT Python calculate_ev function
    private decimal CalculateEVPython(decimal prob, decimal gain, decimal loss)
    {
        // Python: return (prob * gain) - ((1 - prob) * loss)
        return (prob * gain) - ((1 - prob) * loss);
    }

    // Keep the interface methods for compatibility
    public decimal CalculateExpectedValue(decimal probability, decimal odd)
    {
        return CalculateEVPython(probability, odd - 1, 1);
    }

    public decimal[] CalculateImpliedProbabilities(decimal[] odds)
    {
        return odds.Select(odd => 1m / odd).ToArray();
    }
}