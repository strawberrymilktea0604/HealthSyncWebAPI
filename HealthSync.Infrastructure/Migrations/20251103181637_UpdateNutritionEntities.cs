using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNutritionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodEntries_FoodItems_FoodItemId",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "CaloriesKcal",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CarbsGrams",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "FatGrams",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ProteinGrams",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CaloriesKcal",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "CarbsGrams",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "FatGrams",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "ProteinGrams",
                table: "FoodEntries");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCalories",
                table: "NutritionLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "NutritionLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "NutritionLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "NutritionLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCarbsG",
                table: "NutritionLogs",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFatG",
                table: "NutritionLogs",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProteinG",
                table: "NutritionLogs",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ServingUnit",
                table: "FoodItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ServingSize",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FoodItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesPerServing",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsG",
                table: "FoodItems",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "FoodItems",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FoodItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAdminId",
                table: "FoodItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FoodItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatG",
                table: "FoodItems",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FiberG",
                table: "FoodItems",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "FoodItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinG",
                table: "FoodItems",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SugarG",
                table: "FoodItems",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FoodItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "FoodEntries",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "MealType",
                table: "FoodEntries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "Calories",
                table: "FoodEntries",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsG",
                table: "FoodEntries",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumedAt",
                table: "FoodEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatG",
                table: "FoodEntries",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FoodEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinG",
                table: "FoodEntries",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogs_ApplicationUserId",
                table: "NutritionLogs",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogs_LogDate",
                table: "NutritionLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogs_UserId_LogDate",
                table: "NutritionLogs",
                columns: new[] { "UserId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Category",
                table: "FoodItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_CreatedByAdminId",
                table: "FoodItems",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Name",
                table: "FoodItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_NutritionLogId_MealType",
                table: "FoodEntries",
                columns: new[] { "NutritionLogId", "MealType" });

            migrationBuilder.AddForeignKey(
                name: "FK_FoodEntries_FoodItems_FoodItemId",
                table: "FoodEntries",
                column: "FoodItemId",
                principalTable: "FoodItems",
                principalColumn: "FoodItemId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodItems_AspNetUsers_CreatedByAdminId",
                table: "FoodItems",
                column: "CreatedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NutritionLogs_AspNetUsers_ApplicationUserId",
                table: "NutritionLogs",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodEntries_FoodItems_FoodItemId",
                table: "FoodEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodItems_AspNetUsers_CreatedByAdminId",
                table: "FoodItems");

            migrationBuilder.DropForeignKey(
                name: "FK_NutritionLogs_AspNetUsers_ApplicationUserId",
                table: "NutritionLogs");

            migrationBuilder.DropIndex(
                name: "IX_NutritionLogs_ApplicationUserId",
                table: "NutritionLogs");

            migrationBuilder.DropIndex(
                name: "IX_NutritionLogs_LogDate",
                table: "NutritionLogs");

            migrationBuilder.DropIndex(
                name: "IX_NutritionLogs_UserId_LogDate",
                table: "NutritionLogs");

            migrationBuilder.DropIndex(
                name: "IX_FoodItems_Category",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_FoodItems_CreatedByAdminId",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_FoodItems_Name",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_FoodEntries_NutritionLogId_MealType",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "TotalCarbsG",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "TotalFatG",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "TotalProteinG",
                table: "NutritionLogs");

            migrationBuilder.DropColumn(
                name: "CaloriesPerServing",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CarbsG",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "FatG",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "FiberG",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ProteinG",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SugarG",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Calories",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "CarbsG",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "ConsumedAt",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "FatG",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "ProteinG",
                table: "FoodEntries");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCalories",
                table: "NutritionLogs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,2)",
                oldPrecision: 8,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ServingUnit",
                table: "FoodItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "ServingSize",
                table: "FoodItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,2)",
                oldPrecision: 8,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FoodItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesKcal",
                table: "FoodItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsGrams",
                table: "FoodItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGrams",
                table: "FoodItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGrams",
                table: "FoodItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "FoodEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "MealType",
                table: "FoodEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesKcal",
                table: "FoodEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbsGrams",
                table: "FoodEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FatGrams",
                table: "FoodEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinGrams",
                table: "FoodEntries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodEntries_FoodItems_FoodItemId",
                table: "FoodEntries",
                column: "FoodItemId",
                principalTable: "FoodItems",
                principalColumn: "FoodItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
