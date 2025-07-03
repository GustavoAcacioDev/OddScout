using OddScout.Domain.Common;
using OddScout.Domain.Enums;

namespace OddScout.Domain.Entities;

public class Bet : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public MarketType MarketType { get; private set; }
    public OutcomeType SelectedOutcome { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Odds { get; private set; }
    public decimal PotentialReturn { get; private set; }
    public decimal? ActualReturn { get; private set; }
    public BetStatus Status { get; private set; }
    public DateTime PlacedAt { get; private set; }
    public DateTime? SettledAt { get; private set; }

    // EF Core constructor
    private Bet() { }

    // Domain constructor
    public Bet(Guid userId, Guid eventId, MarketType marketType, OutcomeType selectedOutcome,
               decimal amount, decimal odds)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EventId = eventId;
        MarketType = marketType;
        SelectedOutcome = selectedOutcome;
        Amount = ValidateAmount(amount);
        Odds = ValidateOdds(odds);
        PotentialReturn = CalculatePotentialReturn(amount, odds);
        Status = BetStatus.Open;
        PlacedAt = DateTime.UtcNow;
    }

    public void SettleAsWon()
    {
        if (Status != BetStatus.Open)
            throw new InvalidOperationException("Only open bets can be settled");

        Status = BetStatus.Won;
        ActualReturn = PotentialReturn;
        SettledAt = DateTime.UtcNow;
    }

    public void SettleAsLost()
    {
        if (Status != BetStatus.Open)
            throw new InvalidOperationException("Only open bets can be settled");

        Status = BetStatus.Lost;
        ActualReturn = 0;
        SettledAt = DateTime.UtcNow;
    }

    public void VoidBet()
    {
        if (Status != BetStatus.Open)
            throw new InvalidOperationException("Only open bets can be voided");

        Status = BetStatus.Void;
        ActualReturn = Amount; // Return original stake
        SettledAt = DateTime.UtcNow;
    }

    public decimal CalculateProfit()
    {
        return Status switch
        {
            BetStatus.Won => PotentialReturn - Amount,
            BetStatus.Lost => -Amount,
            BetStatus.Void => 0,
            _ => 0 // Open bets have no realized profit yet
        };
    }

    private static decimal ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Bet amount must be greater than zero", nameof(amount));

        if (amount > 10000) // Arbitrary maximum bet limit
            throw new ArgumentException("Bet amount exceeds maximum limit", nameof(amount));

        return amount;
    }

    private static decimal ValidateOdds(decimal odds)
    {
        if (odds <= 1.0m)
            throw new ArgumentException("Odds must be greater than 1.0", nameof(odds));

        return odds;
    }

    private static decimal CalculatePotentialReturn(decimal amount, decimal odds)
    {
        return amount * odds;
    }
}