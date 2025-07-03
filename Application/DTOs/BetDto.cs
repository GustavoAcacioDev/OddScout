using OddScout.Domain.Enums;

namespace OddScout.Application.DTOs;

public class BetDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string League { get; set; } = string.Empty;
    public DateTime EventDateTime { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public MarketType MarketType { get; set; }
    public OutcomeType SelectedOutcome { get; set; }
    public string SelectedOutcomeDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Odds { get; set; }
    public decimal PotentialReturn { get; set; }
    public decimal? ActualReturn { get; set; }
    public BetStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public decimal Profit { get; set; }
}