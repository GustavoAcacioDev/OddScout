using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : ICommand;