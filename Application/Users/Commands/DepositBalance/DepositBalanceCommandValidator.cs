using FluentValidation;

namespace OddScout.Application.Users.Commands.DepositBalance;

public class DepositBalanceCommandValidator : AbstractValidator<DepositBalanceCommand>
{
    public DepositBalanceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Deposit amount must be greater than zero")
            .LessThanOrEqualTo(50000)
            .WithMessage("Deposit amount cannot exceed 50,000")
            .Must(BeValidAmount)
            .WithMessage("Deposit amount must have at most 2 decimal places");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.ExternalReference)
            .MaximumLength(100)
            .WithMessage("External reference cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalReference));
    }

    private static bool BeValidAmount(decimal amount)
    {
        // Verificar se tem no máximo 2 casas decimais
        return decimal.Round(amount, 2) == amount;
    }
}