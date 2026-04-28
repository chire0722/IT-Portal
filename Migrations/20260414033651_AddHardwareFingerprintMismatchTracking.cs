// Project comment: 20260414033651_AddHardwareFingerprintMismatchTracking.cs is part of the IT Form application source code.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddHardwareFingerprintMismatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HardwareFingerprintMismatchDetectedAt",
                table: "ITAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardwareFingerprintWarning",
                table: "ITAssets",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHardwareFingerprintMismatch",
                table: "ITAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LatestHardwareAuditJson",
                table: "ITAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LatestHardwareHash",
                table: "ITAssets",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardwareFingerprintWarning",
                table: "DeviceVerificationTokens",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHardwareFingerprintMismatch",
                table: "DeviceVerificationTokens",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedAssetTag",
                table: "DeviceVerificationTokens",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardwareFingerprintMismatchDetectedAt",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "HardwareFingerprintWarning",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "HasHardwareFingerprintMismatch",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "LatestHardwareAuditJson",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "LatestHardwareHash",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "HardwareFingerprintWarning",
                table: "DeviceVerificationTokens");

            migrationBuilder.DropColumn(
                name: "HasHardwareFingerprintMismatch",
                table: "DeviceVerificationTokens");

            migrationBuilder.DropColumn(
                name: "VerifiedAssetTag",
                table: "DeviceVerificationTokens");
        }
    }
}
