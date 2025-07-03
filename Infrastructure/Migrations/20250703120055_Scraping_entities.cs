using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Scraping_entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    League = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Team1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Team2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ExternalLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Odds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketType = table.Column<int>(type: "int", nullable: false),
                    Team1Odd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DrawOdd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Team2Odd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Odds_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueBets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketType = table.Column<int>(type: "int", nullable: false),
                    OutcomeType = table.Column<int>(type: "int", nullable: false),
                    BetbyOdd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PinnacleOdd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ImpliedProbability = table.Column<decimal>(type: "decimal(8,6)", nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "decimal(8,6)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValueBets_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDateTime",
                table: "Events",
                column: "EventDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Source",
                table: "Events",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Teams_DateTime",
                table: "Events",
                columns: new[] { "Team1", "Team2", "EventDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Odds_Event_Market_Source",
                table: "Odds",
                columns: new[] { "EventId", "MarketType", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_Odds_EventId",
                table: "Odds",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueBets_CalculatedAt",
                table: "ValueBets",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ValueBets_EventId",
                table: "ValueBets",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueBets_ExpectedValue",
                table: "ValueBets",
                column: "ExpectedValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Odds");

            migrationBuilder.DropTable(
                name: "ValueBets");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
