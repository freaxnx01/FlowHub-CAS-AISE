# ADR 0006 — Vector Search: pgvector + Embeddings (self-hosted default)

**Date:** 2026-05-07  
**Status:** Accepted · **Amended 2026-06-15** (default provider → self-hosted `multilingual-e5-small`, column → `vector(384)`; see *Amendment* below)

## Context

FlowHub accumulates Captures over time — todos, quotes, links, receipts — captured without friction and without manual organisation. Weeks later the user no longer remembers the exact words. Keyword search fails when the query doesn't share vocabulary with stored titles or content (e.g. "the saying about simplicity" won't match a stored quote that never uses the word *simplicity*). Semantic (embedding-based) search finds by **meaning** instead. This "capture without friction, retrieve by meaning later" loop is the core product value; the CAS AISE rubric item "Intelligente Services mit KI" (6 pts) also requires a KI-based search feature.

## Decision

- **Vector store:** pgvector extension on the existing PostgreSQL instance. No additional service.
- **Column type:** `vector(384)` — sized for the default `multilingual-e5-small`. (`mistral-embed` @ 1024 is a documented swap — see *Switching Providers*.)
- **Index:** HNSW (`hnsw (Embedding vector_cosine_ops)`) — approximate nearest-neighbour, sub-millisecond at <1 M rows.
- **Embedding provider (default):** self-hosted `intfloat/multilingual-e5-small` (384-dim, multilingual) via HF text-embeddings-inference — OpenAI-compatible, €0, no external API. Live in the public demo. Hosted `mistral-embed` (1024) remains supported.
- **Provider abstraction:** OpenAI-compatible base URL + model name in env vars (MEAI `IEmbeddingGenerator<string, Embedding<float>>`). Switching providers = change env vars + a migration if dimensions differ.
- **Generation pipeline:** Embedding is generated **off the request path** by `CaptureEmbeddingConsumer`, which subscribes to `CaptureCreated` (same shape as `CaptureEnrichmentConsumer`). `POST /api/v1/captures` returns as soon as the row is persisted and the message is published — embedding latency does not count against NF-09 (p95 < 200 ms). Retries: 3 intervals at 500 ms / 2 s / 5 s; on exhaustion the Capture remains without an embedding and can be backfilled via `POST /api/v1/admin/embeddings/rebuild`.
- **Graceful degradation:** If `Embeddings__ApiKey` is absent, `IEmbeddingService` is a no-op and the consumer skips storage; Captures are persisted without embeddings. Search returns `503` when not configured.

## Provider Configuration

```env
# Default — self-hosted, OpenAI-compatible (the demo's `embedder` service):
Embeddings__BaseUrl=http://embedder:80/v1
Embeddings__ApiKey=local            # any non-empty value; the local server ignores it
Embeddings__Model=intfloat/multilingual-e5-small
Embeddings__Dimensions=             # empty → native 384 dims (no truncation param sent)
Embeddings__TimeoutSeconds=10
```

`Embeddings__Dimensions` is forwarded to the provider via `EmbeddingGenerationOptions.Dimensions` — required when using OpenAI `text-embedding-3-*` to truncate to a non-native size. `Embeddings__TimeoutSeconds` is bounded on the underlying `OpenAIClient` (`NetworkTimeout`); on timeout the consumer retries per its policy.

## Switching Providers

To switch to hosted Mistral `mistral-embed` (1024 dims):
1. Change env vars: `BaseUrl=https://api.mistral.ai/v1`, `ApiKey=<key>`, `Model=mistral-embed` (leave `Dimensions` empty — Mistral rejects the `dimensions` field).
2. Add an EF Core migration: `ALTER TABLE "Captures" ALTER COLUMN "Embedding" TYPE vector(1024)` — the inverse of migration `0013`.
3. Run `POST /api/v1/admin/embeddings/rebuild` to backfill.

(Same recipe for OpenAI `text-embedding-3-*`: set its base URL/model and the matching `vector(N)` migration.) OpenRouter and Anthropic are **not supported** for embeddings — neither exposes an embeddings API.

## Amendment 2026-06-15/16 — self-hosted embedder trialled on the demo, then rolled back

The column was migrated to `vector(384)` (migration `0013`) and a **self-hosted
`multilingual-e5-small`** (HF text-embeddings-inference, €0) was wired into the public demo,
with a web Search UI, to make the feature demonstrable live. It was **rolled back** (2026-06-16):
on the tiny demo dataset `multilingual-e5-small` separated short, similar snippets only modestly
(near-synonym queries returned correct-but-near-tie orderings), the web Search UI behaved
inconsistently, and the embedder cost ~1 GB on a shared VPS — not worth it.

**Current state:** the column stays `vector(384)`; the endpoint + pgvector + admin-rebuild +
integration tests remain as the deliverable. Embeddings are **disabled on the public demo**
(`/search` returns 503, transparent). Any OpenAI-compatible embedder enables it — `mistral-embed`
needs a one-migration swap back to `vector(1024)`; a 384-dim provider needs no migration. The
embedding pipeline is exact; ranking quality is bounded by the chosen model (`bge-m3` / `e5-large`
separate better at higher memory cost).

## Consequences

- `+` No new service to operate; pgvector is an extension, not a separate vector DB.
- `+` Provider-agnostic via env vars; no embedder required to ship the code/tests.
- `−` Dimension change requires migration + rebuild.
- `−` HNSW index is not exact; results may differ slightly from exhaustive scan — at small
  scale exact seqscan is both faster and more correct.
- `−` Ranking quality is bounded by the embedding model; small models give near-tie
  orderings on short, similar texts. This is model-separation quality, not a pipeline defect.
