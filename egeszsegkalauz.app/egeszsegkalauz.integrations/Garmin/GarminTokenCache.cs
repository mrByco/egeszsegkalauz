using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;

namespace egeszsegkalauz.integrations.Garmin;

public class GarminTokenCache
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, CachedToken> tokens = new();
    private readonly TimeProvider timeProvider;

    public GarminTokenCache(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public string? Get(string apiKey)
    {
        return tokens.TryGetValue(CacheKey(apiKey), out var cached)
               && cached.ExpiresAt - RefreshMargin > timeProvider.GetUtcNow()
            ? cached.Token
            : null;
    }

    public void Set(string apiKey, string token)
    {
        tokens[CacheKey(apiKey)] = new CachedToken { Token = token, ExpiresAt = ExpiryOf(token) };
    }

    public void Remove(string apiKey)
    {
        tokens.TryRemove(CacheKey(apiKey), out _);
    }

    private DateTimeOffset ExpiryOf(string token)
    {
        try
        {
            return new JsonWebToken(token).ValidTo is var validTo && validTo > DateTime.MinValue
                ? new DateTimeOffset(validTo, TimeSpan.Zero)
                : timeProvider.GetUtcNow() + FallbackLifetime;
        }
        catch (ArgumentException)
        {
            return timeProvider.GetUtcNow() + FallbackLifetime;
        }
    }

    private static string CacheKey(string apiKey) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

    private class CachedToken
    {
        public required string Token { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
    }
}
