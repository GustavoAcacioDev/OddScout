using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;
using OddScout.Domain.Enums;

namespace OddScout.Application.Bets.Commands.PlaceBet;

public sealed record PlaceBetCommand(
    Guid UserId,
    Guid EventId,
    MarketType MarketType,
    OutcomeType SelectedOutcome,
    decimal Amount,
    decimal Odds
) : ICommand<BetDto>;