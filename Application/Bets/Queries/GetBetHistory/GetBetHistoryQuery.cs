using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Bets.Queries.GetBetHistory;

public sealed record GetBetHistoryQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<PagedResult<BetDto>>;