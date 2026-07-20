using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPersonalWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    VerificationCode = table.Column<string>(type: "TEXT", nullable: true),
                    VerificationCodeExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Content", "Summary", "Title" },
                values: new object[] { "\r\n                        <h2>🎉 开篇之作</h2>\r\n                        <p>欢迎来到我的个人网站！这是我用 ASP.NET Core 10.0 制作的第一篇博客。</p>\r\n                        <p>这个网站将会记录我的：</p>\r\n                        <ul>\r\n                            <li>技术学习心得</li>\r\n                            <li>项目开发经验</li>\r\n                            <li>编程技巧分享</li>\r\n                            <li>生活感悟随笔</li>\r\n                        </ul>\r\n                        <p>希望通过这个平台，能够和大家交流技术，共同进步。</p>\r\n                        <blockquote class='blockquote'>\r\n                            <p class='mb-0'>代码改变世界，技术创造未来。</p>\r\n                            <footer class='blockquote-footer'>Chris Hopper</footer>\r\n                        </blockquote>\r\n                    ", "这是我的第一篇博客，欢迎大家来到我的技术空间！", "我的第一个博客 - 欢迎来到 Chris 的技术空间" });

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

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsAdmin", "IsEmailVerified", "LastLoginAt", "PasswordHash", "Username", "VerificationCode", "VerificationCodeExpiry" },
                values: new object[] { 1, new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "2908685235@qq.com", true, true, null, "AQAAAAIAAYagAAAAEJ4Zj6zVqZMjSx5k5r5WYg==", "admin", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "Blogs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Content", "Summary", "Title" },
                values: new object[] { "<p>欢迎来到我的个人网站！这是我用 ASP.NET Core 制作的第一篇博客。</p>", "开篇之作，欢迎阅读", "我的第一个博客" });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ProjectUrl", "TechStack" },
                values: new object[] { "使用 ASP.NET Core 10.0 构建的个人网站", "https://github.com/yourusername", "ASP.NET Core 10.0, SQLite, Bootstrap 5" });
        }
    }
}
