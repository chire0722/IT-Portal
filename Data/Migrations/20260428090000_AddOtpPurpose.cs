// Project comment: AddOtpPurpose.cs adds an OTP purpose discriminator.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    public partial class AddOtpPurpose : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "OtpRecords",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "PasswordReset");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "OtpRecords");
        }
    }
}
