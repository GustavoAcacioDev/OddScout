using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Scraping.Commands.CalculateValueBets;

public class CalculateValueBetsCommandHandler : ICommandHandler<CalculateValueBetsCommand, List<ValueBetDto>>
{
    private readonly IValueBetCalculationService _valueCalculationService;

    public CalculateValueBetsCommandHandler(IValueBetCalculationService valueCalculationService)
    {
        _valueCalculationService = valueCalculationService;
    }

    public async Task<List<ValueBetDto>> Handle(CalculateValueBetsCommand request, CancellationToken cancellationToken)
    {
        return await _valueCalculationService.CalculateValueBetsAsync(cancellationToken);
    }
}