# Non-Functional Attributes (NfA) — SMART Criteria

Every requirement below is decomposed along all five SMART dimensions —
**S**pecific, **M**easurable, **A**chievable, **R**elevant, **T**ime-bound — so
each carries an unambiguous target and a stated way to verify it.

| NfA | Category | Dimension covered |
|---|---|---|
| NfA-01 | Performance | S · M · A · R · T |
| NfA-02 | Performance | S · M · A · R · T |
| NfA-03 | Operability | S · M · A · R · T |
| NfA-04 | Scalability | S · M · A · R · T |
| NfA-05 | Reliability | S · M · A · R · T |
| NfA-D1..D3 | Deployment | S · M · A · R · T |
| NfA-O1 | Observability | S · M · A · R · T |
| NfA-P1..P2 | Privacy & Compliance | S · M · A · R · T |

> **Relationship to the `NF-01..NF-13` table.** The `NF-*` list in
> `docs/spec/use-cases.md` is the original Block-2 quality-attribute catalogue
> (response time, availability, security, testability, …). This `NfA-*` document
> is its SMART-decomposed successor and is **authoritative** where the two
> overlap: `NfA-01` refines `NF-01`/`NF-09` (latency), `NfA-O1` refines `NF-01`
> (observability), `NfA-D1..D3` cover deployment (`NF-02`), `NfA-P1` refines
> `NF-08` (data privacy), and `NfA-P2` adds the AI-Act transparency requirement.
> The `NF-*` numbers are retained only as stable references from existing
> acceptance criteria and use cases.

## NfA-01: Query Latency

**Specific:** All Capture list queries (`ICaptureService.ListAsync`) with a limit ≤ 50 must complete within 100ms at p95 under normal load.  
**Measurable:** Measured via OpenTelemetry span duration on the `ListAsync` span; threshold surfaced in Grafana.  
**Achievable:** Index-backed queries on `Stage`, `CreatedAt`, and `MatchedSkill` columns; cursor pagination avoids full-table scans.  
**Relevant:** Dashboard and Captures list are the two highest-traffic read paths.  
**Time-bound:** Verified against Testcontainers PostgreSQL with 10k seeded rows in Slice 4.

## NfA-02: Index Strategy

**Specific:** Every high-frequency filter column carries a dedicated B-tree index, so the hot read paths never fall back to a sequential scan:

| Index | Column(s) | Query pattern |
|---|---|---|
| `IX_Captures_Stage` | `Stage` | Dashboard "Needs Attention", lifecycle filter |
| `IX_Captures_CreatedAt_DESC` | `CreatedAt DESC` | Recent Captures, cursor pagination |
| `IX_Captures_MatchedSkill` | `MatchedSkill` | Skill-based queries |
| `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC` | `(IntegrationName, SampledAt DESC)` | Health history queries |

**Measurable:** Each index is declared via `HasIndex` in the EF Core model and present in the committed migration; `\d captures` in psql lists them, and `EXPLAIN` on the four query patterns shows index scans, not `Seq Scan`.  
**Achievable:** Indexes are created code-first through EF Core migrations — no manual DBA step.  
**Relevant:** Directly underwrites the NfA-01 p95 latency target on the two highest-traffic read paths.  
**Time-bound:** In place since the Block 4 persistence slice; verified against the Testcontainers PostgreSQL suite.

## NfA-03: Migration Strategy

**Specific:** All schema changes ship as EF Core code-first migrations committed to Git; production applies an idempotent SQL script as a separate init step, never auto-migrating inside `app.Run()`.  
**Measurable:** Migration files exist under `source/FlowHub.Persistence/Migrations/`; production deploy runs `dotnet ef migrations script --idempotent` (reviewed pre-deploy) via the migrations init-container; dev convenience is the `MigrationRunner` hosted service.  
**Achievable:** Standard EF Core tooling plus a dedicated init-job in `docker-compose.yml`.  
**Relevant:** Satisfies 12-Factor XII (admin processes) and keeps production deploys safe and repeatable.  
**Time-bound:** Established in Block 4; exercised end-to-end in the Block 5 deployment.

## NfA-04: Data Volume Assumptions

**Specific:** The design targets these row volumes — Captures ≤ 100,000 (Block 4 scope; partitioning beyond ~1M is out of scope), IntegrationHealthSamples ≤ 10,000 per integration (90-day prune deferred to Block 5), SkillRuns ≤ 500,000 (archival deferred to Block 5).  
**Measurable:** Volume assumptions are documented per table; the NfA-01 load test seeds 10,000 Capture rows to validate behaviour under realistic data.  
**Achievable:** The indexed schema (NfA-02) and cursor pagination handle the stated volumes without partitioning.  
**Relevant:** Bounds the performance and scaling envelope the other NfAs are verified against.  
**Time-bound:** Valid for the Block 4 scope; revisited if a table is projected to exceed its stated ceiling.

## NfA-05: Connection Resilience

**Specific:** Database access uses the Npgsql connection pool (dev defaults min=0, max=100) with built-in transient-fault retries; production pool sizing is set via connection-string parameters.  
**Measurable:** Pool and retry behaviour are configured through the connection string / Npgsql data-source options and asserted by the Testcontainers integration tests that run against real PostgreSQL.  
**Achievable:** Relies on Npgsql's built-in pooling and retry policy — no custom resilience code.  
**Relevant:** Keeps the single-instance dev deployment and the production instance resilient to transient connection faults.  
**Time-bound:** In place from Block 4; confirmed under the Block 5 deployed stack.

## NfA-D1: Container Build Time

**Category:** Deployment  
**Specific:** The multi-stage Docker image for `flowhub.web` MUST build from scratch in under 5 minutes on a GitHub-hosted `ubuntu-latest` runner (2 vCPUs, 7 GB RAM).  
**Measurable:** GitHub Actions `release.yml` workflow — `docker/build-push-action` step duration; target ≤ 300 seconds.  
**Achievable:** Multi-stage Alpine build with layer caching keeps the cold build inside the budget.  
**Relevant:** Fast CI builds keep the release pipeline (and the inner-dev loop) responsive.  
**Time-bound:** Enforced from Block 5 onward on every `release.yml` run.

## NfA-D2: Image Size

**Category:** Deployment  
**Specific:** The published `flowhub-web` Docker image MUST be under 200 MB compressed.  
**Measurable:** `docker image inspect ghcr.io/freaxnx01/flowhub-web:<version> --format='{{.Size}}'` (uncompressed) and GHCR layer sizes (compressed); target ≤ 200 MB compressed (≤ 400 MB uncompressed).  
**Achievable:** `aspnet:10.0-alpine` runtime base plus a self-contained trimmed publish keep the image small.  
**Relevant:** Small images cut pull time and registry/storage cost on the VPS.  
**Time-bound:** Checked on each published image from Block 5 onward.

## NfA-D3: Startup Time

**Category:** Deployment  
**Specific:** After migrations complete, `flowhub.web` MUST reach a healthy `/health/live` state within 30 seconds on a cold container start.  
**Measurable:** Docker Compose healthcheck (`interval: 10s, retries: 3` = max 30 s) — `/health/live` returns HTTP 200 within 30 seconds of container start.  
**Achievable:** Stateless startup with migrations run as a separate init step (NfA-03) keeps cold start fast.  
**Relevant:** Bounds rollout time and makes orchestrated restarts predictable.  
**Time-bound:** Verified against the deployed compose stack in Block 5.

## NfA-O1: Observability — Metrics Endpoint

**Category:** Observability  
**Specific:** The running `flowhub.web` process MUST expose Prometheus-format metrics at `/metrics`.  
**Measurable:** `curl http://localhost:5070/metrics` returns HTTP 200 with `Content-Type: text/plain; version=0.0.4` and at least `dotnet_*` and `http_*` metric families.  
**Achievable:** OpenTelemetry's Prometheus exporter is wired at the composition root and scraped by the bundled Prometheus.  
**Relevant:** Metrics are the backbone of the NfA-01 latency check and the runtime health dashboards.  
**Time-bound:** Available from the Block 5 observability slice and confirmed on the live demo.

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
