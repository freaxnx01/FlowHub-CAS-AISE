# ADR 0006 — Vector Search: pgvector + Mistral Embeddings

**Date:** 2026-05-07  
**Status:** Accepted

## Context

FlowHub accumulates Captures over time. Keyword search fails when the query doesn't share vocabulary with stored titles or content. Semantic (embedding-based) search finds by meaning. The CAS AISE rubric item "Intelligente Services mit KI" (6 pts) requires a KI-based search feature.

## Decision

- **Vector store:** pgvector extension on the existing PostgreSQL instance. No additional service.
- **Column type:** `vector(1024)` — fixed by `mistral-embed` output dimensionality.
- **Index:** HNSW (`hnsw (Embedding vector_cosine_ops)`) — approximate nearest-neighbour, sub-millisecond at <1 M rows.
- **Embedding provider:** Mistral `mistral-embed` via MEAI `IEmbeddingGenerator<string, Embedding<float>>`.
- **Provider abstraction:** OpenAI-compatible base URL + model name in env vars. Switching to OpenAI = change 3 env vars + new migration for column if dimensions differ.
- **Graceful degradation:** If `Embeddings__ApiKey` is absent, `IEmbeddingService` is a no-op and Captures are stored without embeddings. Search returns `503` when not configured.

## Provider Configuration

```env
Embeddings__BaseUrl=https://api.mistral.ai/v1
Embeddings__ApiKey=<key>
Embeddings__Model=mistral-embed
Embeddings__Dimensions=1024
```

## Switching Providers

To switch to OpenAI (`text-embedding-3-small`, 1536 dims):
1. Change env vars: `BaseUrl=https://api.openai.com/v1`, `Model=text-embedding-3-small`, `Dimensions=1536`.
2. Add an EF Core migration: `ALTER TABLE "Captures" ALTER COLUMN "Embedding" TYPE vector(1536)`.
3. Run `POST /api/v1/admin/embeddings/rebuild` to backfill.

OpenRouter and Anthropic are **not supported** for embeddings — neither exposes an embeddings API.

## Consequences

- `+` No new service to operate; pgvector is an extension, not a separate vector DB.
- `+` Provider-agnostic via env vars.
- `−` Dimension change requires migration + rebuild.
- `−` HNSW index is not exact; results may differ slightly from exhaustive scan.
