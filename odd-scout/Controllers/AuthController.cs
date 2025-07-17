using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Users.Commands.ChangePassword;
using OddScout.Application.Users.Commands.RequestPasswordReset;
using OddScout.Application.Users.Commands.ResetPassword;
using OddScout.Application.Users.Commands.SignIn;
using OddScout.Application.Users.Commands.SignUp;

namespace OddScout.API.Controllers;

public class AuthController : BaseController
{
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn([FromBody] SignInCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("request-password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "If the email exists, a password reset token has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await Mediator.Send(command);
        return Ok(new { message = "Password has been reset successfully." });
    }

    [HttpPost("change-password")]
    [Authorize] // Requires authentication
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId(); // Get from JWT token

        var command = new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmPassword
        );

        await Mediator.Send(command);
        return Ok(new { message = "Password has been changed successfully." });
    }
}

// DTO for the request body
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);