using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Users.Queries.GetUserProfile;
using System.Security.Claims;

namespace OddScout.API.Controllers;

[Authorize]
public class UsersController : BaseController
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query = new GetUserProfileQuery(userId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}
