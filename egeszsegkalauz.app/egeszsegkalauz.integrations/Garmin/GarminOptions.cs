namespace egeszsegkalauz.integrations.Garmin;

public class GarminOptions
{
    public const string SectionName = "Integrations:Garmin";

    public Uri BaseUrl { get; set; } = new("https://personal.connectapi.garmin.com");
    public TimeSpan SummaryRequestInterval { get; set; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan DailyRequestInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan RateLimitPause { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxAttempts { get; set; } = 5;
}
