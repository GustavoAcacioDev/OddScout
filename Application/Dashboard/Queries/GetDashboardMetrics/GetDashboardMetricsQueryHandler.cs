using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Models;
using OddScout.Application.DTOs.Dashboard;
using OddScout.Domain.Enums;

namespace OddScout.Application.Dashboard.Queries.GetDashboardMetrics;

public class GetDashboardMetricsQueryHandler : IQueryHandler<GetDashboardMetricsQuery, ApiResponse<DashboardMetricsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetDashboardMetricsQueryHandler> _logger;

    public GetDashboardMetricsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetDashboardMetricsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting dashboard metrics for user {UserId}", request.UserId);

            // Verificar se o usuário existe
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
            {
                return ApiResponse<DashboardMetricsDto>.Failure(
                    new[] { "User not found" });
            }

            // Definir período
            var periodInDays = request.PeriodInDays ?? 30;
            var currentPeriodStart = DateTime.UtcNow.AddDays(-periodInDays);
            var previousPeriodStart = currentPeriodStart.AddDays(-periodInDays);

            // Obter apostas do período atual
            var currentPeriodBets = await _context.Bets
                .Where(b => b.UserId == request.UserId &&
                           b.PlacedAt >= currentPeriodStart)
                .ToListAsync(cancellationToken);

            // Obter apostas do período anterior (para comparação)
            var previousPeriodBets = await _context.Bets
                .Where(b => b.UserId == request.UserId &&
                           b.PlacedAt >= previousPeriodStart &&
                           b.PlacedAt < currentPeriodStart)
                .ToListAsync(cancellationToken);

            // Obter apostas ativas (para todos os períodos)
            var activeBets = await _context.Bets
                .Include(b => b.Event)
                .Where(b => b.UserId == request.UserId &&
                           b.Status == BetStatus.Open)
                .ToListAsync(cancellationToken);

            // Calcular métricas do período atual
            var currentMetrics = CalculatePeriodMetrics(currentPeriodBets);
            var previousMetrics = CalculatePeriodMetrics(previousPeriodBets);

            // Calcular comparações percentuais
            var totalBetsComparison = CalculatePercentageChange(
                previousMetrics.TotalBets, currentMetrics.TotalBets);

            var winRateComparison = CalculatePercentageChange(
                previousMetrics.WinRate, currentMetrics.WinRate);

            var profitComparison = CalculatePercentageChange(
                previousMetrics.TotalProfit, currentMetrics.TotalProfit);

            // Apostas que terminam hoje
            var betsEndingToday = activeBets.Count(b =>
                b.Event.EventDateTime.Date == DateTime.UtcNow.Date);

            // Montar resultado
            var dashboardMetrics = new DashboardMetricsDto
            {
                Total = new DashboardMetricDto
                {
                    Title = "Total Bets",
                    Value = currentMetrics.TotalBets,
                    Unit = "count",
                    ChangeValue = totalBetsComparison,
                    ChangeUnit = "percentage",
                    ChangeText = "from last period"
                },
                WinRate = new DashboardMetricDto
                {
                    Title = "Win Rate",
                    Value = currentMetrics.WinRate,
                    Unit = "percentage",
                    ChangeValue = winRateComparison,
                    ChangeUnit = "percentage_points",
                    ChangeText = "from last period"
                },
                Profit = new DashboardMetricDto
                {
                    Title = "Total Profit",
                    Value = currentMetrics.TotalProfit,
                    Unit = "currency",
                    ChangeValue = profitComparison,
                    ChangeUnit = "percentage",
                    ChangeText = "from last period"
                },
                Active = new DashboardMetricDto
                {
                    Title = "Active Bets",
                    Value = activeBets.Count,
                    Unit = "count",
                    ChangeValue = null, // Não aplicável para bets ativas
                    ChangeUnit = "text",
                    ChangeText = betsEndingToday > 0
                        ? $"{betsEndingToday} ending today"
                        : "No bets ending today"
                }
            };

            var warnings = new List<string>();

            // Adicionar warnings se necessário
            if (currentMetrics.TotalBets < 5)
            {
                warnings.Add("Statistics may not be reliable with fewer than 5 bets");
            }

            if (previousMetrics.TotalBets == 0 && currentMetrics.TotalBets > 0)
            {
                warnings.Add("No previous period data available for comparison");
            }

            _logger.LogInformation(
                "Dashboard metrics calculated successfully for user {UserId}. " +
                "Current period: {CurrentBets} bets, Previous period: {PreviousBets} bets",
                request.UserId, currentMetrics.TotalBets, previousMetrics.TotalBets);

            return ApiResponse<DashboardMetricsDto>.Success(
                dashboardMetrics,
                warnings.Count > 0 ? warnings.ToArray() : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calculating dashboard metrics for user {UserId}", request.UserId);

            return ApiResponse<DashboardMetricsDto>.Failure(
                new[] { "An error occurred while calculating dashboard metrics" });
        }
    }

    private static PeriodMetrics CalculatePeriodMetrics(List<Domain.Entities.Bet> bets)
    {
        var totalBets = bets.Count;
        var wonBets = bets.Count(b => b.Status == BetStatus.Won);
        var settledBets = bets.Count(b => b.Status != BetStatus.Open);

        var winRate = settledBets > 0 ? (decimal)wonBets / settledBets * 100 : 0;

        var totalStaked = bets.Sum(b => b.Amount);
        var totalReturns = bets.Where(b => b.Status == BetStatus.Won)
            .Sum(b => b.ActualReturn ?? 0);
        var totalProfit = totalReturns - totalStaked;

        return new PeriodMetrics
        {
            TotalBets = totalBets,
            WinRate = winRate,
            TotalProfit = totalProfit
        };
    }

    private static decimal CalculatePercentageChange(decimal oldValue, decimal newValue)
    {
        if (oldValue == 0)
            return newValue > 0 ? 100 : 0;

        return ((newValue - oldValue) / oldValue) * 100;
    }

    private class PeriodMetrics
    {
        public int TotalBets { get; set; }
        public decimal WinRate { get; set; }
        public decimal TotalProfit { get; set; }
    }
}