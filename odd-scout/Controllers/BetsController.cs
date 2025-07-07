using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Bets.Commands.PlaceBet;
using OddScout.Application.Bets.Commands.SettleBet;
using OddScout.Application.Bets.Queries.GetBetHistory;
using OddScout.Application.Bets.Queries.GetOpenBets;
using OddScout.Application.Common.Models;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.API.Controllers;

[Authorize]
public class BetsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> PlaceBet([FromBody] PlaceBetRequest request)
    {
        var userId = GetCurrentUserId();

        var command = new PlaceBetCommand(
            userId,
            request.EventId,
            request.MarketType,
            request.SelectedOutcome,
            request.Amount,
            request.Odds
        );

        var result = await Mediator.Send(command);

        return Ok(result);
    }

    [HttpPost("{betId:guid}/settle")]
    public async Task<IActionResult> SettleBet(Guid betId, [FromBody] SettleBetRequest request)
    {
        var userId = GetCurrentUserId();

        var command = new SettleBetCommand(betId, userId, request.Outcome);
        var result = await Mediator.Send(command);

        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetBetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        var query = new GetBetHistoryQuery(userId, pageNumber, pageSize);
        var result = await Mediator.Send(query);

        return Ok(result);
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpenBets()
    {
        var userId = GetCurrentUserId();
        var query = new GetOpenBetsQuery(userId);
        var result = await Mediator.Send(query);

        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetBetStatistics()
    {
        var userId = GetCurrentUserId();

        // Get all user bets for statistics (using a large page size to get all)
        var allBetsQuery = new GetBetHistoryQuery(userId, 1, 10000);
        var allBetsResult = await Mediator.Send(allBetsQuery);
        var allBets = allBetsResult.Items;

        var totalBets = allBets.Count;
        var openBets = allBets.Count(b => b.Status == BetStatus.Open);
        var wonBets = allBets.Count(b => b.Status == BetStatus.Won);
        var lostBets = allBets.Count(b => b.Status == BetStatus.Lost);
        var voidBets = allBets.Count(b => b.Status == BetStatus.Void);
        var totalStaked = allBets.Sum(b => b.Amount);
        var totalProfit = allBets.Sum(b => b.Profit);
        var winRate = totalBets > 0 ? (decimal)wonBets / (wonBets + lostBets) * 100 : 0;
        var roi = totalStaked > 0 ? (totalProfit / totalStaked) * 100 : 0;

        var statistics = new
        {
            TotalBets = totalBets,
            OpenBets = openBets,
            WonBets = wonBets,
            LostBets = lostBets,
            VoidBets = voidBets,
            TotalStaked = totalStaked,
            TotalProfit = totalProfit,
            WinRate = Math.Round(winRate, 2),
            ROI = Math.Round(roi, 2)
        };

        var warnings = totalBets < 10 ? new[] { "Statistics may not be reliable with fewer than 10 bets" } : null;

        if (warnings != null)
        {
            return Ok(ApiResponse<object>.Success(statistics, warnings));
        }

        return Ok(statistics);
    }

    [HttpPost("{betId:guid}/cancel")]
    public async Task<IActionResult> CancelBet(Guid betId)
    {
        var userId = GetCurrentUserId();

        var bet = await GetBetAsync(betId, userId);
        if (bet == null)
        {
            var errors = new[] { "Bet not found" };
            return NotFound(ApiResponse<object>.Failure(errors));
        }

        if (bet.Status != BetStatus.Open)
        {
            var errors = new[] { "Only open bets can be cancelled" };
            return BadRequest(ApiResponse<object>.Failure(errors));
        }

        return Ok(ApiResponse<object>.Success(new { message = "Bet cancelled successfully" }));
    }
    private async Task<BetDto?> GetBetAsync(Guid betId, Guid userId)
    {
        return null;
    }
    
    [HttpPost("settle-multiple")]
    public async Task<IActionResult> SettleMultipleBets([FromBody] BulkSettleBetsRequest request)
    {
        var userId = GetCurrentUserId();
        var results = new List<object>();
        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var settlement in request.Settlements)
        {
            try
            {
                var command = new SettleBetCommand(settlement.BetId, userId, settlement.Outcome);
                var result = await Mediator.Send(command);
                results.Add(new { betId = settlement.BetId, success = true, bet = result });
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to settle bet {settlement.BetId}: {ex.Message}");
                results.Add(new { betId = settlement.BetId, success = false, error = ex.Message });
            }
        }

        var successCount = results.Count(r => ((dynamic)r).success);
        if (successCount < request.Settlements.Count)
        {
            warnings.Add($"Only {successCount} of {request.Settlements.Count} bets were settled successfully");
        }

        var response = new
        {
            message = $"Bulk settlement completed. {successCount}/{request.Settlements.Count} successful.",
            results
        };

        if (errors.Any() || warnings.Any())
        {
            return Ok(new ApiResponse<object>
            {
                Value = response,
                IsSuccess = !errors.Any(),
                HasWarnings = warnings.Any(),
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            });
        }

        return Ok(response);
    }
}

public class PlaceBetRequest
{
    public Guid EventId { get; set; }
    public MarketType MarketType { get; set; }
    public OutcomeType SelectedOutcome { get; set; }
    public decimal Amount { get; set; }
    public decimal Odds { get; set; }
}

public class SettleBetRequest
{
    public BetOutcome Outcome { get; set; }
}

public class BulkSettleBetsRequest
{
    public List<BetSettlement> Settlements { get; set; } = new();
}

public class BetSettlement
{
    public Guid BetId { get; set; }
    public BetOutcome Outcome { get; set; }
}