using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egeszsegkalauz.data.Migrations
{
    /// <inheritdoc />
    public partial class SourceShapedBuffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyBatteryDailySummaries");

            migrationBuilder.DropTable(
                name: "BodyBatterySamples");

            migrationBuilder.DropTable(
                name: "BodyCompositionMeasurements");

            migrationBuilder.DropTable(
                name: "DailyActivitySummaries");

            migrationBuilder.DropTable(
                name: "DailyHeartRateSummaries");

            migrationBuilder.DropTable(
                name: "ExerciseActivities");

            migrationBuilder.DropTable(
                name: "HeartRateSamples");

            migrationBuilder.DropTable(
                name: "SleepSummaries");

            migrationBuilder.DropTable(
                name: "Vo2MaxMeasurements");

            migrationBuilder.EnsureSchema(
                name: "garmin");

            migrationBuilder.EnsureSchema(
                name: "hevy");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKeyHint",
                table: "UserIntegrations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessTokenExpiresAt",
                table: "UserIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "UserIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUserId",
                table: "UserIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "UserIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAttemptAt",
                table: "UserIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "UserIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedRefreshToken",
                table: "UserIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Activities",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    ActivityName = table.Column<string>(type: "text", nullable: true),
                    ActivityTypeKey = table.Column<string>(type: "text", nullable: true),
                    StartTimeLocal = table.Column<string>(type: "text", nullable: true),
                    StartTimeGMT = table.Column<string>(type: "text", nullable: true),
                    BeginTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    Duration = table.Column<double>(type: "double precision", nullable: true),
                    ElapsedDuration = table.Column<double>(type: "double precision", nullable: true),
                    MovingDuration = table.Column<double>(type: "double precision", nullable: true),
                    Distance = table.Column<double>(type: "double precision", nullable: true),
                    Calories = table.Column<double>(type: "double precision", nullable: true),
                    BmrCalories = table.Column<double>(type: "double precision", nullable: true),
                    AverageHR = table.Column<double>(type: "double precision", nullable: true),
                    MaxHR = table.Column<double>(type: "double precision", nullable: true),
                    ElevationGain = table.Column<double>(type: "double precision", nullable: true),
                    ElevationLoss = table.Column<double>(type: "double precision", nullable: true),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: true),
                    MaxSpeed = table.Column<double>(type: "double precision", nullable: true),
                    Steps = table.Column<double>(type: "double precision", nullable: true),
                    LapCount = table.Column<double>(type: "double precision", nullable: true),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: true),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationName = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    HrTimeInZone1 = table.Column<double>(type: "double precision", nullable: true),
                    HrTimeInZone2 = table.Column<double>(type: "double precision", nullable: true),
                    HrTimeInZone3 = table.Column<double>(type: "double precision", nullable: true),
                    HrTimeInZone4 = table.Column<double>(type: "double precision", nullable: true),
                    HrTimeInZone5 = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => new { x.UserId, x.ActivityId });
                    table.ForeignKey(
                        name: "FK_Activities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyBatteryDays",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Charged = table.Column<double>(type: "double precision", nullable: true),
                    Drained = table.Column<double>(type: "double precision", nullable: true),
                    StartTimestampGMT = table.Column<string>(type: "text", nullable: true),
                    EndTimestampGMT = table.Column<string>(type: "text", nullable: true),
                    StartTimestampLocal = table.Column<string>(type: "text", nullable: true),
                    EndTimestampLocal = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyBatteryDays", x => new { x.UserId, x.Date });
                    table.ForeignKey(
                        name: "FK_BodyBatteryDays_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyBatteryValues",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    BodyBatteryLevel = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyBatteryValues", x => new { x.UserId, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_BodyBatteryValues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyMeasurements",
                schema: "hevy",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    LeanMassKg = table.Column<double>(type: "double precision", nullable: true),
                    FatPercent = table.Column<double>(type: "double precision", nullable: true),
                    NeckCm = table.Column<double>(type: "double precision", nullable: true),
                    ShoulderCm = table.Column<double>(type: "double precision", nullable: true),
                    ChestCm = table.Column<double>(type: "double precision", nullable: true),
                    LeftBicepCm = table.Column<double>(type: "double precision", nullable: true),
                    RightBicepCm = table.Column<double>(type: "double precision", nullable: true),
                    LeftForearmCm = table.Column<double>(type: "double precision", nullable: true),
                    RightForearmCm = table.Column<double>(type: "double precision", nullable: true),
                    Abdomen = table.Column<double>(type: "double precision", nullable: true),
                    Waist = table.Column<double>(type: "double precision", nullable: true),
                    Hips = table.Column<double>(type: "double precision", nullable: true),
                    LeftThigh = table.Column<double>(type: "double precision", nullable: true),
                    RightThigh = table.Column<double>(type: "double precision", nullable: true),
                    LeftCalf = table.Column<double>(type: "double precision", nullable: true),
                    RightCalf = table.Column<double>(type: "double precision", nullable: true),
                    PictureUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMeasurements", x => new { x.UserId, x.Date });
                    table.ForeignKey(
                        name: "FK_BodyMeasurements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseTemplates",
                schema: "hevy",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    PrimaryMuscleGroup = table.Column<string>(type: "text", nullable: true),
                    SecondaryMuscleGroups = table.Column<List<string>>(type: "text[]", nullable: false),
                    Equipment = table.Column<string>(type: "text", nullable: true),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTemplates", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExerciseTemplates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateDays",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTimestampGMT = table.Column<string>(type: "text", nullable: true),
                    EndTimestampGMT = table.Column<string>(type: "text", nullable: true),
                    StartTimestampLocal = table.Column<string>(type: "text", nullable: true),
                    EndTimestampLocal = table.Column<string>(type: "text", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MinHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    RestingHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    LastSevenDaysAvgRestingHeartRate = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateDays", x => new { x.UserId, x.CalendarDate });
                    table.ForeignKey(
                        name: "FK_HeartRateDays_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateValues",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HeartRate = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateValues", x => new { x.UserId, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_HeartRateValues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SleepSummaries",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepId = table.Column<long>(type: "bigint", nullable: true),
                    SleepTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    NapTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    SleepWindowConfirmed = table.Column<bool>(type: "boolean", nullable: true),
                    SleepStartTimestampGMT = table.Column<long>(type: "bigint", nullable: true),
                    SleepEndTimestampGMT = table.Column<long>(type: "bigint", nullable: true),
                    SleepStartTimestampLocal = table.Column<long>(type: "bigint", nullable: true),
                    SleepEndTimestampLocal = table.Column<long>(type: "bigint", nullable: true),
                    UnmeasurableSleepSeconds = table.Column<double>(type: "double precision", nullable: true),
                    DeepSleepSeconds = table.Column<double>(type: "double precision", nullable: true),
                    LightSleepSeconds = table.Column<double>(type: "double precision", nullable: true),
                    RemSleepSeconds = table.Column<double>(type: "double precision", nullable: true),
                    AwakeSleepSeconds = table.Column<double>(type: "double precision", nullable: true),
                    DeviceRemCapable = table.Column<bool>(type: "boolean", nullable: true),
                    AverageRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    LowestRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    HighestRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    AwakeCount = table.Column<double>(type: "double precision", nullable: true),
                    AvgSleepStress = table.Column<double>(type: "double precision", nullable: true),
                    SleepScoreFeedback = table.Column<string>(type: "text", nullable: true),
                    SleepScoreInsight = table.Column<string>(type: "text", nullable: true),
                    SleepScoresOverallValue = table.Column<double>(type: "double precision", nullable: true),
                    SleepScoresOverallQualifierKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SleepSummaries", x => new { x.UserId, x.CalendarDate });
                    table.ForeignKey(
                        name: "FK_SleepSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSummaries",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    ActiveKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    BmrKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    TotalSteps = table.Column<double>(type: "double precision", nullable: true),
                    DailyStepGoal = table.Column<double>(type: "double precision", nullable: true),
                    TotalDistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    WellnessDistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    WellnessStartTimeGmt = table.Column<string>(type: "text", nullable: true),
                    WellnessEndTimeGmt = table.Column<string>(type: "text", nullable: true),
                    HighlyActiveSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ActiveSeconds = table.Column<double>(type: "double precision", nullable: true),
                    SedentarySeconds = table.Column<double>(type: "double precision", nullable: true),
                    SleepingSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ModerateIntensityMinutes = table.Column<double>(type: "double precision", nullable: true),
                    VigorousIntensityMinutes = table.Column<double>(type: "double precision", nullable: true),
                    IntensityMinutesGoal = table.Column<double>(type: "double precision", nullable: true),
                    FloorsAscended = table.Column<double>(type: "double precision", nullable: true),
                    FloorsDescended = table.Column<double>(type: "double precision", nullable: true),
                    MinHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    RestingHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    LastSevenDaysAvgRestingHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MinAvgHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MaxAvgHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    AverageStressLevel = table.Column<double>(type: "double precision", nullable: true),
                    MaxStressLevel = table.Column<double>(type: "double precision", nullable: true),
                    StressDuration = table.Column<double>(type: "double precision", nullable: true),
                    RestStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    ActivityStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    UncategorizedStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    TotalStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    LowStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    MediumStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    HighStressDuration = table.Column<double>(type: "double precision", nullable: true),
                    StressQualifier = table.Column<string>(type: "text", nullable: true),
                    MeasurableAwakeDuration = table.Column<double>(type: "double precision", nullable: true),
                    MeasurableAsleepDuration = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryChargedValue = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryDrainedValue = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryHighestValue = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryLowestValue = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryMostRecentValue = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryDuringSleep = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryAtWakeTime = table.Column<double>(type: "double precision", nullable: true),
                    AverageSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    LowestSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    LatestSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    AvgWakingRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    HighestRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    LowestRespirationValue = table.Column<double>(type: "double precision", nullable: true),
                    LatestRespirationValue = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSummaries", x => new { x.UserId, x.CalendarDate });
                    table.ForeignKey(
                        name: "FK_UserSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vo2Max",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Sport = table.Column<string>(type: "text", nullable: true),
                    SubSport = table.Column<string>(type: "text", nullable: true),
                    Vo2MaxPreciseValue = table.Column<double>(type: "double precision", nullable: true),
                    Vo2MaxValue = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vo2Max", x => new { x.UserId, x.CalendarDate, x.Kind });
                    table.ForeignKey(
                        name: "FK_Vo2Max_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Weights",
                schema: "garmin",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TimestampGMT = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<long>(type: "bigint", nullable: true),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WeightGrams = table.Column<double>(type: "double precision", nullable: true),
                    Bmi = table.Column<double>(type: "double precision", nullable: true),
                    BodyFat = table.Column<double>(type: "double precision", nullable: true),
                    BodyWater = table.Column<double>(type: "double precision", nullable: true),
                    BoneMass = table.Column<double>(type: "double precision", nullable: true),
                    MuscleMass = table.Column<double>(type: "double precision", nullable: true),
                    PhysiqueRating = table.Column<double>(type: "double precision", nullable: true),
                    VisceralFat = table.Column<double>(type: "double precision", nullable: true),
                    MetabolicAge = table.Column<double>(type: "double precision", nullable: true),
                    WeightDelta = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weights", x => new { x.UserId, x.TimestampGMT });
                    table.ForeignKey(
                        name: "FK_Weights_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                schema: "hevy",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    RoutineId = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_Workouts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercises",
                schema: "hevy",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    WorkoutId = table.Column<string>(type: "text", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ExerciseTemplateId = table.Column<string>(type: "text", nullable: true),
                    SupersetId = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercises", x => new { x.UserId, x.WorkoutId, x.Index });
                    table.ForeignKey(
                        name: "FK_WorkoutExercises_Workouts_UserId_WorkoutId",
                        columns: x => new { x.UserId, x.WorkoutId },
                        principalSchema: "hevy",
                        principalTable: "Workouts",
                        principalColumns: new[] { "UserId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                schema: "hevy",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    WorkoutId = table.Column<string>(type: "text", nullable: false),
                    ExerciseIndex = table.Column<int>(type: "integer", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    Reps = table.Column<double>(type: "double precision", nullable: true),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Rpe = table.Column<double>(type: "double precision", nullable: true),
                    CustomMetric = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => new { x.UserId, x.WorkoutId, x.ExerciseIndex, x.Index });
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutExercises_UserId_WorkoutId_ExerciseIndex",
                        columns: x => new { x.UserId, x.WorkoutId, x.ExerciseIndex },
                        principalSchema: "hevy",
                        principalTable: "WorkoutExercises",
                        principalColumns: new[] { "UserId", "WorkoutId", "Index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserIntegrations_Source_ExternalUserId",
                table: "UserIntegrations",
                columns: new[] { "Source", "ExternalUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_UserId_BeginTimestamp",
                schema: "garmin",
                table: "Activities",
                columns: new[] { "UserId", "BeginTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BodyBatteryValues_UserId_Date",
                schema: "garmin",
                table: "BodyBatteryValues",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_HeartRateValues_UserId_CalendarDate",
                schema: "garmin",
                table: "HeartRateValues",
                columns: new[] { "UserId", "CalendarDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_UserId_StartTime",
                schema: "hevy",
                table: "Workouts",
                columns: new[] { "UserId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "BodyBatteryDays",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "BodyBatteryValues",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "BodyMeasurements",
                schema: "hevy");

            migrationBuilder.DropTable(
                name: "ExerciseTemplates",
                schema: "hevy");

            migrationBuilder.DropTable(
                name: "HeartRateDays",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "HeartRateValues",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "SleepSummaries",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "UserSummaries",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "Vo2Max",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "Weights",
                schema: "garmin");

            migrationBuilder.DropTable(
                name: "WorkoutSets",
                schema: "hevy");

            migrationBuilder.DropTable(
                name: "WorkoutExercises",
                schema: "hevy");

            migrationBuilder.DropTable(
                name: "Workouts",
                schema: "hevy");

            migrationBuilder.DropIndex(
                name: "IX_UserIntegrations_Source_ExternalUserId",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "AccessTokenExpiresAt",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "ExternalUserId",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "LastSyncAttemptAt",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "ProtectedRefreshToken",
                table: "UserIntegrations");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKeyHint",
                table: "UserIntegrations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BodyBatteryDailySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Charged = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Drained = table.Column<int>(type: "integer", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyBatteryDailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyBatteryDailySummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyBatterySamples",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Level = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyBatterySamples", x => new { x.UserId, x.Source, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_BodyBatterySamples_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyCompositionMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Bmi = table.Column<double>(type: "double precision", nullable: true),
                    BodyFatPercent = table.Column<double>(type: "double precision", nullable: true),
                    BodyWaterPercent = table.Column<double>(type: "double precision", nullable: true),
                    BoneMassKg = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MetabolicAge = table.Column<int>(type: "integer", nullable: true),
                    MuscleMassKg = table.Column<double>(type: "double precision", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VisceralFat = table.Column<double>(type: "double precision", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyCompositionMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyCompositionMeasurements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyActivitySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ActiveKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    ActiveSeconds = table.Column<int>(type: "integer", nullable: true),
                    AverageSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    AverageStressLevel = table.Column<int>(type: "integer", nullable: true),
                    AverageWakingRespiration = table.Column<double>(type: "double precision", nullable: true),
                    BmrKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    BodyBatteryAtWake = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryCharged = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryDrained = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryHighest = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryLowest = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    FloorsAscended = table.Column<double>(type: "double precision", nullable: true),
                    FloorsDescended = table.Column<double>(type: "double precision", nullable: true),
                    HighStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    HighlyActiveSeconds = table.Column<int>(type: "integer", nullable: true),
                    LowStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    LowestSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    MaxHeartRate = table.Column<int>(type: "integer", nullable: true),
                    MaxStressLevel = table.Column<int>(type: "integer", nullable: true),
                    MediumStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    MinHeartRate = table.Column<int>(type: "integer", nullable: true),
                    ModerateIntensityMinutes = table.Column<int>(type: "integer", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    RestStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    RestingHeartRate = table.Column<int>(type: "integer", nullable: true),
                    SedentarySeconds = table.Column<int>(type: "integer", nullable: true),
                    SleepingSeconds = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    TotalKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VigorousIntensityMinutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyActivitySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyActivitySummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyHeartRateSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxBpm = table.Column<int>(type: "integer", nullable: true),
                    MinBpm = table.Column<int>(type: "integer", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    RestingBpm = table.Column<int>(type: "integer", nullable: true),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyHeartRateSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyHeartRateSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AverageHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: true),
                    Calories = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    ElevationGain = table.Column<double>(type: "double precision", nullable: true),
                    ElevationLoss = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone1Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone2Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone3Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone4Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone5Seconds = table.Column<double>(type: "double precision", nullable: true),
                    LocationName = table.Column<string>(type: "text", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MaxSpeed = table.Column<double>(type: "double precision", nullable: true),
                    MovingSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    SourceActivityId = table.Column<string>(type: "text", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateSamples",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Bpm = table.Column<short>(type: "smallint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateSamples", x => new { x.UserId, x.Source, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_HeartRateSamples_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SleepSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AverageRespiration = table.Column<double>(type: "double precision", nullable: true),
                    AverageSleepStress = table.Column<double>(type: "double precision", nullable: true),
                    AwakeCount = table.Column<int>(type: "integer", nullable: true),
                    AwakeSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DeepSeconds = table.Column<int>(type: "integer", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HighestRespiration = table.Column<double>(type: "double precision", nullable: true),
                    LightSeconds = table.Column<int>(type: "integer", nullable: true),
                    LowestRespiration = table.Column<double>(type: "double precision", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    NapSeconds = table.Column<int>(type: "integer", nullable: true),
                    RemSeconds = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    ScoreQualifier = table.Column<string>(type: "text", nullable: true),
                    SleepSeconds = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnmeasurableSeconds = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SleepSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SleepSummariesUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vo2MaxMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Sport = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vo2MaxMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vo2MaxMeasurements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyBatteryDailySummaries_Nanoid",
                table: "BodyBatteryDailySummaries",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodyBatteryDailySummaries_UserId_Source_Date",
                table: "BodyBatteryDailySummaries",
                columns: new[] { "UserId", "Source", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodyBatterySamples_UserId_Source_Date",
                table: "BodyBatterySamples",
                columns: new[] { "UserId", "Source", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_BodyCompositionMeasurements_Nanoid",
                table: "BodyCompositionMeasurements",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodyCompositionMeasurements_UserId_Source_Timestamp",
                table: "BodyCompositionMeasurements",
                columns: new[] { "UserId", "Source", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivitySummaries_Nanoid",
                table: "DailyActivitySummaries",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyActivitySummaries_UserId_Source_Date",
                table: "DailyActivitySummaries",
                columns: new[] { "UserId", "Source", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyHeartRateSummaries_Nanoid",
                table: "DailyHeartRateSummaries",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyHeartRateSummaries_UserId_Source_Date",
                table: "DailyHeartRateSummaries",
                columns: new[] { "UserId", "Source", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseActivities_Nanoid",
                table: "ExerciseActivities",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseActivities_UserId_Source_SourceActivityId",
                table: "ExerciseActivities",
                columns: new[] { "UserId", "Source", "SourceActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseActivities_UserId_StartUtc",
                table: "ExerciseActivities",
                columns: new[] { "UserId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HeartRateSamples_UserId_Source_Date",
                table: "HeartRateSamples",
                columns: new[] { "UserId", "Source", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SleepSummariesNanoid",
                table: "SleepSummaries",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SleepSummariesUserId_Source_Date",
                table: "SleepSummaries",
                columns: new[] { "UserId", "Source", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vo2MaxMeasurements_Nanoid",
                table: "Vo2MaxMeasurements",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vo2MaxMeasurements_UserId_Source_Date_Sport",
                table: "Vo2MaxMeasurements",
                columns: new[] { "UserId", "Source", "Date", "Sport" },
                unique: true);
        }
    }
}
