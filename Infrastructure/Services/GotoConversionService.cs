using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Infrastructure.Services;

public class GotoConversionService : IGotoConversionService
{
    private readonly ILogger<GotoConversionService> _logger;

    public GotoConversionService(ILogger<GotoConversionService> logger)
    {
        _logger = logger;
    }

    public decimal[] ConvertOddsToProbabilities(decimal[] odds)
    {
        ValidateOdds(odds);

        try
        {
            var inverseProbs = odds.Select(odd => 1m / odd).ToArray();
            var totalInverse = inverseProbs.Sum();
            var margin = totalInverse - 1m;

            if (margin < 0.001m)
            {
                return NormalizeProbabilities(inverseProbs);
            }

            var adjustedProbs = ApplyGotoConversion(inverseProbs, odds, margin);
            var result = NormalizeProbabilities(adjustedProbs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in goto_conversion algorithm with odds: [{Odds}]",
                string.Join(", ", odds));

            _logger.LogWarning("Falling back to simple normalization");
            return FallbackNormalization(odds);
        }
    }

    private decimal[] ApplyGotoConversion(decimal[] inverseProbs, decimal[] odds, decimal margin)
    {
        var n = inverseProbs.Length;
        var adjustedProbs = new decimal[n];

        var weights = new decimal[n];
        for (int i = 0; i < n; i++)
        {
            var p = inverseProbs[i];
            var variance = p * (1m - p) * odds[i];
            weights[i] = (decimal)Math.Sqrt((double)variance);
        }

        var totalWeight = weights.Sum();

        for (int i = 0; i < n; i++)
        {
            var weightRatio = weights[i] / totalWeight;
            var marginReduction = margin * weightRatio;

            var biasCorrection = CalculateBiasCorrection(odds[i], inverseProbs[i]);
            var totalReduction = marginReduction * biasCorrection;

            adjustedProbs[i] = Math.Max(0.001m, inverseProbs[i] - totalReduction);
        }

        return adjustedProbs;
    }

    private decimal CalculateBiasCorrection(decimal odds, decimal inverseProb)
    {
        if (odds <= 2.0m)
        {
            return 0.7m;
        }
        else if (odds <= 4.0m)
        {
            return 1.0m;
        }
        else if (odds <= 10.0m)
        {
            return 1.3m;
        }
        else
        {
            return 1.6m;
        }
    }

    private decimal[] NormalizeProbabilities(decimal[] probabilities)
    {
        var sum = probabilities.Sum();

        if (sum <= 0)
        {
            throw new InvalidOperationException("Sum of probabilities must be greater than zero");
        }

        return probabilities.Select(p => p / sum).ToArray();
    }

    private decimal[] FallbackNormalization(decimal[] odds)
    {
        var inverseProbs = odds.Select(odd => 1m / odd).ToArray();
        return NormalizeProbabilities(inverseProbs);
    }

    private void ValidateOdds(decimal[] odds)
    {
        if (odds == null)
            throw new ArgumentNullException(nameof(odds), "Odds array cannot be null");

        if (odds.Length < 2)
            throw new ArgumentException("Odds array must contain at least 2 elements", nameof(odds));

        for (int i = 0; i < odds.Length; i++)
        {
            if (odds[i] <= 1.0m)
                throw new ArgumentException($"Odds[{i}] = {odds[i]} is invalid. All odds must be > 1.0", nameof(odds));

            if (odds[i] > 1000.0m)
                throw new ArgumentException($"Odds[{i}] = {odds[i]} is unrealistically high", nameof(odds));
        }
    }

    public (decimal[] gotoConversion, decimal[] simpleNormalization) CompareConversionMethods(decimal[] odds)
    {
        var gotoResult = ConvertOddsToProbabilities(odds);
        var simpleResult = FallbackNormalization(odds);

        return (gotoResult, simpleResult);
    }
}