using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Scraping.Queries.GetValueBets;

public class GetValueBetsQueryHandler : IQueryHandler<GetValueBetsQuery, PagedResult<ValueBetDto>>
{
    private readonly IApplicationDbContext _context;

    public GetValueBetsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ValueBetDto>> Handle(GetValueBetsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ValueBets
            .Include(vb => vb.Event)
            .Where(vb => vb.ExpectedValue > 0);

        // Aplicar filtro de EV mínimo se especificado
        if (request.MinimumEV.HasValue)
        {
            query = query.Where(vb => vb.ExpectedValue >= request.MinimumEV.Value);
        }

        // Ordenar por EV decrescente (value bets mais lucrativos primeiro)
        query = query.OrderByDescending(vb => vb.ExpectedValue);

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var valueBets = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var valueBetDtos = valueBets.Select(vb => new ValueBetDto
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

        return PagedResult<ValueBetDto>.Create(valueBetDtos, request.PageNumber, request.PageSize, totalCount);
    }
}