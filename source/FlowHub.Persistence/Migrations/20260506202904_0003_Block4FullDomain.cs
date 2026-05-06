using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0003_Block4FullDomain : Migration
    {
        private static readonly string[] IntegrationHealthSamplesIndexColumns = ["IntegrationName", "SampledAt"];
        private static readonly bool[] IntegrationHealthSamplesIndexDescending = [false, true];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastWriteAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastWriteDurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "SkillRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillRuns_Captures_CaptureId",
                        column: x => x.CaptureId,
                        principalTable: "Captures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillRuns_Skills_SkillName",
                        column: x => x.SkillName,
                        principalTable: "Skills",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    CaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => new { x.CaptureId, x.Value });
                    table.ForeignKey(
                        name: "FK_Tags_Captures_CaptureId",
                        column: x => x.CaptureId,
                        principalTable: "Captures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationHealthSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SampledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationHealthSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationHealthSamples_Integrations_IntegrationName",
                        column: x => x.IntegrationName,
                        principalTable: "Integrations",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC",
                table: "IntegrationHealthSamples",
                columns: IntegrationHealthSamplesIndexColumns,
                descending: IntegrationHealthSamplesIndexDescending);

            migrationBuilder.CreateIndex(
                name: "IX_SkillRuns_CaptureId",
                table: "SkillRuns",
                column: "CaptureId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillRuns_SkillName",
                table: "SkillRuns",
                column: "SkillName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationHealthSamples");

            migrationBuilder.DropTable(
                name: "SkillRuns");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Integrations");
        }
    }
}
