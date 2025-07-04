namespace OddScout.Application.Common.Interfaces;

public interface IEventManagementService
{
    Task ArchiveOldEventsAsync(CancellationToken cancellationToken = default);
    Task CleanupEventsForScrapingAsync(OddScout.Domain.Enums.OddsSource source, CancellationToken cancellationToken = default);
    Task MarkEventAsHavingBetsAsync(Guid eventId, CancellationToken cancellationToken = default);
}