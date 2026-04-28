// Project comment: 20260318020014_Phase4_SoftDeletes.cs is part of the IT Form application source code.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_SoftDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StandardSoftware",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "StandardSoftware",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StandardSoftware",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SoftwareLicenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "SoftwareLicenses",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SoftwareLicenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SoftwareKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "SoftwareKeys",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SoftwareKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ITAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "ITAssets",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ITAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StandardSoftware");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StandardSoftware");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StandardSoftware");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ITAssets");
        }
    }
}
