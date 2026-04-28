// Project comment: 20260413030720_AddCompanyBusinessHours.cs is part of the IT Form application source code.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyBusinessHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "BusinessEndTime",
                table: "Companies",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 17, 30, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "BusinessStartTime",
                table: "Companies",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 8, 30, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessEndTime",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BusinessStartTime",
                table: "Companies");
        }
    }
}
