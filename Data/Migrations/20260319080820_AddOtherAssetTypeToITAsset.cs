// Project comment: 20260319080820_AddOtherAssetTypeToITAsset.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherAssetTypeToITAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtherAssetType",
                table: "ITAssets",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherAssetType",
                table: "IssuedAssets",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherAssetType",
                table: "ITAssets");

            migrationBuilder.DropColumn(
                name: "OtherAssetType",
                table: "IssuedAssets");
        }
    }
}
