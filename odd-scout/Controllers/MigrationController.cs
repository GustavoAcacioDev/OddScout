using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OddScout.Infrastructure.Data;

namespace OddScout.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MigrationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(ApplicationDbContext context, ILogger<MigrationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunMigrations()
    {
        try
        {
            _logger.LogInformation("Starting manual database migration...");
            
            // Set timeout for migrations
            _context.Database.SetCommandTimeout(300); // 5 minutes
            
            await _context.Database.MigrateAsync();
            
            _logger.LogInformation("Database migration completed successfully");
            
            return Ok(new
            {
                status = "success",
                message = "Database migrations completed successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed");
            
            return StatusCode(500, new
            {
                status = "error",
                message = "Database migration failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetMigrationStatus()
    {
        try
        {
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            
            return Ok(new
            {
                appliedMigrations = appliedMigrations.ToList(),
                pendingMigrations = pendingMigrations.ToList(),
                hasPendingMigrations = pendingMigrations.Any(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get migration status");
            
            return StatusCode(500, new
            {
                status = "error",
                message = "Failed to get migration status",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}