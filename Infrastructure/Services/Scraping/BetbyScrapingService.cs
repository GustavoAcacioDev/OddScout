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

            // Primeiro vamos verificar se ainda existe Shadow DOM
            var hasShadowRoot = await CheckShadowRootExistsAsync(page, "#bt-root");
            _logger.LogInformation("Shadow DOM exists: {HasShadow}", hasShadowRoot);

            IElementHandle? container = null;

            if (hasShadowRoot)
            {
                // Tenta o método antigo com Shadow DOM
                var shadowRoot = await GetShadowRootAsync(page, "#bt-root");
                if (shadowRoot != null)
                {
                    container = await shadowRoot.QuerySelectorAsync("#bt-root");
                }
            }
            else
            {
                // Sem Shadow DOM - acessa diretamente
                container = await page.QuerySelectorAsync("#bt-root");
            }

            if (container == null)
            {
                _logger.LogError("Could not find container element");
                return new List<ScrapedEventDto>();
            }

            await DismissModalAsync(page);

            // Aguarda os elementos carregarem
            await page.WaitForTimeoutAsync(3000);

            // Tenta encontrar os cards com os novos seletores baseados na imagem
            var eventCards = await FindEventCardsAsync(container);

            if (eventCards.Count == 0)
            {
                _logger.LogWarning("No event cards found. Let's debug the page structure.");
                await DebugPageStructureAsync(page, container);
                return new List<ScrapedEventDto>();
            }

            var cleanedData = new List<ScrapedEventDto>();

            foreach (var card in eventCards)
            {
                try
                {
                    var fullText = await card.InnerTextAsync();
                    var parsed = ParseRawText(fullText);

                    if (parsed != null)
                    {
                        // Tenta encontrar o link do evento
                        var linkElement = await card.QuerySelectorAsync("a") ??
                                        await card.QuerySelectorAsync("[href]");
                        var href = linkElement != null ? await linkElement.GetAttributeAsync("href") : null;

                        parsed.Link = href;
                        cleanedData.Add(parsed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse event card: {Error}", ex.Message);
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

    private async Task<bool> CheckShadowRootExistsAsync(IPage page, string selector)
    {
        try
        {
            var shadowHost = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 20000 });
            if (shadowHost == null) return false;

            var hasShadowRoot = await shadowHost.EvaluateAsync<bool>("node => node.shadowRoot !== null");
            return hasShadowRoot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check shadow root for {Selector}", selector);
            return false;
        }
    }

    private async Task<IElementHandle?> GetShadowRootAsync(IPage page, string selector)
    {
        try
        {
            var shadowHost = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 20000 });
            if (shadowHost == null) return null;

            // Verificação mais robusta do Shadow DOM
            var shadowRootHandle = await shadowHost.EvaluateHandleAsync("node => node.shadowRoot");

            // Verifica se realmente é um elemento válido
            var isElement = await shadowRootHandle.EvaluateAsync<bool>("node => node && node.nodeType === 11"); // Node.DOCUMENT_FRAGMENT_NODE

            if (!isElement)
            {
                _logger.LogWarning("Shadow root is not a valid document fragment");
                return null;
            }

            return shadowRootHandle.AsElement();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shadow root for {Selector}", selector);
            return null;
        }
    }

    private async Task<List<IElementHandle>> FindEventCardsAsync(IElementHandle container)
    {
        // Tenta vários seletores possíveis baseados na nova estrutura
        var possibleSelectors = new[]
        {
            "[data-editor-id=\"eventCard\"]", // Seletor original
            ".bt264", // Baseado na imagem - parece ser o container dos cards
            "[class*=\"bt2\"]", // Classes que começam com bt2
            "[class*=\"card\"]", // Qualquer coisa com card no nome
            "div[class*=\"bt\"][class*=\"64\"]" // Divs com classes bt e 64
        };

        foreach (var selector in possibleSelectors)
        {
            try
            {
                var elements = await container.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                {
                    _logger.LogInformation("Found {Count} elements with selector: {Selector}", elements.Count, selector);
                    return elements.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to query selector {Selector}: {Error}", selector, ex.Message);
            }
        }

        return new List<IElementHandle>();
    }

    private async Task DebugPageStructureAsync(IPage page, IElementHandle container)
    {
        try
        {
            // Loga a estrutura da página para debug
            var outerHTML = await container.InnerHTMLAsync();
            _logger.LogInformation("Container HTML structure (first 1000 chars): {HTML}",
                outerHTML.Length > 1000 ? outerHTML.Substring(0, 1000) + "..." : outerHTML);

            // Tenta encontrar qualquer div dentro do container
            var allDivs = await container.QuerySelectorAllAsync("div");
            _logger.LogInformation("Found {Count} div elements in container", allDivs.Count);

            // Analisa as classes dos primeiros divs
            for (int i = 0; i < Math.Min(10, allDivs.Count); i++)
            {
                var className = await allDivs[i].GetAttributeAsync("class");
                var innerHTML = await allDivs[i].InnerHTMLAsync();
                _logger.LogInformation("Div {Index}: class='{Class}', content length={Length}",
                    i, className ?? "no-class", innerHTML?.Length ?? 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to debug page structure");
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
        catch
        {
            // Modal dismissal is optional, continue silently
        }
    }

    private async Task<int> GetTotalBetsAsync(IElementHandle btRoot)
    {
        try
        {
            // Tenta o seletor original primeiro
            var btnContainer = await btRoot.QuerySelectorAsync("[data-editor-id=\"eventCardPaginator\"]");

            // Se não encontrar, tenta seletores alternativos
            if (btnContainer == null)
            {
                var possiblePaginatorSelectors = new[]
                {
                    "[class*=\"paginator\"]",
                    "[class*=\"pagination\"]",
                    "div:has-text(\"of\")" // Divs que contêm "of" (como "1 of 10")
                };

                foreach (var selector in possiblePaginatorSelectors)
                {
                    btnContainer = await btRoot.QuerySelectorAsync(selector);
                    if (btnContainer != null) break;
                }
            }

            if (btnContainer != null)
            {
                var itemsText = await btnContainer.QuerySelectorAllAsync("span");
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
            if (btnsContainer.Count > 1)
            {
                return btnsContainer[1];
            }

            // Tenta seletores alternativos para o botão next
            var possibleNextSelectors = new[]
            {
                "button:has-text(\"Next\")",
                "button:has-text(\">\")",
                "[class*=\"next\"]",
                "[class*=\"arrow\"]:last-child"
            };

            foreach (var selector in possibleNextSelectors)
            {
                var nextBtn = await btRoot.QuerySelectorAsync(selector);
                if (nextBtn != null) return nextBtn;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get next button: {Error}", ex.Message);
        }

        return null;
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

                    var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                    var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, brazilTimeZone);
                    var todayDate = localNow.Date;
                    var eventDate = dayIndicator == "today" ? todayDate : todayDate.AddDays(1);

                    if (TimeSpan.TryParse(timeStr, out var eventTime))
                    {
                        var localDateTime = eventDate.Add(eventTime);
                        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, brazilTimeZone);
                        dateTime = utcDateTime.ToString("yyyy-MM-dd, HH:mm");
                    }
                }
            }

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

    private string ConvertOddToDecimal(string rawOdd)
    {
        try
        {
            if (string.IsNullOrEmpty(rawOdd)) return "0";

            var cleanOdd = Regex.Replace(rawOdd, @"[^\d]", "");

            if (int.TryParse(cleanOdd, out var intOdd) && intOdd > 0)
            {
                string result;

                if (intOdd >= 100)
                {
                    var decimalOdd = intOdd / 100.0;
                    result = decimalOdd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (intOdd >= 10)
                {
                    var decimalOdd = intOdd / 10.0;
                    result = decimalOdd.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    result = intOdd.ToString();
                }

                return result;
            }

            return rawOdd;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error converting odd '{RawOdd}': {Error}", rawOdd, ex.Message);
            return "0";
        }
    }
}