using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalWebsite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContactRequestWithUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "ContactRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ContactRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "ContactRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_UserId",
                table: "ContactRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactRequests_Users_UserId",
                table: "ContactRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactRequests_Users_UserId",
                table: "ContactRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_UserId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "ContactRequests");
        }
    }
}
