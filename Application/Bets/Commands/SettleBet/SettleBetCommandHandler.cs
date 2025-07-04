using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Commands.SettleBet;

public class SettleBetCommandHandler : ICommandHandler<SettleBetCommand, BetDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SettleBetCommandHandler> _logger;

    public SettleBetCommandHandler(
        IApplicationDbContext context,
        ILogger<SettleBetCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BetDto> Handle(SettleBetCommand request, CancellationToken cancellationToken)
    {
        // Find bet with related entities
        var bet = await _context.Bets
            .Include(b => b.Event)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BetId, cancellationToken);

        if (bet == null)
            throw new InvalidOperationException("Bet not found");

        if (bet.UserId != request.UserId)
            throw new InvalidOperationException("You can only settle your own bets");

        if (bet.Status != BetStatus.Open)
            throw new InvalidOperationException($"Cannot settle bet with status: {bet.Status}");

        var user = bet.User;
        var oldBalance = user.Balance;
        decimal transactionAmount = 0;
        string transactionDescription = "";
        TransactionType transactionType;

        // Settle the bet based on outcome
        switch (request.Outcome)
        {
            case BetOutcome.Won:
                bet.SettleAsWon();
                transactionAmount = bet.PotentialReturn ?? 0;
                transactionDescription = $"Bet won: {bet.Event.Team1} vs {bet.Event.Team2}";
                transactionType = TransactionType.BetWon;

                // Update user balance with winnings
                user.UpdateBalance(user.Balance + transactionAmount);
                _logger.LogInformation("Bet {BetId} settled as WON. User {UserId} received {Amount}",
                    bet.Id, user.Id, transactionAmount);
                break;

            case BetOutcome.Lost:
                bet.SettleAsLost();
                transactionAmount = 0; // No money back
                transactionDescription = $"Bet lost: {bet.Event.Team1} vs {bet.Event.Team2}";
                transactionType = TransactionType.BetPlaced; // No new transaction needed, original stake already deducted
                _logger.LogInformation("Bet {BetId} settled as LOST. User {UserId}", bet.Id, user.Id);
                break;

            case BetOutcome.Void:
                bet.VoidBet();
                transactionAmount = bet.Amount; // Return original stake
                transactionDescription = $"Bet voided: {bet.Event.Team1} vs {bet.Event.Team2}";
                transactionType = TransactionType.BetRefund;

                // Return original stake to user
                user.UpdateBalance(user.Balance + transactionAmount);
                _logger.LogInformation("Bet {BetId} settled as VOID. User {UserId} refunded {Amount}",
                    bet.Id, user.Id, transactionAmount);
                break;

            default:
                throw new ArgumentException($"Invalid bet outcome: {request.Outcome}");
        }

        // Create transaction only if there's money movement
        if (transactionAmount > 0)
        {
            var transaction = new Transaction(
                user.Id,
                transactionType,
                transactionAmount,
                oldBalance,
                transactionDescription,
                null, // No external reference
                bet.Id
            );

            transaction.CompleteTransaction();
            _context.Transactions.Add(transaction);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Return updated bet DTO
        return new BetDto
        {
            Id = bet.Id,
            EventId = bet.EventId,
            League = bet.Event.League,
            EventDateTime = bet.Event.EventDateTime,
            Team1 = bet.Event.Team1,
            Team2 = bet.Event.Team2,
            MarketType = bet.MarketType,
            SelectedOutcome = bet.SelectedOutcome,
            SelectedOutcomeDescription = GetOutcomeDescription(bet.SelectedOutcome, bet.Event),
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