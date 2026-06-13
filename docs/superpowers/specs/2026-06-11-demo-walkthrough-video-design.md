# Demo Walkthrough Video — Design

- **Date:** 2026-06-11
- **Status:** Approved (brainstorming) → ready for implementation plan
- **Topic:** A third explainer video: a silent, screenshot-driven walkthrough of the live VPS-DE public demo, showing every sample captured, classified, and landing in its downstream service.

---

## Goal

Produce a short (~60–75s), **voice-less** video that demonstrates *using* the public demo at <https://demo.flowhub.freaxnx01.ch>:

1. Submit each of the four one-click sample captures.
2. Show each capture's detail (AI classifier trace + lifecycle).
3. Follow the banner's **Live services** links and show the data that landed in each service.

A synthetic mouse pointer makes it read as a real person operating the demo. The video joins the two existing explainers in the `video/` Remotion subproject and the README.

## Non-goals

- No narration / TTS (the existing Piper pipeline is untouched).
- No changes to FlowHub application code or the demo deployment.
- Not a replacement for the two explainer videos — this is additive.
- No attempt to *enable* services that the live demo doesn't expose; the capture is faithful to whatever the live banner shows.

---

## Architecture

Two stages inside `video/`, plus tooling — mirrors the existing capture-then-compose split (`tts → durations.json → render`).

```
live demo ──(Playwright)──▶ video/public/demo/*.png + manifest.json ──(Remotion)──▶ out/flowhub-demo.en.mp4
```

### Stage 1 — Capture (`video/tools/capture-demo.mjs`)

A Node + Playwright script, headless Chromium, fixed **1920×1080** viewport (matches the explainers). It drives the live demo and writes screenshots + a manifest. Steps:

1. **Home** — open the demo root, wait for the quick-capture chips + demo banner to render, screenshot (`home`).
2. **For each of the 4 sample chips** (in DOM order): 🎬 movie, 📜 Zitat, ✅ `todo:`, 🔗 URL —
   - Record the chip's bounding-box centre as the **cursor target** for this action.
   - Click the chip; the field submits the example and (per `QuickCaptureField`) navigates to the Captures list after `ExampleSubmitDelayMs`.
   - **Poll** the Captures list until this capture reaches a terminal lifecycle stage (`Routed`, `Unhandled`, or `Failed`) or a timeout (~25 s); screenshot the list (`capture-<key>-list`).
   - Open the capture's **detail** page, wait for the classifier-trace panel, screenshot (`capture-<key>-detail`).
3. **Services** — read the banner's `Live services` buttons (`Vikunja`, `Zitate`, `Wallabag`, `paperless-ngx`) and the `ServiceLogin` hint chip. For **each** link present:
   - Record the button centre as the cursor target.
   - Open the link's `href` in a new page. If the destination shows a login form (Wallabag, paperless), fill it with the parsed `ServiceLogin` credentials (format `user / password` from the chip) and submit.
   - Wait for the board/list content, screenshot (`service-<name>`).
4. Write `video/public/demo/manifest.json` and exit non-zero if **no** screenshots were produced or any sample never left `New`/`Classifying` (so a broken run fails loudly rather than rendering a stale video).

**Adaptivity:** the script enumerates whatever service links the live banner actually renders. Services absent from the banner are simply skipped — no hard-coded service list, so the video matches the live deployment.

**Robustness:**
- Selectors prefer stable hooks: chip text/emoji, MudBlazor component roles, and `data-*`/`aria` where present. Where the app lacks a stable hook, the script uses text content; any selector additions to the app are out of scope (read-only against prod).
- Rate limit is 10/min, burst 20 — four captures plus a handful of navigations stay well under it; small inter-action delays (~500 ms) keep margin.
- The 15-min data reset is irrelevant: a full run completes in well under a minute.
- Each `wait` has a timeout; on timeout the script screenshots the current state and records the reached stage, keeping the run faithful rather than hanging.

### Manifest schema

`video/public/demo/manifest.json`:

```json
{
  "capturedAt": "<ISO8601, stamped by the caller, not Date.now() in-script>",
  "viewport": { "width": 1920, "height": 1080 },
  "shots": [
    {
      "id": "capture-todo-list",
      "file": "demo/capture-todo-list.png",
      "section": "capture",
      "sample": "todo",
      "caption": "AI classifies → todo → Vikunja",
      "cursor": { "x": 312, "y": 540, "click": true }
    }
  ]
}
```

- `cursor` is optional; present on **action** shots (chip clicks, service-link clicks). `x/y` are viewport coordinates of the click target; `click:true` triggers the click-pulse.
- `section` ∈ `intro | capture | service | outro` for grouping/section cards.
- `caption` is authored by the capture script from a small lookup keyed by sample/service, so captions stay correct as the live routing changes.

### Stage 2 — Compose (Remotion composition `demo-walkthrough`)

A new silent composition registered in `src/Root.tsx`, reusing existing infrastructure (`theme`, `TitleCard`, `SceneFrame`, the music-bed `Audio`).

- **Per shot:** the screenshot rendered full-frame with a gentle Ken-Burns zoom (≤4% scale drift) and a caption strip (bottom, theme-styled) showing `caption`.
- **Animated cursor:** a single `<Cursor>` overlay component reads the ordered shots' `cursor` targets. At each action shot it **glides** from its previous resting point to `{x,y}` (spring/interpolated over ~0.5 s) and plays a **click pulse** (scale-down + expanding ripple ring) at the moment of click, timed just before the cut to the result screenshot. Coordinates are scaled from the 1920×1080 manifest space to the composition size (also 1920×1080, so 1:1). Cursor art is an inline SVG arrow — no external asset.
- **Structure:** logo `TitleCard` intro → "Capture" section → four sample sub-sequences (chip → list → detail) → "Where it lands" section → one sub-sequence per service → logo `TitleCard` outro.
- **Timing:** fixed per-shot durations (no narration to drive length) — roughly 2.5 s for context shots, 3.5 s for result shots, +0.5 s glide on action shots; section title cards ~1.5 s. Total ~60–75 s. Durations live in a small constant table in the composition (not `durations.json`, which stays TTS-owned).
- **Audio:** the existing music bed, faded in like the other compositions. No narration track.

### Tooling

- Add `@playwright/test` (or `playwright`) to `video/package.json` devDependencies (isolated Node subproject — no NuGet/solution impact).
- `video/tools/setup.sh`: after `npm install`, run `npx playwright install chromium` (idempotent; Chromium cached under the Playwright home). Vendored-tools philosophy preserved — nothing system-wide.
- `justfile` (video group):
  - `video-capture` — run `capture-demo.mjs` against the live demo (overridable `DEMO_URL`), producing `video/public/demo/`.
  - `render:demo` (npm script) / `video-render` extended to render `flowhub-demo` → `out/flowhub-demo.en.mp4`.
  - `just video` continues to render the two explainers; the demo video is rendered on demand (it depends on a live capture, so it's not folded into the default `video` pipeline). Documented in `video/README.md`.

### Reproducibility

The captured PNGs + `manifest.json` are **committed** under `video/public/demo/`, so the Remotion render is fully offline/reproducible. `just video-capture` refreshes them against the live demo when the demo changes. (`video/.gitignore` keeps ignoring `out/`, `node_modules/`, `tools/*` binaries — the committed demo PNGs are a new tracked asset dir, like `public/audio/music/bed.mp3`.)

### README integration

Add a third entry to the README "Explainer videos" section: **"See it in action — the live demo"**. Same GitHub native-player approach as the two explainers (a `user-attachments` asset URL). Producing that URL needs a manual one-time upload of the rendered MP4 by the operator; the implementation renders the MP4 and hands it over for that step.

---

## Error handling & risks

| Risk | Mitigation |
|---|---|
| Async classification slower than expected | Poll terminal lifecycle stage with ~25 s timeout per sample; screenshot reached state on timeout. |
| Rate limiting (10/min, burst 20) | 4 captures + few navigations; ~500 ms spacing; well within burst. |
| Login-gated service (Wallabag/paperless) | Parse `ServiceLogin` chip → fill+submit login form; if creds absent or login fails, screenshot the login page and continue (faithful). |
| Service link absent from live banner | Skip it; manifest omits the shot. No hard-coded service set. |
| Selector fragility on prod UI | Prefer text/role hooks; capture is read-only; failures surface as a non-zero exit, not a silent stale render. |
| Demo down at capture time | `video-capture` fails loudly; committed PNGs from the last good run remain until re-captured. |
| Playwright headless cursor invisible | By design — the pointer is composited in Remotion from manifest coordinates, not relied upon from the page. |

## Verification

- **Capture:** dry run `just video-capture` against the live demo; assert every expected PNG exists and is non-blank (size > a few KB / not uniform), and `manifest.json` validates against the schema.
- **Compose:** `npm run typecheck`; render smoke `render:demo`; probe the MP4 (h264 video stream, expected duration, music-bed audio present).
- **Visual:** extract a few frames to confirm the cursor glides to the right targets, captions match, and service screenshots show real data.

## Assumptions

- The live demo runs in `Demo:Mode` with the example chips and the `Live services` banner visible (it does — public demo).
- `ServiceLogin` is a shared, low-value demo credential safe to use in an automated capture (it is shown publicly in the banner).
- Capturing screenshots of the operator's own public demo and its public service shares is intended and authorised (operator-owned infrastructure).
