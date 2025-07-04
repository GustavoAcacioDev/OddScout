using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Users.Queries.GetTransactionHistory;

public sealed record GetTransactionHistoryQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<PagedResult<TransactionDto>>;