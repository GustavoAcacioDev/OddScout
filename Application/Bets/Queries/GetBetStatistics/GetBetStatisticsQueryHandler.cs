using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Queries.GetBetStatistics;

public class GetBetStatisticsQueryHandler : IQueryHandler<GetBetStatisticsQuery, BetStatisticsDto>
{
    private readonly IApplicationDbContext _context;

    public GetBetStatisticsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BetStatisticsDto> Handle(GetBetStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Buscar todas as apostas do usuário de uma vez só para calcular estatísticas
        var allBets = await _context.Bets
            .Where(b => b.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var totalBets = allBets.Count;
        var openBets = allBets.Count(b => b.Status == BetStatus.Open);
        var wonBets = allBets.Count(b => b.Status == BetStatus.Won);
        var lostBets = allBets.Count(b => b.Status == BetStatus.Lost);
        var voidBets = allBets.Count(b => b.Status == BetStatus.Void);

        var totalStaked = allBets.Sum(b => b.Amount);
        var totalProfit = allBets.Sum(b => b.CalculateProfit());

        // Calcular win rate apenas com apostas resolvidas (excluindo abertas e void)
        var settledBets = wonBets + lostBets;
        var winRate = settledBets > 0 ? (decimal)wonBets / settledBets * 100 : 0;

        var roi = totalStaked > 0 ? (totalProfit / totalStaked) * 100 : 0;
        var averageOdds = allBets.Count > 0 ? allBets.Average(b => b.Odds) : 0;

        var biggestWin = allBets.Where(b => b.Status == BetStatus.Won)
            .Select(b => b.CalculateProfit())
            .DefaultIfEmpty(0)
            .Max();

        var biggestLoss = allBets.Where(b => b.Status == BetStatus.Lost)
            .Select(b => Math.Abs(b.CalculateProfit()))
            .DefaultIfEmpty(0)
            .Max();

        return new BetStatisticsDto
        {
            TotalBets = totalBets,
            OpenBets = openBets,
            WonBets = wonBets,
            LostBets = lostBets,
            VoidBets = voidBets,
            TotalStaked = totalStaked,
            TotalProfit = totalProfit,
            WinRate = winRate,
            Roi = roi,
            AverageOdds = averageOdds,
            BiggestWin = biggestWin,
            BiggestLoss = biggestLoss,
            ProfitLoss = totalProfit
        };
    }
}