using OddScout.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace OddScout.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; private set; }

    // Construtor privado para EF Core
    private Email() { Value = string.Empty; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value.ToLowerInvariant().Trim();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(email);
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string email) => new(email);
}