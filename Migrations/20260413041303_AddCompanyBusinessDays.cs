using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyBusinessDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WorksOnFriday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnMonday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnSaturday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnSunday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnThursday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnTuesday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "WorksOnWednesday",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorksOnFriday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnMonday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnSaturday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnSunday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnThursday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnTuesday",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WorksOnWednesday",
                table: "Companies");
        }
    }
}
