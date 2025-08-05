using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;

namespace OddScout.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HealthController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Simple database connectivity check
            await _context.Users.Take(1).ToListAsync();
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}