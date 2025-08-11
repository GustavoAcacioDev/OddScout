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

            // Debug: Vamos verificar se a paginação existe
            var totalBets = await GetTotalBetsAsync(container);
            var lastPage = totalBets > 0 ? (int)Math.Ceiling(totalBets / 24.0) : 1;
            var nextButton = await GetNextButtonAsync(container);

            if (totalBets > 0)
            {
                _logger.LogInformation("Found {TotalBets} total events across {LastPage} pages", totalBets, lastPage);
            }

            var cleanedData = new List<ScrapedEventDto>();

            // Loop através das páginas
            for (int currentPage = 1; currentPage <= lastPage; currentPage++)
            {
                // Aguarda os cards carregarem na página atual
                await page.WaitForTimeoutAsync(2000);

                // Tenta encontrar os cards na página atual
                var eventCards = await FindEventCardsAsync(container);

                if (eventCards.Count == 0)
                {
                    _logger.LogWarning("No event cards found on page {CurrentPage}", currentPage);
                    break;
                }

                // Processa os cards da página atual
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

                // Se não é a última página e tem botão next, clica nele
                if (currentPage < lastPage && nextButton != null)
                {
                    try
                    {
                        await nextButton.ClickAsync();

                        // Aguarda a página carregar
                        await page.WaitForTimeoutAsync(3000);

                        // Aguarda pelos novos cards aparecerem
                        await container.WaitForSelectorAsync("[data-editor-id=\"eventCard\"]:nth-child(1)",
                            new ElementHandleWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });

                        // Atualiza a referência do botão next para a nova página
                        nextButton = await GetNextButtonAsync(container);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to navigate to next page {NextPage}", currentPage + 1);
                        break;
                    }
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
            _logger.LogWarning("Failed to check shadow root: {Error}", ex.Message);
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
                return null;
            }

            return shadowRootHandle.AsElement();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get shadow root: {Error}", ex.Message);
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

    private async Task<int> GetTotalBetsAsync(IElementHandle container)
    {
        try
        {
            // Tenta o seletor original primeiro
            var btnContainer = await container.QuerySelectorAsync("[data-editor-id=\"eventCardPaginator\"]");

            // Se não encontrar, tenta seletores alternativos
            if (btnContainer == null)
            {
                var possiblePaginatorSelectors = new[]
                {
                    "[class*=\"paginator\"]",
                    "[class*=\"pagination\"]",
                    "[class*=\"pager\"]",
                    "div:has-text(\"of\")",
                    "span:has-text(\"of\")"
                };

                foreach (var selector in possiblePaginatorSelectors)
                {
                    try
                    {
                        btnContainer = await container.QuerySelectorAsync(selector);
                        if (btnContainer != null) break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to query paginator selector {Selector}: {Error}", selector, ex.Message);
                    }
                }
            }

            if (btnContainer != null)
            {
                var paginatorText = await btnContainer.InnerTextAsync();

                // Tenta várias formas de extrair o número total
                var patterns = new[]
                {
                    @"of\s+(\d+)",           // "1 of 150"
                    @"\/\s*(\d+)",           // "1 / 150"
                    @"total:\s*(\d+)",       // "total: 150"
                    @"(\d+)\s*total",        // "150 total"
                    @"\d+\s*-\s*\d+\s*of\s*(\d+)", // "1-24 of 150"
                    @"(\d+)$"                // último número na string
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(paginatorText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var total))
                    {
                        return total;
                    }
                }

                // Se não conseguiu extrair o padrão, tenta pegar spans com números
                var itemsText = await btnContainer.QuerySelectorAllAsync("span");
                if (itemsText != null && itemsText.Count > 0)
                {
                    for (int i = 0; i < itemsText.Count; i++)
                    {
                        var text = await itemsText[i].InnerTextAsync();
                        var match = Regex.Match(text, @"\d+");
                        if (match.Success && int.TryParse(match.Value, out var total) && total > 24)
                        {
                            return total;
                        }
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

    private async Task<IElementHandle?> GetNextButtonAsync(IElementHandle container)
    {
        try
        {
            // Tenta o seletor original primeiro
            var btnsContainer = await container.QuerySelectorAllAsync("[data-editor-id=\"eventCardPaginatorArrow\"]");
            if (btnsContainer.Count > 1)
            {
                return btnsContainer[1];
            }

            // Tenta seletores alternativos para o botão next
            var possibleNextSelectors = new[]
            {
                "button:has-text(\"Next\")",
                "button:has-text(\">\")",
                "button:has-text(\"›\")",
                "button:has-text(\"»\")",
                "[class*=\"next\"]",
                "[class*=\"arrow\"][class*=\"right\"]",
                "[class*=\"pager\"] button:last-child",
                "[aria-label*=\"next\"]",
                "[aria-label*=\"Next\"]"
            };

            foreach (var selector in possibleNextSelectors)
            {
                try
                {
                    var nextBtn = await container.QuerySelectorAsync(selector);
                    if (nextBtn != null)
                    {
                        // Verifica se o botão não está desabilitado
                        var isDisabled = await nextBtn.GetAttributeAsync("disabled");
                        var ariaDisabled = await nextBtn.GetAttributeAsync("aria-disabled");

                        if (isDisabled == null && ariaDisabled != "true")
                        {
                            return nextBtn;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to query next button selector {Selector}: {Error}", selector, ex.Message);
                }
            }

            // Se não encontrou com seletores específicos, tenta encontrar qualquer botão que pareça ser de paginação
            var allButtons = await container.QuerySelectorAllAsync("button");

            for (int i = 0; i < allButtons.Count; i++)
            {
                var button = allButtons[i];
                var buttonText = await button.InnerTextAsync();

                // Verifica se é um botão de próxima página baseado no texto
                if (!string.IsNullOrEmpty(buttonText))
                {
                    var text = buttonText.Trim().ToLower();
                    if (text.Contains("next") || text == ">" || text == "›" || text == "»")
                    {
                        var isDisabled = await button.GetAttributeAsync("disabled");
                        if (isDisabled == null)
                        {
                            return button;
                        }
                    }
                }
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