using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Application.DTOs.Scraping;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OddScout.Infrastructure.Services.Scraping;

public class PinnacleScrapingService : IPinnacleScrapingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinnacleScrapingService> _logger;

    public PinnacleScrapingService(HttpClient httpClient, IConfiguration configuration,
                                   ILogger<PinnacleScrapingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
    }

    public async Task<List<ScrapedEventDto>> ScrapeEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Pinnacle scraping...");

            var apiKey = _configuration["RapidApi:PinnacleKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Pinnacle API key not configured");
                return new List<ScrapedEventDto>();
            }

            _httpClient.DefaultRequestHeaders.Remove("x-rapidapi-key");
            _httpClient.DefaultRequestHeaders.Remove("x-rapidapi-host");
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "pinnacle-odds.p.rapidapi.com");

            var endpoint = "/kit/v1/markets?is_have_odds=true&sport_id=1";
            var fullUrl = $"https://pinnacle-odds.p.rapidapi.com{endpoint}";

            var response = await _httpClient.GetAsync(fullUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to fetch data from Pinnacle API. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                return new List<ScrapedEventDto>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            var pinnacleResponse = JsonSerializer.Deserialize<PinnacleApiResponse>(jsonContent, options);

            if (pinnacleResponse?.Events == null)
            {
                _logger.LogError("Failed to deserialize Pinnacle response or no events found");
                return new List<ScrapedEventDto>();
            }

            var cleanedBets = new List<ScrapedEventDto>();

            foreach (var eventData in pinnacleResponse.Events)
            {
                try
                {
                    var moneyLine = eventData.Periods?.Num0?.MoneyLine;

                    if (moneyLine != null && !string.IsNullOrEmpty(eventData.Starts))
                    {
                        if (moneyLine.Home.HasValue || moneyLine.Draw.HasValue || moneyLine.Away.HasValue)
                        {
                            var dt = DateTime.Parse(eventData.Starts, null, DateTimeStyles.RoundtripKind);

                            DateTime utcDateTime;
                            if (dt.Kind == DateTimeKind.Unspecified)
                            {
                                utcDateTime = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                            }
                            else if (dt.Kind == DateTimeKind.Local)
                            {
                                utcDateTime = dt.ToUniversalTime();
                            }
                            else
                            {
                                utcDateTime = dt;
                            }

                            var formattedDateTime = utcDateTime.ToString("yyyy-MM-dd, HH:mm");

                            var scrapedEvent = new ScrapedEventDto
                            {
                                League = eventData.LeagueName ?? "",
                                DateTime = formattedDateTime,
                                Team1 = eventData.Home ?? "",
                                Team2 = eventData.Away ?? "",
                                OddTeam1 = moneyLine.Home?.ToString() ?? "0",
                                OddDraw = moneyLine.Draw?.ToString() ?? "0",
                                OddTeam2 = moneyLine.Away?.ToString() ?? "0"
                            };

                            cleanedBets.Add(scrapedEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse event data for {Home} vs {Away}: {Error}",
                        eventData.Home, eventData.Away, ex.Message);
                }
            }

            _logger.LogInformation("Pinnacle scraping completed. Found {Count} events with valid odds", cleanedBets.Count);
            return cleanedBets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pinnacle scraping");
            return new List<ScrapedEventDto>();
        }
    }

    private class PinnacleApiResponse
    {
        [JsonPropertyName("events")]
        public List<PinnacleEvent> Events { get; set; } = new();
    }

    private class PinnacleEvent
    {
        [JsonPropertyName("league_name")]
        public string? LeagueName { get; set; }

        [JsonPropertyName("starts")]
        public string? Starts { get; set; }

        [JsonPropertyName("home")]
        public string? Home { get; set; }

        [JsonPropertyName("away")]
        public string? Away { get; set; }

        [JsonPropertyName("periods")]
        public PinnaclePeriods? Periods { get; set; }
    }

    private class PinnaclePeriods
    {
        [JsonPropertyName("num_0")]
        public PinnacleMoneyLineContainer? Num0 { get; set; }
    }

    private class PinnacleMoneyLineContainer
    {
        [JsonPropertyName("money_line")]
        public PinnacleMoneyLine? MoneyLine { get; set; }
    }

    private class PinnacleMoneyLine
    {
        [JsonPropertyName("home")]
        public decimal? Home { get; set; }

        [JsonPropertyName("draw")]
        public decimal? Draw { get; set; }

        [JsonPropertyName("away")]
        public decimal? Away { get; set; }
    }
}