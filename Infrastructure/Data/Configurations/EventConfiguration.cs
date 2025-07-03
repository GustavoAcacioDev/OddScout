using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
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
            .IsRequired();

        // Índices para performance
        builder.HasIndex(e => e.EventDateTime)
            .HasDatabaseName("IX_Events_EventDateTime");

        builder.HasIndex(e => e.Source)
            .HasDatabaseName("IX_Events_Source");

        builder.HasIndex(e => new { e.Team1, e.Team2, e.EventDateTime })
            .HasDatabaseName("IX_Events_Teams_DateTime");

        builder.ToTable("Events");
    }
}