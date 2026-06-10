using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0011_AddClassifierTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassifierTrace_CompletionTokens",
                table: "Captures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassifierTrace_Kind",
                table: "Captures",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassifierTrace_LatencyMs",
                table: "Captures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassifierTrace_Model",
                table: "Captures",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassifierTrace_PromptTokens",
                table: "Captures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassifierTrace_Provider",
                table: "Captures",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassifierTrace_CompletionTokens",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ClassifierTrace_Kind",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ClassifierTrace_LatencyMs",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ClassifierTrace_Model",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ClassifierTrace_PromptTokens",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "ClassifierTrace_Provider",
                table: "Captures");
        }
    }
}
