using FluentValidation;

namespace OddScout.Application.Bets.Commands.PlaceBet;

public class PlaceBetCommandValidator : AbstractValidator<PlaceBetCommand>
{
    public PlaceBetCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Bet amount must be greater than zero")
            .LessThanOrEqualTo(10000)
            .WithMessage("Bet amount cannot exceed 10,000");

        RuleFor(x => x.Odds)
            .GreaterThan(1.0m)
            .WithMessage("Odds must be greater than 1.0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Odds cannot exceed 1,000");

        RuleFor(x => x.MarketType)
            .IsInEnum()
            .WithMessage("Invalid market type");

        RuleFor(x => x.SelectedOutcome)
            .IsInEnum()
            .WithMessage("Invalid outcome selection");
    }
}