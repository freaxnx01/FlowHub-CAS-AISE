using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0004_AddEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Captures",
                type: "vector(1024)",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS captures_embedding_hnsw_idx
                ON "Captures" USING hnsw ("Embedding" vector_cosine_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS captures_embedding_hnsw_idx;""");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Captures");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
