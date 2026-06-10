# FlowHub Explainer Videos — Design

**Date:** 2026-06-08
**Status:** Approved (design)
**Topic:** Two short (~90s) German explainer videos for FlowHub — one for a technical audience, one for end users — produced code-based with AI narration and background music.

---

## Goal

Produce two short explainer videos that highlight FlowHub's **features and benefits**:

- **Video A — End users** (`flowhub-users.de.mp4`): non-technical, pain → relief → benefits.
- **Video B — Technical** (`flowhub-technical.de.mp4`): architecture, AI routing, stack & quality.

Both ~60–90 seconds, German narration, shared visual identity.

## Decisions (locked during brainstorming)

| Decision | Choice | Rationale |
|---|---|---|
| Production approach | **Code-based** | Reproducible, version-controlled, fits dev workflow; re-renders on a text edit. |
| Framework | **Remotion** (React → video) | Built for slide/UI animation + audio mixing + MP4 export. Manim is for math, wrong shape here. |
| Narration | **AI TTS** | Script-driven, easy to edit, no recording. |
| TTS engine | **Piper (local)** | Free, offline, homelab-friendly, reproducible. German voice `de_DE-thorsten-medium`. |
| Language | **German** | Matches CAS coursework. Script-driven design keeps a later EN pass cheap. |
| Length | **~60–90s each** | Tight: hero feature + key benefits. |

## Non-goals (YAGNI)

- No English versions in this iteration (pipeline keeps them cheap to add later).
- No live app screen-capture; everything is animated slides/diagrams.
- No CI rendering pipeline; rendering is a local/manual command.

---

## Architecture & repo layout

Self-contained `video/` directory at repo root, **fully isolated** from the .NET solution (own `package.json` + Node toolchain; does not touch `FlowHub.slnx` or any `.csproj`).

```
video/
├── package.json                 ← Remotion + deps (Node toolchain, isolated)
├── scripts/
│   ├── flowhub-technical.de.md  ← narration script (technical audience)
│   └── flowhub-users.de.md      ← narration script (end-user audience)
├── audio/
│   ├── tts/                     ← Piper-generated voiceover (.wav per scene)
│   └── music/                   ← royalty-free background track + LICENSE.md
├── src/
│   ├── Root.tsx                 ← registers both compositions
│   ├── theme.ts                 ← FlowHub colors/fonts/logo (shared)
│   ├── durations.ts             ← loads durations.json (audio-driven timing)
│   ├── components/              ← TitleCard, FeatureSlide, WorkflowDiagram, BenefitList, ArchitectureDiagram, Badge…
│   ├── TechnicalVideo.tsx       ← composition #1
│   └── UserVideo.tsx            ← composition #2
├── tools/
│   └── tts.sh                   ← runs Piper over a script → audio/tts/*.wav + durations.json
├── out/                         ← rendered .mp4 (gitignored)
└── README.md                    ← one-time setup + render instructions
```

Two Remotion **compositions** share one component library and `theme.ts`, so both videos look like a family and a brand tweak hits both at once.

## Production pipeline (data flow)

Script-first: editing narration text re-renders the video, no manual re-recording.

```
narration script (.md, one block per scene with scene IDs)
   │
   ▼
tools/tts.sh ──► Piper (de_DE-thorsten-medium) ──► audio/tts/<scene>.wav
   │                                                   │
   │  ffprobe reads each .wav duration                 │
   ▼                                                   ▼
durations.json ───────────────────────────►  Remotion composition
                                                │  - scene length = its audio length
                                                │  - <Audio> narration per scene (vol 1.0)
                                                │  - <Audio> music bed (vol ~0.12, ducked)
                                                ▼
                                  npx remotion render ──► out/flowhub-{technical,users}.de.mp4
```

- **Audio drives timing.** Each scene's on-screen duration is computed from its narration `.wav` length via `ffprobe`, written to `durations.json`, consumed by the composition. Narration and slides stay synced even after rewording.
- **Music ducking.** One royalty-free track under the whole video at low volume; narration on top at full.
- **Commands** wrapped in npm scripts: `npm run tts`, `npm run render:technical`, `npm run render:users`, `npm run dev` (Studio preview).

---

## Storyboards

Both ~90s (≈180–210 words German narration, ~6 scenes). Content tunable in scripts; these are skeletons.

### Video A — End users ("Was es für dich tut")
Framing: pain → relief → benefits. No jargon.

| # | Scene | Visual | Beat |
|---|-------|--------|------|
| 1 | Hook | Title card, FlowHub logo | "Notiz hier, Link da, To-do irgendwo…" — the chaos |
| 2 | Problem | Scattered app icons, copy-paste arrows | Fragmented workflows, context-switching |
| 3 | Solution | One inbox, a note dropping in | "Reinwerfen — FlowHub erledigt den Rest" |
| 4 | Demo | "Inception – rewatch" → 🎬 → Vikunja card; article URL → Wallabag | Auto-routing magic, 2 concrete examples |
| 5 | Benefits | Icon list: Zeit sparen · kein Copy-Paste · alles am richtigen Ort · selbst-gehostet/privat | Why you care |
| 6 | Close | Logo + tagline | "FlowHub — dein intelligenter Posteingang" |

### Video B — Technical ("Wie es funktioniert")
Framing: problem → architecture → AI routing → stack/quality. Comfortable with terms.

| # | Scene | Visual | Beat |
|---|-------|--------|------|
| 1 | Hook | Title card | Integration-Hub orchestrating self-hosted services |
| 2 | Architecture | Modular-monolith diagram (Core · AI · Persistence · Skills/Integrations · Web) | Shape of the system |
| 3 | AI routing | Flow: Capture → MEAI classifier → Skill/Integration → target service | Categorize-and-route core (ADR 0004) |
| 4 | Integrations | Adapters fanning out: Wallabag, Vikunja, Telegram | Hexagonal ports/adapters, ISkillIntegration |
| 5 | Stack & quality | Badges: .NET 10 · Blazor · EF Core · OpenTelemetry · health endpoints · tests | Engineering rigor |
| 6 | Close | Logo + tagline | "FlowHub — modular, testbar, erweiterbar" |

**Shared features & benefits thread**, pitched per audience:
- End users hear: *time saved · no copy-paste · everything in the right place · private/self-hosted.*
- Technical viewers hear: *auto-classification · modular & testable · extensible integrations.*

---

## Theme & visual identity

- `theme.ts` holds FlowHub colors, fonts, logo.
- Starting palette derived from MudBlazor defaults to stay consistent with the app; adjusted if a brand palette/logo is provided.
- Consistent iconography (Material icons, matching the app's `Icons.Material.Filled.*`).

## Prerequisites (one-time, isolated under `video/`)

- **Node.js** — Remotion toolchain (only used in `video/`).
- **Piper** binary + German voice model `de_DE-thorsten-medium`.
- **ffprobe** (from ffmpeg) for audio durations. Remotion bundles ffmpeg for rendering.

No NuGet packages, no .NET project changes — respects repo agent guardrails.

## Testing / feedback loop

Video is not unit-testable like app code; the "test" is a fast verification loop:

- `npm run dev` → Remotion Studio live preview (scrub timeline), verify visually before full render.
- TypeScript typecheck / lint pass on the TSX.
- Draft render at low res (`--scale 0.5`) for quick review; full-res for final.

## Licensing

- Background music from a royalty-free source (e.g. Pixabay Music / Incompetech CC-BY).
- Attribution + license recorded in `audio/music/LICENSE.md`.

## Risks / open items

- **Piper German quality** is "good, not top-tier." If it grates, the pipeline allows swapping the TTS engine (Azure Speech / ElevenLabs) by replacing `tools/tts.sh` output — composition is unaffected.
- **Logo/brand assets** — if none exist, propose a palette and a simple wordmark in `theme.ts`.
- **`out/` size** — rendered MP4s are gitignored; decide separately whether/where to publish finals.
