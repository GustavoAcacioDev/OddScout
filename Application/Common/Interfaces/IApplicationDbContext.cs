using Microsoft.EntityFrameworkCore;
using OddScout.Domain.Entities;

namespace OddScout.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Event> Events { get; }
    DbSet<Odd> Odds { get; }
    DbSet<ValueBet> ValueBets { get; }
    DbSet<Bet> Bets { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}