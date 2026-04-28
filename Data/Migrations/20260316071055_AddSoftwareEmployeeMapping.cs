// Project comment: 20260316071055_AddSoftwareEmployeeMapping.cs is part of the IT Form application source code.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareEmployeeMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LicenseType",
                table: "StandardSoftware",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AllocatedSeats",
                table: "SoftwareLicenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSeats",
                table: "SoftwareLicenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeSoftwares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    SoftwareId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoftwareLicenseId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UnassignedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsITSetupPending = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsITRevokePending = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSoftwares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSoftwares_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSoftwares_SoftwareLicenses_SoftwareLicenseId",
                        column: x => x.SoftwareLicenseId,
                        principalTable: "SoftwareLicenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeSoftwares_StandardSoftware_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "StandardSoftware",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSoftwares_EmployeeId",
                table: "EmployeeSoftwares",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSoftwares_SoftwareId",
                table: "EmployeeSoftwares",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSoftwares_SoftwareLicenseId",
                table: "EmployeeSoftwares",
                column: "SoftwareLicenseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSoftwares");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "StandardSoftware");

            migrationBuilder.DropColumn(
                name: "AllocatedSeats",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "TotalSeats",
                table: "SoftwareLicenses");
        }
    }
}
