using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;
using egeszsegkalauz.tests.Integrations;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Services;

public class GarminStoreTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    private int userId;

    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
        userId = await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Id).SingleAsync();
    }

    [Test]
    public async Task AHeartRateDayIsCopiedAsGarminSentItIncludingOffWristReadings()
    {
        await StoreAsync(Record(GarminAdapterService.HeartRateQuery, "2026-11-20", Day, GarminSamples.HeartRateDay(Day)));

        await using var db = PostgresFixture.CreateDbContext();
        var day = await db.GarminHeartRateDays.SingleAsync();
        Assert.That((day.CalendarDate, day.RestingHeartRate, day.LastSevenDaysAvgRestingHeartRate), Is.EqualTo((Day, (double?)52, (double?)51)));
        Assert.That(day.StartTimestampGMT, Is.EqualTo("2026-11-19T23:00:00.0"));
        var values = await db.GarminHeartRateValues.OrderBy(x => x.Timestamp).ToListAsync();
        Assert.That(values.Select(x => x.HeartRate), Is.EqualTo(new double?[] { 60, null, 64 }));
        Assert.That(values[0].Timestamp, Is.EqualTo(1795132800000));
    }

    [Test]
    public async Task ADayThatComesBackEmptyClearsTheBufferedDay()
    {
        await StoreAsync(Record(GarminAdapterService.HeartRateQuery, "2026-11-20", Day, GarminSamples.HeartRateDay(Day)));

        await StoreAsync(Record(GarminAdapterService.HeartRateQuery, "2026-11-20", Day, "null"));

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.GarminHeartRateDays.AnyAsync(), Is.False);
        Assert.That(await db.GarminHeartRateValues.AnyAsync(), Is.False);
    }

    [Test]
    public async Task UserSummaryFieldsKeepGarminsNamesAndValues()
    {
        await StoreAsync(Record(GarminAdapterService.UserSummaryQuery, "2026-11-20", Day, GarminSamples.UserSummary(Day)));

        await using var db = PostgresFixture.CreateDbContext();
        var summary = await db.GarminUserSummaries.SingleAsync();
        Assert.That((summary.TotalSteps, summary.VigorousIntensityMinutes, summary.StressQualifier, summary.AverageSpo2),
            Is.EqualTo(((double?)8123, (double?)12, (string?)"BALANCED", (double?)null)));
        Assert.That(await db.RawHealthRecords.Select(x => x.Payload).SingleAsync(), Does.Contain("someNewField"));
    }

    [Test]
    public async Task SummariesLandInTheirOwnGarminTables()
    {
        await StoreAsync(
            Record(GarminAdapterService.SleepSummariesEndpoint, "2026-11-19", new DateOnly(2026, 11, 19), Item(GarminSamples.SleepSummaries, 0)),
            Record(GarminAdapterService.SleepSummariesEndpoint, "2026-11-20", Day, Item(GarminSamples.SleepSummaries, 1)),
            Record(GarminAdapterService.BodyBatteryEndpoint, "2026-11-20", Day, Item(GarminSamples.BodyBattery, 0)),
            Record(GarminAdapterService.Vo2MaxEndpoint, "2026-11-20", Day, Item(GarminSamples.Vo2Max, 0)),
            Record(GarminAdapterService.WeightEndpoint, "1795248882000", null, Item(GarminSamples.Weight, 0)),
            Record(GarminAdapterService.ActivityEndpoint, "42", Day, GarminSamples.Activity));

        await using var db = PostgresFixture.CreateDbContext();
        var sleep = await db.GarminSleepSummaries.SingleAsync(x => x.CalendarDate == new DateOnly(2026, 11, 19));
        Assert.That((sleep.DeepSleepSeconds, sleep.SleepScoresOverallValue, sleep.SleepScoresOverallQualifierKey, sleep.SleepStartTimestampGMT),
            Is.EqualTo(((double?)5400, (double?)81, (string?)"GOOD", (long?)1795125600000)));
        Assert.That(await db.GarminSleepSummaries.CountAsync(), Is.EqualTo(2));

        Assert.That((await db.GarminBodyBatteryDays.SingleAsync()).Charged, Is.EqualTo(60));
        Assert.That(await db.GarminBodyBatteryValues.Select(x => x.BodyBatteryLevel).ToListAsync(), Is.EquivalentTo(new double?[] { 5, 7 }));

        var vo2 = await db.GarminVo2Max.SingleAsync();
        Assert.That((vo2.Kind, vo2.Sport, vo2.Vo2MaxPreciseValue), Is.EqualTo(("generic", (string?)"RUNNING", (double?)50.1)));

        var weight = await db.GarminWeights.SingleAsync();
        Assert.That((weight.TimestampGMT, weight.WeightGrams, weight.BoneMass), Is.EqualTo((1795248882000L, (double?)70839, (double?)4730)));

        var activity = await db.GarminActivities.SingleAsync();
        Assert.That((activity.ActivityId, activity.ActivityTypeKey, activity.HrTimeInZone3), Is.EqualTo((42L, (string?)"running", (double?)600)));
    }

    [Test]
    public async Task EndpointsWithoutTablesAreKeptRawOnly()
    {
        await StoreAsync(Record("stress", "2026-11-20", Day, """{"calendarDate":"2026-11-20","values":{"overallStressLevel":30}}"""));

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.RawHealthRecords.Select(x => x.Endpoint).SingleAsync(), Is.EqualTo("stress"));
    }

    private async Task StoreAsync(params RawRecord[] records)
    {
        await using var db = PostgresFixture.CreateDbContext();
        await TestStores.Create(db, TimeProvider.System)
            .StoreChunkAsync(userId, HealthDataSource.Garmin, new SyncChunk { Records = records.ToList() }, CancellationToken.None);
    }

    private static RawRecord Record(string endpoint, string key, DateOnly? date, string json) =>
        new() { Endpoint = endpoint, Key = key, Date = date, Json = json };

    private static string Item(string jsonArray, int index) =>
        System.Text.Json.JsonDocument.Parse(jsonArray).RootElement[index].GetRawText();
}
