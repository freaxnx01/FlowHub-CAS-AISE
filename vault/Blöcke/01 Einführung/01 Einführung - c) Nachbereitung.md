---
tags:
  - claude-updated
updated: 2026-05-17
---

# Block 1 — Einführung · Nachbereitung

**Phase budget:** 28 h
**PVA war:** 2026-02-21
**Nächste PVA:** 2026-03-21

## Lernziel

- Ein KI-gestütztes Software-Engineering-Projekt von Grund auf aufsetzen und die tragenden Architekturentscheidungen begründen.
- Architekturstile (Monolith, modularer Monolith, Microservices) für ein Szenario gegeneinander abwägen und die Wahl begründen.
- Die KI-Werkzeugkette (Claude Code, IDE-Integration, eigene Skills) für ein neues Repository einrichten.

## Auftrag (Zusammenfassung)

- Sichtung der Materialien und Ergebnisse der Präsenzveranstaltung 1
- Projektbeschreibung verfassen (Idee, Solution Vision, fokussierte Sub-Systeme, Beispiel-Eingabedaten)
- Architektur-Optionen evaluieren und eine begründete Entscheidung dokumentieren
- Initiales Repository-Setup mit dem gewählten Stack

> **FlowHub-Kontext:**
> - **Stack-Entscheid** für .NET 10 + Blazor Web App (statt Quarkus / Jakarta EE) — in der Projektbeschreibung dokumentiert (`vault/Projektarbeit/Dev.md`).
> - **ADR 0001** (Frontend Render Mode & Architecture, Accepted 2026-02-15) und **ADR 0002** (Service Architecture & Async Communication, Accepted 2026-03-01) halten die beiden tragenden Architekturentscheidungen fest.

---

## Bewertungskriterien (Block 1)

Snapshot am Ende der Block-1-Phase (~2026-03-21) gegen die offizielle Moodle-Rubrik aus [[Bewertungskriterien]]. Punkteangaben in Klammern zeigen Max-Score. **Block 1 ist Foundational** — viele Items sind hier erwartungsgemäss noch nicht adressiert; die Rubrik wird kumulativ erst bei Block 5 abschliessend bewertet.

> **Sektion nachträglich ergänzt (2026-05-17)** — damit das Pattern aus Block 3 / 4 / 5 auch in Block 1 / 2 vorhanden ist.

> **Rubrik-Update (Juni 2026):** Programmierkriterium ist framework-neutral → für .NET direkt erfüllt, alle 100 Punkte erreichbar (`docs/spec/modern-app-concepts.md`).

### Spezifikation

- [-] **Use Cases & fachliche Anforderungen (5)** — informelle Liste in `vault/Projektarbeit/Idee FlowHub.md` (Capture-Idee, Channel-Konzept); formales UC-Dokument folgt in Block 2/3
- [x] **NfA SMART (5)** — nicht in Block 1; ✅ geliefert Block 3 (NF-09..NF-13 in `docs/spec/use-cases.md`)
- [x] **Solution Vision (5)** — `vault/Projektarbeit/Idee FlowHub.md` + Projektbeschreibung-Draft + ADR 0001/0002 (Vision der modularen Lösung)

### Entwurf

- [-] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0001 + ADR 0002 textuell vorhanden; Diagramme (C4, Sequence) folgen ab Block 2/3
- [-] **Struktur / Verhalten / Interaktion (7)** — Struktur über ADR 0002 Modul-Split angerissen; Verhalten/Interaktion noch nicht modelliert
- [x] **DB-Modell (3)** — out of scope Block 1; ✅ geliefert Block 4 (ADR 0005, `docs/design/db/er.md`)

### Programmierung

- [-] **Code lesbar / strukturiert (7)** — Solution-Skelett `FlowHub.slnx` mit `FlowHub.Core` Domain-Typen (`Capture`, `LifecycleStage`, `ChannelKind`); strukturell sauber, aber kaum Code-Volumen
- [x] **Konzepte des gewählten Frameworks sachgerecht eingesetzt (max. 10)** — framework-neutral (Rubrik-Update Juni 2026); für .NET 10 / ASP.NET Core direkt erfüllt: DI, REST-Schnittstellen, Konfiguration, Fehlerbehandlung (`docs/spec/modern-app-concepts.md`)
- [-] **Erkenntnisse dokumentiert (3)** — Reflexion-Sektion in dieser Datei + ADR 0001/0002 §"Alternatives considered"; ausführliche Insights folgen ab `docs/insights/block-1.md`
- [x] **Source in Git (2)** — `github.com/freaxnx01/FlowHub-CAS-AISE` aufgesetzt, Initial-Commits gepusht

### Validierung

- [x] **Abnahmekriterien (5)** — out of scope Block 1; ✅ geliefert Block 3 (UC-08..UC-11 mit Acceptance in `docs/spec/use-cases.md`)
- [x] **Test-Strategie + Technologien (5)** — out of scope Block 1; ✅ geliefert ab Block 2 (`docs/spec/testing-strategy.md`)
- [x] **Unit-Tests (3)** — kein Test-Projekt in Block 1; ✅ geliefert Block 2 (bUnit, 31/31 grün)
- [x] **Test-Ergebnisse dokumentiert (3)** — out of scope Block 1; ✅ geliefert ab Block 2 (CHANGELOG `[Unreleased]` + `dotnet test` Counts)

### KI, Sub-Systeme & Reflexion

- [-] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ — Reflexion-Sektion dieser Datei listet eingesetzte Tools (Claude Code, Brainstorming-Skill) und konkrete KI-Korrekturen; konsolidierte Doku in `docs/ai-usage.md` folgt ab Block 3
- [x] **Intelligente Services mit KI (6)** — out of scope Block 1; ✅ geliefert Block 3 (`FlowHub.AI`: KeywordClassifier + AiClassifier)
- [x] **Sub-Systeme als Container (5)** — out of scope Block 1; ✅ geliefert Block 5 (docker-compose Stack)
- [-] **KI-Reflexion / Fazit (7)** — Block-1-Reflexion-Sektion enthält Tooling-Erfahrungen; finales Fazit gehört in `vault/Projektarbeit/Learnings.md` und das Submission-PDF (Block 5)

---

## TODO

### ✅ Done

- [x] Sichten Materialien und Ergebnisse der Präsenzveranstaltung 1
- [x] Projektbeschreibung ändern auf .NET, Upload
- [x] Architekturen erklären lassen und entscheiden, dokumentieren — ADR 0001 (Frontend / Modular Monolith) + ADR 0002 (Service-Architektur)
- [x] Focus on a few Services / Categories: Vikunja, Wallabag — `vault/Projektarbeit/External Services.md` + `FlowHub.Integrations/` Placeholder
- [x] Sample input data: Books, Movies, … — Bogus-Stubs in `source/FlowHub.Web/Stubs/` (umgesetzt in Block 2)
- [x] Solution-Skelett `FlowHub.slnx` mit `FlowHub.Core` Domain-Modell (`Capture`, `LifecycleStage`, `ChannelKind`, `ICaptureService`, `ICaptureRepository`)
- [x] `Directory.Build.props` (Warnings-as-Errors, embedded PDB) + `Directory.Packages.props` (zentrale Paketverwaltung) eingerichtet
- [x] `global.json` SDK-Pin auf .NET 10 für reproduzierbare Builds über Maschinen hinweg

---

## Reflexion

### Was hat geklappt

- **Stack-Entscheid früh und begründet.** Die Entscheidung gegen Quarkus / Jakarta EE und für .NET 10 wurde vor jeglichem Code getroffen und in der Projektbeschreibung dokumentiert. Damit war für alle Folgeblöcke klar, welche Äquivalenzen (EF Core ↔ Hibernate / Panache, MassTransit ↔ in-prozess-äquivalent zu Queue-basiertem Messaging) zu wählen sind.
- **Solution-Layout zuerst.** Das Modular-Monolith-Layout (`FlowHub.Core` Domain, `FlowHub.Web` Host, geplante `FlowHub.Persistence` / `FlowHub.Api` / `FlowHub.AI` Projekte) wurde im Block 1 grob gesetzt — Folgeblöcke konnten Projekte addieren, ohne das Grundgerüst zu rebrokern.
- **Zentrale Paketverwaltung.** `Directory.Packages.props` zahlt sich ab dem zweiten Projekt aus — Versions-Bumps an genau einer Stelle.

### Was war schwieriger als erwartet

- **PKM-Domain-Modellierung.** `Capture`, `LifecycleStage`, `ChannelKind` als minimale Domain wirken trivial, aber jeder spätere Block hat Felder ergänzt (`SkillId`, `FailureReason`, `Embedding` …). Mehr Zeit in der initialen Domain-Modellierung hätte spätere Migrationen verkleinert.
- **Architektur-Trade-offs.** Modular Monolith vs. Microservices wurde länger diskutiert als geplant. ADR 0002 §"Alternatives considered" hält die verworfenen Varianten fest.

### Verwendete KI-Werkzeuge

- **Claude Code** für Solution-Scaffolding, Domain-Typ-Skizzen und ADR-Drafts.
- **Brainstorming-Skill** für die Architektur-Diskussion vor ADR 0001 / ADR 0002.

Konkrete KI-Korrekturen in diesem Block:

- Vorschlag `IAsyncEnumerable<Capture>` in `ICaptureRepository` verworfen — Overkill für die initiale Skala; `Task<IReadOnlyList<Capture>>` reicht.
- `CancellationToken`-Propagation in `ICaptureService` von KI vorgeschlagen und übernommen.

## Verweise

- ADR 0001 — Frontend Render Mode and Architecture (`docs/adr/0001-frontend-render-mode-and-architecture.md`)
- ADR 0002 — Service Architecture and Async Communication (`docs/adr/0002-service-architecture-and-async-communication.md`)
- Projektbeschreibung — `docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md`
- Insights — `docs/insights/block-1.md`
