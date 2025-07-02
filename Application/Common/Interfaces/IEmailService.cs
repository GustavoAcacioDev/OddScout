namespace OddScout.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}