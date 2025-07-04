using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.BalanceBefore)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TransactionStatus.Pending)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.ExternalReference)
            .HasMaxLength(100);

        builder.Property(t => t.RelatedEntityId);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ProcessedAt);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(1000);

        // Relacionamento com User
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_Transactions_UserId");

        builder.HasIndex(t => t.Type)
            .HasDatabaseName("IX_Transactions_Type");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Transactions_Status");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Transactions_CreatedAt");

        builder.HasIndex(t => new { t.UserId, t.Type })
            .HasDatabaseName("IX_Transactions_User_Type");

        builder.HasIndex(t => new { t.UserId, t.Status })
            .HasDatabaseName("IX_Transactions_User_Status");

        builder.HasIndex(t => t.ExternalReference)
            .HasDatabaseName("IX_Transactions_ExternalReference")
            .HasFilter("[ExternalReference] IS NOT NULL");

        builder.ToTable("Transactions");
    }
}