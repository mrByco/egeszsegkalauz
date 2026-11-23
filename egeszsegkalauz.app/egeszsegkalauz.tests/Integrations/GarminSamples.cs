namespace egeszsegkalauz.tests.Integrations;

// Shaped like real Garmin payloads, with made-up values.
public static class GarminSamples
{
    public static string HeartRateDay(DateOnly date) => $$"""
        {"calendarDate":"{{date:yyyy-MM-dd}}","startTimestampGMT":"{{date.AddDays(-1):yyyy-MM-dd}}T23:00:00.0",
         "maxHeartRate":131,"minHeartRate":48,"restingHeartRate":52,"lastSevenDaysAvgRestingHeartRate":51,
         "heartRateValues":[[{{Ms(date, 0)}},60],[{{Ms(date, 2)}},null],[{{Ms(date, 4)}},64]]}
        """;

    public static string UserSummary(DateOnly date) => $$"""
        {"calendarDate":"{{date:yyyy-MM-dd}}","totalSteps":8123.0,"vigorousIntensityMinutes":12,"stressQualifier":"BALANCED",
         "bodyBatteryHighestValue":91,"averageSpo2":null,"someNewField":"kept in raw only"}
        """;

    public const string SleepSummaries = """
        [{"id":1795125600000,"calendarDate":"2026-11-19","sleepTimeSeconds":27000,"deepSleepSeconds":5400.0,
          "sleepStartTimestampGMT":1795125600000,"sleepScores":{"overall":{"value":81,"qualifierKey":"GOOD"}}},
         {"calendarDate":"2026-11-20","sleepTimeSeconds":21000}]
        """;

    public const string BodyBattery = """
        [{"date":"2026-11-20","charged":60,"drained":55,"bodyBatteryValuesArray":[[1795129200000,5],[1795129200000,5],[1795129740000,7]]}]
        """;

    public const string Vo2Max = """
        [{"generic":{"calendarDate":"2026-11-20","vo2MaxPreciseValue":50.1,"vo2MaxValue":50.0,"sport":"RUNNING"},"cycling":null}]
        """;

    public const string Weight = """
        [{"date":1795252482000,"calendarDate":"2026-11-21","timestampGMT":1795248882000,"weight":70839.0,"boneMass":4730,"bodyFat":18.1}]
        """;

    public const string Activity = """
        {"activityId":42,"activityName":"Futás","activityType":{"typeKey":"running"},"beginTimestamp":1795212000000,
         "duration":1800.5,"averageHR":150.0,"hrTimeInZone_3":600.0}
        """;

    private static long Ms(DateOnly date, int minutes) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddMinutes(minutes).ToUnixTimeMilliseconds();
}
