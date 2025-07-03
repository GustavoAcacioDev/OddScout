using FluentValidation;

namespace OddScout.Application.Bets.Queries.GetBetHistory;

public class GetBetHistoryQueryValidator : AbstractValidator<GetBetHistoryQuery>
{
    public GetBetHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be at least 1")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}