using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Users.Commands.SignIn;

public sealed record SignInCommand(
    string Email,
    string Password
) : ICommand<SignInResult>;