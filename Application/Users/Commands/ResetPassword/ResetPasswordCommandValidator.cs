using FluentValidation;

namespace OddScout.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-mai obrigatório")
            .EmailAddress()
            .WithMessage("Formato de e-mail inválido");

        RuleFor(x => x.ResetToken)
            .NotEmpty()
            .WithMessage("Reset token is required")
            .Length(6)
            .WithMessage("Reset token must be 6 digits");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Senha obrigatória")
            .MinimumLength(8)
            .WithMessage("A senha precisa conter no mínimo 8 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("A senha precisa conter pelo menos uma letra minúscula, uma letra maiúscula, um digito, e um caractere especial");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("As senhas não conhecidem");
    }
}