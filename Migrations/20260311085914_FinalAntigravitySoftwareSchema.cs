using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class FinalAntigravitySoftwareSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "SoftwareKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentDate",
                table: "SoftwareKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "SoftwareKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryAttemptCount",
                table: "ITAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareKeys_AssignedToUserId",
                table: "SoftwareKeys",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareKeys_AspNetUsers_AssignedToUserId",
                table: "SoftwareKeys",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareKeys_AspNetUsers_AssignedToUserId",
                table: "SoftwareKeys");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareKeys_AssignedToUserId",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "AssignmentDate",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "ExpiryAttemptCount",
                table: "ITAssets");
        }
    }
}
