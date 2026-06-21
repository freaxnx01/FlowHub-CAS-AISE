using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0001_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Captures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MatchedSkill = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    ExternalRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Captures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captures_CreatedAt_DESC",
                table: "Captures",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Captures_MatchedSkill",
                table: "Captures",
                column: "MatchedSkill");

            migrationBuilder.CreateIndex(
                name: "IX_Captures_Stage",
                table: "Captures",
                column: "Stage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Captures");
        }
    }
}
