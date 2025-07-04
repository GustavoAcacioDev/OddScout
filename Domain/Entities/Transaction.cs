using OddScout.Domain.Common;
using OddScout.Domain.Enums;

namespace OddScout.Domain.Entities;

public class Transaction : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ExternalReference { get; private set; }
    public Guid? RelatedEntityId { get; private set; } // ID da aposta, se aplicável
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    // EF Core constructor
    private Transaction() { }

    // Domain constructor
    public Transaction(Guid userId, TransactionType type, decimal amount,
                      decimal balanceBefore, string description,
                      string? externalReference = null, Guid? relatedEntityId = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Amount = ValidateAmount(amount, type);
        BalanceBefore = ValidateBalance(balanceBefore);
        BalanceAfter = CalculateBalanceAfter(balanceBefore, amount, type);
        Status = TransactionStatus.Pending;
        Description = ValidateDescription(description);
        ExternalReference = externalReference;
        RelatedEntityId = relatedEntityId;
        CreatedAt = DateTime.UtcNow;
    }

    public void CompleteTransaction()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be completed");

        Status = TransactionStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void FailTransaction(string failureReason)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be failed");

        Status = TransactionStatus.Failed;
        FailureReason = ValidateDescription(failureReason);
        ProcessedAt = DateTime.UtcNow;
    }

    public void CancelTransaction()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be cancelled");

        Status = TransactionStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsCredit()
    {
        return Type == TransactionType.Deposit ||
               Type == TransactionType.BetWon ||
               Type == TransactionType.BetRefund ||
               Type == TransactionType.Bonus;
    }

    public bool IsDebit()
    {
        return Type == TransactionType.Withdrawal ||
               Type == TransactionType.BetPlaced ||
               Type == TransactionType.Fee;
    }

    private static decimal ValidateAmount(decimal amount, TransactionType type)
    {
        if (amount <= 0)
            throw new ArgumentException("Transaction amount must be greater than zero", nameof(amount));

        // Validar limites por tipo de transação
        switch (type)
        {
            case TransactionType.Deposit:
                if (amount > 50000) // Limite de R$ 50.000 por depósito
                    throw new ArgumentException("Deposit amount exceeds maximum limit of 50,000", nameof(amount));
                break;
            case TransactionType.Withdrawal:
                if (amount > 25000) // Limite de R$ 25.000 por saque
                    throw new ArgumentException("Withdrawal amount exceeds maximum limit of 25,000", nameof(amount));
                break;
        }

        return amount;
    }

    private static decimal ValidateBalance(decimal balance)
    {
        if (balance < 0)
            throw new ArgumentException("Balance cannot be negative", nameof(balance));

        return balance;
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters", nameof(description));

        return description.Trim();
    }

    private static decimal CalculateBalanceAfter(decimal balanceBefore, decimal amount, TransactionType type)
    {
        return type switch
        {
            TransactionType.Deposit or TransactionType.BetWon or TransactionType.BetRefund or TransactionType.Bonus
                => balanceBefore + amount,
            TransactionType.Withdrawal or TransactionType.BetPlaced or TransactionType.Fee
                => balanceBefore - amount,
            _ => throw new ArgumentException($"Unknown transaction type: {type}")
        };
    }
}