---
tags:
  - claude-updated
updated: 2026-05-16
---

# Block 1 — Einführung · Nachbereitung

**Phase budget:** 28 h
**PVA war:** 2026-02-21
**Nächste PVA:** 2026-03-21

## Lernziel

- Ich kann ein KI-gestütztes Software-Engineering-Projekt initial aufsetzen und die wichtigsten Architekturentscheidungen begründen.
- Ich kann Software-Architekturoptionen (Monolith, Modular Monolith, Microservices) für ein Projektszenario gegeneinander abwägen.
- Ich kann das Tooling für KI-assistierte Entwicklung (Claude Code, IDE-Integration, Skills) für ein neues Repository einrichten.

## Auftrag (Moodle)

- Sichtung der Materialien und Ergebnisse der Präsenzveranstaltung 1
- Projektbeschreibung verfassen (Idee, Solution Vision, fokussierte Sub-Systeme, Beispiel-Eingabedaten)
- Architektur-Optionen evaluieren und eine begründete Entscheidung dokumentieren
- Initiales Repository-Setup mit dem gewählten Stack

> **FlowHub-Kontext:** Stack-Entscheidung gegen Quarkus / Jakarta EE und für .NET 10 + Blazor Web App in der Projektbeschreibung dokumentiert (siehe `vault/Projektarbeit/Dev.md`). ADR 0001 (Frontend Render Mode and Architecture, Accepted 2026-02-15) und ADR 0002 (Service Architecture and Async Communication, Accepted 2026-03-01) halten die beiden tragenden Architekturentscheidungen fest.

---

## TODO

### ✅ Done

- [x] Sichten Materialien und Ergebnisse der Präsenzveranstaltung 1
- [x] Projektbeschreibung ändern auf .NET, Upload
- [x] Architekturen erklären lassen und entscheiden, dokumentieren — ADR 0001 (Frontend / Modular Monolith) + ADR 0002 (Service-Architektur)
- [x] Focus on a few Services / Categories: Vikunja, Wekan, Wallabag — `vault/Projektarbeit/External Services.md` + `FlowHub.Integrations/` Placeholder
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
