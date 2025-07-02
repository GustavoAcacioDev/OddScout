using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Users.Commands.SignUp;

public sealed record SignUpCommand(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword
) : ICommand<AuthResult>;