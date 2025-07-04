using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBets",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Active_Source",
                table: "Events",
                columns: new[] { "IsActive", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_HasBets",
                table: "Events",
                column: "HasBets");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive",
                table: "Events",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Active_Source",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_HasBets",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "HasBets",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Events");
        }
    }
}
