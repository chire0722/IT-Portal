// Project comment: 20260404054818_RemoveChassisType.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChassisType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChassisType",
                table: "ITAssets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChassisType",
                table: "ITAssets",
                type: "INTEGER",
                nullable: true);
        }
    }
}
