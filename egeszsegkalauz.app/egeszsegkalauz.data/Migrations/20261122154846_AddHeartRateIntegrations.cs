using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace egeszsegkalauz.data.Migrations
{
    /// <inheritdoc />
    public partial class AddHeartRateIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyHeartRateSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RestingBpm = table.Column<int>(type: "integer", nullable: true),
                    MinBpm = table.Column<int>(type: "integer", nullable: true),
                    MaxBpm = table.Column<int>(type: "integer", nullable: true),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateSamples",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Bpm = table.Column<short>(type: "smallint", nullable: false)
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
                name: "UserIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "text", nullable: false),
                    ApiKeyHint = table.Column<string>(type: "text", nullable: false),
                    LastImportAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Nanoid = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserIntegrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_HeartRateSamples_UserId_Source_Date",
                table: "HeartRateSamples",
                columns: new[] { "UserId", "Source", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_UserIntegrations_Nanoid",
                table: "UserIntegrations",
                column: "Nanoid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserIntegrations_UserId_Source",
                table: "UserIntegrations",
                columns: new[] { "UserId", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyHeartRateSummaries");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "HeartRateSamples");

            migrationBuilder.DropTable(
                name: "UserIntegrations");
        }
    }
}
