using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSubjectRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSubjectRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestReference = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RequestType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CompanyScope = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AssignedToUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    DecisionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CompletionEvidenceReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSubjectRequestEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DataSubjectRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ToStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSubjectRequestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSubjectRequestEvents_DataSubjectRequests_DataSubjectRequestId",
                        column: x => x.DataSubjectRequestId,
                        principalTable: "DataSubjectRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequestEvents_DataSubjectRequestId",
                table: "DataSubjectRequestEvents",
                column: "DataSubjectRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_Status_UpdatedAtUtc",
                table: "DataSubjectRequests",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_SubmittedByUserId_IdempotencyKey",
                table: "DataSubjectRequests",
                columns: new[] { "SubmittedByUserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSubjectRequestEvents");

            migrationBuilder.DropTable(
                name: "DataSubjectRequests");
        }
    }
}
