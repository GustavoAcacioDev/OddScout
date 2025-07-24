using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations.PostgreSQL;

public class PostgreSQLEventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.League)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Team1)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Team2)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EventDateTime)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(EventStatus.Scheduled);

        builder.Property(e => e.Source)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ExternalLink)
            .HasMaxLength(500);

        builder.Property(e => e.ScrapedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.HasBets)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ArchivedAt)
            .HasColumnType("timestamp with time zone");

        // Relacionamentos
        builder.HasMany(e => e.Bets)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(e => e.EventDateTime)
            .HasDatabaseName("IX_Events_EventDateTime");

        builder.HasIndex(e => e.Source)
            .HasDatabaseName("IX_Events_Source");

        builder.HasIndex(e => new { e.Team1, e.Team2, e.EventDateTime })
            .HasDatabaseName("IX_Events_Teams_DateTime");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Events_IsActive");

        builder.HasIndex(e => e.HasBets)
            .HasDatabaseName("IX_Events_HasBets");

        builder.ToTable("Events");
    }
}