// Project comment: 20260401011503_UpdatePendingModelChanges.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "AuditNotificationEmail",
                column: "Value",
                value: "itdept@ytcprecast.com;tayziheng@yengtong.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "AuditNotificationEmail",
                column: "Value",
                value: "itdept@ytcprecast.com");
        }
    }
}
