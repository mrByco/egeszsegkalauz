using System.Text.Json;
using System.Text.Json.Serialization;

namespace egeszsegkalauz.integrations.Hevy;

// Hevy's payloads as Hevy names them.

public static class HevyJson
{
    public static T? Read<T>(string json) => JsonSerializer.Deserialize<T>(json);
}

internal class HevyTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public double? ExpiresIn { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

internal class HevyPage
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    [JsonPropertyName("workouts")]
    public List<JsonElement>? Workouts { get; set; }

    [JsonPropertyName("events")]
    public List<JsonElement>? Events { get; set; }

    [JsonPropertyName("exercise_templates")]
    public List<JsonElement>? ExerciseTemplates { get; set; }

    [JsonPropertyName("body_measurements")]
    public List<JsonElement>? BodyMeasurements { get; set; }
}

internal class HevyEvent
{
    // "updated" carries the full workout, "deleted" only its id.
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("workout")]
    public JsonElement? Workout { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class HevyWorkout
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("routine_id")]
    public string? RoutineId { get; set; }

    [JsonPropertyName("start_time")]
    public DateTimeOffset? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTimeOffset? EndTime { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("exercises")]
    public List<HevyExercise>? Exercises { get; set; }
}

public class HevyExercise
{
    [JsonPropertyName("index")]
    public double? Index { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("exercise_template_id")]
    public string? ExerciseTemplateId { get; set; }

    [JsonPropertyName("superset_id")]
    public double? SupersetId { get; set; }

    [JsonPropertyName("sets")]
    public List<HevySet>? Sets { get; set; }
}

public class HevySet
{
    [JsonPropertyName("index")]
    public double? Index { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("weight_kg")]
    public double? WeightKg { get; set; }

    [JsonPropertyName("reps")]
    public double? Reps { get; set; }

    [JsonPropertyName("distance_meters")]
    public double? DistanceMeters { get; set; }

    [JsonPropertyName("duration_seconds")]
    public double? DurationSeconds { get; set; }

    [JsonPropertyName("rpe")]
    public double? Rpe { get; set; }

    [JsonPropertyName("custom_metric")]
    public double? CustomMetric { get; set; }
}

public class HevyExerciseTemplate
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("primary_muscle_group")]
    public string? PrimaryMuscleGroup { get; set; }

    [JsonPropertyName("secondary_muscle_groups")]
    public List<string>? SecondaryMuscleGroups { get; set; }

    [JsonPropertyName("equipment")]
    public string? Equipment { get; set; }

    [JsonPropertyName("is_custom")]
    public bool? IsCustom { get; set; }
}

public class HevyBodyMeasurement
{
    [JsonPropertyName("date")]
    public DateOnly? Date { get; set; }

    [JsonPropertyName("weight_kg")]
    public double? WeightKg { get; set; }

    [JsonPropertyName("lean_mass_kg")]
    public double? LeanMassKg { get; set; }

    [JsonPropertyName("fat_percent")]
    public double? FatPercent { get; set; }

    [JsonPropertyName("neck_cm")]
    public double? NeckCm { get; set; }

    [JsonPropertyName("shoulder_cm")]
    public double? ShoulderCm { get; set; }

    [JsonPropertyName("chest_cm")]
    public double? ChestCm { get; set; }

    [JsonPropertyName("left_bicep_cm")]
    public double? LeftBicepCm { get; set; }

    [JsonPropertyName("right_bicep_cm")]
    public double? RightBicepCm { get; set; }

    [JsonPropertyName("left_forearm_cm")]
    public double? LeftForearmCm { get; set; }

    [JsonPropertyName("right_forearm_cm")]
    public double? RightForearmCm { get; set; }

    [JsonPropertyName("abdomen")]
    public double? Abdomen { get; set; }

    [JsonPropertyName("waist")]
    public double? Waist { get; set; }

    [JsonPropertyName("hips")]
    public double? Hips { get; set; }

    [JsonPropertyName("left_thigh")]
    public double? LeftThigh { get; set; }

    [JsonPropertyName("right_thigh")]
    public double? RightThigh { get; set; }

    [JsonPropertyName("left_calf")]
    public double? LeftCalf { get; set; }

    [JsonPropertyName("right_calf")]
    public double? RightCalf { get; set; }

    [JsonPropertyName("picture_url")]
    public string? PictureUrl { get; set; }
}

internal class HevyWebhook
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("workoutId")]
    public string? WorkoutId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}
