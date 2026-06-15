using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _0013_ResizeEmbeddingTo384 : Migration
    {
        // Resize the embedding column 1024 -> 384 for the self-hosted multilingual-e5-small
        // embedder (see ADR 0006). A plain AlterColumn can't cast existing vectors and the HNSW
        // index blocks the type change, so do it explicitly: drop the index, null existing
        // embeddings (they're rebuilt via POST /api/v1/admin/embeddings/rebuild once the new
        // embedder is wired), resize, then recreate the index.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS captures_embedding_hnsw_idx;""");
            migrationBuilder.Sql("""UPDATE "Captures" SET "Embedding" = NULL;""");
            migrationBuilder.Sql("""ALTER TABLE "Captures" ALTER COLUMN "Embedding" TYPE vector(384);""");
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS captures_embedding_hnsw_idx
                ON "Captures" USING hnsw ("Embedding" vector_cosine_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS captures_embedding_hnsw_idx;""");
            migrationBuilder.Sql("""UPDATE "Captures" SET "Embedding" = NULL;""");
            migrationBuilder.Sql("""ALTER TABLE "Captures" ALTER COLUMN "Embedding" TYPE vector(1024);""");
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS captures_embedding_hnsw_idx
                ON "Captures" USING hnsw ("Embedding" vector_cosine_ops);
                """);
        }
    }
}
