using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Application.DTOs.Scraping;
using System.Text.RegularExpressions;

namespace OddScout.Infrastructure.Services.Scraping;

public class BetbyScrapingService : IBetbyScrapingService
{
    private readonly ILogger<BetbyScrapingService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public BetbyScrapingService(ILogger<BetbyScrapingService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ScrapedEventDto>> ScrapeEventsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting Betby scraping...");

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = await context.NewPageAsync();

            const string url = "https://demo.betby.com/sportsbook/tile/event-builder?selectedSports=soccer-1&selectedRange=1";
            await page.GotoAsync(url);
            await page.WaitForTimeoutAsync(5000);

            await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 20000 });
            _logger.LogInformation("Page loaded successfully");

            // Obter o Shadow Root
            var shadowRoot = await GetShadowRootAsync(page, "#bt-inner-page");
            if (shadowRoot == null)
            {
                _logger.LogError("Could not access Shadow DOM");
                return new List<ScrapedEventDto>();
            }

            var btRoot = await shadowRoot.QuerySelectorAsync("#bt-root");
            if (btRoot == null)
            {
                _logger.LogError("#bt-root element not found in Shadow DOM");
                return new List<ScrapedEventDto>();
            }

            // Dismiss modal se existir
            await DismissModalAsync(page);

            // Calcular total de páginas
            await btRoot.WaitForSelectorAsync("[data-editor-id=\"eventCardPaginator\"]");
            var totalBets = await GetTotalBetsAsync(btRoot);
            var lastPage = (int)Math.Ceiling(totalBets / 24.0);
            var nextButton = await GetNextButtonAsync(btRoot);

            var cleanedData = new List<ScrapedEventDto>();

            for (int currentPage = 1; currentPage <= lastPage; currentPage++)
            {
                _logger.LogInformation("Scraping page {CurrentPage} of {LastPage}", currentPage, lastPage);

                await btRoot.WaitForSelectorAsync("[data-editor-id=\"eventCard\"]");
                var eventCards = await btRoot.QuerySelectorAllAsync("[data-editor-id=\"eventCard\"]");

                foreach (var card in eventCards)
                {
                    try
                    {
                        var linkElement = await card.QuerySelectorAsync("[data-editor-id=\"eventCardContent\"]");
                        var href = linkElement != null ? await linkElement.GetAttributeAsync("href") : null;
                        var fullText = await card.InnerTextAsync();
                        var parsed = ParseRawText(fullText);

                        if (parsed != null)
                        {
                            parsed.Link = href;
                            cleanedData.Add(parsed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to parse event card: {Error}", ex.Message);
                    }
                }

                if (currentPage < lastPage && nextButton != null)
                {
                    await nextButton.ClickAsync();
                    await btRoot.WaitForSelectorAsync("[data-editor-id=\"eventCard\"]:nth-child(1)",
                        new ElementHandleWaitForSelectorOptions { State = WaitForSelectorState.Attached });
                }
            }

            _logger.LogInformation("Betby scraping completed. Found {Count} events", cleanedData.Count);
            return cleanedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Betby scraping");
            return new List<ScrapedEventDto>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IElementHandle?> GetShadowRootAsync(IPage page, string selector)
    {
        try
        {
            var shadowHost = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 20000 });
            if (shadowHost == null) return null;

            var shadowRootHandle = await shadowHost.EvaluateHandleAsync("node => node.shadowRoot");
            return shadowRootHandle.AsElement();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shadow root for {Selector}", selector);
            return null;
        }
    }

    private async Task DismissModalAsync(IPage page)
    {
        try
        {
            var dismissBtn = await page.QuerySelectorAsync("button");
            if (dismissBtn != null)
            {
                await dismissBtn.ClickAsync(new ElementHandleClickOptions { Force = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("No modal to dismiss or failed to dismiss: {Error}", ex.Message);
        }
    }

    private async Task<int> GetTotalBetsAsync(IElementHandle btRoot)
    {
        try
        {
            var btnContainer = await btRoot.QuerySelectorAsync("[data-editor-id=\"eventCardPaginator\"]");
            var itemsText = await btnContainer?.QuerySelectorAllAsync("span");
            if (itemsText != null && itemsText.Count > 1)
            {
                var text = await itemsText[1].InnerTextAsync();
                var match = Regex.Match(text, @"\d+$");
                if (match.Success && int.TryParse(match.Value, out var total))
                {
                    return total;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get total bets count: {Error}", ex.Message);
        }

        return 0;
    }

    private async Task<IElementHandle?> GetNextButtonAsync(IElementHandle btRoot)
    {
        try
        {
            var btnsContainer = await btRoot.QuerySelectorAllAsync("[data-editor-id=\"eventCardPaginatorArrow\"]");
            return btnsContainer.Count > 1 ? btnsContainer[1] : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get next button: {Error}", ex.Message);
            return null;
        }
    }

    private ScrapedEventDto? ParseRawText(string rawText)
    {
        try
        {
            var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                              .Select(line => line.Trim())
                              .Where(line => !string.IsNullOrEmpty(line))
                              .ToList();

            if (lines.Count < 6) return null;

            var league = $"{lines[0]} - {lines[1]}";
            var rawDateTime = lines[2].Contains(',') ? lines[2] : null;
            var team1 = lines[3];
            var team2 = lines[4];

            string? dateTime = null;
            if (rawDateTime != null)
            {
                var parts = rawDateTime.Split(',');
                if (parts.Length == 2)
                {
                    var dayIndicator = parts[0].Trim().ToLower();
                    var timeStr = parts[1].Trim();

                    // 🔧 CORREÇÃO: Betby mostra horário local brasileiro, mas vamos converter para UTC
                    var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                    // Replace this line:
                    // var localNow = TimeZoneInfo.ConvertFromUtc(DateTime.UtcNow, brazilTimeZone);

                    // With this line:
                    var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone);
                    var todayDate = localNow.Date;
                    var eventDate = dayIndicator == "today" ? todayDate : todayDate.AddDays(1);

                    if (TimeSpan.TryParse(timeStr, out var eventTime))
                    {
                        // Criar datetime local brasileiro
                        var localDateTime = eventDate.Add(eventTime);

                        // 🔧 CONVERTER PARA UTC antes de salvar
                        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, brazilTimeZone);

                        dateTime = utcDateTime.ToString("yyyy-MM-dd, HH:mm");

                        _logger.LogDebug("Converted Betby time: Local {Local} -> UTC {Utc}",
                            localDateTime.ToString("yyyy-MM-dd HH:mm"),
                            utcDateTime.ToString("yyyy-MM-dd HH:mm"));
                    }
                }
            }

            // Procurar pelas odds 1X2
            string? oddTeam1 = null, oddDraw = null, oddTeam2 = null;

            var i1x2Index = lines.FindIndex(line => line.Equals("1x2", StringComparison.OrdinalIgnoreCase));
            if (i1x2Index >= 0 && i1x2Index + 6 < lines.Count)
            {
                var rawOddTeam1 = lines[i1x2Index + 2];
                var rawOddDraw = lines[i1x2Index + 4];
                var rawOddTeam2 = lines[i1x2Index + 6];

                oddTeam1 = ConvertOddToDecimal(rawOddTeam1);
                oddDraw = ConvertOddToDecimal(rawOddDraw);
                oddTeam2 = ConvertOddToDecimal(rawOddTeam2);
            }

            return new ScrapedEventDto
            {
                League = league,
                DateTime = dateTime ?? "",
                Team1 = team1,
                Team2 = team2,
                OddTeam1 = oddTeam1 ?? "0",
                OddDraw = oddDraw ?? "0",
                OddTeam2 = oddTeam2 ?? "0"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse raw text: {Error}", ex.Message);
            return null;
        }
    }

    // NOVO MÉTODO: Converter odds para formato decimal
    private string ConvertOddToDecimal(string rawOdd)
    {
        try
        {
            if (string.IsNullOrEmpty(rawOdd)) return "0";

            // Remover caracteres não numéricos
            var cleanOdd = Regex.Replace(rawOdd, @"[^\d]", "");

            if (int.TryParse(cleanOdd, out var intOdd) && intOdd > 0)
            {
                string result;

                // Se o número tem 3 dígitos (ex: 165), dividir por 100 (resultado: 1.65)
                if (intOdd >= 100)
                {
                    var decimalOdd = intOdd / 100.0;
                    result = decimalOdd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                }
                // Se o número tem 2 dígitos (ex: 35), dividir por 10 (resultado: 3.5)
                else if (intOdd >= 10)
                {
                    var decimalOdd = intOdd / 10.0;
                    result = decimalOdd.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                }
                // Se o número tem 1 dígito, manter como está
                else
                {
                    result = intOdd.ToString();
                }

                return result;
            }

            _logger.LogWarning("❌ Could not parse odd: '{RawOdd}'", rawOdd);
            return rawOdd;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error converting odd '{RawOdd}': {Error}", rawOdd, ex.Message);
            return "0";
        }
    }
}