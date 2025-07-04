namespace OddScout.Domain.Enums;

public enum TransactionStatus
{
    Pending = 1,        // Pendente (aguardando processamento)
    Completed = 2,      // Concluída
    Failed = 3,         // Falhou
    Cancelled = 4       // Cancelada
}