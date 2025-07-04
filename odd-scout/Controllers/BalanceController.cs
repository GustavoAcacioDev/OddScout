using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OddScout.Application.Users.Commands.DepositBalance;
using OddScout.Application.Users.Queries.GetTransactionHistory;
using OddScout.Application.Users.Queries.GetUserProfile;
using OddScout.Domain.Enums;

namespace OddScout.API.Controllers;

[Authorize]
public class BalanceController : BaseController
{
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        var userId = GetCurrentUserId();

        var command = new DepositBalanceCommand(
            userId,
            request.Amount,
            request.Description,
            request.ExternalReference
        );

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentBalance()
    {
        var userId = GetCurrentUserId();
        var query = new GetUserProfileQuery(userId);
        var user = await Mediator.Send(query);

        return Ok(new { balance = user.Balance, lastUpdated = DateTime.UtcNow });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TransactionType? type = null,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var userId = GetCurrentUserId();

        var query = new GetTransactionHistoryQuery(
            userId,
            pageNumber,
            pageSize,
            type,
            status,
            fromDate,
            toDate
        );

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetBalanceSummary()
    {
        var userId = GetCurrentUserId();

        // Obter transações recentes para calcular resumo
        var transactionsQuery = new GetTransactionHistoryQuery(userId, 1, 1000);
        var transactionsResult = await Mediator.Send(transactionsQuery);
        var transactions = transactionsResult.Items;

        var totalDeposits = transactions
            .Where(t => t.Type == TransactionType.Deposit && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        var totalWithdrawals = transactions
            .Where(t => t.Type == TransactionType.Withdrawal && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        var totalBetsPlaced = transactions
            .Where(t => t.Type == TransactionType.BetPlaced && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        var totalBetWinnings = transactions
            .Where(t => t.Type == TransactionType.BetWon && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        var lastTransactions = transactions.Take(5).ToList();

        var userQuery = new GetUserProfileQuery(userId);
        var user = await Mediator.Send(userQuery);

        return Ok(new
        {
            currentBalance = user.Balance,
            totalDeposits,
            totalWithdrawals,
            totalBetsPlaced,
            totalBetWinnings,
            netProfitFromBets = totalBetWinnings - totalBetsPlaced,
            lastTransactions,
            summary = new
            {
                totalIn = totalDeposits + totalBetWinnings,
                totalOut = totalWithdrawals + totalBetsPlaced,
                netChange = (totalDeposits + totalBetWinnings) - (totalWithdrawals + totalBetsPlaced)
            }
        });
    }
}

public class DepositRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}