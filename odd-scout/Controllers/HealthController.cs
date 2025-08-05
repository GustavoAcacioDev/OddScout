using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;

namespace OddScout.API.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HealthController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("health")]
    [HttpGet("/health")]  // Explicit route for Railway
    public async Task<IActionResult> Get()
    {
        try
        {
            // Basic health check without database dependency (for initial deployment)
            var canConnectToDb = await CanConnectToDatabase();
            
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                version = "1.0.0",
                database = canConnectToDb ? "connected" : "connection_failed"
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

    private async Task<bool> CanConnectToDatabase()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet("/")]
    [HttpGet("")]
    public IActionResult Root()
    {
        return Ok(new
        {
            message = "OddScout API is running",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            version = "1.0.0"
        });
    }
}