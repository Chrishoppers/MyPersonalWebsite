using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalWebsite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContactRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "ContactRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsedBy",
                table: "ContactRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedTime",
                table: "ContactRequests",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UsedBy",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UsedTime",
                table: "ContactRequests");
        }
    }
}
