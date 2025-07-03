using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations;

public class OddConfiguration : IEntityTypeConfiguration<Odd>
{
    public void Configure(EntityTypeBuilder<Odd> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.MarketType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.Team1Odd)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(o => o.DrawOdd)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(o => o.Team2Odd)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(o => o.Source)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        // Relacionamento com Event
        builder.HasOne(o => o.Event)
            .WithMany(e => e.Odds)
            .HasForeignKey(o => o.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(o => o.EventId)
            .HasDatabaseName("IX_Odds_EventId");

        builder.HasIndex(o => new { o.EventId, o.MarketType, o.Source })
            .HasDatabaseName("IX_Odds_Event_Market_Source");

        builder.ToTable("Odds");
    }
}