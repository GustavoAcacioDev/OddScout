using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Dashboard.Queries.GetDashboardMetrics;

namespace OddScout.API.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    /// <summary>
    /// Get dashboard metrics summary for the current user
    /// </summary>
    /// <returns>Dashboard metrics including total bets, win rate, profit, and active bets</returns>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var userId = GetCurrentUserId();
        var query = new GetDashboardMetricsQuery(userId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get dashboard metrics for a specific period
    /// </summary>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <returns>Dashboard metrics for the specified period</returns>
    [HttpGet("metrics/period")]
    public async Task<IActionResult> GetDashboardMetricsForPeriod([FromQuery] int days = 30)
    {
        if (days <= 0 || days > 365)
        {
            return BadRequest(new { error = "Days must be between 1 and 365" });
        }

        var userId = GetCurrentUserId();
        var query = new GetDashboardMetricsQuery(userId, days);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}