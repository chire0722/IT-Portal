// Project comment: 20260319052057_AddSignatureHmacFields.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureHmacFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeSignatureHmac",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignatureHmac",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterSignatureHmac",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeSignatureHmac",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODSignatureHmac",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HRSignatureHmac",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignatureHmac",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeSignatureHmac",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODSignatureHmac",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HRSignatureHmac",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignatureHmac",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeSignatureHmac",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ITAdminSignatureHmac",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "RequesterSignatureHmac",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "EmployeeSignatureHmac",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HODSignatureHmac",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HRSignatureHmac",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "ITAdminSignatureHmac",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "EmployeeSignatureHmac",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HODSignatureHmac",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HRSignatureHmac",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ITAdminSignatureHmac",
                table: "EmployeeTransfers");
        }
    }
}
