using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWorkoutEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSessions_Exercises_ExerciseId",
                table: "ExerciseSessions");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "WorkoutLogs",
                newName: "TotalDurationMinutes");

            migrationBuilder.RenameColumn(
                name: "Difficulty",
                table: "Exercises",
                newName: "DifficultyLevel");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "WorkoutLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "WorkoutLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WorkoutLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCaloriesBurned",
                table: "WorkoutLogs",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightKg",
                table: "ExerciseSessions",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "ExerciseSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ExerciseSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ExerciseSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Exercises",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MuscleGroup",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Exercises",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CaloriesPerMinute",
                table: "Exercises",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAdminId",
                table: "Exercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Exercises",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_ApplicationUserId",
                table: "WorkoutLogs",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserId_WorkoutDate",
                table: "WorkoutLogs",
                columns: new[] { "UserId", "WorkoutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_WorkoutDate",
                table: "WorkoutLogs",
                column: "WorkoutDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSessions_WorkoutLogId_OrderIndex",
                table: "ExerciseSessions",
                columns: new[] { "WorkoutLogId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CreatedByAdminId",
                table: "Exercises",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_MuscleGroup",
                table: "Exercises",
                column: "MuscleGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Name",
                table: "Exercises",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_AspNetUsers_CreatedByAdminId",
                table: "Exercises",
                column: "CreatedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSessions_Exercises_ExerciseId",
                table: "ExerciseSessions",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_AspNetUsers_ApplicationUserId",
                table: "WorkoutLogs",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_CreatedByAdminId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSessions_Exercises_ExerciseId",
                table: "ExerciseSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_AspNetUsers_ApplicationUserId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_ApplicationUserId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_UserId_WorkoutDate",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_WorkoutDate",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSessions_WorkoutLogId_OrderIndex",
                table: "ExerciseSessions");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_CreatedByAdminId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_MuscleGroup",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_Name",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "EstimatedCaloriesBurned",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "ExerciseSessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ExerciseSessions");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ExerciseSessions");

            migrationBuilder.DropColumn(
                name: "CaloriesPerMinute",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Exercises");

            migrationBuilder.RenameColumn(
                name: "TotalDurationMinutes",
                table: "WorkoutLogs",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "DifficultyLevel",
                table: "Exercises",
                newName: "Difficulty");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightKg",
                table: "ExerciseSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "MuscleGroup",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSessions_Exercises_ExerciseId",
                table: "ExerciseSessions",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
