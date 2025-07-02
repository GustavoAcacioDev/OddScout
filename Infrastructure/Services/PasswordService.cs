using Microsoft.AspNetCore.Identity;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _passwordHasher;

    public PasswordService()
    {
        _passwordHasher = new PasswordHasher<object>();
    }

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(new object(), hashedPassword, password);
        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}