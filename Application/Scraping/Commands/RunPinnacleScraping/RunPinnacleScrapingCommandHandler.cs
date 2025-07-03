using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Scraping.Commands.RunPinnacleScraping;

public class RunPinnacleScrapingCommandHandler : ICommandHandler<RunPinnacleScrapingCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IPinnacleScrapingService _pinnacleScrapingService;
    private readonly ILogger<RunPinnacleScrapingCommandHandler> _logger;

    public RunPinnacleScrapingCommandHandler(
        IApplicationDbContext context,
        IPinnacleScrapingService pinnacleScrapingService,
        ILogger<RunPinnacleScrapingCommandHandler> logger)
    {
        _context = context;
        _pinnacleScrapingService = pinnacleScrapingService;
        _logger = logger;
    }

    public async Task<int> Handle(RunPinnacleScrapingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Pinnacle scraping process...");

            // Executar scraping
            var scrapedEvents = await _pinnacleScrapingService.ScrapeEventsAsync(cancellationToken);

            if (!scrapedEvents.Any())
            {
                _logger.LogWarning("No events scraped from Pinnacle");
                return 0;
            }

            // Limpar eventos antigos do Pinnacle
            var oldPinnacleEvents = await _context.Events
                .Where(e => e.Source == OddsSource.Pinnacle)
                .ToListAsync(cancellationToken);

            _context.Events.RemoveRange(oldPinnacleEvents);

            var savedCount = 0;

            foreach (var scrapedEvent in scrapedEvents)
            {
                try
                {
                    // Parse da data
                    if (!DateTime.TryParse(scrapedEvent.DateTime, out var eventDateTime))
                    {
                        _logger.LogWarning("Failed to parse datetime: {DateTime}", scrapedEvent.DateTime);
                        continue;
                    }

                    eventDateTime = DateTime.SpecifyKind(eventDateTime, DateTimeKind.Utc);

                    // Parse das odds
                    if (!decimal.TryParse(scrapedEvent.OddTeam1, out var oddTeam1) ||
                        !decimal.TryParse(scrapedEvent.OddDraw, out var oddDraw) ||
                        !decimal.TryParse(scrapedEvent.OddTeam2, out var oddTeam2) ||
                        oddTeam1 <= 0 || oddDraw <= 0 || oddTeam2 <= 0)
                    {
                        _logger.LogWarning("Invalid odds for event: {Team1} vs {Team2}", scrapedEvent.Team1, scrapedEvent.Team2);
                        continue;
                    }

                    // Criar evento
                    var eventEntity = new Event(
                        scrapedEvent.League,
                        eventDateTime,
                        scrapedEvent.Team1,
                        scrapedEvent.Team2,
                        OddsSource.Pinnacle
                    );

                    _context.Events.Add(eventEntity);

                    // Criar odds
                    var oddEntity = new Odd(
                        eventEntity.Id,
                        MarketType.Match1X2,
                        oddTeam1,
                        oddDraw,
                        oddTeam2,
                        OddsSource.Pinnacle
                    );

                    _context.Odds.Add(oddEntity);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scraped event: {Team1} vs {Team2}",
                        scrapedEvent.Team1, scrapedEvent.Team2);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Pinnacle scraping completed. Saved {Count} events", savedCount);
            return savedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pinnacle scraping process");
            throw;
        }
    }
}