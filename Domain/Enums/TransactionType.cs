namespace OddScout.Domain.Enums;

public enum TransactionType
{
    Deposit = 1,        // Depósito
    Withdrawal = 2,     // Saque
    BetPlaced = 3,      // Aposta realizada (débito)
    BetWon = 4,         // Aposta ganha (crédito)
    BetRefund = 5,      // Reembolso de aposta (crédito)
    Bonus = 6,          // Bônus (crédito)
    Fee = 7            // Taxa (débito)
}