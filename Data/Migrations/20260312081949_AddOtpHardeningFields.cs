// Project comment: 20260312081949_AddOtpHardeningFields.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpHardeningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "OtpRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "OtpRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "OtpRecords");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "OtpRecords");
        }
    }
}
