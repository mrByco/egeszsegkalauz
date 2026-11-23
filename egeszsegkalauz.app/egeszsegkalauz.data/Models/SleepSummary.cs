namespace egeszsegkalauz.data.Models;

public class SleepSummary : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public int? SleepSeconds { get; set; }
    public int? NapSeconds { get; set; }
    public int? DeepSeconds { get; set; }
    public int? LightSeconds { get; set; }
    public int? RemSeconds { get; set; }
    public int? AwakeSeconds { get; set; }
    public int? UnmeasurableSeconds { get; set; }
    public int? AwakeCount { get; set; }
    public int? Score { get; set; }
    public string? ScoreQualifier { get; set; }
    public double? AverageSleepStress { get; set; }
    public double? AverageRespiration { get; set; }
    public double? LowestRespiration { get; set; }
    public double? HighestRespiration { get; set; }
}
