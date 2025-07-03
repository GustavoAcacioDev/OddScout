using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Interfaces.IScraping;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Scraping.Commands.RunBetbyScraping;

public class RunBetbyScrapingCommandHandler : ICommandHandler<RunBetbyScrapingCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IBetbyScrapingService _betbyScrapingService;
    private readonly ILogger<RunBetbyScrapingCommandHandler> _logger;

    public RunBetbyScrapingCommandHandler(
        IApplicationDbContext context,
        IBetbyScrapingService betbyScrapingService,
        ILogger<RunBetbyScrapingCommandHandler> logger)
    {
        _context = context;
        _betbyScrapingService = betbyScrapingService;
        _logger = logger;
    }

    public async Task<int> Handle(RunBetbyScrapingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Betby scraping process...");

            // Executar scraping
            var scrapedEvents = await _betbyScrapingService.ScrapeEventsAsync(cancellationToken);

            if (!scrapedEvents.Any())
            {
                _logger.LogWarning("No events scraped from Betby");
                return 0;
            }

            // Limpar eventos antigos do Betby
            var oldBetbyEvents = await _context.Events
                .Where(e => e.Source == OddsSource.Betby)
                .ToListAsync(cancellationToken);

            _context.Events.RemoveRange(oldBetbyEvents);

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

                    // 🔍 ADICIONAR ESTES LOGS DETALHADOS
                    _logger.LogInformation("🔍 PROCESSING SCRAPED EVENT:");
                    _logger.LogInformation("  - Team1: {Team1} vs Team2: {Team2}", scrapedEvent.Team1, scrapedEvent.Team2);
                    _logger.LogInformation("  - RAW ODDS: Team1='{OddTeam1}', Draw='{OddDraw}', Team2='{OddTeam2}'",
                        scrapedEvent.OddTeam1, scrapedEvent.OddDraw, scrapedEvent.OddTeam2);

                    // Parse das odds com cultura específica
                    var culture = System.Globalization.CultureInfo.InvariantCulture;

                    if (!decimal.TryParse(scrapedEvent.OddTeam1, System.Globalization.NumberStyles.Any, culture, out var oddTeam1) ||
                        !decimal.TryParse(scrapedEvent.OddDraw, System.Globalization.NumberStyles.Any, culture, out var oddDraw) ||
                        !decimal.TryParse(scrapedEvent.OddTeam2, System.Globalization.NumberStyles.Any, culture, out var oddTeam2) ||
                        oddTeam1 <= 0 || oddDraw <= 0 || oddTeam2 <= 0)
                    {
                        _logger.LogError("❌ FAILED TO PARSE ODDS: Team1='{T1}', Draw='{D}', Team2='{T2}'",
                            scrapedEvent.OddTeam1, scrapedEvent.OddDraw, scrapedEvent.OddTeam2);
                        continue;
                    }

                    // 🔍 VERIFICAR OS VALORES PARSEADOS
                    _logger.LogInformation("✅ PARSED ODDS: Team1={T1}, Draw={D}, Team2={T2}",
                        oddTeam1, oddDraw, oddTeam2);

                    // Criar evento
                    var eventEntity = new Event(
                        scrapedEvent.League,
                        eventDateTime,
                        scrapedEvent.Team1,
                        scrapedEvent.Team2,
                        OddsSource.Betby,
                        scrapedEvent.Link
                    );

                    _context.Events.Add(eventEntity);

                    // Criar odds
                    var oddEntity = new Odd(
                        eventEntity.Id,
                        MarketType.Match1X2,
                        oddTeam1,
                        oddDraw,
                        oddTeam2,
                        OddsSource.Betby
                    );

                    // 🔍 VERIFICAR AS ODDS QUE VÃO PARA O BANCO
                    _logger.LogInformation("💾 SAVING TO DB: Team1={T1}, Draw={D}, Team2={T2}",
                        oddEntity.Team1Odd, oddEntity.DrawOdd, oddEntity.Team2Odd);

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

            _logger.LogInformation("Betby scraping completed. Saved {Count} events", savedCount);
            return savedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Betby scraping process");
            throw;
        }
    }
}