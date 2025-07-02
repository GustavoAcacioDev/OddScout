using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        // For development/demo purposes, just log the reset token
        _logger.LogInformation("Password reset requested for {Email}. Reset token: {ResetToken}",
            email, resetToken);

        // Simulate email sending delay
        await Task.Delay(100, cancellationToken);

        // In production, you would integrate with:
        // - SendGrid
        // - Amazon SES
        // - SMTP server
        // - etc.

        /*
        Example implementation:
        var emailBody = $@"
            <h2>Password Reset Request</h2>
            <p>Your password reset token is: <strong>{resetToken}</strong></p>
            <p>This token will expire in 1 hour.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await _emailProvider.SendAsync(email, "Password Reset", emailBody, cancellationToken);
        */
    }
}