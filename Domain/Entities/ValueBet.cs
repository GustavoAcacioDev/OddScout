using OddScout.Domain.Common;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

public class ValueBet : Entity<Guid>
{
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public MarketType MarketType { get; private set; }
    public OutcomeType OutcomeType { get; private set; }
    public decimal BetbyOdd { get; private set; }
    public decimal PinnacleOdd { get; private set; }
    public decimal ImpliedProbability { get; private set; }
    public decimal ExpectedValue { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private ValueBet() { }

    public ValueBet(Guid eventId, MarketType marketType, OutcomeType outcomeType,
                    decimal betbyOdd, decimal pinnacleOdd, decimal impliedProbability,
                    decimal expectedValue, decimal confidenceScore)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        MarketType = marketType;
        OutcomeType = outcomeType;
        BetbyOdd = betbyOdd;
        PinnacleOdd = pinnacleOdd;
        ImpliedProbability = impliedProbability;
        ExpectedValue = expectedValue;
        ConfidenceScore = confidenceScore;
        CalculatedAt = DateTime.UtcNow;
    }

    public bool HasPositiveValue() => ExpectedValue > 0.01m;
}