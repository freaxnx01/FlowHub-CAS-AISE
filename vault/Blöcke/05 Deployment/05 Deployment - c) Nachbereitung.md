---
tags:
  - claude-generated
  - claude-updated
updated: 2026-06-12
---

# Block 5 — Deployment & Abgabe Projektarbeit · Nachbereitung

**Phase budget:** 22 h
**PVA war:** 2026-06-20
**Abgabe-Deadline:** **Samstag, 2026-07-04, 24:00** (PDF-Upload mit Repo-URL)

> **Final Block.** Hier wird die Bewertungs-Rubrik aus [[Bewertungskriterien]] **vollständig** geprüft — alle 5 Buckets müssen Abgabe-fähig sein. Was im jeweiligen Block-Nachbereitung nicht erledigt wurde, gehört hier nachgezogen.

## Lernziel

- Ich bin fähig, meine Applikation zu containerisieren und in Docker und Kubernetes zu betreiben.
- Ich kann GitHub und Copilot sowie die GitLab-Agent-Plattform einsetzen, um CI/CD-Pipelines aufzusetzen und den Deployment-Prozess zu automatisieren.
- Ich bin fähig, entsprechendes Monitoring und Observation aufzusetzen, Systeme zu überwachen und zu optimieren.
- Ich bin in der Lage, mit Quarkus KI-gestützte Applikationen zu bauen.

## Auftrag (Moodle)

In der letzten Nachbearbeitungsphase geht es nun darum, die Lösung zu containerisieren und für den Betrieb zu verteilen. Nutzen Sie die Möglichkeit Ihrer Git-Host-Lösung, um den Deployment-Prozess weitgehend zu automatisieren. Erweitern Sie Ihre Applikation um KI-basierende Suche und Workflows. Schliessen Sie Ihre Arbeit ab und laden Sie diese als PDF hoch. Die Arbeit enthält die URL auf das Git-Repository Ihrer Lösung.

**Termin:** Bis zwei Wochen nach der letzten PVA — **konkret: 2026-07-04, 24:00**.

**Reflexion & Auswertung:** Reflexion über die anschliessende Bewertung der Arbeit und der Lösung.

> **FlowHub-Stack-Mapping (.NET + GitHub statt Quarkus + GitLab):**
> - Containerisierung → Multi-Stage Dockerfile (Build: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`, Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`, non-root, siehe `CLAUDE.md` § Docker)
> - CI/CD → **GitHub Actions** (Repo liegt auf `github.com/freaxnx01/FlowHub-CAS-AISE`); GitLab-Agent-Plattform/-Runner als Lerninhalt zur Kenntnis, Implementierung in GitHub
> - Monitoring/Observability → **OpenTelemetry** (Traces, Metrics, Logs) + Prometheus + Grafana (`/metrics` Endpoint ist im Health-Plan); strukturiertes Logging mit Serilog → stdout (12-Factor XI)
> - KI-gestützte Apps "mit Quarkus" → mit `Microsoft.Extensions.AI` / Semantic Kernel; KI-Suche via Vector-DB-Provider (z.B. pgvector auf bestehender PostgreSQL aus Block 4)
> - Kubernetes → Manifests / Helm-Chart (oder lediglich Docker-Compose, falls K8s-Aufwand sprengt — dann begründen)
> - Authentik / OIDC → finaler Schritt (siehe ADR 0001 Plan)

---

## Bewertungskriterien (Final / Block 5)

⚠️ **Hier zählt's:** Alle 18 Items aus [[Bewertungskriterien]] müssen Abgabe-fähig sein. Punkte in Klammern = Max-Score.

> **Rubrik-Update Juni 2026:** Das Programmierkriterium ist jetzt framework-neutral (nicht mehr Quarkus/Jakarta-EE-spezifisch) und für FlowHub (.NET) **direkt erfüllt** — kein ausgeklammertes Item mehr, alle 100 Punkte erreichbar (siehe `vault/Organisation/Bewertungskriterien.md` + `docs/spec/modern-app-concepts.md`).

### Spezifikation

- [x] **Use Cases (5)** — finale, vollständige Use-Case-Liste der gesamten Applikation (alle Blöcke konsolidiert)
- [x] **NfA SMART (5)** — vollständige NfA-Liste: Performance, Verfügbarkeit, Sicherheit, Skalierbarkeit, Betrieb (Logs, Monitoring), Deployment-NfAs (Build-Zeit, Image-Grösse)
- [x] **Solution Vision (5)** — finale Vision: Modular Monolith + Async-Pipeline + KI-Services + PostgreSQL + Container-Deployment + Observability

### Entwurf

- [x] **Lösungsansatz & Architektur textuell + bildlich (7)** — finale Architektur-Doku mit allen ADRs (0001–0006+), C4-Diagramm (Context, Container, Component)
- [x] **Struktur / Verhalten / Interaktion (7)** — vollständige Sicht: Modul-Struktur, Hot-Path-Sequenzen, Interaktion mit Channels/Integrationen
- [x] **DB-Modell vollständig (3)** — finales ER-Diagramm + Indizes + ggf. Vector-Spalten für KI-Suche

### Programmierung

- [x] **Code lesbar/dokumentiert/strukturiert (7)** — alle Module sauber, README pro Hauptprojekt, Inline-Doku wo Why nicht obvious
- [x] ~~Quarkus / Jakarta EE~~ — N/A (Stack: .NET 10)
- [x] **Erkenntnisse dokumentiert (3)** — `docs/insights/` mit Block-1 bis -5 Erkenntnissen
- [x] **Source in Git (2)** — alles auf `main` gepusht; Tag `v0.1.0` für die Abgabe-Version (Directory.Build.props ships 0.1.0; Bewertungskriterien-Item ist 0/2 binär — erreicht)

### Validierung

- [x] **Abnahmekriterien (5)** — vollständige Liste über alle Use Cases, im Submission-PDF aufgeführt
- [x] **Test-Strategie (5)** — finales `docs/spec/testing-strategy.md` (im Repo unter `docs/spec/` einsortiert, nicht direkt `docs/`): Unit (xUnit/FluentAssertions/NSubstitute), Component (bUnit), Integration (ASP.NET Mvc.Testing + Testcontainers), E2E (Playwright), MassTransit Test Harness
- [x] **Unit-Tests (3)** — Coverage über alle Module
- [x] **Test-Ergebnisse dokumentiert (3)** — CI-Run-Ergebnisse, Coverage-Reports, im Submission-PDF zitiert

### KI, Sub-Systeme & Reflexion

- [x] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ höchstgewichtetes Kriterium — finaler `docs/ai-usage.md`: alle eingesetzten Tools (Claude Code, Copilot, ChatGPT, Cursor, …), pro Block welche Aufgaben, Prompt-Strategien, generiert-vs-handgeschrieben-Quote, beobachtete Fehlerklassen
- [x] **Intelligente Services mit KI (6)** — Capture-Klassifikation + KI-basierte Suche (Embeddings + Vector-Search) + ggf. KI-gestützte Workflows
- [x] **Sub-Systeme als unabhängige Container (5)** — finale Compose- und/oder K8s-Manifests: FlowHub.Web, FlowHub.Api, PostgreSQL, RabbitMQ, ggf. Authentik, Prometheus/Grafana, alle als getrennte Container; CI baut + pusht Images
- [x] **KI-Reflexion / Fazit (7)** — finales Kapitel im Submission-PDF: was hat KI im gesamten Projekt geleistet, wo waren Grenzen, persönliche lessons learned, Empfehlungen

---

## TODO

### Containerisierung

- [x] Multi-Stage `Dockerfile` für `FlowHub.Web` (build → publish → runtime, non-root user `appuser`)
- [x] ~~Multi-Stage `Dockerfile` für `FlowHub.Api`~~ — **N/A:** `FlowHub.Api` ist ein Library-Projekt (Minimal-API-Endpoint-Definitionen), kein eigenständig gehosteter Service. Endpoints werden via `MapFlowHubApi()` in `FlowHub.Web` registriert; das Web-Image enthält die Api.
- [x] `.dockerignore` sauber (bin/, obj/, node_modules, .git/, …)
- [x] EF-Migrations-Container (separater Init-Container — 12-Factor XII)
- [x] Image-Grösse minimieren (`-alpine` Base, AOT/trim wenn praktikabel)
- [x] Image-Tags: `<sha>` + `latest` auf main + `vX.Y.Z` auf Release-Tags

### Compose / Kubernetes

- [x] `docker-compose.yml` (Production-orientiert) + `docker-compose.override.yml` (Dev)
- [x] Services: web, api, postgres, rabbitmq, prometheus, grafana, ggf. authentik
- [x] Volumes für Postgres-Daten, RabbitMQ, Grafana-Dashboards
- [x] Healthchecks (Compose) gegen `/health/live` (und `/health/ready` deferred) — Compose-Healthcheck nutzt `/health/live`; `/health/ready` mit DB+RabbitMQ-Probes ist bewusst zurückgestellt (Liveness deckt die Block-5-Smoke-Anforderung; Readiness-Probes brauchen Kubernetes-Kontext, der per PE-4 für später).
- [x] ~~(Optional) Helm-Chart oder Kustomize-Manifests für Kubernetes~~ — **bewusst out of scope** (Vault-Wortlaut: "falls Zeitbudget reicht; sonst bewusst out of scope dokumentieren"). Docker Compose erfüllt den Block-5-Deployment-Anspruch; K8s-Migration ist explizit nach-CAS (siehe PE-4).

### CI/CD (GitHub Actions)

- [x] Workflow `ci.yml`: restore → build → test → coverage upload (Hauptbranch + PRs)
- [x] Workflow `release.yml`: bei Tag `v*` → Docker-Images bauen + zu GHCR pushen + Release Notes via `git-cliff` generieren
- [x] Workflow `migrations.yml`: separater Job, der Migrations-Bundle generiert
- [x] Branch-Protection auf `main` (PRs grün)
- [x] Doku in `docs/ci-cd.md`

### KI-Suche & Workflows (Auftrag-Erweiterung)

- [x] ADR 0006 — KI-Suche (Embeddings-Provider, pgvector vs. eigener Vector-Store, Index-Strategie)
- [x] Embedding-Pipeline: Capture (Title + Body) → Embedding → Persistenz
- [x] Such-Endpoint: `GET /api/v1/captures/search?q=…` (vector-only) — Hybrid-Match (full-text + PostgreSQL `tsvector` + Vector) deferred; aktueller Endpoint nutzt pgvector-Cosine, FluentAssertions-Integration-Tests + `tests/FlowHub.Api.IntegrationTests/SearchEndpointTests.cs`.
- [x] KI-Workflow-Beispiel: **automatisches Skill-Routing via Klassifikation** (`AiClassifier` → `MatchedSkill` → `SkillRoutingConsumer` → `ISkillIntegration`) ist der eingebaute KI-Workflow. Embedding-Cluster-Routing und LLM-Tag-Suggestions sind in docs/project/ROADMAP.md ("Capture Enrichment") als post-CAS-Erweiterung skizziert.

### Monitoring / Observability

- [x] OpenTelemetry: Traces + Metrics (Logs via Serilog → stdout, 12-Factor XI) — `Microsoft.Extensions.AI.UseOpenTelemetry()` instrumentiert MEAI; ASP.NET Core + Runtime-Instrumentation in `Program.cs`; Logs-Pipeline durch Serilog statt OTel-Log-Records (gleicher Effekt im Container-Kontext: alles auf stdout, Collector sammelt strukturiert).
- [x] ~~OTLP-Exporter konfigurierbar via `OTEL_EXPORTER_OTLP_ENDPOINT`~~ — **deferred:** aktueller Export-Pfad ist Prometheus-Scrape (`/metrics`) + Grafana-Dashboard; OTLP-Push wäre Doppel-Pfad. Add-on ist trivial (`AddOtlpExporter()` + Env-Var) sobald ein OTel-Collector gebraucht wird; aktuelle Topology hat keinen.
- [x] Prometheus-Endpoint `/metrics` — `MapPrometheusScrapingEndpoint().AllowAnonymous()` in `Program.cs`; durch `make smoke-prod` Schritt [4/6] verifiziert (`dotnet_*` + `http_*` Series).
- [x] Grafana-Dashboard JSON eingecheckt (`docs/monitoring/grafana/flowhub-dashboard.json`)
- [x] Strukturiertes Logging mit Serilog → stdout (12-Factor XI); Korrelations-IDs via OpenTelemetry W3C Trace-Context (Activity.Current trägt TraceId/SpanId quer durch alle MEAI-/EF-/HTTP-Spans). Pro-Request-Serilog-Enrichment ist deferred — der OTel-TraceId reicht für Cross-Span-Korrelation im Grafana-Dashboard.
- [x] Healthchecks `/health/live` (DB- und MQ-Probes deferred) — Live-Check liefert nur Process-Liveness, kein Readiness; per Block-5-Smoke deckt das den Deployment-Anspruch ("Container started + Prozess lebt"). DB-/MQ-Probes wären Block-6-Stretch im Kubernetes-Kontext.

### Authentifizierung (Block-5-Stretch falls Zeit)

- [x] OIDC gegen Authentik (Homelab SSO) statt Dev-Auth-Handler
- [x] Client-Registration in Authentik dokumentieren
- [x] Tests für Auth-Flow — `DemoAuthHandler` ist via Web.ComponentTests abgedeckt (Auth-bypass auto-signs für Tests); voller OIDC-End-to-End-Test gegen Authentik bewusst deferred (Authentik läuft homelab-only, kein public-CI-zugänglicher IdP).

### Submission

- [x] **Projektarbeit-PDF schreiben** — Inhalte: Vision, Use Cases, NfAs, Architektur (alle ADRs), DB-Modell, Programmierung-Highlights, Test-Strategie + Resultate, KI-Nutzung + Reflexion, Repo-URL
- [x] Repo-URL prominent ins PDF: `github.com/freaxnx01/FlowHub-CAS-AISE`
- [x] Tag `v0.1.0` setzen + zu Release pushen, CHANGELOG-Eintrag final — Tag `v0.1.0` (matches `Directory.Build.props` `<Version>0.1.0</Version>`) gepusht, `release.yml` grün, GitHub-Release veröffentlicht: <https://github.com/freaxnx01/FlowHub-CAS-AISE/releases/tag/v0.1.0>
- [ ] PDF auf Moodle hochladen vor **2026-07-04 24:00**

### Spezifikation & Doku konsolidieren

- [x] Use-Case-Liste final
- [x] NfA-Liste final (SMART)
- [x] ADR-Index in `docs/adr/README.md`
- [x] `docs/ai-usage.md` final (höchstgewichtetes Kriterium!)
- [x] `docs/insights/` für alle 5 Blöcke vorhanden
- [x] `docs/spec/testing-strategy.md` final (Pfad unter `docs/spec/`, nicht direkt `docs/`)
- [x] CHANGELOG `[0.1.0]` Section (entspricht `<Version>0.1.0</Version>` aus `Directory.Build.props`; nicht `v1.0.0` wie ursprünglich geplant)

### 🚫 Out of Scope (auch in Block 5)

- Multi-Tenancy / RBAC über Single-User hinaus
- Production-Backup-Automation jenseits von Dokumentation
- Mobile App / Native Clients
- Skill-Marketplace / Plugin-Loader

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- Block 5 Vorbereitung: [[05 Deployment - a) Vorbereitung]]
- Block 4 Nachbereitung: [[04 Persitence - c) Nachbereitung]]
- Bewertungskriterien: [[Bewertungskriterien]]
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- 12-Factor: siehe `CLAUDE.md` § "12-Factor Compliance"
