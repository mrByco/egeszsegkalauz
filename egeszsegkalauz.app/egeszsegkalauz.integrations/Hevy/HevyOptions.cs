namespace egeszsegkalauz.integrations.Hevy;

public class HevyOptions
{
    public const string SectionName = "Integrations:Hevy";

    public Uri ApiBaseUrl { get; set; } = new("https://api.hevyapp.com");
    public Uri AuthorizeUrl { get; set; } = new("https://hevy.com/oauth/authorize");
    public Uri TokenUrl { get; set; } = new("https://hevy.com/api/token");
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public TimeSpan RequestInterval { get; set; } = TimeSpan.FromMilliseconds(300);

    // Hevy's limits are fixed five-minute windows and a 429 carries no Retry-After.
    public TimeSpan RateLimitPause { get; set; } = TimeSpan.FromMinutes(5);
}
