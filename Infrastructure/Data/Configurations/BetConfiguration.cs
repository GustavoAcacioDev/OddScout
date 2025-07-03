using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.MarketType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.SelectedOutcome)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.Odds)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(b => b.PotentialReturn)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.ActualReturn)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.Status)
            .HasConversion<int>()
            .HasDefaultValue(BetStatus.Open)
            .IsRequired();

        builder.Property(b => b.PlacedAt)
            .IsRequired();

        builder.Property(b => b.SettledAt);

        // Relacionamentos
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Event)
            .WithMany()
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("IX_Bets_UserId");

        builder.HasIndex(b => b.EventId)
            .HasDatabaseName("IX_Bets_EventId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Bets_Status");

        builder.HasIndex(b => b.PlacedAt)
            .HasDatabaseName("IX_Bets_PlacedAt");

        builder.HasIndex(b => new { b.UserId, b.Status })
            .HasDatabaseName("IX_Bets_User_Status");

        builder.ToTable("Bets");
    }
}