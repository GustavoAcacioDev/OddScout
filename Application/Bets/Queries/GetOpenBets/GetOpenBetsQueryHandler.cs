using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Queries.GetOpenBets;

public class GetOpenBetsQueryHandler : IQueryHandler<GetOpenBetsQuery, List<BetDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOpenBetsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BetDto>> Handle(GetOpenBetsQuery request, CancellationToken cancellationToken)
    {
        var openBets = await _context.Bets
            .Include(b => b.Event)
            .Where(b => b.UserId == request.UserId && b.Status == BetStatus.Open)
            .OrderByDescending(b => b.PlacedAt)
            .ToListAsync(cancellationToken);

        return openBets.Select(b => new BetDto
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
            StatusDescription = "Open",
            PlacedAt = b.PlacedAt,
            SettledAt = b.SettledAt,
            Profit = 0 // Open bets have no profit yet
        }).ToList();
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
}