using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(u => u.Status)
            .HasConversion<int>()
            .HasDefaultValue(UserStatus.Active);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.RefreshTokenExpiry);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(10);

        builder.Property(u => u.PasswordResetTokenExpiry);

        // Índices para performance
        builder.HasIndex(u => u.Email)
            .HasDatabaseName("IX_Users_Email")
            .IsUnique();

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.ToTable("Users");
    }
}