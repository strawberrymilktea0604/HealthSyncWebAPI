using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChallengeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_UserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_Challenges_ChallengeId",
                table: "ChallengeParticipations");

            migrationBuilder.DropTable(
                name: "UserChallengeSubmissions");

            migrationBuilder.DropTable(
                name: "CommunityChallenges");

            migrationBuilder.RenameColumn(
                name: "ReviewedBy",
                table: "ChallengeParticipations",
                newName: "ReviewedByAdminId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Challenges",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Challenges",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Challenges",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ChallengeType",
                table: "Challenges",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Challenges",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAdminId",
                table: "Challenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Criteria",
                table: "Challenges",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Challenges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "Challenges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RewardDescription",
                table: "Challenges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Challenges",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "SubmissionUrl",
                table: "ChallengeParticipations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChallengeParticipations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "ChallengeParticipations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ChallengeParticipations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChallengeParticipations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "ChallengeParticipations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionText",
                table: "ChallengeParticipations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ChallengeParticipations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CreatedByAdminId",
                table: "Challenges",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_EndDate",
                table: "Challenges",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_StartDate",
                table: "Challenges",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_Status",
                table: "Challenges",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipations_ApplicationUserId",
                table: "ChallengeParticipations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipations_ChallengeId_UserId",
                table: "ChallengeParticipations",
                columns: new[] { "ChallengeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipations_ReviewedByAdminId",
                table: "ChallengeParticipations",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeParticipations_Status",
                table: "ChallengeParticipations",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_ApplicationUserId",
                table: "ChallengeParticipations",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_ReviewedByAdminId",
                table: "ChallengeParticipations",
                column: "ReviewedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_UserId",
                table: "ChallengeParticipations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_Challenges_ChallengeId",
                table: "ChallengeParticipations",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "ChallengeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_AspNetUsers_CreatedByAdminId",
                table: "Challenges",
                column: "CreatedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_ApplicationUserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_ReviewedByAdminId",
                table: "ChallengeParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_UserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeParticipations_Challenges_ChallengeId",
                table: "ChallengeParticipations");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_AspNetUsers_CreatedByAdminId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_CreatedByAdminId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_EndDate",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_StartDate",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_Status",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeParticipations_ApplicationUserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeParticipations_ChallengeId_UserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeParticipations_ReviewedByAdminId",
                table: "ChallengeParticipations");

            migrationBuilder.DropIndex(
                name: "IX_ChallengeParticipations_Status",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "ChallengeType",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "Criteria",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "RewardDescription",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "SubmissionText",
                table: "ChallengeParticipations");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ChallengeParticipations");

            migrationBuilder.RenameColumn(
                name: "ReviewedByAdminId",
                table: "ChallengeParticipations",
                newName: "ReviewedBy");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "SubmissionUrl",
                table: "ChallengeParticipations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChallengeParticipations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "CommunityChallenges",
                columns: table => new
                {
                    CommunityChallengeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChallengeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false),
                    RewardDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RewardPoints = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Rules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetMetric = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityChallenges", x => x.CommunityChallengeId);
                    table.ForeignKey(
                        name: "FK_CommunityChallenges_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserChallengeSubmissions",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunityChallengeId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentProgress = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EarnedPoints = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EvidenceImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceVideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RankPosition = table.Column<int>(type: "int", nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetProgress = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChallengeSubmissions", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_UserChallengeSubmissions_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserChallengeSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChallengeSubmissions_CommunityChallenges_CommunityChallengeId",
                        column: x => x.CommunityChallengeId,
                        principalTable: "CommunityChallenges",
                        principalColumn: "CommunityChallengeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityChallenges_CreatedByUserId",
                table: "CommunityChallenges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeSubmissions_CommunityChallengeId",
                table: "UserChallengeSubmissions",
                column: "CommunityChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeSubmissions_ReviewedByUserId",
                table: "UserChallengeSubmissions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallengeSubmissions_UserId",
                table: "UserChallengeSubmissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_AspNetUsers_UserId",
                table: "ChallengeParticipations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeParticipations_Challenges_ChallengeId",
                table: "ChallengeParticipations",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "ChallengeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
