using OddScout.Application.Common.Interfaces;
using OddScout.Application.Common.Models;
using OddScout.Application.DTOs.Dashboard;

namespace OddScout.Application.Dashboard.Queries.GetDashboardMetrics;

public record GetDashboardMetricsQuery(Guid UserId, int? PeriodInDays = null) : IQuery<ApiResponse<DashboardMetricsDto>>;