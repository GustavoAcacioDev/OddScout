namespace OddScout.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string name);
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiry();
}