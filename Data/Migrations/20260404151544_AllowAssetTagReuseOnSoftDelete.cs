// Project comment: 20260404151544_AllowAssetTagReuseOnSoftDelete.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowAssetTagReuseOnSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ITAssets_AssetTag",
                table: "ITAssets");

            migrationBuilder.CreateIndex(
                name: "IX_ITAssets_AssetTag",
                table: "ITAssets",
                column: "AssetTag",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ITAssets_AssetTag",
                table: "ITAssets");

            migrationBuilder.CreateIndex(
                name: "IX_ITAssets_AssetTag",
                table: "ITAssets",
                column: "AssetTag",
                unique: true);
        }
    }
}
