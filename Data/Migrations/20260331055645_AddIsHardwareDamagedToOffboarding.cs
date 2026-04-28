// Project comment: 20260331055645_AddIsHardwareDamagedToOffboarding.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsHardwareDamagedToOffboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHardwareDamaged",
                table: "OffboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHardwareDamaged",
                table: "OffboardingRequests");
        }
    }
}
