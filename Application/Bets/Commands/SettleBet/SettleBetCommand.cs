using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Commands.SettleBet;

public sealed record SettleBetCommand(
    Guid BetId,
    Guid UserId,
    BetOutcome Outcome
) : ICommand<BetDto>;

public enum BetOutcome
{
    Won = 1,
    Lost = 2,
    Void = 3
}