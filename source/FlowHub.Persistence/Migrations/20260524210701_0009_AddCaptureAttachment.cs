using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0009_AddCaptureAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Attachment_ContentType",
                table: "Captures",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Attachment_FileName",
                table: "Captures",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Attachment_RelativePath",
                table: "Captures",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Attachment_SizeBytes",
                table: "Captures",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Attachment_UploadedAt",
                table: "Captures",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attachment_ContentType",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Attachment_FileName",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Attachment_RelativePath",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Attachment_SizeBytes",
                table: "Captures");

            migrationBuilder.DropColumn(
                name: "Attachment_UploadedAt",
                table: "Captures");
        }
    }
}
