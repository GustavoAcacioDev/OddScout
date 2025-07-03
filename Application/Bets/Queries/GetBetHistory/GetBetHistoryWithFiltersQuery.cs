using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Queries.GetBetHistory;

public sealed record GetBetHistoryWithFiltersQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10,
    BetStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    MarketType? MarketType = null
) : IQuery<PagedResult<BetDto>>;