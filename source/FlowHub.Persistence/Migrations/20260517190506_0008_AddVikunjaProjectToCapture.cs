using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0008_AddVikunjaProjectToCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VikunjaProject",
                table: "Captures",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VikunjaProject",
                table: "Captures");
        }
    }
}
