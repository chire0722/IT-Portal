// Project comment: 20260401090955_AddEmployeeTransferClearanceFields.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeTransferClearanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClearanceRemarks",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClearedKeys",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClearedLaptop",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClearedMobile",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClearedOtherAssets",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FinesRemarks",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardwareNotes",
                table: "EmployeeTransfers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MissingItemFines",
                table: "EmployeeTransfers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckAbcGroupRemove",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckDomainDisjoin",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckEmailMailGroupRemove",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckEmailSignOut",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckGeneralSoftwareDeactivate",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckO365Deactivate",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckPhysicalClean",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckPrinterRemove",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckScanFolderRemove",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckSharedDriveRemove",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckSpecificSoftwareDeactivate",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckVPNDisable",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckWiFiDisable",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetCheckWindowsDisable",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckAbcGroup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckDomainJoin",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckEmailMailGroup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckEmailSetup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckGeneralSoftware",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckO365Setup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckPhysicalClean",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckPrinterSetup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckScanFolder",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckSharedDrive",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckSpecificSoftware",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckVPNSetup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckWiFiSetup",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupCheckWindowsLogin",
                table: "EmployeeTransfers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClearanceRemarks",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ClearedKeys",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ClearedLaptop",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ClearedMobile",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "ClearedOtherAssets",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "FinesRemarks",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "HardwareNotes",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "MissingItemFines",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckAbcGroupRemove",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckDomainDisjoin",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckEmailMailGroupRemove",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckEmailSignOut",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckGeneralSoftwareDeactivate",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckO365Deactivate",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckPhysicalClean",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckPrinterRemove",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckScanFolderRemove",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckSharedDriveRemove",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckSpecificSoftwareDeactivate",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckVPNDisable",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckWiFiDisable",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "RetCheckWindowsDisable",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckAbcGroup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckDomainJoin",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckEmailMailGroup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckEmailSetup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckGeneralSoftware",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckO365Setup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckPhysicalClean",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckPrinterSetup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckScanFolder",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckSharedDrive",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckSpecificSoftware",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckVPNSetup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckWiFiSetup",
                table: "EmployeeTransfers");

            migrationBuilder.DropColumn(
                name: "SupCheckWindowsLogin",
                table: "EmployeeTransfers");
        }
    }
}
