using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGoalAndProgressRecordEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "ProgressRecords",
                newName: "WeightKg");

            migrationBuilder.RenameColumn(
                name: "RecordValue",
                table: "ProgressRecords",
                newName: "RecordedValue");

            migrationBuilder.AddColumn<decimal>(
                name: "ChestCm",
                table: "ProgressRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProgressRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "HipCm",
                table: "ProgressRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ProgressRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Goals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Goals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChestCm",
                table: "ProgressRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProgressRecords");

            migrationBuilder.DropColumn(
                name: "HipCm",
                table: "ProgressRecords");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ProgressRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Goals");

            migrationBuilder.RenameColumn(
                name: "WeightKg",
                table: "ProgressRecords",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "RecordedValue",
                table: "ProgressRecords",
                newName: "RecordValue");
        }
    }
}
