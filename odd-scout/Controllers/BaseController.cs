using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OddScout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format in token");
        }

        return userId;
    }

    /// <summary>
    /// Gets the current user email from the JWT token
    /// </summary>
    protected string GetCurrentUserEmail()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("User email not found in token");
        }

        return email;
    }

    /// <summary>
    /// Gets the current user name from the JWT token
    /// </summary>
    protected string GetCurrentUserName()
    {
        var name = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(name))
        {
            throw new UnauthorizedAccessException("User name not found in token");
        }

        return name;
    }
}