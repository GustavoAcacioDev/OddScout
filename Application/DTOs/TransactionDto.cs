using OddScout.Domain.Enums;

namespace OddScout.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public TransactionStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public bool IsCredit { get; set; }
    public bool IsDebit { get; set; }
}