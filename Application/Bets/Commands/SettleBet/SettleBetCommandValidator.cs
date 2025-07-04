using FluentValidation;

namespace OddScout.Application.Bets.Commands.SettleBet;

public class SettleBetCommandValidator : AbstractValidator<SettleBetCommand>
{
    public SettleBetCommandValidator()
    {
        RuleFor(x => x.BetId)
            .NotEmpty()
            .WithMessage("Bet ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Outcome)
            .IsInEnum()
            .WithMessage("Valid outcome is required (Won, Lost, or Void)");
    }
}