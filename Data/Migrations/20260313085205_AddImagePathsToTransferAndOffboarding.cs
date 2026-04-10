using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePathsToTransferAndOffboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HardwareReturnImagePath",
                table: "OffboardingRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardwareDamageImagePath",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardwareReturnImagePath",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardwareReturnImagePath",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "HardwareDamageImagePath",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HardwareReturnImagePath",
                table: "EmployeeTransfers");
        }
    }
}
