using System.Runtime.CompilerServices;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Hevy;

namespace egeszsegkalauz.tests.Integrations;

public class FakeHevyAdapter : IHevyAdapterService
{
    public const string ValidCode = "valid-code";
    public const string ValidSignature = "sha256=valid";

    private int issued;

    public string ExternalUserId { get; set; } = "hevy-user-1";
    public DateTime Now { get; set; } = new(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);
    public List<string> RefreshTokensUsed { get; } = [];
    public List<DateTime?> SyncSince { get; } = [];
    public List<string> AccessTokensUsed { get; } = [];
    public List<string> WorkoutIds { get; set; } = ["w1", "w2"];
    public List<string> DeletedWorkoutIds { get; set; } = [];

    public string SignatureHeader => "Hevy-Signature";

    public string BuildAuthorizationUrl(string state)
    {
        return $"https://hevy.test/oauth/authorize?state={Uri.EscapeDataString(state)}";
    }

    public Task<OAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (code != ValidCode)
            throw new IntegrationKeyRejectedException("Hevy rejected the authorization.");
        return Task.FromResult(Issue());
    }

    public Task<OAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        RefreshTokensUsed.Add(refreshToken);
        return Task.FromResult(Issue());
    }

    public async IAsyncEnumerable<SyncChunk> SyncAsync(string accessToken, DateTime? since,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SyncSince.Add(since);
        AccessTokensUsed.Add(accessToken);
        await Task.Yield();
        yield return new SyncChunk
        {
            Records = WorkoutIds.Select(WorkoutRecord).ToList(),
            Deleted = DeletedWorkoutIds.Select(id => new RawRecordKey { Endpoint = HevyAdapterService.WorkoutEndpoint, Key = id }).ToList(),
        };
        yield return new SyncChunk
        {
            Records =
            [
                new RawRecord { Endpoint = HevyAdapterService.ExerciseTemplateEndpoint, Key = "T1", Json = HevySamples.Template },
                new RawRecord
                {
                    Endpoint = HevyAdapterService.BodyMeasurementEndpoint, Key = "2026-11-20", Date = new DateOnly(2026, 11, 20),
                    Json = HevySamples.BodyMeasurement,
                },
            ],
        };
    }

    public IReadOnlyList<string> CompletelyListedEndpoints(bool fullSync)
    {
        return fullSync
            ? [HevyAdapterService.WorkoutEndpoint, HevyAdapterService.ExerciseTemplateEndpoint, HevyAdapterService.BodyMeasurementEndpoint]
            : [HevyAdapterService.ExerciseTemplateEndpoint, HevyAdapterService.BodyMeasurementEndpoint];
    }

    public bool IsSignatureValid(string body, string? signature)
    {
        return signature == ValidSignature;
    }

    public WebhookNotification? Parse(string body)
    {
        return new WebhookNotification { Type = "workout.created", ExternalUserId = ExternalUserId, ItemId = body };
    }

    public Task<SyncChunk> FetchAsync(string accessToken, WebhookNotification notification, CancellationToken cancellationToken)
    {
        AccessTokensUsed.Add(accessToken);
        return Task.FromResult(new SyncChunk { Records = [WorkoutRecord(notification.ItemId ?? "")] });
    }

    private static RawRecord WorkoutRecord(string id)
    {
        return new RawRecord
        {
            Endpoint = HevyAdapterService.WorkoutEndpoint, Key = id, Date = new DateOnly(2026, 11, 18), Json = HevySamples.Workout(id),
        };
    }

    private OAuthTokens Issue()
    {
        issued++;
        return new OAuthTokens
        {
            AccessToken = $"access-{issued}",
            RefreshToken = $"refresh-{issued}",
            ExpiresAtUtc = Now.AddHours(1),
            ExternalUserId = ExternalUserId,
            AccountName = "teszt_elek",
        };
    }
}
