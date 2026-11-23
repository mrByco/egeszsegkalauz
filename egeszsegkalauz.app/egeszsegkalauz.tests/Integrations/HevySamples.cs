namespace egeszsegkalauz.tests.Integrations;

// Shaped like real Hevy payloads, with made-up values.
public static class HevySamples
{
    public static string Workout(string id, int exercises = 1) => $$"""
        {"id":"{{id}}","title":"Afternoon workout","routine_id":null,"start_time":"2026-11-18T11:42:00+01:00",
         "end_time":"2026-11-18T12:23:00+01:00","updated_at":"2026-11-18T12:30:00Z","created_at":"2026-11-18T12:30:00Z",
         "exercises":[{{string.Join(",", Enumerable.Range(0, exercises).Select(i => $$"""
            {"index":{{i}},"title":"Bench Press (Barbell)","exercise_template_id":"T1","superset_id":null,
             "sets":[{"index":0,"type":"warmup","weight_kg":40,"reps":10},{"index":1,"type":"normal","weight_kg":80,"reps":5,"rpe":8.5}]}
            """))}}]}
        """;

    public const string Template = """
        {"id":"T1","title":"Bench Press (Barbell)","type":"weight_reps","primary_muscle_group":"chest",
         "secondary_muscle_groups":["triceps","shoulders"],"equipment":"barbell","is_custom":false}
        """;

    public const string BodyMeasurement = """
        {"date":"2026-11-20","weight_kg":71.2,"fat_percent":17.5,"waist":80,"left_calf":38}
        """;
}
