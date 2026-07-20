using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddBanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "BanExpiry",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminReplyTime",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MessageLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLikes_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Content", "CoverImageUrl", "Summary", "Title" },
                values: new object[] { "<p>这是我的第一篇博客，欢迎大家！</p>", null, "开篇之作", "欢迎来到 Chris Hopper 的技术空间" });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ProjectUrl", "TechStack" },
                values: new object[] { "使用 ASP.NET Core 10.0 构建", "#", "ASP.NET Core 10.0, SQLite, Bootstrap 5" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BanExpiry", "BanReason", "IsBanned" },
                values: new object[] { null, null, false });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UserId",
                table: "Messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLikes_MessageId_UserId",
                table: "MessageLikes",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLikes_UserId",
                table: "MessageLikes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessageLikes");

            migrationBuilder.DropIndex(
                name: "IX_Messages_UserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BanExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AdminReplyTime",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Messages");

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Content", "CoverImageUrl", "Summary", "Title" },
                values: new object[] { "\r\n                        <h2>🎉 开篇之作</h2>\r\n                        <p>欢迎来到我的个人网站！这是我用 ASP.NET Core 10.0 制作的第一篇博客。</p>\r\n                        <p>这个网站将会记录我的：</p>\r\n                        <ul>\r\n                            <li>技术学习心得</li>\r\n                            <li>项目开发经验</li>\r\n                            <li>编程技巧分享</li>\r\n                            <li>生活感悟随笔</li>\r\n                        </ul>\r\n                        <p>希望通过这个平台，能够和大家交流技术，共同进步。</p>\r\n                        <blockquote class='blockquote'>\r\n                            <p class='mb-0'>代码改变世界，技术创造未来。</p>\r\n                            <footer class='blockquote-footer'>Chris Hopper</footer>\r\n                        </blockquote>\r\n                    ", "/images/blog1.jpg", "这是我的第一篇博客，欢迎大家来到我的技术空间！", "我的第一个博客 - 欢迎来到 Chris 的技术空间" });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ProjectUrl", "TechStack" },
                values: new object[] { "使用 ASP.NET Core 10.0 构建的个人博客与作品展示网站", "https://github.com/chrishopper/personal-website", "ASP.NET Core 10.0, Entity Framework Core, SQLite, Bootstrap 5" });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Description", "ImageUrl", "Name", "ProjectUrl", "TechStack" },
                values: new object[] { 2, "更多精彩项目正在开发中，敬请期待...", "/images/project2.jpg", "待开发项目 2", "#", "即将公布" });
        }
    }
}
