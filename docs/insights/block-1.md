# Block 1 — Insights

**Block:** 1 — Vorbereitung & Konzept
**Date range:** 2026-02-01 – 2026-03-08

## What We Built

Project concept and initial architecture. Chose .NET 10 / Blazor Web App over Quarkus / Jakarta EE (justified in the Projektbeschreibung — stack-neutral rubric). Defined FlowHub as a modular-monolith PKM automation hub. Created `FlowHub.Core` domain model: `Capture`, `LifecycleStage`, `ChannelKind`, `ICaptureService`, `ICaptureRepository`.

Set up solution structure: `FlowHub.slnx`, `Directory.Build.props` (warnings-as-errors, embedded PDB), `Directory.Packages.props` (central package management), `global.json` (SDK pin).

## Key Decisions

- **Stack:** .NET 10 Blazor Server-interactive + MudBlazor (ADR 0001).
- **Architecture:** Modular Monolith with async pipeline boundary instead of physical Microservices split (ADR 0002).
- **Persistence placeholder:** `InMemoryCaptureRepository` stub — EF Core PostgreSQL wires in Block 4.
- **Test framework:** xUnit + FluentAssertions + NSubstitute + bUnit.

## Process

1. **Architecture brainstorming** — Claude Code brainstorming session weighing Monolith / Modular Monolith / Microservices against the FlowHub scenario (single operator, homelab deployment).
2. **ADR drafting** — Two ADRs drafted in parallel (0001 frontend, 0002 service architecture), reviewed for "Alternatives considered" completeness, then accepted.
3. **Scaffolding** — Solution + projects + central package management generated via AI; verified file-by-file.

## Lessons Learned

- `global.json` SDK pin prevents silent .NET version drift across machines (caught an issue we'd otherwise have hit when moving between the WSL2 dev box and the homelab Linux runner).
- `Directory.Packages.props` pays off immediately — one version bump in one file, instead of N project files.
- MudBlazor's `IsDarkMode` must be wired via `MudThemeProvider.MudThemeChanged` event, not static config (cost ~30 min of debugging in Block 2 because of an assumption set here).
- Starting with the domain model (`Capture` as record, `LifecycleStage` enum) before any UI kept the architecture clean across all five blocks.

## AI Usage by Activity

### Solution scaffolding

AI generated the project layout (`FlowHub.slnx`, four projects, central package management, common props). Reviewed every generated file; no functional corrections. `Directory.Build.props` `TreatWarningsAsErrors=true` was a human addition after the AI defaulted to `Nullable=enable` only.

### Domain modeling

AI proposed `Capture` as a class; human switched to a `record` to lean on value equality for test assertions. AI's initial `ICaptureRepository.GetAllAsync()` returning `IAsyncEnumerable<Capture>` was rejected — overkill for current scale; substituted `Task<IReadOnlyList<Capture>>`. AI's `CancellationToken` propagation in `ICaptureService` was accepted as-is.

### ADR drafting

AI drafted Context / Decision / Consequences sections; "Alternatives considered" sections were extended by the human after pushback on "why not Microservices?" The "Stack mismatch — N/A justification" pattern was introduced in ADR 0002 and reused in ADRs 0003 / 0004 / 0005.

## AI Usage Metrics (Block 1, estimate)

Block 1 had relatively little production code — most output was configuration, ADR prose, and the Projektbeschreibung. Estimates are conservative.

| Artifact | Approx. lines | AI-generated | Human lines | AI % (est.) |
|---|--:|--:|--:|--:|
| Solution files (`.slnx`, `.props`, `global.json`) | ~80 | ~70 | ~10 | 88% |
| Domain types (`Capture`, `LifecycleStage`, `ChannelKind`, ports) | ~120 | ~100 | ~20 | 83% |
| `InMemoryCaptureRepository` stub | ~40 | ~36 | ~4 | 90% |
| ADRs 0001 + 0002 prose | ~400 | ~280 | ~120 | 70% |
| Projektbeschreibung (initial draft) | ~600 | ~360 | ~240 | 60% |

Human share concentrates on the architectural decisions themselves (Modular Monolith over Microservices, .NET over Quarkus), the "why not" sections of ADRs, and the Projektbeschreibung narrative.

## KI-Reflexion / Fazit

### Stärken

- **Boilerplate.** Solution-Setup, zentrale Paketverwaltung, Domain-Typ-Skelette: alles generiert, alles auf Anhieb compilierbar.
- **ADR-Struktur.** Das Nygard-Format (Context / Decision / Consequences) liefert die KI zuverlässig — das spart die immer wiederkehrende Strukturierungsarbeit.
- **Alternativen-Exploration.** Im Brainstorming-Skill hat die KI Microservices-, Monolith- und Modular-Monolith-Optionen mit jeweils 2–3 konkreten Trade-offs gegenübergestellt; das hat die ADR-0002-Argumentation deutlich beschleunigt.

### Grenzen

- **Stack-Entscheid.** Die finale Entscheidung Quarkus vs. .NET ist eine Domain- und Kontext-Entscheidung (Vorwissen, Werkzeug-Reife, Deployment-Pfad), die die KI nicht treffen kann — sie kann nur Argumente sammeln und gegenüberstellen.
- **Über-Engineering-Tendenz.** Erstvorschläge waren regelmäßig "vollständiger" als nötig (z. B. `IAsyncEnumerable` für ein Repository ohne Streaming-Anforderung). Disziplinierte Ablehnung von Komfort-Features hat das Layer auf das Notwendige beschränkt.
- **Begründungen müssen menschlich werden.** KI-Erstdrafts der ADR-Begründungen wirkten oft generisch ("for scalability") — die Spezifika (homelab, single operator, Block-5-Deployment-Constraint) wurden manuell ergänzt.

### Fazit

Block 1 hat die Grundlage für KI-assistierte Entwicklung über die gesamte Projektlaufzeit gelegt: Solution-Layout, ADR-Format, Domain-Vokabular. Die KI war hier ein effizienter Scaffolder; die Architektur-Entscheidungen selbst blieben menschlich und bewusst dokumentiert.
