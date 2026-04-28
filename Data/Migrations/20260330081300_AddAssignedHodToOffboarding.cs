// Project comment: 20260330081300_AddAssignedHodToOffboarding.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedHodToOffboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportingManagers");

            migrationBuilder.AddColumn<string>(
                name: "AssignedHodId",
                table: "OffboardingRequests",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_AssignedHodId",
                table: "OffboardingRequests",
                column: "AssignedHodId");

            migrationBuilder.AddForeignKey(
                name: "FK_OffboardingRequests_AspNetUsers_AssignedHodId",
                table: "OffboardingRequests",
                column: "AssignedHodId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OffboardingRequests_AspNetUsers_AssignedHodId",
                table: "OffboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingRequests_AssignedHodId",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "AssignedHodId",
                table: "OffboardingRequests");

            migrationBuilder.CreateTable(
                name: "ReportingManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingManagers", x => x.Id);
                });
        }
    }
}
