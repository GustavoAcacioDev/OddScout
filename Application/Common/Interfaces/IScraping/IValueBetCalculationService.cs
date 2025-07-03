using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Common.Interfaces.IScraping;

public interface IValueBetCalculationService
{
    Task<List<ValueBetDto>> CalculateValueBetsAsync(CancellationToken cancellationToken = default);
    decimal CalculateExpectedValue(decimal probability, decimal odd);
    decimal[] CalculateImpliedProbabilities(decimal[] odds);
}