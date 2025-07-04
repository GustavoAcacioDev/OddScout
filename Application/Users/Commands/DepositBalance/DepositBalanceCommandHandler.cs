using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;
using OddScout.Domain.Enums;

namespace OddScout.Application.Users.Commands.DepositBalance;

public class DepositBalanceCommandHandler : ICommandHandler<DepositBalanceCommand, TransactionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DepositBalanceCommandHandler> _logger;

    public DepositBalanceCommandHandler(
        IApplicationDbContext context,
        ILogger<DepositBalanceCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionDto> Handle(DepositBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing deposit for user {UserId}, amount {Amount}",
                request.UserId, request.Amount);

            // Verificar se o usuário existe
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null)
                throw new InvalidOperationException("User not found");

            // Verificar se a conta está ativa
            if (user.Status != UserStatus.Active)
                throw new InvalidOperationException("User account is not active");

            // Criar transação
            var transaction = new Transaction(
                request.UserId,
                TransactionType.Deposit,
                request.Amount,
                user.Balance,
                request.Description,
                request.ExternalReference
            );

            // Atualizar saldo do usuário
            var newBalance = user.Balance + request.Amount;
            user.UpdateBalance(newBalance);

            // Salvar transação
            _context.Transactions.Add(transaction);

            // Completar transação (em um cenário real, isso seria feito após confirmação do pagamento)
            transaction.CompleteTransaction();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deposit completed successfully. User {UserId}, Transaction {TransactionId}, New Balance {NewBalance}",
                request.UserId, transaction.Id, newBalance);

            // Retornar DTO
            return new TransactionDto
            {
                Id = transaction.Id,
                Type = transaction.Type,
                TypeDescription = GetTypeDescription(transaction.Type),
                Amount = transaction.Amount,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Status = transaction.Status,
                StatusDescription = GetStatusDescription(transaction.Status),
                Description = transaction.Description,
                ExternalReference = transaction.ExternalReference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                FailureReason = transaction.FailureReason,
                IsCredit = transaction.IsCredit(),
                IsDebit = transaction.IsDebit()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for user {UserId}", request.UserId);
            throw;
        }
    }

    private static string GetTypeDescription(TransactionType type)
    {
        return type switch
        {
            TransactionType.Deposit => "Deposit",
            TransactionType.Withdrawal => "Withdrawal",
            TransactionType.BetPlaced => "Bet Placed",
            TransactionType.BetWon => "Bet Won",
            TransactionType.BetRefund => "Bet Refund",
            TransactionType.Bonus => "Bonus",
            TransactionType.Fee => "Fee",
            _ => "Unknown"
        };
    }

    private static string GetStatusDescription(TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.Pending => "Pending",
            TransactionStatus.Completed => "Completed",
            TransactionStatus.Failed => "Failed",
            TransactionStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }
}