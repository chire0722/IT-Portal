// Project comment: 20260314085509_RestoreLegacyProperties.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestoreLegacyProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalLicenses",
                table: "SoftwareLicenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasAdobe",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAntivirus",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAutoCAD",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAutoDesk",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDWGTrueView",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEShare",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFortiClientVPN",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGStarCAD",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGoogleChrome",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasIFlexiHRMS",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSOffice",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSProject",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSTeams",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasMSVisio",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOS",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOtherSoftware",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSQLAccounting",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSketchUp",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxAccount",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxAdmin",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxIT",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxInfo",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxIntern",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxLogistic",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxStore",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MailboxSupervisor",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherSoftwareDetails",
                table: "OnboardingRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrecastGroup",
                table: "OnboardingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalLicenses",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "HasAdobe",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasAntivirus",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasAutoCAD",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasAutoDesk",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasDWGTrueView",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasEShare",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasFortiClientVPN",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasGStarCAD",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasGoogleChrome",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasIFlexiHRMS",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasMSOffice",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasMSProject",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasMSTeams",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasMSVisio",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasOS",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasOtherSoftware",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasSQLAccounting",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "HasSketchUp",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxAccount",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxAdmin",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxIT",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxInfo",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxIntern",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxLogistic",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxStore",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "MailboxSupervisor",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "OtherSoftwareDetails",
                table: "OnboardingRequests");

            migrationBuilder.DropColumn(
                name: "PrecastGroup",
                table: "OnboardingRequests");
        }
    }
}
