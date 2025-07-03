using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Commands.PlaceBet;

public class PlaceBetCommandHandler : ICommandHandler<PlaceBetCommand, BetDto>
{
    private readonly IApplicationDbContext _context;

    public PlaceBetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BetDto> Handle(PlaceBetCommand request, CancellationToken cancellationToken)
    {
        // Validate user exists and has sufficient balance
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("User not found");

        if (user.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient balance");

        // Validate event exists and is not finished
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (eventEntity is null)
            throw new InvalidOperationException("Event not found");

        if (eventEntity.EventDateTime <= DateTime.UtcNow.AddHours(-2)) // Allow 2 hours grace period
            throw new InvalidOperationException("Cannot bet on past events");

        if (eventEntity.Status == EventStatus.Finished || eventEntity.Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Cannot bet on finished or cancelled events");

        // Create bet
        var bet = new Bet(
            request.UserId,
            request.EventId,
            request.MarketType,
            request.SelectedOutcome,
            request.Amount,
            request.Odds
        );

        // Update user balance
        user.UpdateBalance(user.Balance - request.Amount);

        // Save bet
        _context.Bets.Add(bet);
        await _context.SaveChangesAsync(cancellationToken);

        // Return DTO
        return new BetDto
        {
            Id = bet.Id,
            EventId = bet.EventId,
            League = eventEntity.League,
            EventDateTime = eventEntity.EventDateTime,
            Team1 = eventEntity.Team1,
            Team2 = eventEntity.Team2,
            MarketType = bet.MarketType,
            SelectedOutcome = bet.SelectedOutcome,
            SelectedOutcomeDescription = GetOutcomeDescription(bet.SelectedOutcome, eventEntity),
            Amount = bet.Amount,
            Odds = bet.Odds,
            PotentialReturn = bet.PotentialReturn,
            ActualReturn = bet.ActualReturn,
            Status = bet.Status,
            StatusDescription = GetStatusDescription(bet.Status),
            PlacedAt = bet.PlacedAt,
            SettledAt = bet.SettledAt,
            Profit = bet.CalculateProfit()
        };
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