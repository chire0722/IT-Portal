// Project comment: 20260316013221_AuditExpansion.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetRemarks",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FolderRemarks",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasOtherFolder",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOtherSharedMailbox",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherBranchDetails",
                table: "OnboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDepartmentDetails",
                table: "OnboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherFolderDetails",
                table: "OnboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherSharedMailboxDetails",
                table: "OnboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareRemarks",
                table: "OnboardingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherBranchDetails",
                table: "OffboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDepartmentDetails",
                table: "OffboardingRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChassisType",
                table: "ITAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageDescription",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAdobe",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAntivirus",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAutoCAD",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAutoDesk",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDWGTrueView",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEShare",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFortiClientVPN",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGStarCAD",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGoogleChrome",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasIFlexiHRMS",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSOffice",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSProject",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSTeams",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSVisio",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOS",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOtherSoftware",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSQLAccounting",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSketchUp",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxAccount",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxAdmin",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxIT",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxInfo",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxIntern",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxLogistic",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxStore",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxSupervisor",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherFolderDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherFromBranchDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherFromDepartmentDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherSharedMailboxDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherSoftwareDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherToBranchDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherToDepartmentDetails",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrecastGroup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetRemarks",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "FolderRemarks",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasOtherFolder",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasOtherSharedMailbox",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherBranchDetails",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherDepartmentDetails",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherFolderDetails",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherSharedMailboxDetails",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "SoftwareRemarks",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherBranchDetails",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherDepartmentDetails",
                table: "OffboardingRequests");

            migrationBuilder.DropColumn(
                name: "ChassisType",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "DamageDescription",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasAdobe",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasAntivirus",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasAutoCAD",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasAutoDesk",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasDWGTrueView",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasEShare",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasFortiClientVPN",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasGStarCAD",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasGoogleChrome",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasIFlexiHRMS",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasMSOffice",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasMSProject",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasMSTeams",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasMSVisio",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasOS",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasOtherSoftware",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasSQLAccounting",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HasSketchUp",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxAccount",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxAdmin",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxIT",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxInfo",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxIntern",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxLogistic",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxStore",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MailboxSupervisor",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherFolderDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherFromBranchDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherFromDepartmentDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherSharedMailboxDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherSoftwareDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherToBranchDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "OtherToDepartmentDetails",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "PrecastGroup",
                table: "EmployeeTransfers");
        }
    }
}
