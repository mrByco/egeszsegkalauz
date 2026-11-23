namespace egeszsegkalauz.integrations.Common;

public class SleepSession
{
    public required DateOnly Date { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public int? SleepSeconds { get; init; }
    public int? NapSeconds { get; init; }
    public int? DeepSeconds { get; init; }
    public int? LightSeconds { get; init; }
    public int? RemSeconds { get; init; }
    public int? AwakeSeconds { get; init; }
    public int? UnmeasurableSeconds { get; init; }
    public int? AwakeCount { get; init; }
    public int? Score { get; init; }
    public string? ScoreQualifier { get; init; }
    public double? AverageSleepStress { get; init; }
    public double? AverageRespiration { get; init; }
    public double? LowestRespiration { get; init; }
    public double? HighestRespiration { get; init; }
}
