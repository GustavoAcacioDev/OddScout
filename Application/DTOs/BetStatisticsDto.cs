namespace OddScout.Application.DTOs;

public class BetStatisticsDto
{
    public int TotalBets { get; set; }
    public int OpenBets { get; set; }
    public int WonBets { get; set; }
    public int LostBets { get; set; }
    public int VoidBets { get; set; }
    public decimal TotalStaked { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal WinRate { get; set; }
    public decimal Roi { get; set; }
    public decimal AverageOdds { get; set; }
    public decimal BiggestWin { get; set; }
    public decimal BiggestLoss { get; set; }
    public decimal ProfitLoss { get; set; }
}