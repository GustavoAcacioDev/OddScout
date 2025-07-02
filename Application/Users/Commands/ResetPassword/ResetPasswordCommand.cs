using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string ResetToken,
    string NewPassword,
    string ConfirmPassword
) : ICommand;