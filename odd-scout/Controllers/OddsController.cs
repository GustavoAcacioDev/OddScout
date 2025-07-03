using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Domain.Enums;

namespace OddScout.API.Controllers;

[Authorize]
public class OddsController : BaseController
{
    private readonly IApplicationDbContext _context;

    public OddsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] OddsSource? source = null, [FromQuery] int take = 50)
    {
        var query = _context.Events
            .Include(e => e.Odds)
            .Where(e => e.EventDateTime >= DateTime.UtcNow.AddHours(-2))
            .OrderBy(e => e.EventDateTime);

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value).OrderBy(e => e.EventDateTime);
        }

        var events = await query
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.League,
                e.EventDateTime,
                e.Team1,
                e.Team2,
                e.Status,
                e.Source,
                e.ExternalLink,
                Odds = e.Odds.Select(o => new
                {
                    o.Id,
                    o.MarketType,
                    o.Team1Odd,
                    o.DrawOdd,
                    o.Team2Odd,
                    o.Source,
                    o.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("events/{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Odds)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        var result = new
        {
            eventEntity.Id,
            eventEntity.League,
            eventEntity.EventDateTime,
            eventEntity.Team1,
            eventEntity.Team2,
            eventEntity.Status,
            eventEntity.Source,
            eventEntity.ExternalLink,
            eventEntity.ScrapedAt,
            Odds = eventEntity.Odds.Select(o => new
            {
                o.Id,
                o.MarketType,
                o.Team1Odd,
                o.DrawOdd,
                o.Team2Odd,
                o.Source,
                o.CreatedAt
            }).ToList()
        };

        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalEvents = await _context.Events.CountAsync();
        var pinnacleEvents = await _context.Events.CountAsync(e => e.Source == OddsSource.Pinnacle);
        var betbyEvents = await _context.Events.CountAsync(e => e.Source == OddsSource.Betby);
        var totalValueBets = await _context.ValueBets.CountAsync();
        var recentValueBets = await _context.ValueBets.CountAsync(vb => vb.CalculatedAt >= DateTime.UtcNow.AddHours(-24));

        var topValueBets = await _context.ValueBets
            .Include(vb => vb.Event)
            .OrderByDescending(vb => vb.ExpectedValue)
            .Take(5)
            .Select(vb => new
            {
                vb.ExpectedValue,
                vb.Event.Team1,
                vb.Event.Team2,
                vb.OutcomeType,
                vb.BetbyOdd
            })
            .ToListAsync();

        return Ok(new
        {
            totalEvents,
            pinnacleEvents,
            betbyEvents,
            totalValueBets,
            recentValueBets,
            topValueBets
        });
    }
}