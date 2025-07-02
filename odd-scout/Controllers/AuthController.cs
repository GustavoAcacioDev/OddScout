using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Users.Commands.RequestPasswordReset;
using OddScout.Application.Users.Commands.ResetPassword;
using OddScout.Application.Users.Commands.SignIn;
using OddScout.Application.Users.Commands.SignUp;

namespace OddScout.API.Controllers;

[AllowAnonymous]
public class AuthController : BaseController
{
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "If the email exists, a password reset token has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "Password has been reset successfully." });
    }
}