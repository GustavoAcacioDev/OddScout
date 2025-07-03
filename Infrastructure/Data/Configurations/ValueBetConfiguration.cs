using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;

namespace OddScout.Infrastructure.Data.Configurations;

public class ValueBetConfiguration : IEntityTypeConfiguration<ValueBet>
{
    public void Configure(EntityTypeBuilder<ValueBet> builder)
    {
        builder.HasKey(vb => vb.Id);

        builder.Property(vb => vb.Id)
            .ValueGeneratedNever();

        builder.Property(vb => vb.MarketType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(vb => vb.OutcomeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(vb => vb.BetbyOdd)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(vb => vb.PinnacleOdd)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        // CORREÇÃO: Aumentar precisão para suportar valores maiores
        builder.Property(vb => vb.ImpliedProbability)
            .HasColumnType("decimal(12,8)") // Era decimal(8,6) - agora permite até 9999.99999999
            .IsRequired();

        // CORREÇÃO: Aumentar precisão para Expected Value
        builder.Property(vb => vb.ExpectedValue)
            .HasColumnType("decimal(12,8)") // Era decimal(8,6) - agora permite até 9999.99999999
            .IsRequired();

        // CORREÇÃO: Aumentar precisão para Confidence Score  
        builder.Property(vb => vb.ConfidenceScore)
            .HasColumnType("decimal(8,4)") // Era decimal(5,2) - agora permite até 9999.9999
            .IsRequired();

        builder.Property(vb => vb.CalculatedAt)
            .IsRequired();

        // Relacionamento com Event
        builder.HasOne(vb => vb.Event)
            .WithMany()
            .HasForeignKey(vb => vb.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(vb => vb.ExpectedValue)
            .HasDatabaseName("IX_ValueBets_ExpectedValue");

        builder.HasIndex(vb => vb.CalculatedAt)
            .HasDatabaseName("IX_ValueBets_CalculatedAt");

        builder.ToTable("ValueBets");
    }
}