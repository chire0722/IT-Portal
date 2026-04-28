// Project comment: 20260311085207_AntigravitySoftwareOptimization.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AntigravitySoftwareOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalLicenses",
                table: "SoftwareLicenses",
                newName: "ExpiryAttemptCount");

            migrationBuilder.AddColumn<int>(
                name: "ContractLimit",
                table: "SoftwareLicenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SoftwareLicenses",
                type: "BLOB",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KeyValue",
                table: "SoftwareKeys",
                type: "TEXT",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "KeyValueHash",
                table: "SoftwareKeys",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareKeys_KeyValueHash",
                table: "SoftwareKeys",
                column: "KeyValueHash",
                unique: true,
                filter: "[KeyValueHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SoftwareKeys_KeyValueHash",
                table: "SoftwareKeys");

            migrationBuilder.DropColumn(
                name: "ContractLimit",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SoftwareLicenses");

            migrationBuilder.DropColumn(
                name: "KeyValueHash",
                table: "SoftwareKeys");

            migrationBuilder.RenameColumn(
                name: "ExpiryAttemptCount",
                table: "SoftwareLicenses",
                newName: "TotalLicenses");

            migrationBuilder.AlterColumn<string>(
                name: "KeyValue",
                table: "SoftwareKeys",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
