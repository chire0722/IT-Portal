using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyToServerFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ServerFolders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerFolders_CompanyId",
                table: "ServerFolders",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServerFolders_Companies_CompanyId",
                table: "ServerFolders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerFolders_Companies_CompanyId",
                table: "ServerFolders");

            migrationBuilder.DropIndex(
                name: "IX_ServerFolders_CompanyId",
                table: "ServerFolders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ServerFolders");
        }
    }
}
