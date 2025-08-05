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
            
            // Check if database exists, create if not
            await _context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("Database migration completed successfully");
            
            return Ok(new
            {
                status = "success",
                message = "Database created/migrated successfully",
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

    [HttpPost("force-create")]
    public async Task<IActionResult> ForceCreateDatabase()
    {
        try
        {
            _logger.LogInformation("Force creating database schema...");
            
            // Drop and recreate database (use with caution!)
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("Database force created successfully");
            
            return Ok(new
            {
                status = "success",
                message = "Database force created successfully",
                timestamp = DateTime.UtcNow,
                warning = "All existing data was deleted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database force creation failed");
            
            return StatusCode(500, new
            {
                status = "error",
                message = "Database force creation failed",
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