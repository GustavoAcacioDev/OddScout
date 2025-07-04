using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Services;

public class EventManagementService : IEventManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EventManagementService> _logger;

    public EventManagementService(IApplicationDbContext context, ILogger<EventManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ArchiveOldEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Arquivar eventos de mais de 7 dias

            var oldEventsToArchive = await _context.Events
                .Where(e => e.IsActive &&
                           e.EventDateTime < cutoffDate &&
                           !e.HasBets) // Só arquivar se não tiver apostas
                .ToListAsync(cancellationToken);

            foreach (var eventEntity in oldEventsToArchive)
            {
                eventEntity.Archive();
            }

            if (oldEventsToArchive.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Archived {Count} old events", oldEventsToArchive.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old events");
            throw;
        }
    }

    public async Task CleanupEventsForScrapingAsync(OddsSource source, CancellationToken cancellationToken = default)
    {
        try
        {
            // Buscar eventos ativos da fonte que podem ser removidos (sem apostas)
            var eventsToRemove = await _context.Events
                .Where(e => e.Source == source &&
                           e.IsActive &&
                           !e.HasBets)
                .ToListAsync(cancellationToken);

            // Buscar eventos com apostas para marcar como arquivados
            var eventsWithBetsToArchive = await _context.Events
                .Where(e => e.Source == source &&
                           e.IsActive &&
                           e.HasBets)
                .ToListAsync(cancellationToken);

            // Remover eventos sem apostas
            if (eventsToRemove.Any())
            {
                _context.Events.RemoveRange(eventsToRemove);
                _logger.LogInformation("Removing {Count} events without bets from {Source}",
                    eventsToRemove.Count, source);
            }

            // Arquivar eventos com apostas
            foreach (var eventEntity in eventsWithBetsToArchive)
            {
                eventEntity.Archive();
            }

            if (eventsWithBetsToArchive.Any())
            {
                _logger.LogInformation("Archiving {Count} events with bets from {Source}",
                    eventsWithBetsToArchive.Count, source);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up events for {Source}", source);
            throw;
        }
    }

    public async Task MarkEventAsHavingBetsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity != null && !eventEntity.HasBets)
            {
                eventEntity.MarkAsHavingBets();
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Marked event {EventId} as having bets", eventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking event {EventId} as having bets", eventId);
            // Não propagar o erro, pois isso é secundário
        }
    }
}