using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureIntegrityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeSignerName",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignerName",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterSignerName",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeSignerName",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODSignerName",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HRSignerName",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignerName",
                table: "OffboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODSignerName",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HRSignerName",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ITAdminSignerName",
                table: "EmployeeTransfers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeSignerName",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "ITAdminSignerName",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "RequesterSignerName",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "EmployeeSignerName",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HODSignerName",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HRSignerName",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "ITAdminSignerName",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HODSignerName",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HRSignerName",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ITAdminSignerName",
                table: "EmployeeTransfers");
        }
    }
}
