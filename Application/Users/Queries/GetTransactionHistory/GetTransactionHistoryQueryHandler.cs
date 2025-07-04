using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Users.Queries.GetTransactionHistory;

public class GetTransactionHistoryQueryHandler : IQueryHandler<GetTransactionHistoryQuery, PagedResult<TransactionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTransactionHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Transactions
            .Where(t => t.UserId == request.UserId);

        // Aplicar filtros
        if (request.Type.HasValue)
        {
            query = query.Where(t => t.Type == request.Type.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);
        }

        // Ordenar por data mais recente primeiro
        query = query.OrderByDescending(t => t.CreatedAt);

        // Obter total para paginação
        var totalCount = await query.CountAsync(cancellationToken);

        // Aplicar paginação
        var skip = (request.PageNumber - 1) * request.PageSize;
        var transactions = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var transactionDtos = transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            Type = t.Type,
            TypeDescription = GetTypeDescription(t.Type),
            Amount = t.Amount,
            BalanceBefore = t.BalanceBefore,
            BalanceAfter = t.BalanceAfter,
            Status = t.Status,
            StatusDescription = GetStatusDescription(t.Status),
            Description = t.Description,
            ExternalReference = t.ExternalReference,
            CreatedAt = t.CreatedAt,
            ProcessedAt = t.ProcessedAt,
            FailureReason = t.FailureReason,
            IsCredit = t.IsCredit(),
            IsDebit = t.IsDebit()
        }).ToList();

        return PagedResult<TransactionDto>.Create(transactionDtos, request.PageNumber, request.PageSize, totalCount);
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