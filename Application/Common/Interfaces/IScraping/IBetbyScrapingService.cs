using OddScout.Application.DTOs.Scraping;

namespace OddScout.Application.Common.Interfaces.IScraping;

public interface IBetbyScrapingService
{
    Task<List<ScrapedEventDto>> ScrapeEventsAsync(CancellationToken cancellationToken = default);
}