using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blazor.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string adminRoleId = "c9d14ef5-344f-4cf0-a8ee-6838b0ad70a1";
            const string adminUserId = "a79c97c0-0fd2-4ed4-8987-aeb40822ccba";

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[]
                {
                    adminRoleId,
                    "Admin",
                    "ADMIN",
                    "fb6fdd4b-366d-4076-8f75-a588e87863ed"
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[]
                {
                    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed",
                    "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed",
                    "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
                },
                values: new object[]
                {
                    adminUserId,
                    "admin@connectx.local",
                    "ADMIN@CONNECTX.LOCAL",
                    "admin@connectx.local",
                    "ADMIN@CONNECTX.LOCAL",
                    true,
                    "AQAAAAIAAYagAAAAEPTonrz7lKspIQrfBOxZvO5UP1jBQrNgXT302AZSelyufLtjIncZR9VKOhoYtVQsQQ==",
                    "6228fae3-cf55-4f7d-a536-fc1a8c40e1b1",
                    "a0206200-ed30-4da8-8f81-59daf7144389",
                    null,
                    false,
                    false,
                    null,
                    true,
                    0
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { adminUserId, adminRoleId });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "UserId", "RoleId" },
                keyValues: new object[]
                {
                    "a79c97c0-0fd2-4ed4-8987-aeb40822ccba",
                    "c9d14ef5-344f-4cf0-a8ee-6838b0ad70a1"
                });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "a79c97c0-0fd2-4ed4-8987-aeb40822ccba");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c9d14ef5-344f-4cf0-a8ee-6838b0ad70a1");
        }
    }
}
