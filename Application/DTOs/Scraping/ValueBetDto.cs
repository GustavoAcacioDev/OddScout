using OddScout.Domain.Enums;

namespace OddScout.Application.DTOs.Scraping;

public class ValueBetDto
{
    public Guid Id { get; set; }
    public string League { get; set; } = string.Empty;
    public DateTime EventDateTime { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public string? Link { get; set; }
    public OutcomeType BestOutcome { get; set; }
    public decimal BetbyOdd { get; set; }
    public decimal PinnacleOdd { get; set; }
    public decimal ImpliedProbability { get; set; }
    public decimal ExpectedValue { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTime CalculatedAt { get; set; }
}