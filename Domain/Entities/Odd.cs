using OddScout.Domain.Common;
using OddScout.Domain.Enums;

namespace OddScout.Domain.Entities;

public class Odd : Entity<Guid>
{
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public MarketType MarketType { get; private set; }
    public decimal Team1Odd { get; private set; }
    public decimal DrawOdd { get; private set; }
    public decimal Team2Odd { get; private set; }
    public OddsSource Source { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Odd() { }

    public Odd(Guid eventId, MarketType marketType, decimal team1Odd,
               decimal drawOdd, decimal team2Odd, OddsSource source)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        MarketType = marketType;
        Team1Odd = ValidateOdd(team1Odd, nameof(team1Odd));
        DrawOdd = ValidateOdd(drawOdd, nameof(drawOdd));
        Team2Odd = ValidateOdd(team2Odd, nameof(team2Odd));
        Source = source;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateOdds(decimal team1Odd, decimal drawOdd, decimal team2Odd)
    {
        Team1Odd = ValidateOdd(team1Odd, nameof(team1Odd));
        DrawOdd = ValidateOdd(drawOdd, nameof(drawOdd));
        Team2Odd = ValidateOdd(team2Odd, nameof(team2Odd));
    }

    private static decimal ValidateOdd(decimal odd, string propertyName)
    {
        if (odd <= 0)
            throw new ArgumentException($"{propertyName} must be greater than zero", propertyName);

        return odd;
    }
}