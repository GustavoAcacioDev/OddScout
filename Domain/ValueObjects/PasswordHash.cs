using OddScout.Domain.Common;

namespace OddScout.Domain.ValueObjects;

public sealed class PasswordHash : ValueObject
{
    public string Value { get; private set; }

    // Construtor privado para EF Core
    private PasswordHash() { Value = string.Empty; }

    public PasswordHash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Password hash cannot be empty", nameof(hashedPassword));

        Value = hashedPassword;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PasswordHash passwordHash) => passwordHash.Value;
}