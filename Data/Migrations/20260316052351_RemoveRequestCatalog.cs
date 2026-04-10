using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequestCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogRequests");

            migrationBuilder.DeleteData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HODSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HardwareRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsHardwareRequest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSoftwareRequest = table.Column<bool>(type: "INTEGER", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SlaNotifyStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    SoftwareRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRequests", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SlaSettings",
                columns: new[] { "Id", "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2", "NotifyEmail", "RequestType", "TargetDays", "UpdatedAt" },
                values: new object[] { 3, 3, 7, null, null, null, "Catalog", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
