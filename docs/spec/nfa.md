# Non-Functional Attributes (NfA) — SMART Criteria

## NfA-01: Query Latency

**Specific:** All Capture list queries (`ICaptureService.ListAsync`) with a limit ≤ 50 must complete within 100ms at p95 under normal load.  
**Measurable:** Measured via OpenTelemetry span duration on the `ListAsync` span; threshold surfaced in Grafana.  
**Achievable:** Index-backed queries on `Stage`, `CreatedAt`, and `MatchedSkill` columns; cursor pagination avoids full-table scans.  
**Relevant:** Dashboard and Captures list are the two highest-traffic read paths.  
**Time-bound:** Verified against Testcontainers PostgreSQL with 10k seeded rows in Slice 4.

## NfA-02: Index Strategy

All high-frequency filter columns carry dedicated B-tree indexes:

| Index | Column(s) | Query pattern |
|---|---|---|
| `IX_Captures_Stage` | `Stage` | Dashboard "Needs Attention", lifecycle filter |
| `IX_Captures_CreatedAt_DESC` | `CreatedAt DESC` | Recent Captures, cursor pagination |
| `IX_Captures_MatchedSkill` | `MatchedSkill` | Skill-based queries |
| `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC` | `(IntegrationName, SampledAt DESC)` | Health history queries |

## NfA-03: Migration Strategy

- All schema changes via EF Core migrations (code-first, migration files committed to Git).
- Production apply: `dotnet ef migrations script --idempotent` generates an idempotent SQL script reviewed before each deploy.
- Never `EnsureCreated` or auto-migrate inside `app.Run()` in production — migrations run as a separate init step (12-Factor XII).
- Dev: `MigrationRunner` hosted service auto-applies on startup for developer convenience.

## NfA-04: Data Volume Assumptions

- Captures: up to 100,000 rows in Block 4 scope. Beyond 1M, consider partitioning (out of scope).
- IntegrationHealthSamples: up to 10,000 rows per integration. Prune policy (retain 90 days) deferred to Block 5.
- SkillRuns: up to 500,000 rows. Archival deferred to Block 5.

## NfA-05: Connection Resilience

Npgsql connection pool defaults (min=0, max=100) are sufficient for single-instance dev deployment. Production pool sizing configured via connection string parameters. Connection retries handled by Npgsql's built-in retry policy.

## NfA-D1: Container Build Time

**Category:** Deployment  
**Statement:** The multi-stage Docker image for `flowhub.web` MUST build from scratch in under 5 minutes on a GitHub-hosted `ubuntu-latest` runner (2 vCPUs, 7 GB RAM).  
**Measurement:** GitHub Actions `release.yml` workflow — `docker/build-push-action` step duration.  
**Target:** ≤ 300 seconds.

## NfA-D2: Image Size

**Category:** Deployment  
**Statement:** The published `flowhub-web` Docker image MUST be under 200 MB compressed.  
**Measurement:** `docker image inspect ghcr.io/freaxnx01/flowhub-web:<version> --format='{{.Size}}'` after pull (uncompressed); GHCR layer sizes (compressed).  
**Target:** ≤ 200 MB compressed (≤ 400 MB uncompressed).

## NfA-D3: Startup Time

**Category:** Deployment  
**Statement:** After migrations complete, `flowhub.web` MUST reach a healthy `/health/live` state within 30 seconds on a cold container start.  
**Measurement:** Docker Compose healthcheck — `interval: 10s, retries: 3` = max 30 s.  
**Target:** `/health/live` returns HTTP 200 within 30 seconds of container start.

## NfA-O1: Observability — Metrics Endpoint

**Category:** Observability  
**Statement:** The running `flowhub.web` process MUST expose Prometheus-format metrics at `/metrics`.  
**Measurement:** `curl http://localhost:5070/metrics` returns HTTP 200 with `Content-Type: text/plain; version=0.0.4`.  
**Target:** Endpoint available and returns at least `dotnet_*` and `http_*` metrics.

## NfA-P1: Personendaten-Residenz

**Category:** Privacy & Compliance
**Statement:** Capture-Inhalte (Body, URL, Metadaten) MÜSSEN auf vom Betreiber selbst betriebener Infrastruktur verarbeitet und persistiert werden. Outbound-Calls an Drittsysteme sind ausschliesslich für explizite Skill-Targets (Vikunja, Wallabag) zulässig; LLM-Inferenz erfolgt per Default lokal via Ollama.
**Measurable:**
1. Statisch: `dotnet list package` zeigt keinen Cloud-LLM-Client als transitive Default-Dependency. Cloud-LLM-Provider sind hinter `EmbeddingsOptions.Provider != "Local"` opt-in.
2. Dynamisch: integration test `OutboundCallAuditTests` verifiziert, dass im Default-Profil kein HTTP-Request an `*.openai.com`, `*.anthropic.com` oder `api.cohere.ai` erfolgt.
3. Doku: `docs/design/data-flow.md` enthält ein Sequenzdiagramm mit Legende, das Capture-Pfad → Klassifikation → Skill-Routing zeigt und Homelab-Boundary markiert.
**Achievable:** Standard-Konfiguration in `source/FlowHub.Web/appsettings.json` setzt `Embeddings:Provider=Local`; Cloud-Provider erfordern bewusste Environment-Variable `Embeddings__Provider=OpenAI` + `Embeddings__ApiKey`.
**Relevant:** GDPR Art. 2(2)(c) Haushaltsausnahme + CH revDSG bleiben nur tragfähig, solange Capture-Inhalte das Homelab nicht verlassen. Cloud-Opt-in dokumentiert die bewusste Abkehr von diesem Default und zwingt zu separater DPA-Betrachtung.
**Time-bound:** Verifiziert bis Ende Block 5 — Outbound-Audit-Test grün in CI, Data-Flow-Diagramm im Submission-PDF, ADR LLM-Hosting gemerged.

## NfA-P2: KI-Transparenz (AI Act Art. 50)

**Category:** Privacy & Compliance
**Statement:** Jeder Capture, dessen `MatchedSkill` durch eine LLM-gestützte Klassifikation gesetzt wurde, MUSS in der UI sichtbar als "AI-classified" gekennzeichnet sein und im Datensatz die Klassifikations-Provenienz tragen (`ClassificationSource`, `ClassifiedAt`, optional `ConfidenceScore`).
**Measurable:**
1. UI: bUnit-Test `LifecycleBadgeTests.AiClassified_ShowsAiBadge` bestätigt, dass `LifecycleBadge` für `ClassificationSource = "AI"` das KI-Badge rendert.
2. Datenmodell: `Capture` hat Spalten `ClassificationSource` (enum: `None | Heuristic | AI | Manual`), `ClassifiedAt`, `ConfidenceScore`; EF-Migration committed.
3. API: `GET /api/captures/{id}` gibt diese Felder in der Response zurück.
**Achievable:** Bereits vorhandener `LifecycleBadge`-Component wird um einen AI-Zweig erweitert; `IClassifier`-Implementierung in `FlowHub.AI` setzt `ClassificationSource` beim Schreiben des Resultats.
**Relevant:** EU AI Act Art. 50 verpflichtet Anbieter / Betreiber von KI-Systemen, Nutzer:innen erkennbar zu machen, dass sie mit KI-Output interagieren. Minimal-Risk-Einstufung von FlowHub ist nur tragfähig, wenn diese Transparenzpflicht aktiv umgesetzt ist.
**Time-bound:** Verifiziert bis Ende Block 5 — bUnit-Test grün, Migration angewandt, UI-Screenshot im Submission-PDF.
