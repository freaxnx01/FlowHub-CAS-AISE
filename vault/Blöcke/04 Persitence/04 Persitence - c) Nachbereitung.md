---
tags:
  - claude-generated
  - claude-updated
updated: 2026-05-04
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

> Quarkus/Jakarta-EE-Item ist für FlowHub (.NET-Stack) **nicht relevant** — bewusst ausgeklammert.

### Spezifikation

- [ ] **Use Cases (5)** — datenseitige Use Cases benannt: Capture-CRUD, Such-/Filter-Abfragen über Lifecycle/Channel/Tags, Skill-Run-Historie, Integration-Health-Verlauf
- [ ] **NfA SMART (5)** — Persistenz-NfAs: Query-Latenz, Index-Anforderungen, Datenvolumen-Annahmen, Backup/Restore-Ziele, Migrations-Strategie (Zero-Downtime?)
- [ ] **Solution Vision (5)** — aktualisiert um Persistenzschicht: PostgreSQL via EF Core, Migrations-First-Ansatz, Repository-Abstraktion-Entscheid

### Entwurf

- [ ] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0005 für Persistenz: Provider-Wahl (PostgreSQL), ORM-Wahl (EF Core), Repository-vs-DbContext-direkt, Migrations-Workflow
- [ ] **Struktur / Verhalten / Interaktion (7)**:
  - Struktur: `FlowHub.Persistence`-Layer, `DbContext`, Entities, Migrations, Repositories
  - Verhalten: CRUD-Flows, Migrations-Run, Query-Plans für Hot-Path-Queries
  - Interaktion: Aufrufkette `FlowHub.Web/.Api → Application Service → Repository → DbContext → DB`
- [ ] **DB-Modell vollständig (3)** ⭐ in diesem Block der Schwerpunkt — ER-Diagramm mit Entitäten (Capture, Skill, SkillRun, Channel, Integration, IntegrationHealthSample, Tag, …), Beziehungen, Indizes, Constraints. Antizipation zukünftiger Bedürfnisse begründen

### Programmierung

- [ ] **Code lesbar/dokumentiert/strukturiert (7)** — `FlowHub.Persistence` als eigenes Projekt, sauber getrennt von Domain (`FlowHub.Core`)
- [ ] ~~Quarkus / Jakarta EE / moderne Java-Konzepte~~ — N/A (Stack: .NET 10)
- [ ] **Erkenntnisse dokumentiert (3)** — Migrations-Strategie, Performance-Beobachtungen, EF-Core-Pitfalls (Tracking, Includes, N+1) in `docs/insights/block-4.md`
- [ ] **Source in Git (2)** — alle Block-4-Commits gepusht

### Validierung

- [ ] **Abnahmekriterien (5)** — pro Use Case Datenflüsse spezifiziert, inkl. Edge Cases (leere Resultate, Konkurrenz, Migrations-Rollback)
- [ ] **Test-Strategie (5)** — Erweiterung von `docs/test-strategy.md`: Repository-Tests gegen Testcontainers PostgreSQL; Integration-Tests gegen reale DB; Migrations-Tests
- [ ] **Unit-Tests (3)** — Repository-Implementierungen, Query-Builder, Migrations-Smoketest
- [ ] **Test-Ergebnisse dokumentiert (3)** — `dotnet test` voll grün; Counts + Coverage in CHANGELOG `[Unreleased]`

### KI, Sub-Systeme & Reflexion

- [ ] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ — wie wurde KI im Datenmodell-Entwurf, in Migrations-Generierung, in Query-Optimierung eingesetzt? Doku-Update in `docs/ai-usage.md`
- [ ] **Intelligente Services mit KI (6)** — KI-Klassifikator nutzt jetzt persistente Daten (statt Stubs); ggf. Embedding-Speicherung für Suche (Vorbereitung Block 5)
- [ ] **Sub-Systeme als Container (5)** — PostgreSQL als eigener Container; FlowHub.Web/.Api gegen DB-Container deploybar; Compose-Profil aktualisiert (auch Migrations als separater Init-Container — siehe 12-Factor XII in `CLAUDE.md`)
- [ ] **KI-Reflexion / Fazit (7)** — Block-4-Reflexion: KI bei Datenmodellierung — Stärken (Boilerplate, Migrations) und Schwächen (Schema-Antizipation, Performance-Blindheit)

---

## TODO

### Datenmodell

- [ ] ER-Diagramm der FlowHub-Domäne (`docs/design/db/er.md` oder Mermaid in ADR 0005)
- [ ] Entity-Klassen in `FlowHub.Core` (Capture, Skill, SkillRun, Channel, Integration, IntegrationHealthSample, Tag, Capture-Tag-Join)
- [ ] Indizes / Constraints / Unique-Keys spezifizieren
- [ ] Soft-Delete-Strategie entscheiden (Lifecycle vs. echte Löschung)
- [ ] Audit-Felder (`CreatedAt`, `UpdatedAt`, `CreatedBy`, …) konsequent

### Architektur & Entscheide

- [ ] ADR 0005 — Persistence (Provider, ORM, Repository-Pattern-Entscheid, Migrations-Workflow)
- [x] Stack-Mapping-Notiz: Hibernate/Panache/Jakarta Data → EF Core (kurze Doku, warum, was äquivalent) — landed im Auftrag-Intro oben (FlowHub-Stack-Mapping)

### Implementierung (`source/FlowHub.Persistence/`)

- [x] Projekt scaffolden, in `FlowHub.slnx` registrieren — Beta MVP (`source/FlowHub.Persistence/FlowHub.Persistence.csproj`)
- [x] `FlowHubDbContext` mit `DbSet<T>` für alle Entities — **partial**: Beta MVP nur `Captures`-DbSet; weitere Entities (Skill, SkillRun, Channel, Integration, Tag) in Block 4
- [ ] `EntityTypeConfiguration<T>` pro Entity (Fluent API statt Annotations) — Beta nutzt inline `OnModelCreating`; Refactor zu separaten Config-Klassen in Block 4
- [ ] PostgreSQL-Connection via `ConnectionStrings__Default` ENV — Beta nutzt SQLite; PostgreSQL-Switch in Block 4
- [x] Initial-Migration `0001_Initial` generieren (`dotnet ef migrations add`) — Beta MVP (`Migrations/20260504120638_Initial.cs`)
- [ ] Repository-Interfaces in `FlowHub.Core`, Implementierungen in `FlowHub.Persistence` — Beta MVP nutzt `ICaptureService` direkt gegen `DbContext`; Repository-Layer-Entscheid in ADR 0005
- [x] DI-Registration als `IServiceCollection`-Extension (`AddFlowHubPersistence(connectionString)`) — Beta MVP (`AddFlowHubPersistence(IConfiguration)` mit `ConnectionStrings:Default`-Lookup)

### Dynamische Abfragen

- [ ] LINQ + Expression Trees als "Criteria-API"-Äquivalent — Beispiel: dynamischer Capture-Filter (Lifecycle, Channel, Tags, Search) — Beta MVP nur statische Filter; voller dynamischer Filter inkl. Tag/Search in Block 4
- [x] Pagination-Helper (Skip/Take + Cursor-basiert für lange Listen) — Beta MVP (`EfCaptureService.ListAsync` mit `CaptureCursor` keyset-pagination, `limit+1`-Probe)
- [ ] N+1-Problem aktiv vermeiden (`Include` / Projektion / Read-Models)

### Migrations & Deployment-Vorbereitung

- [ ] Migrations laufen separat (nicht in `app.Run()`) — 12-Factor XII
- [ ] `make db-up` / `make db-migrate` Targets oder Skript
- [ ] Docker-Compose-Snippet für PostgreSQL (lokal testen)
- [ ] EF-Migrations-Bundle / SQL-Script-Generierung getestet (für Block 5 Production-Deployment)

### Stub-Replacement

- [ ] Bogus-basierte Stubs aus Block 2 durch DB-gestützte Implementierungen ersetzen — *aber* Bogus-Seed für Dev-Mode behalten (`appsettings.Development.json`-Flag) — Beta MVP hat Bogus-Stubs entfernt; deterministisches Seeding nur im `IntegrationTestFactory` für API-Tests; Dev-Mode-Seed offen
- [x] `ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService` jetzt gegen Repository statt In-Memory — **partial**: nur `ICaptureService` swap (Beta MVP); `ISkillRegistry` + `IIntegrationHealthService` bleiben Stubs (per Beta-Spec D3 — orthogonal zu Architektur-Validierung)

### Tests

- [ ] Test-Strategie ergänzen (`docs/test-strategy.md`)
- [ ] Repository-Tests (Testcontainers PostgreSQL bevorzugt vor SQLite-In-Memory wegen Provider-Quirks)
- [ ] Integration-Tests für API-Endpoints gegen reale DB
- [ ] Migrations-Smoketest (auf leerer DB rauf, runter, rauf)

### Spezifikation & Doku

- [x] CHANGELOG `[Unreleased]` mit Block-4-Deliverables — **partial**: Beta-MVP-Persistence-Eintrag landed (commits `d7b7af0..c5340e9`); reine Block-4-Erweiterungen (PostgreSQL, Repository-Pattern, ADR 0005) kommen nach
- [ ] Use-Case-Liste um datenseitige Use Cases erweitern
- [ ] Performance-NfAs SMART formulieren (mit Mess-Methodik)
- [ ] `docs/insights/block-4.md` — Erkenntnisse Datenmodellierung mit KI

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
- Bewertungskriterien: [[Bewertungskriterien]]
