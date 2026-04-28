// Project comment: 20260413103000_AddCompanyTimeZone.cs is part of the IT Form application source code.

using IT_Form.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260413103000_AddCompanyTimeZone")]
    /// <summary>
    /// Defines the AddCompanyTimeZone class used by the IT Form application.
    /// </summary>
    public partial class AddCompanyTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Companies",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "Asia/Kuala_Lumpur");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Companies");
        }
    }
}
