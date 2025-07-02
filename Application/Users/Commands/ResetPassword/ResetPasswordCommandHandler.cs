using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null)
            throw new InvalidOperationException("Invalid reset token");

        if (!user.IsPasswordResetTokenValid(request.ResetToken))
            throw new InvalidOperationException("Invalid or expired reset token");

        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.ResetPassword(newPasswordHash);

        await _context.SaveChangesAsync(cancellationToken);
    }
}