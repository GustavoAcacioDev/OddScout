using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Entities;

namespace OddScout.Application.Users.Commands.SignUp;

public class SignUpCommandHandler : ICommandHandler<SignUpCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public SignUpCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("User with this email already exists");

        // Hash password
        var hashedPassword = _passwordService.HashPassword(request.Password);

        // Create new user
        var user = new User(request.Email, request.Name, hashedPassword);

        // Generate tokens
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();
        user.SetRefreshToken(refreshToken, refreshTokenExpiry);

        // Save to database
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Generate access token
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshTokenExpiry,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Balance = user.Balance,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }
}