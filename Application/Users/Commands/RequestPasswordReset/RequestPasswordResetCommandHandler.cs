using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public RequestPasswordResetCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration attacks
        if (user is null)
            return;

        // Generate reset token (6 digits for simplicity)
        var resetToken = new Random().Next(100000, 999999).ToString();
        var tokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

        user.GeneratePasswordResetToken(resetToken, tokenExpiry);
        await _context.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, cancellationToken);
    }
}