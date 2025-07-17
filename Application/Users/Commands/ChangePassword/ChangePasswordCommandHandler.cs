using Microsoft.EntityFrameworkCore;
using OddScout.Application.Common.Interfaces;

namespace OddScout.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user in database
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException("User not found");

        // Verify current password
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        // Hash new password
        var hashedNewPassword = _passwordService.HashPassword(request.NewPassword);

        // Update user's password - using ResetPassword method but clearing tokens is not needed for change password
        // In a production scenario, we might want to add a specific ChangePassword method to the User entity
        user.ResetPassword(hashedNewPassword);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);
    }
}