using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(
    string Email
) : ICommand;