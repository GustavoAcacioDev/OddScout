using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Bets.Commands.PlaceBet;
using OddScout.Application.Bets.Commands.SettleBet;
using OddScout.Application.Bets.Queries.GetBetHistory;
using OddScout.Application.Bets.Queries.GetOpenBets;
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

        return Ok(new
        {
            message = $"Bet settled as {request.Outcome}",
            bet = result
        });
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

        return Ok(new
        {
            totalBets,
            openBets,
            wonBets,
            lostBets,
            voidBets,
            totalStaked,
            totalProfit,
            winRate = Math.Round(winRate, 2),
            roi = Math.Round(roi, 2),
            averageBetAmount = totalBets > 0 ? Math.Round(totalStaked / totalBets, 2) : 0,
            biggestWin = allBets.Where(b => b.Status == BetStatus.Won).DefaultIfEmpty().Max(b => b?.Profit ?? 0),
            biggestLoss = allBets.Where(b => b.Status == BetStatus.Lost).DefaultIfEmpty().Min(b => b?.Profit ?? 0)
        });
    }

    // NOVO: Settle múltiplas apostas de uma vez
    [HttpPost("settle-bulk")]
    public async Task<IActionResult> SettleMultipleBets([FromBody] BulkSettleBetsRequest request)
    {
        var userId = GetCurrentUserId();
        var results = new List<object>();

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
                results.Add(new { betId = settlement.BetId, success = false, error = ex.Message });
            }
        }

        var successCount = results.Count(r => ((dynamic)r).success);
        return Ok(new
        {
            message = $"Processed {results.Count} bets. {successCount} successful.",
            results
        });
    }

    // NOVO: Quick actions para apostas abertas
    [HttpPost("{betId:guid}/quick-win")]
    public async Task<IActionResult> QuickWin(Guid betId)
    {
        var userId = GetCurrentUserId();
        var command = new SettleBetCommand(betId, userId, BetOutcome.Won);
        var result = await Mediator.Send(command);

        return Ok(new { message = "Bet marked as WON! 🎉", bet = result });
    }

    [HttpPost("{betId:guid}/quick-loss")]
    public async Task<IActionResult> QuickLoss(Guid betId)
    {
        var userId = GetCurrentUserId();
        var command = new SettleBetCommand(betId, userId, BetOutcome.Lost);
        var result = await Mediator.Send(command);

        return Ok(new { message = "Bet marked as LOST 😞", bet = result });
    }

    [HttpPost("{betId:guid}/quick-void")]
    public async Task<IActionResult> QuickVoid(Guid betId)
    {
        var userId = GetCurrentUserId();
        var command = new SettleBetCommand(betId, userId, BetOutcome.Void);
        var result = await Mediator.Send(command);

        return Ok(new { message = "Bet VOIDED - stake returned 💰", bet = result });
    }
}

// Request DTOs
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