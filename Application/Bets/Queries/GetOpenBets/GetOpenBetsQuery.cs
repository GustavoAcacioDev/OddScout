using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Bets.Queries.GetOpenBets;

public sealed record GetOpenBetsQuery(Guid UserId) : IQuery<List<BetDto>>;