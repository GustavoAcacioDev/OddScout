namespace OddScout.Application.DTOs.Scraping;

public class ScrapedEventDto
{
    public string League { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public string OddTeam1 { get; set; } = string.Empty;
    public string OddDraw { get; set; } = string.Empty;
    public string OddTeam2 { get; set; } = string.Empty;
    public string? Link { get; set; }
}