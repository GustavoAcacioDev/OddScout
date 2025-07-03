using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OddScout.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OddScout.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // CORRIGIDO: Usar claims que funcionam com [Authorize]
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // MUDADO: Usar ClaimTypes.NameIdentifier
            new Claim(ClaimTypes.Email, email),                      // MUDADO: Usar ClaimTypes.Email
            new Claim(ClaimTypes.Name, name),                        // MUDADO: Usar ClaimTypes.Name
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            // Adicionar claim para facilitar debugging
            new Claim("user_id", userId.ToString())
        };

        var tokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: tokenExpiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!);
        return DateTime.UtcNow.AddDays(expirationDays);
    }
}