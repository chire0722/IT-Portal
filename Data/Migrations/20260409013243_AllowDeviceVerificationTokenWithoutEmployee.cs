// Project comment: 20260409013243_AllowDeviceVerificationTokenWithoutEmployee.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowDeviceVerificationTokenWithoutEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceVerificationTokens_AspNetUsers_EmployeeId",
                table: "DeviceVerificationTokens");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeId",
                table: "DeviceVerificationTokens",
                type: "TEXT",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 450);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceVerificationTokens_AspNetUsers_EmployeeId",
                table: "DeviceVerificationTokens",
                column: "EmployeeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceVerificationTokens_AspNetUsers_EmployeeId",
                table: "DeviceVerificationTokens");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeId",
                table: "DeviceVerificationTokens",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceVerificationTokens_AspNetUsers_EmployeeId",
                table: "DeviceVerificationTokens",
                column: "EmployeeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
