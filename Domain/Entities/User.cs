using OddScout.Domain.Common;
using OddScout.Domain.Enums;

namespace OddScout.Domain.Entities;

public class User : Entity<Guid>
{
    // Propriedades simples por enquanto
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Campos JWT
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    // Campos Reset de Senha
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    // EF Core constructor
    private User() { }

    // Domain constructor
    public User(string email, string name, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = ValidateAndNormalizeEmail(email);
        Name = ValidateName(name);
        PasswordHash = ValidatePasswordHash(passwordHash);
        Balance = 0;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    // Validation methods (business logic inside entity)
    private static string ValidateAndNormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var normalizedEmail = email.ToLowerInvariant().Trim();

        if (!IsValidEmail(normalizedEmail))
            throw new ArgumentException("Invalid email format", nameof(email));

        return normalizedEmail;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return name.Trim();
    }

    private static string ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return passwordHash;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Domain methods
    public void UpdateBalance(decimal amount)
    {
        if (amount < 0)
            throw new InvalidOperationException("Balance cannot be negative");

        Balance = amount;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        Status = UserStatus.Active;
    }

    public void SetRefreshToken(string refreshToken, DateTime expiry)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiry = expiry;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void GeneratePasswordResetToken(string resetToken, DateTime expiry)
    {
        PasswordResetToken = resetToken;
        PasswordResetTokenExpiry = expiry;
    }

    public void ResetPassword(string newPasswordHash)
    {
        PasswordHash = ValidatePasswordHash(newPasswordHash);
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = ValidatePasswordHash(newPasswordHash);
    }

    public bool IsPasswordResetTokenValid(string token)
    {
        return !string.IsNullOrEmpty(PasswordResetToken) &&
               PasswordResetToken == token &&
               PasswordResetTokenExpiry.HasValue &&
               PasswordResetTokenExpiry.Value > DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid(string refreshToken)
    {
        return !string.IsNullOrEmpty(RefreshToken) &&
               RefreshToken == refreshToken &&
               RefreshTokenExpiry.HasValue &&
               RefreshTokenExpiry.Value > DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        ClearRefreshToken();
    }

    public void ChangeEmail(string newEmail)
    {
        Email = ValidateAndNormalizeEmail(newEmail);
    }
}
