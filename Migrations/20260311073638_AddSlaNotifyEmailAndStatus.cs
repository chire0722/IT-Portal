// Project comment: 20260311073638_AddSlaNotifyEmailAndStatus.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaNotifyEmailAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotifyEmail",
                table: "SlaSettings",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlaNotifyStatus",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlaNotifyStatus",
                table: "OffboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlaNotifyStatus",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlaNotifyStatus",
                table: "CatalogRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "NotifyEmail",
                value: null);

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "NotifyEmail",
                value: null);

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "NotifyEmail",
                value: null);

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "NotifyEmail",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyEmail",
                table: "SlaSettings");

            migrationBuilder.DropColumn(
                name: "SlaNotifyStatus",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "SlaNotifyStatus",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "SlaNotifyStatus",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SlaNotifyStatus",
                table: "CatalogRequests");
        }
    }
}
