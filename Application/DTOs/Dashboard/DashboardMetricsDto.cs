namespace OddScout.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public DashboardMetricDto Total { get; set; } = null!;
    public DashboardMetricDto WinRate { get; set; } = null!;
    public DashboardMetricDto Profit { get; set; } = null!;
    public DashboardMetricDto Active { get; set; } = null!;
}

public class DashboardMetricDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty; // "currency", "percentage", "count", "text"
    public decimal? ChangeValue { get; set; } // Valor numérico da mudança
    public string ChangeUnit { get; set; } = string.Empty; // "percentage", "absolute", "text"
    public string ChangeText { get; set; } = string.Empty; // "from last period", "ending today", etc.
}