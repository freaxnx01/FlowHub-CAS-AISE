using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0005_SeedSkillsAndIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Integrations",
                columns: new[] { "Name", "LastWriteAt", "LastWriteDurationMs", "Status" },
                values: new object[,]
                {
                    { "Authentik", null, null, "Healthy" },
                    { "Obsidian", null, null, "Healthy" },
                    { "Paperless", null, null, "Healthy" },
                    { "Vikunja", null, null, "Healthy" },
                    { "Wallabag", null, null, "Healthy" },
                    { "Wekan", null, null, "Healthy" }
                });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Name", "LastResetAt", "Status" },
                values: new object[,]
                {
                    { "Articles", null, "Healthy" },
                    { "Belege", null, "Healthy" },
                    { "Books", null, "Healthy" },
                    { "Knowledge", null, "Healthy" },
                    { "Movies", null, "Healthy" },
                    { "Quotes", null, "Degraded" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Authentik");

            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Obsidian");

            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Paperless");

            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Vikunja");

            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Wallabag");

            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Wekan");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Articles");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Belege");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Books");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Knowledge");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Movies");

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Quotes");
        }
    }
}
