using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Users.Commands.SignIn;

public class SignInCommandHandler : ICommandHandler<SignInCommand, SignInResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public SignInCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<SignInResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        // Record login
        user.RecordLogin();

        // Generate new tokens
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);

        await _context.SaveChangesAsync(cancellationToken);

        // Generate access token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);

        // Retorna apenas os dados de autenticação
        return new SignInResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshTokenExpiry
        };
    }
}