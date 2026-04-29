---
tags:
  - claude-generated
updated: 2026-04-29
---

# Block 5 — Deployment & Abgabe Projektarbeit · Nachbereitung

**Phase budget:** 22 h
**PVA war:** 2026-06-20
**Abgabe-Deadline:** **Montag, 2026-07-06, 00:00** (PDF-Upload mit Repo-URL)

> **Final Block.** Hier wird die Bewertungs-Rubrik aus [[Bewertungskriterien]] **vollständig** geprüft — alle 5 Buckets müssen Abgabe-fähig sein. Was im jeweiligen Block-Nachbereitung nicht erledigt wurde, gehört hier nachgezogen.

## Lernziel

- Ich bin fähig, meine Applikation zu containerisieren und in Docker und Kubernetes zu betreiben.
- Ich kann GitHub und Copilot sowie die GitLab-Agent-Plattform einsetzen, um CI/CD-Pipelines aufzusetzen und den Deployment-Prozess zu automatisieren.
- Ich bin fähig, entsprechendes Monitoring und Observation aufzusetzen, Systeme zu überwachen und zu optimieren.
- Ich bin in der Lage, mit Quarkus KI-gestützte Applikationen zu bauen.

## Auftrag (Moodle)

In der letzten Nachbearbeitungsphase geht es nun darum, die Lösung zu containerisieren und für den Betrieb zu verteilen. Nutzen Sie die Möglichkeit Ihrer Git-Host-Lösung, um den Deployment-Prozess weitgehend zu automatisieren. Erweitern Sie Ihre Applikation um KI-basierende Suche und Workflows. Schliessen Sie Ihre Arbeit ab und laden Sie diese als PDF hoch. Die Arbeit enthält die URL auf das Git-Repository Ihrer Lösung.

**Termin:** Bis zwei Wochen nach der letzten PVA — **konkret: 2026-07-06, 00:00**.

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

> Quarkus/Jakarta-EE-Item ist für FlowHub (.NET) **nicht relevant** — bewusst ausgeklammert.

### Spezifikation

- [ ] **Use Cases (5)** — finale, vollständige Use-Case-Liste der gesamten Applikation (alle Blöcke konsolidiert)
- [ ] **NfA SMART (5)** — vollständige NfA-Liste: Performance, Verfügbarkeit, Sicherheit, Skalierbarkeit, Betrieb (Logs, Monitoring), Deployment-NfAs (Build-Zeit, Image-Grösse)
- [ ] **Solution Vision (5)** — finale Vision: Modular Monolith + Async-Pipeline + KI-Services + PostgreSQL + Container-Deployment + Observability

### Entwurf

- [ ] **Lösungsansatz & Architektur textuell + bildlich (7)** — finale Architektur-Doku mit allen ADRs (0001–0006+), C4-Diagramm (Context, Container, Component)
- [ ] **Struktur / Verhalten / Interaktion (7)** — vollständige Sicht: Modul-Struktur, Hot-Path-Sequenzen, Interaktion mit Channels/Integrationen
- [ ] **DB-Modell vollständig (3)** — finales ER-Diagramm + Indizes + ggf. Vector-Spalten für KI-Suche

### Programmierung

- [ ] **Code lesbar/dokumentiert/strukturiert (7)** — alle Module sauber, README pro Hauptprojekt, Inline-Doku wo Why nicht obvious
- [ ] ~~Quarkus / Jakarta EE~~ — N/A (Stack: .NET 10)
- [ ] **Erkenntnisse dokumentiert (3)** — `docs/insights/` mit Block-1 bis -5 Erkenntnissen
- [ ] **Source in Git (2)** — alles auf `main` gepusht; Tag `v1.0.0` für die Abgabe-Version

### Validierung

- [ ] **Abnahmekriterien (5)** — vollständige Liste über alle Use Cases, im Submission-PDF aufgeführt
- [ ] **Test-Strategie (5)** — finales `docs/test-strategy.md`: Unit (xUnit/FluentAssertions/NSubstitute), Component (bUnit), Integration (ASP.NET Mvc.Testing + Testcontainers), E2E (Playwright), MassTransit Test Harness
- [ ] **Unit-Tests (3)** — Coverage über alle Module
- [ ] **Test-Ergebnisse dokumentiert (3)** — CI-Run-Ergebnisse, Coverage-Reports, im Submission-PDF zitiert

### KI, Sub-Systeme & Reflexion

- [ ] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ höchstgewichtetes Kriterium — finaler `docs/ai-usage.md`: alle eingesetzten Tools (Claude Code, Copilot, ChatGPT, Cursor, …), pro Block welche Aufgaben, Prompt-Strategien, generiert-vs-handgeschrieben-Quote, beobachtete Fehlerklassen
- [ ] **Intelligente Services mit KI (6)** — Capture-Klassifikation + KI-basierte Suche (Embeddings + Vector-Search) + ggf. KI-gestützte Workflows
- [ ] **Sub-Systeme als unabhängige Container (5)** — finale Compose- und/oder K8s-Manifests: FlowHub.Web, FlowHub.Api, PostgreSQL, RabbitMQ, ggf. Authentik, Prometheus/Grafana, alle als getrennte Container; CI baut + pusht Images
- [ ] **KI-Reflexion / Fazit (7)** — finales Kapitel im Submission-PDF: was hat KI im gesamten Projekt geleistet, wo waren Grenzen, persönliche lessons learned, Empfehlungen

---

## TODO

### Containerisierung

- [ ] Multi-Stage `Dockerfile` für `FlowHub.Web` (build → publish → runtime, non-root user `appuser`)
- [ ] Multi-Stage `Dockerfile` für `FlowHub.Api`
- [ ] `.dockerignore` sauber (bin/, obj/, node_modules, .git/, …)
- [ ] EF-Migrations-Container (separater Init-Container — 12-Factor XII)
- [ ] Image-Grösse minimieren (`-alpine` Base, AOT/trim wenn praktikabel)
- [ ] Image-Tags: `<sha>` + `latest` auf main + `vX.Y.Z` auf Release-Tags

### Compose / Kubernetes

- [ ] `docker-compose.yml` (Production-orientiert) + `docker-compose.override.yml` (Dev)
- [ ] Services: web, api, postgres, rabbitmq, prometheus, grafana, ggf. authentik
- [ ] Volumes für Postgres-Daten, RabbitMQ, Grafana-Dashboards
- [ ] Healthchecks (Compose) gegen `/health/live` und `/health/ready`
- [ ] (Optional) Helm-Chart oder Kustomize-Manifests für Kubernetes — falls Zeitbudget reicht; sonst bewusst out of scope dokumentieren

### CI/CD (GitHub Actions)

- [ ] Workflow `ci.yml`: restore → build → test → coverage upload (Hauptbranch + PRs)
- [ ] Workflow `release.yml`: bei Tag `v*` → Docker-Images bauen + zu GHCR pushen + Release Notes via `git-cliff` generieren
- [ ] Workflow `migrations.yml`: separater Job, der Migrations-Bundle generiert
- [ ] Branch-Protection auf `main` (PRs grün)
- [ ] Doku in `docs/ci-cd.md`

### KI-Suche & Workflows (Auftrag-Erweiterung)

- [ ] ADR 0006 — KI-Suche (Embeddings-Provider, pgvector vs. eigener Vector-Store, Index-Strategie)
- [ ] Embedding-Pipeline: Capture (Title + Body) → Embedding → Persistenz
- [ ] Such-Endpoint: `GET /api/captures/search?q=…` mit Hybrid-Match (full-text + vector)
- [ ] KI-Workflow-Beispiel: automatisches Skill-Routing basierend auf Embedding-Cluster, oder LLM-gestützte Tag-Vorschläge

### Monitoring / Observability

- [ ] OpenTelemetry: Traces + Metrics + Logs Pipeline
- [ ] OTLP-Exporter konfigurierbar via `OTEL_EXPORTER_OTLP_ENDPOINT`
- [ ] Prometheus-Endpoint `/metrics`
- [ ] Grafana-Dashboard JSON eingecheckt (`docs/monitoring/grafana/`)
- [ ] Strukturiertes Logging mit Serilog → stdout (12-Factor XI), Korrelations-IDs
- [ ] Healthchecks `/health/live` und `/health/ready` mit DB- und MQ-Probes

### Authentifizierung (Block-5-Stretch falls Zeit)

- [ ] OIDC gegen Authentik (Homelab SSO) statt Dev-Auth-Handler
- [ ] Client-Registration in Authentik dokumentieren
- [ ] Tests für Auth-Flow

### Submission

- [ ] **Projektarbeit-PDF schreiben** — Inhalte: Vision, Use Cases, NfAs, Architektur (alle ADRs), DB-Modell, Programmierung-Highlights, Test-Strategie + Resultate, KI-Nutzung + Reflexion, Repo-URL
- [ ] Repo-URL prominent ins PDF: `github.com/freaxnx01/FlowHub-CAS-AISE`
- [ ] Tag `v1.0.0` setzen + zu Release pushen, CHANGELOG-Eintrag final
- [ ] PDF auf Moodle hochladen vor **2026-07-06 00:00**

### Spezifikation & Doku konsolidieren

- [ ] Use-Case-Liste final
- [ ] NfA-Liste final (SMART)
- [ ] ADR-Index in `docs/adr/README.md`
- [ ] `docs/ai-usage.md` final (höchstgewichtetes Kriterium!)
- [ ] `docs/insights/` für alle 5 Blöcke vorhanden
- [ ] `docs/test-strategy.md` final
- [ ] CHANGELOG `[v1.0.0]` Section

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
