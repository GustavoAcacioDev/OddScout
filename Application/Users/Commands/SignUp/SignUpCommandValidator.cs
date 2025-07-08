using FluentValidation;

namespace OddScout.Application.Users.Commands.SignUp;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório")
            .MaximumLength(100)
            .WithMessage("Nome não pode ter mais de 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-mai obrigatório")
            .EmailAddress()
            .WithMessage("Formato de e-mail inválido")
            .MaximumLength(255)
            .WithMessage("E-mail não pode ter mais de 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Senha obrigatória")
            .MinimumLength(8)
            .WithMessage("A senha precisa conter no mínimo 8 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("A senha precisa conter pelo menos uma letra minúscula, uma letra maiúscula, um digito, e um caractere especial");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("As senhas não conhecidem");
    }
}