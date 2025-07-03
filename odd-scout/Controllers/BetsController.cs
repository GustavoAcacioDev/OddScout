using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Bets.Commands.PlaceBet;
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
        var userId = GetCurrentUserId(); // MUDADO: Usar método do BaseController

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

    [HttpGet("history")]
    public async Task<IActionResult> GetBetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId(); // MUDADO: Usar método do BaseController
        var query = new GetBetHistoryQuery(userId, pageNumber, pageSize);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpenBets()
    {
        var userId = GetCurrentUserId(); // MUDADO: Usar método do BaseController
        var query = new GetOpenBetsQuery(userId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetBetStatistics()
    {
        var userId = GetCurrentUserId(); // MUDADO: Usar método do BaseController

        // Get all user bets for statistics (using a large page size to get all)
        var allBetsQuery = new GetBetHistoryQuery(userId, 1, 10000);
        var allBetsResult = await Mediator.Send(allBetsQuery);
        var allBets = allBetsResult.Items;

        var totalBets = allBets.Count;
        var openBets = allBets.Count(b => b.Status == BetStatus.Open);
        var wonBets = allBets.Count(b => b.Status == BetStatus.Won);
        var lostBets = allBets.Count(b => b.Status == BetStatus.Lost);
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
            totalStaked,
            totalProfit,
            winRate = Math.Round(winRate, 2),
            roi = Math.Round(roi, 2)
        });
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