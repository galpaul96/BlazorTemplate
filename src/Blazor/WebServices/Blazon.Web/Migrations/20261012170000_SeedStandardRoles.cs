using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blazor.Web.Migrations
{
    public partial class SeedStandardRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { "25d1e5a3-c232-4380-b501-34f9253f2b68", "User", "USER", "9e2e2b0c-7ee1-4eb8-8c6f-0d5fc9641e7c" },
                    { "6a23ff47-94d6-4bb6-b076-773936f6b21e", "Moderator", "MODERATOR", "c96fc836-66e0-42bd-9065-0cfa5a542605" },
                    { "d1ccbd73-293e-4437-8a57-4b42bd58e93c", "Reviewer", "REVIEWER", "cdb5d3a5-a8a9-4f70-9392-52f701053643" },
                    { "5e51d2a9-74b3-49cb-9429-057c0d8cda7e", "Editor", "EDITOR", "a0aa3875-bd5a-43a3-871f-18156ca7530b" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    "25d1e5a3-c232-4380-b501-34f9253f2b68",
                    "6a23ff47-94d6-4bb6-b076-773936f6b21e",
                    "d1ccbd73-293e-4437-8a57-4b42bd58e93c",
                    "5e51d2a9-74b3-49cb-9429-057c0d8cda7e"
                });
        }
    }
}
