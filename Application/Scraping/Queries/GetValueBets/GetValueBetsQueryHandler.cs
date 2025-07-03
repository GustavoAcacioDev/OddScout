using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs.Scraping;
using OddScout.Domain.Enums;

namespace OddScout.Application.Scraping.Queries.GetValueBets;

public class GetValueBetsQueryHandler : IQueryHandler<GetValueBetsQuery, List<ValueBetDto>>
{
    private readonly IApplicationDbContext _context;

    public GetValueBetsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ValueBetDto>> Handle(GetValueBetsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ValueBets
            .Include(vb => vb.Event)
            .Where(vb => vb.ExpectedValue > 0);

        // Aplicar filtro de EV mínimo se especificado
        if (request.MinimumEV.HasValue)
        {
            query = query.Where(vb => vb.ExpectedValue >= request.MinimumEV.Value);
        }

        // Ordenar por EV decrescente
        query = query.OrderByDescending(vb => vb.ExpectedValue);

        // Aplicar limite se especificado
        if (request.Take.HasValue)
        {
            query = query.Take(request.Take.Value);
        }

        var valueBets = await query.ToListAsync(cancellationToken);

        return valueBets.Select(vb => new ValueBetDto
        {
            Id = vb.Id,
            League = vb.Event.League,
            EventDateTime = vb.Event.EventDateTime,
            Team1 = vb.Event.Team1,
            Team2 = vb.Event.Team2,
            Link = vb.Event.ExternalLink,
            BestOutcome = vb.OutcomeType,
            BetbyOdd = vb.BetbyOdd,
            PinnacleOdd = vb.PinnacleOdd,
            ImpliedProbability = vb.ImpliedProbability,
            ExpectedValue = vb.ExpectedValue,
            ConfidenceScore = vb.ConfidenceScore,
            CalculatedAt = vb.CalculatedAt
        }).ToList();
    }
}