using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Common.Interfaces;

namespace OddScout.API.Controllers;

[Authorize]
public class GotoConversionController : BaseController
{
    private readonly IGotoConversionService _gotoConversionService;

    public GotoConversionController(IGotoConversionService gotoConversionService)
    {
        _gotoConversionService = gotoConversionService;
    }

    /// <summary>
    /// Compare goto_conversion algorithm with simple normalization for given odds
    /// </summary>
    [HttpPost("compare")]
    public IActionResult CompareConversionMethods([FromBody] CompareOddsRequest request)
    {
        try
        {
            if (request.Odds == null || request.Odds.Length < 2)
            {
                return BadRequest(new { error = "At least 2 odds values are required" });
            }

            if (request.Odds.Any(odd => odd <= 1.0m))
            {
                return BadRequest(new { error = "All odds must be greater than 1.0" });
            }

            var (gotoResult, simpleResult) = _gotoConversionService.CompareConversionMethods(request.Odds);

            // Calculate some additional metrics for comparison
            var gotoSum = gotoResult.Sum();
            var simpleSum = simpleResult.Sum();
            var maxDifference = gotoResult.Zip(simpleResult, (g, s) => Math.Abs(g - s)).Max();

            return Ok(new
            {
                inputOdds = request.Odds,
                gotoConversion = new
                {
                    probabilities = gotoResult.Select(p => Math.Round(p, 6)).ToArray(),
                    sum = Math.Round(gotoSum, 6),
                    percentages = gotoResult.Select(p => $"{Math.Round(p * 100, 2)}%").ToArray()
                },
                simpleNormalization = new
                {
                    probabilities = simpleResult.Select(p => Math.Round(p, 6)).ToArray(),
                    sum = Math.Round(simpleSum, 6),
                    percentages = simpleResult.Select(p => $"{Math.Round(p * 100, 2)}%").ToArray()
                },
                comparison = new
                {
                    maxDifference = Math.Round(maxDifference, 6),
                    maxDifferencePercentage = $"{Math.Round(maxDifference * 100, 2)}%",
                    significantImprovement = maxDifference > 0.01m ? "Yes" : "No"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test goto_conversion with common betting scenarios
    /// </summary>
    [HttpGet("test-scenarios")]
    public IActionResult TestCommonScenarios()
    {
        var scenarios = new[]
        {
            new { name = "Close Match", odds = new decimal[] { 2.10m, 3.40m, 3.20m } },
            new { name = "Strong Favorite", odds = new decimal[] { 1.25m, 5.50m, 8.00m } },
            new { name = "Longshot Special", odds = new decimal[] { 1.15m, 7.00m, 15.00m } },
            new { name = "Three-Way Even", odds = new decimal[] { 2.90m, 3.10m, 2.95m } },
            new { name = "Tennis Match", odds = new decimal[] { 1.50m, 2.75m } },
            new { name = "High Margin Bookmaker", odds = new decimal[] { 1.80m, 1.80m, 4.50m } }
        };

        var results = scenarios.Select(scenario =>
        {
            var (gotoResult, simpleResult) = _gotoConversionService.CompareConversionMethods(scenario.odds);
            var maxDiff = gotoResult.Zip(simpleResult, (g, s) => Math.Abs(g - s)).Max();

            return new
            {
                scenario = scenario.name,
                odds = scenario.odds,
                gotoConversion = gotoResult.Select(p => $"{Math.Round(p * 100, 1)}%").ToArray(),
                simpleNormalization = simpleResult.Select(p => $"{Math.Round(p * 100, 1)}%").ToArray(),
                maxDifference = $"{Math.Round(maxDiff * 100, 2)}%",
                bookmakerMargin = $"{Math.Round((scenario.odds.Select(o => 1m / o).Sum() - 1) * 100, 2)}%"
            };
        }).ToArray();

        return Ok(new
        {
            message = "Comparison of goto_conversion vs simple normalization across different betting scenarios",
            note = "goto_conversion typically shows larger differences in scenarios with high margins or extreme favorites/longshots",
            scenarios = results
        });
    }

    /// <summary>
    /// Get detailed explanation of the goto_conversion algorithm
    /// </summary>
    [HttpGet("algorithm-info")]
    public IActionResult GetAlgorithmInfo()
    {
        return Ok(new
        {
            algorithm = "goto_conversion",
            description = "Advanced probability conversion method that corrects for favorite-longshot bias in betting markets",
            advantages = new[]
            {
                "Corrects for systematic bias where favorites are undervalued and longshots are overvalued",
                "Uses statistical principles rather than simple proportional scaling",
                "Provides more accurate probability estimates compared to 1/odds normalization",
                "Non-iterative algorithm with O(n) complexity",
                "Used in 4+ Kaggle competition gold medal solutions"
            },
            whenToUse = new[]
            {
                "When accuracy in probability estimation is critical",
                "For markets with significant bookmaker margins",
                "When dealing with extreme favorites or longshots",
                "In value betting applications where small differences matter"
            },
            comparison = new
            {
                simpleNormalization = "Applies bookmaker margin proportionally across all outcomes",
                gotoConversion = "Applies margin reduction proportional to statistical uncertainty, correcting for bias"
            },
            technicalDetails = new
            {
                inputValidation = "All odds must be > 1.0, minimum 2 outcomes required",
                outputGuarantees = "Probabilities sum to exactly 1.0, all values between 0 and 1",
                biasCorrection = "Favorites get smaller adjustments, longshots get larger adjustments",
                marginHandling = "Distributed based on variance weights rather than equal scaling"
            }
        });
    }
}

public class CompareOddsRequest
{
    public decimal[] Odds { get; set; } = Array.Empty<decimal>();
}