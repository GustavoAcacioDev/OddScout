using OddScout.Domain.Common;
using OddScout.Domain.Enums;
using System.Security.Cryptography;

namespace OddScout.Domain.Entities;

public class Event : Entity<Guid>
{
    public string League { get; private set; } = string.Empty;
    public DateTime EventDateTime { get; private set; }
    public string Team1 { get; private set; } = string.Empty;
    public string Team2 { get; private set; } = string.Empty;
    public EventStatus Status { get; private set; }
    public string? ExternalLink { get; private set; }
    public DateTime ScrapedAt { get; private set; }
    public OddsSource Source { get; private set; }

    // Navegação para as odds
    public ICollection<Odd> Odds { get; private set; } = new List<Odd>();

    private Event() { }

    public Event(string league, DateTime eventDateTime, string team1, string team2,
                 OddsSource source, string? externalLink = null)
    {
        Id = Guid.NewGuid();
        League = ValidateAndTrimString(league, nameof(league));
        EventDateTime = eventDateTime;
        Team1 = ValidateAndTrimString(team1, nameof(team1));
        Team2 = ValidateAndTrimString(team2, nameof(team2));
        Status = EventStatus.Scheduled;
        ExternalLink = externalLink;
        ScrapedAt = DateTime.UtcNow;
        Source = source;
    }

    public void UpdateStatus(EventStatus status)
    {
        Status = status;
    }

    public void UpdateScrapedAt()
    {
        ScrapedAt = DateTime.UtcNow;
    }

    private static string ValidateAndTrimString(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{propertyName} cannot be empty", propertyName);

        return value.Trim();
    }
}