using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.Bets.Commands.PlaceBet;
using OddScout.Domain.Enums;

namespace OddScout.API.Controllers;

[Authorize]
public class BettingController : BaseController
{
    private readonly IApplicationDbContext _context;

    public BettingController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("available-events")]
    public async Task<IActionResult> GetAvailableEvents(
        [FromQuery] int take = 20,
        [FromQuery] OddsSource? source = null)
    {
        var query = _context.Events
            .Include(e => e.Odds)
            .Where(e => e.EventDateTime >= DateTime.UtcNow &&
                       e.EventDateTime <= DateTime.UtcNow.AddDays(7) &&
                       e.Status == EventStatus.Scheduled)
            .Where(e => e.Odds.Any()); // Só eventos com odds

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value);
        }

        var events = await query
            .OrderBy(e => e.EventDateTime)
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.League,
                e.EventDateTime,
                e.Team1,
                e.Team2,
                e.Source,
                e.ExternalLink,
                Odds = e.Odds.Select(o => new
                {
                    o.Id,
                    o.MarketType,
                    o.Team1Odd,
                    o.DrawOdd,
                    o.Team2Odd,
                    o.Source
                }).ToList(),
                CanBet = e.EventDateTime > DateTime.UtcNow.AddMinutes(15) // Pelo menos 15 min antes
            })
            .ToListAsync();

        return Ok(new
        {
            message = "Available events for betting",
            count = events.Count,
            events
        });
    }

    [HttpGet("value-opportunities")]
    public async Task<IActionResult> GetValueOpportunities([FromQuery] int take = 10)
    {
        var valueBets = await _context.ValueBets
            .Include(vb => vb.Event)
            .Where(vb => vb.Event.EventDateTime >= DateTime.UtcNow &&
                        vb.Event.EventDateTime <= DateTime.UtcNow.AddDays(2) &&
                        vb.ExpectedValue > 0.01m) // Só value bets positivos
            .OrderByDescending(vb => vb.ExpectedValue)
            .Take(take)
            .Select(vb => new
            {
                ValueBetId = vb.Id,
                EventId = vb.EventId,
                League = vb.Event.League,
                EventDateTime = vb.Event.EventDateTime,
                Team1 = vb.Event.Team1,
                Team2 = vb.Event.Team2,
                BetbyLink = vb.Event.ExternalLink,
                Recommendation = new
                {
                    Outcome = vb.OutcomeType,
                    OutcomeDescription = GetOutcomeDescription(vb.OutcomeType, vb.Event.Team1, vb.Event.Team2),
                    BetbyOdd = vb.BetbyOdd,
                    PinnacleOdd = vb.PinnacleOdd,
                    ExpectedValue = vb.ExpectedValue,
                    ImpliedProbability = vb.ImpliedProbability,
                    ConfidenceScore = vb.ConfidenceScore
                },
                CanBet = vb.Event.EventDateTime > DateTime.UtcNow.AddMinutes(15)
            })
            .ToListAsync();

        return Ok(new
        {
            message = "Value betting opportunities",
            count = valueBets.Count,
            opportunities = valueBets
        });
    }

    [HttpPost("place-bet-on-event")]
    public async Task<IActionResult> PlaceBetOnEvent([FromBody] PlaceBetOnEventRequest request)
    {
        var userId = GetCurrentUserId();

        // Verificar se o evento e odds existem
        var eventWithOdds = await _context.Events
            .Include(e => e.Odds)
            .FirstOrDefaultAsync(e => e.Id == request.EventId);

        if (eventWithOdds == null)
            return BadRequest(new { error = "Event not found" });

        // Buscar as odds específicas
        var odds = eventWithOdds.Odds
            .FirstOrDefault(o => o.MarketType == request.MarketType && o.Source == request.OddsSource);

        if (odds == null)
            return BadRequest(new { error = "Odds not found for this market and source" });

        // Determinar a odd específica baseada no outcome
        var selectedOdd = request.SelectedOutcome switch
        {
            OutcomeType.Team1Win => odds.Team1Odd,
            OutcomeType.Draw => odds.DrawOdd,
            OutcomeType.Team2Win => odds.Team2Odd,
            _ => throw new ArgumentException("Invalid outcome")
        };

        // Criar o command usando a odd real
        var command = new PlaceBetCommand(
            userId,
            request.EventId,
            request.MarketType,
            request.SelectedOutcome,
            request.Amount,
            selectedOdd
        );

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("place-value-bet")]
    public async Task<IActionResult> PlaceValueBet([FromBody] PlaceValueBetRequest request)
    {
        var userId = GetCurrentUserId();

        // Buscar o value bet
        var valueBet = await _context.ValueBets
            .Include(vb => vb.Event)
            .FirstOrDefaultAsync(vb => vb.Id == request.ValueBetId);

        if (valueBet == null)
            return BadRequest(new { error = "Value bet not found" });

        if (valueBet.Event.EventDateTime <= DateTime.UtcNow.AddMinutes(15))
            return BadRequest(new { error = "Event is too close to start time" });

        // Criar aposta baseada no value bet
        var command = new PlaceBetCommand(
            userId,
            valueBet.EventId,
            valueBet.MarketType,
            valueBet.OutcomeType,
            request.Amount,
            valueBet.BetbyOdd // Usar a odd da Betby que tem valor
        );

        var result = await Mediator.Send(command);
        return Ok(new
        {
            bet = result,
            valueBetInfo = new
            {
                expectedValue = valueBet.ExpectedValue,
                impliedProbability = valueBet.ImpliedProbability,
                confidenceScore = valueBet.ConfidenceScore,
                pinnacleOdd = valueBet.PinnacleOdd
            }
        });
    }

    [HttpGet("event/{eventId:guid}/odds")]
    public async Task<IActionResult> GetEventOdds(Guid eventId)
    {
        var eventWithOdds = await _context.Events
            .Include(e => e.Odds)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventWithOdds == null)
            return NotFound(new { error = "Event not found" });

        var oddsFormatted = eventWithOdds.Odds
            .GroupBy(o => o.Source)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Select(o => new
                {
                    marketType = o.MarketType,
                    outcomes = new
                    {
                        team1 = new { name = eventWithOdds.Team1, odd = o.Team1Odd },
                        draw = new { name = "Draw", odd = o.DrawOdd },
                        team2 = new { name = eventWithOdds.Team2, odd = o.Team2Odd }
                    }
                }).ToList()
            );

        return Ok(new
        {
            eventInfo = new
            {
                eventWithOdds.Id,
                eventWithOdds.League,
                eventWithOdds.EventDateTime,
                eventWithOdds.Team1,
                eventWithOdds.Team2,
                eventWithOdds.ExternalLink
            },
            oddsBySource = oddsFormatted,
            canBet = eventWithOdds.EventDateTime > DateTime.UtcNow.AddMinutes(15)
        });
    }

    private static string GetOutcomeDescription(OutcomeType outcome, string team1, string team2)
    {
        return outcome switch
        {
            OutcomeType.Team1Win => team1,
            OutcomeType.Draw => "Draw",
            OutcomeType.Team2Win => team2,
            _ => "Unknown"
        };
    }
}

public class PlaceBetOnEventRequest
{
    public Guid EventId { get; set; }
    public MarketType MarketType { get; set; }
    public OutcomeType SelectedOutcome { get; set; }
    public OddsSource OddsSource { get; set; } // Pinnacle ou Betby
    public decimal Amount { get; set; }
}

public class PlaceValueBetRequest
{
    public Guid ValueBetId { get; set; }
    public decimal Amount { get; set; }
}