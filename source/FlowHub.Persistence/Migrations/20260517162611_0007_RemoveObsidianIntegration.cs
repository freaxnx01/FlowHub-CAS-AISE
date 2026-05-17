using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0007_RemoveObsidianIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Obsidian");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Integrations",
                columns: new[] { "Name", "LastWriteAt", "LastWriteDurationMs", "Status" },
                values: new object[] { "Obsidian", null, null, "Healthy" });
        }
    }
}
