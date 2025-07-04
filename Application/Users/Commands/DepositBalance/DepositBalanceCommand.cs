using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Users.Commands.DepositBalance;

public sealed record DepositBalanceCommand(
    Guid UserId,
    decimal Amount,
    string Description,
    string? ExternalReference = null
) : ICommand<TransactionDto>;