using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplayerGameplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Players_PlayerId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_PlayerId",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Sessions",
                newName: "StartTimeUtc");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "Sessions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "Sessions",
                newName: "DurationSeconds");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Sessions",
                newName: "EndTimeUtc");

            migrationBuilder.RenameColumn(
                name: "Duration",
                table: "Sessions",
                newName: "CurrentRound");

            migrationBuilder.AddColumn<int>(
                name: "CurrentNumber",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Players",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SessionAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAnswers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionAnswers_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionParticipants_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionParticipants_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswers_PlayerId",
                table: "SessionAnswers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAnswers_SessionId_PlayerId_Round",
                table: "SessionAnswers",
                columns: new[] { "SessionId", "PlayerId", "Round" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_PlayerId",
                table: "SessionParticipants",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_SessionId_PlayerId",
                table: "SessionParticipants",
                columns: new[] { "SessionId", "PlayerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionAnswers");

            migrationBuilder.DropTable(
                name: "SessionParticipants");

            migrationBuilder.DropColumn(
                name: "CurrentNumber",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Sessions",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "StartTimeUtc",
                table: "Sessions",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "EndTimeUtc",
                table: "Sessions",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "Sessions",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "CurrentRound",
                table: "Sessions",
                newName: "Duration");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlayerId",
                table: "Sessions",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Players_PlayerId",
                table: "Sessions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
