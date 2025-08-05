using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Infrastructure.Data;

namespace OddScout.API.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("/health")]
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