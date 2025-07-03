using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Users.Queries.GetUserProfile;

namespace OddScout.API.Controllers;

[Authorize]
public class UsersController : BaseController
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId(); // MUDADO: Usar método do BaseController
        var query = new GetUserProfileQuery(userId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}