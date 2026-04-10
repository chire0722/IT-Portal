using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeTransferFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FolderPermissions_EmployeeTransfers_EmployeeTransferId",
                table: "FolderPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_IssuedAssets_EmployeeTransfers_EmployeeTransferId",
                table: "IssuedAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_IssuedSoftware_EmployeeTransfers_EmployeeTransferId",
                table: "IssuedSoftware");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestEmailGroups_EmployeeTransfers_EmployeeTransferId",
                table: "RequestEmailGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestSharedMailboxes_EmployeeTransfers_EmployeeTransferId",
                table: "RequestSharedMailboxes");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestSoftware_EmployeeTransfers_EmployeeTransferId",
                table: "RequestSoftware");

            migrationBuilder.DropTable(
                name: "EmployeeTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RequestSoftware_EmployeeTransferId",
                table: "RequestSoftware");

            migrationBuilder.DropIndex(
                name: "IX_RequestSharedMailboxes_EmployeeTransferId",
                table: "RequestSharedMailboxes");

            migrationBuilder.DropIndex(
                name: "IX_RequestEmailGroups_EmployeeTransferId",
                table: "RequestEmailGroups");

            migrationBuilder.DropIndex(
                name: "IX_IssuedSoftware_EmployeeTransferId",
                table: "IssuedSoftware");

            migrationBuilder.DropIndex(
                name: "IX_IssuedAssets_EmployeeTransferId",
                table: "IssuedAssets");

            migrationBuilder.DropIndex(
                name: "IX_FolderPermissions_EmployeeTransferId",
                table: "FolderPermissions");

            migrationBuilder.DeleteData(
                table: "SlaSettings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "RequestSoftware");

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "RequestSharedMailboxes");

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "RequestEmailGroups");

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "IssuedSoftware");

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "IssuedAssets");

            migrationBuilder.DropColumn(
                name: "EmployeeTransferId",
                table: "FolderPermissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "RequestSoftware",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "RequestSharedMailboxes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "RequestEmailGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "IssuedSoftware",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "IssuedAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeTransferId",
                table: "FolderPermissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccessToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetsReturned = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssetsReturnedRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CheckADUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailSignatureUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckFolderPermissionsUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSoftwareUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecklistRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ClearanceRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ClearedKeys = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedLaptop = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedMobile = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedOtherAssets = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    DamageDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignatureHmac = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeSignerName = table.Column<string>(type: "TEXT", nullable: true),
                    FinesRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FromBranch = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FromDepartment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HODSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignatureHmac = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HODSignerName = table.Column<string>(type: "TEXT", nullable: true),
                    HRSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HRSignatureHmac = table.Column<string>(type: "TEXT", nullable: true),
                    HRSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HRSignerName = table.Column<string>(type: "TEXT", nullable: true),
                    HardwareDamageImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HardwareNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HardwareReturnImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HasAdobe = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAntivirus = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAutoCAD = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAutoDesk = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDWGTrueView = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasEShare = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasFortiClientVPN = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasGStarCAD = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasGoogleChrome = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIFlexiHRMS = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMSOffice = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMSProject = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMSTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMSVisio = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasOS = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasOtherSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSQLAccounting = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSketchUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignatureHmac = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ITAdminSignerName = table.Column<string>(type: "TEXT", nullable: true),
                    MailboxAccount = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxIT = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxInfo = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxIntern = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxLogistic = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxStore = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxSupervisor = table.Column<bool>(type: "INTEGER", nullable: false),
                    MissingItemFines = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewContactNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NewEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NewEmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OtherFolderDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherFromBranchDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherFromDepartmentDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtherGroupDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherSharedMailboxDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherSoftwareDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherToBranchDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OtherToDepartmentDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PrecastGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetCheckAbcGroupRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckDomainDisjoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckEmailMailGroupRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckEmailSignOut = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckGeneralSoftwareDeactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckO365Deactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckPhysicalClean = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckPrinterRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckScanFolderRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckSharedDriveRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckSpecificSoftwareDeactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckVPNDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckWiFiDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetCheckWindowsDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    SlaNotifyStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SupCheckAbcGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckDomainJoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckEmailMailGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckEmailSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckGeneralSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckO365Setup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckPhysicalClean = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckPrinterSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckScanFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckSharedDrive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckSpecificSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckVPNSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckWiFiSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupCheckWindowsLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToBranch = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    ToDepartment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTransfers_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "SlaSettings",
                columns: new[] { "Id", "EscalationDaysTier1", "EscalationDaysTier2", "EscalationEmailTier1", "EscalationEmailTier2", "NotifyEmail", "RequestType", "TargetDays", "UpdatedAt" },
                values: new object[] { 4, 3, 7, null, null, null, "Transfer", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_RequestSoftware_EmployeeTransferId",
                table: "RequestSoftware",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSharedMailboxes_EmployeeTransferId",
                table: "RequestSharedMailboxes",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestEmailGroups_EmployeeTransferId",
                table: "RequestEmailGroups",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedSoftware_EmployeeTransferId",
                table: "IssuedSoftware",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedAssets_EmployeeTransferId",
                table: "IssuedAssets",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPermissions_EmployeeTransferId",
                table: "FolderPermissions",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_OnboardingRequestId",
                table: "EmployeeTransfers",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_Status_CreatedAt",
                table: "EmployeeTransfers",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_FolderPermissions_EmployeeTransfers_EmployeeTransferId",
                table: "FolderPermissions",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IssuedAssets_EmployeeTransfers_EmployeeTransferId",
                table: "IssuedAssets",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IssuedSoftware_EmployeeTransfers_EmployeeTransferId",
                table: "IssuedSoftware",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestEmailGroups_EmployeeTransfers_EmployeeTransferId",
                table: "RequestEmailGroups",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestSharedMailboxes_EmployeeTransfers_EmployeeTransferId",
                table: "RequestSharedMailboxes",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestSoftware_EmployeeTransfers_EmployeeTransferId",
                table: "RequestSoftware",
                column: "EmployeeTransferId",
                principalTable: "EmployeeTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
