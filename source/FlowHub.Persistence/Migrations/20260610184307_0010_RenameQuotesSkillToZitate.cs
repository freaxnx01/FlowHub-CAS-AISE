using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0010_RenameQuotesSkillToZitate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Quotes");

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Name", "LastResetAt", "Status" },
                values: new object[] { "Zitate", null, "Degraded" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Name",
                keyValue: "Zitate");

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Name", "LastResetAt", "Status" },
                values: new object[] { "Quotes", null, "Degraded" });
        }
    }
}
