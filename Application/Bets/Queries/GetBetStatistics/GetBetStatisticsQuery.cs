using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Bets.Queries.GetBetStatistics;

public record GetBetStatisticsQuery(Guid UserId) : IQuery<BetStatisticsDto>;