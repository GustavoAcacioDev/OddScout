using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;

namespace OddScout.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TestController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("pinnacle-single-event")]
    public async Task<IActionResult> TestSinglePinnacleEvent([FromQuery] string team1 = "Udinese", [FromQuery] string team2 = "Hellas")
    {
        try
        {
            // Criar HttpClient com descompressão automática
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var httpClient = new HttpClient(handler);

            var apiKey = _configuration["RapidApi:PinnacleKey"];

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "pinnacle-odds.p.rapidapi.com");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            var endpoint = "/kit/v1/markets?is_have_odds=true&sport_id=1";
            var response = await httpClient.GetAsync($"https://pinnacle-odds.p.rapidapi.com{endpoint}");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { Error = $"API returned {response.StatusCode}" });
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(content);

            // Procurar evento específico
            if (!jsonResponse.RootElement.TryGetProperty("events", out var eventsProperty))
            {
                return BadRequest(new { Error = "No 'events' property found" });
            }

            var events = eventsProperty.EnumerateArray();
            var foundEvents = new List<object>();

            foreach (var evt in events)
            {
                var home = evt.TryGetProperty("home", out var homeProp) ? homeProp.GetString() : "";
                var away = evt.TryGetProperty("away", out var awayProp) ? awayProp.GetString() : "";

                if (home?.Contains(team1, StringComparison.OrdinalIgnoreCase) == true ||
                    away?.Contains(team2, StringComparison.OrdinalIgnoreCase) == true ||
                    home?.Contains(team2, StringComparison.OrdinalIgnoreCase) == true ||
                    away?.Contains(team1, StringComparison.OrdinalIgnoreCase) == true)
                {
                    foundEvents.Add(new
                    {
                        Home = home,
                        Away = away,
                        FullEventData = evt.GetRawText()
                    });
                }
            }

            if (foundEvents.Any())
            {
                return Ok(new
                {
                    FoundEvents = foundEvents.Count,
                    Events = foundEvents
                });
            }
            else
            {
                // Se não encontrou, mostrar os primeiros 5 eventos para debug
                var sampleEvents = events.Take(5).Select(evt => new
                {
                    Home = evt.TryGetProperty("home", out var h) ? h.GetString() : "",
                    Away = evt.TryGetProperty("away", out var a) ? a.GetString() : "",
                    League = evt.TryGetProperty("league_name", out var l) ? l.GetString() : ""
                }).ToList();

                return Ok(new
                {
                    FoundEvents = 0,
                    Message = $"No events found containing '{team1}' or '{team2}'",
                    SampleEvents = sampleEvents,
                    TotalEvents = jsonResponse.RootElement.GetProperty("events").GetArrayLength()
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    [HttpGet("pinnacle-raw")]
    public async Task<IActionResult> TestPinnacleApi()
    {
        try
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using var httpClient = new HttpClient(handler);

            var apiKey = _configuration["RapidApi:PinnacleKey"];

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", "pinnacle-odds.p.rapidapi.com");

            var endpoint = "/kit/v1/markets?is_have_odds=true&sport_id=1";
            var response = await httpClient.GetAsync($"https://pinnacle-odds.p.rapidapi.com{endpoint}");

            var content = await response.Content.ReadAsStringAsync();

            return Ok(new
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                ContentLength = content.Length,
                ContentPreview = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }
}