using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Domain.Entities;

namespace OddScout.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Odd> Odds { get; set; } = null!;
    public DbSet<ValueBet> ValueBets { get; set; } = null!;
    public DbSet<Bet> Bets { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplicar configurações baseadas no provider
        if (Database.IsNpgsql())
        {
            // Configurações específicas do PostgreSQL
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly,
                t => t.Namespace?.Contains("PostgreSQL") == true);
        }
        else
        {
            // Configurações padrão (SQL Server)
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly,
                t => t.Namespace?.Contains("PostgreSQL") != true);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}