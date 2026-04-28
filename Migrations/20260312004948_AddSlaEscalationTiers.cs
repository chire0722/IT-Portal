// Project comment: 20260312004948_AddSlaEscalationTiers.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaEscalationTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscalationDaysTier1",
                table: "SlaSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EscalationDaysTier2",
                table: "SlaSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EscalationEmailTier1",
                table: "SlaSettings",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationEmailTier2",
                table: "SlaSettings",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2" },
                values: new object[] { 3, 7, null, null });

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2" },
                values: new object[] { 3, 7, null, null });

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2" },
                values: new object[] { 3, 7, null, null });

            migrationBuilder.UpdateData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2" },
                values: new object[] { 3, 7, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalationDaysTier1",
                table: "SlaSettings");

            migrationBuilder.DropColumn(
                name: "EscalationDaysTier2",
                table: "SlaSettings");

            migrationBuilder.DropColumn(
                name: "EscalationEmailTier1",
                table: "SlaSettings");

            migrationBuilder.DropColumn(
                name: "EscalationEmailTier2",
                table: "SlaSettings");
        }
    }
}
