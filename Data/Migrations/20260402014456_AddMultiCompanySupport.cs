// Project comment: 20260402014456_AddMultiCompanySupport.cs is part of the IT Form application source code.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCompanySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Companies_CompanyId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailGroups_Companies_CompanyId",
                table: "EmailGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerFolders_Companies_CompanyId",
                table: "ServerFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_SharedMailboxes_Companies_CompanyId",
                table: "SharedMailboxes");

            migrationBuilder.DropIndex(
                name: "IX_SharedMailboxes_CompanyId",
                table: "SharedMailboxes");

            migrationBuilder.DropIndex(
                name: "IX_ServerFolders_CompanyId",
                table: "ServerFolders");

            migrationBuilder.DropIndex(
                name: "IX_EmailGroups_CompanyId",
                table: "EmailGroups");

            migrationBuilder.DropIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SharedMailboxes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ServerFolders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "EmailGroups");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Departments");

            migrationBuilder.CreateTable(
                name: "DepartmentCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentCompanies_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailGroupCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmailGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroupCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailGroupCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailGroupCompanies_EmailGroups_EmailGroupId",
                        column: x => x.EmailGroupId,
                        principalTable: "EmailGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerFolderCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerFolderId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerFolderCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerFolderCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerFolderCompanies_ServerFolders_ServerFolderId",
                        column: x => x.ServerFolderId,
                        principalTable: "ServerFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedMailboxCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SharedMailboxId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedMailboxCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedMailboxCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedMailboxCompanies_SharedMailboxes_SharedMailboxId",
                        column: x => x.SharedMailboxId,
                        principalTable: "SharedMailboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentCompanies_CompanyId",
                table: "DepartmentCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentCompanies_DepartmentId",
                table: "DepartmentCompanies",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroupCompanies_CompanyId",
                table: "EmailGroupCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroupCompanies_EmailGroupId",
                table: "EmailGroupCompanies",
                column: "EmailGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerFolderCompanies_CompanyId",
                table: "ServerFolderCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerFolderCompanies_ServerFolderId",
                table: "ServerFolderCompanies",
                column: "ServerFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMailboxCompanies_CompanyId",
                table: "SharedMailboxCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMailboxCompanies_SharedMailboxId",
                table: "SharedMailboxCompanies",
                column: "SharedMailboxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentCompanies");

            migrationBuilder.DropTable(
                name: "EmailGroupCompanies");

            migrationBuilder.DropTable(
                name: "ServerFolderCompanies");

            migrationBuilder.DropTable(
                name: "SharedMailboxCompanies");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SharedMailboxes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ServerFolders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "EmailGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Departments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedMailboxes_CompanyId",
                table: "SharedMailboxes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerFolders_CompanyId",
                table: "ServerFolders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_CompanyId",
                table: "EmailGroups",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Companies_CompanyId",
                table: "Departments",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailGroups_Companies_CompanyId",
                table: "EmailGroups",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServerFolders_Companies_CompanyId",
                table: "ServerFolders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SharedMailboxes_Companies_CompanyId",
                table: "SharedMailboxes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
