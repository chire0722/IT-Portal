// Project comment: 20260410012838_OptimizeDashboardHotPathIndexes.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDashboardHotPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_IsSubscription_ExpiryDate",
                table: "SoftwareLicenses",
                columns: new[] { "IsSubscription", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaSettings_RequestType",
                table: "SlaSettings",
                column: "RequestType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingRequests_RequestDate_Department",
                table: "OnboardingRequests",
                columns: new[] { "RequestDate", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingRequests_Status_EmployeeSignedAt",
                table: "OnboardingRequests",
                columns: new[] { "Status", "EmployeeSignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingRequests_Status_RequestDate",
                table: "OnboardingRequests",
                columns: new[] { "Status", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingRequests_Status_UpdatedAt",
                table: "OnboardingRequests",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_RequestDate_Department",
                table: "OffboardingRequests",
                columns: new[] { "RequestDate", "Department" });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_Status_EmployeeSignedAt",
                table: "OffboardingRequests",
                columns: new[] { "Status", "EmployeeSignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_Status_RequestDate",
                table: "OffboardingRequests",
                columns: new[] { "Status", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_Status_UpdatedAt",
                table: "OffboardingRequests",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ITAssets_PurchaseDate",
                table: "ITAssets",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_ITAssets_WarrantyExpiry",
                table: "ITAssets",
                column: "WarrantyExpiry");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_Status_CreatedAt",
                table: "EmployeeTransfers",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt_Timestamp",
                table: "AuditLogs",
                columns: new[] { "CreatedAt", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_IsSuccess_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityName", "IsSuccess", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SoftwareLicenses_IsSubscription_ExpiryDate",
                table: "SoftwareLicenses");

            migrationBuilder.DropIndex(
                name: "IX_SlaSettings_RequestType",
                table: "SlaSettings");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingRequests_RequestDate_Department",
                table: "OnboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingRequests_Status_EmployeeSignedAt",
                table: "OnboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingRequests_Status_RequestDate",
                table: "OnboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingRequests_Status_UpdatedAt",
                table: "OnboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingRequests_RequestDate_Department",
                table: "OffboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingRequests_Status_EmployeeSignedAt",
                table: "OffboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingRequests_Status_RequestDate",
                table: "OffboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_OffboardingRequests_Status_UpdatedAt",
                table: "OffboardingRequests");

            migrationBuilder.DropIndex(
                name: "IX_ITAssets_PurchaseDate",
                table: "ITAssets");

            migrationBuilder.DropIndex(
                name: "IX_ITAssets_WarrantyExpiry",
                table: "ITAssets");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeTransfers_Status_CreatedAt",
                table: "EmployeeTransfers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CreatedAt_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_IsSuccess_CreatedAt",
                table: "AuditLogs");
        }
    }
}
