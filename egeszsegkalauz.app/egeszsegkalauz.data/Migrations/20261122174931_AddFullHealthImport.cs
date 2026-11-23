using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egeszsegkalauz.data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullHealthImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastImportAt",
                table: "UserIntegrations",
                newName: "SummariesImportedAt");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstDataDate",
                table: "UserIntegrations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SummariesImportedThrough",
                table: "UserIntegrations",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BodyBatteryDailySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Charged = table.Column<int>(type: "integer", nullable: true),
                    Drained = table.Column<int>(type: "integer", nullable: true),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    Bmi = table.Column<double>(type: "double precision", nullable: true),
                    BodyFatPercent = table.Column<double>(type: "double precision", nullable: true),
                    BodyWaterPercent = table.Column<double>(type: "double precision", nullable: true),
                    BoneMassKg = table.Column<double>(type: "double precision", nullable: true),
                    MuscleMassKg = table.Column<double>(type: "double precision", nullable: true),
                    VisceralFat = table.Column<double>(type: "double precision", nullable: true),
                    MetabolicAge = table.Column<int>(type: "integer", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    Source = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    TotalKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    ActiveKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    BmrKilocalories = table.Column<double>(type: "double precision", nullable: true),
                    FloorsAscended = table.Column<double>(type: "double precision", nullable: true),
                    FloorsDescended = table.Column<double>(type: "double precision", nullable: true),
                    ModerateIntensityMinutes = table.Column<int>(type: "integer", nullable: true),
                    VigorousIntensityMinutes = table.Column<int>(type: "integer", nullable: true),
                    HighlyActiveSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActiveSeconds = table.Column<int>(type: "integer", nullable: true),
                    SedentarySeconds = table.Column<int>(type: "integer", nullable: true),
                    SleepingSeconds = table.Column<int>(type: "integer", nullable: true),
                    RestingHeartRate = table.Column<int>(type: "integer", nullable: true),
                    MinHeartRate = table.Column<int>(type: "integer", nullable: true),
                    MaxHeartRate = table.Column<int>(type: "integer", nullable: true),
                    AverageStressLevel = table.Column<int>(type: "integer", nullable: true),
                    MaxStressLevel = table.Column<int>(type: "integer", nullable: true),
                    LowStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    MediumStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    HighStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    RestStressSeconds = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryHighest = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryLowest = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryCharged = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryDrained = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryAtWake = table.Column<int>(type: "integer", nullable: true),
                    AverageSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    LowestSpo2 = table.Column<double>(type: "double precision", nullable: true),
                    AverageWakingRespiration = table.Column<double>(type: "double precision", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "ExerciseActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    SourceActivityId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: true),
                    MovingSeconds = table.Column<double>(type: "double precision", nullable: true),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    Calories = table.Column<double>(type: "double precision", nullable: true),
                    AverageHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    MaxHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    ElevationGain = table.Column<double>(type: "double precision", nullable: true),
                    ElevationLoss = table.Column<double>(type: "double precision", nullable: true),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: true),
                    MaxSpeed = table.Column<double>(type: "double precision", nullable: true),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    LocationName = table.Column<string>(type: "text", nullable: true),
                    HeartRateZone1Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone2Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone3Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone4Seconds = table.Column<double>(type: "double precision", nullable: true),
                    HeartRateZone5Seconds = table.Column<double>(type: "double precision", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalQueries = table.Column<int>(type: "integer", nullable: false),
                    CompletedQueries = table.Column<int>(type: "integer", nullable: false),
                    FailedQueries = table.Column<int>(type: "integer", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RawHealthRecords",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawHealthRecords", x => new { x.UserId, x.Source, x.Endpoint, x.Key });
                    table.ForeignKey(
                        name: "FK_RawHealthRecords_Users_UserId",
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
                    Source = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SleepSeconds = table.Column<int>(type: "integer", nullable: true),
                    NapSeconds = table.Column<int>(type: "integer", nullable: true),
                    DeepSeconds = table.Column<int>(type: "integer", nullable: true),
                    LightSeconds = table.Column<int>(type: "integer", nullable: true),
                    RemSeconds = table.Column<int>(type: "integer", nullable: true),
                    AwakeSeconds = table.Column<int>(type: "integer", nullable: true),
                    UnmeasurableSeconds = table.Column<int>(type: "integer", nullable: true),
                    AwakeCount = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    ScoreQualifier = table.Column<string>(type: "text", nullable: true),
                    AverageSleepStress = table.Column<double>(type: "double precision", nullable: true),
                    AverageRespiration = table.Column<double>(type: "double precision", nullable: true),
                    LowestRespiration = table.Column<double>(type: "double precision", nullable: true),
                    HighestRespiration = table.Column<double>(type: "double precision", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SleepSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SleepSummaries_Users_UserId",
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
                    Source = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Sport = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ImportQueueItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportQueueItems_ImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ImportJobs",
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
                name: "IX_ImportJobs_Nanoid",
                table: "ImportJobs",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_UserId_Source_Status",
                table: "ImportJobs",
                columns: new[] { "UserId", "Source", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportQueueItems_JobId_Status",
                table: "ImportQueueItems",
                columns: new[] { "JobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportQueueItems_Status_NotBefore",
                table: "ImportQueueItems",
                columns: new[] { "Status", "NotBefore" });

            migrationBuilder.CreateIndex(
                name: "IX_RawHealthRecords_UserId_Source_Endpoint_Date",
                table: "RawHealthRecords",
                columns: new[] { "UserId", "Source", "Endpoint", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SleepSummaries_Nanoid",
                table: "SleepSummaries",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SleepSummaries_UserId_Source_Date",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "ExerciseActivities");

            migrationBuilder.DropTable(
                name: "ImportQueueItems");

            migrationBuilder.DropTable(
                name: "RawHealthRecords");

            migrationBuilder.DropTable(
                name: "SleepSummaries");

            migrationBuilder.DropTable(
                name: "Vo2MaxMeasurements");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "FirstDataDate",
                table: "UserIntegrations");

            migrationBuilder.DropColumn(
                name: "SummariesImportedThrough",
                table: "UserIntegrations");

            migrationBuilder.RenameColumn(
                name: "SummariesImportedAt",
                table: "UserIntegrations",
                newName: "LastImportAt");
        }
    }
}
