using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Queries.GetBetHistory;

public class GetBetHistoryWithFiltersQueryHandler : IQueryHandler<GetBetHistoryWithFiltersQuery, PagedResult<BetDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBetHistoryWithFiltersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BetDto>> Handle(GetBetHistoryWithFiltersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Bets
            .Include(b => b.Event)
            .Where(b => b.UserId == request.UserId);

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(b => b.PlacedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(b => b.PlacedAt <= request.ToDate.Value);
        }

        if (request.MarketType.HasValue)
        {
            query = query.Where(b => b.MarketType == request.MarketType.Value);
        }

        // Order by most recent first
        query = query.OrderByDescending(b => b.PlacedAt);

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var bets = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var betDtos = bets.Select(b => new BetDto
        {
            Id = b.Id,
            EventId = b.EventId,
            League = b.Event.League,
            EventDateTime = b.Event.EventDateTime,
            Team1 = b.Event.Team1,
            Team2 = b.Event.Team2,
            MarketType = b.MarketType,
            SelectedOutcome = b.SelectedOutcome,
            SelectedOutcomeDescription = GetOutcomeDescription(b.SelectedOutcome, b.Event),
            Amount = b.Amount,
            Odds = b.Odds,
            PotentialReturn = b.PotentialReturn,
            ActualReturn = b.ActualReturn,
            Status = b.Status,
            StatusDescription = GetStatusDescription(b.Status),
            PlacedAt = b.PlacedAt,
            SettledAt = b.SettledAt,
            Profit = b.CalculateProfit()
        }).ToList();

        return PagedResult<BetDto>.Create(betDtos, request.PageNumber, request.PageSize, totalCount);
    }

    private static string GetOutcomeDescription(OutcomeType outcome, Event eventEntity)
    {
        return outcome switch
        {
            OutcomeType.Team1Win => eventEntity.Team1,
            OutcomeType.Draw => "Draw",
            OutcomeType.Team2Win => eventEntity.Team2,
            _ => "Unknown"
        };
    }

    private static string GetStatusDescription(BetStatus status)
    {
        return status switch
        {
            BetStatus.Open => "Open",
            BetStatus.Won => "Won",
            BetStatus.Lost => "Lost",
            BetStatus.Void => "Void",
            BetStatus.CashedOut => "Cashed Out",
            _ => "Unknown"
        };
    }
}