using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0006_RemoveWekanIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Integrations",
                keyColumn: "Name",
                keyValue: "Wekan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Integrations",
                columns: new[] { "Name", "LastWriteAt", "LastWriteDurationMs", "Status" },
                values: new object[] { "Wekan", null, null, "Healthy" });
        }
    }
}
