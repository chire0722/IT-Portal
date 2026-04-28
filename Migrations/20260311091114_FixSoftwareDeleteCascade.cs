// Project comment: 20260311091114_FixSoftwareDeleteCascade.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class FixSoftwareDeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenses_StandardSoftware_StandardSoftwareId",
                table: "SoftwareLicenses");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenses_StandardSoftware_StandardSoftwareId",
                table: "SoftwareLicenses",
                column: "StandardSoftwareId",
                principalTable: "StandardSoftware",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareLicenses_StandardSoftware_StandardSoftwareId",
                table: "SoftwareLicenses");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareLicenses_StandardSoftware_StandardSoftwareId",
                table: "SoftwareLicenses",
                column: "StandardSoftwareId",
                principalTable: "StandardSoftware",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
