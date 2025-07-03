using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Scraping.Commands.CalculateValueBets;
using OddScout.Application.Scraping.Commands.RunBetbyScraping;
using OddScout.Application.Scraping.Commands.RunPinnacleScraping;
using OddScout.Application.Scraping.Queries.GetValueBets;

namespace OddScout.API.Controllers;

[Authorize]
public class ScrapingController : BaseController
{
    [HttpPost("pinnacle/run")]
    public async Task<IActionResult> RunPinnacleScraping()
    {
        var command = new RunPinnacleScrapingCommand();
        var result = await Mediator.Send(command);
        return Ok(new { message = $"Pinnacle scraping completed. {result} events processed.", count = result });
    }

    [HttpPost("betby/run")]
    public async Task<IActionResult> RunBetbyScraping()
    {
        var command = new RunBetbyScrapingCommand();
        var result = await Mediator.Send(command);
        return Ok(new { message = $"Betby scraping completed. {result} events processed.", count = result });
    }

    [HttpPost("value-bets/calculate")]
    public async Task<IActionResult> CalculateValueBets()
    {
        var command = new CalculateValueBetsCommand();
        var result = await Mediator.Send(command);
        return Ok(new { message = $"Value bet calculation completed. {result.Count} value bets found.", valueBets = result });
    }

    [HttpGet("value-bets")]
    public async Task<IActionResult> GetValueBets([FromQuery] int? take = null, [FromQuery] decimal? minimumEV = null)
    {
        var query = new GetValueBetsQuery(take, minimumEV);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("run-complete-cycle")]
    public async Task<IActionResult> RunCompleteCycle()
    {
        try
        {
            // 1. Scraping do Pinnacle
            var pinnacleCommand = new RunPinnacleScrapingCommand();
            var pinnacleResult = await Mediator.Send(pinnacleCommand);

            // 2. Scraping do Betby
            var betbyCommand = new RunBetbyScrapingCommand();
            var betbyResult = await Mediator.Send(betbyCommand);

            // 3. Calcular Value Bets
            var valueCommand = new CalculateValueBetsCommand();
            var valueResult = await Mediator.Send(valueCommand);

            return Ok(new
            {
                message = "Complete scraping cycle finished successfully.",
                pinnacleEvents = pinnacleResult,
                betbyEvents = betbyResult,
                valueBets = valueResult.Count,
                topValueBets = valueResult.Take(10).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error during complete cycle", error = ex.Message });
        }
    }
}