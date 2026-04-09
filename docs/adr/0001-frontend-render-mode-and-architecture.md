# ADR 0001 — Frontend Render Mode and Architecture

- **Status:** Accepted
- **Date:** 2026-04-09
- **Block:** Block 2 (Frontend) — Nachbereitung
- **Decider:** freax
- **Affects:** `source/FlowHub.Web/`, `source/FlowHub.Core/`, eventual `source/FlowHub.Api/`

---

## Context

The Block 2 Moodle Auftrag (*Projektarbeit: Frontend*) requires us to:

- Decide on a frontend technology and rendering strategy (CSR vs. SSR vs. mix).
- Specify the frontend with wireframes and page-flow diagrams.
- Implement the frontend against services with **static / generated test data** (real persistence comes in Block 4).
- Generate AI-assisted unit tests with bUnit.

The repo's `CLAUDE.md` already locks in **Blazor + MudBlazor** as the UI stack. This ADR fills in the rest: which Blazor render mode, how the UI relates to the (future) REST API, how authentication works, and what high-level Pages we expect.

FlowHub is **operator-facing**: a single user (or small team) running it as a self-hosted service in a homelab. There is no public-facing surface, no SEO requirement, and no anonymous access. Captures arrive primarily from a Telegram bot today; the Web UI must also act as a Channel that can submit new Captures (see *Decision 4*).

The relevant glossary terms (`Capture`, `Channel`, `Skill`, `Integration`, `Page`, `Component`, `Card`, `Widget`, `Render Mode`) are defined in `Projektarbeit/Glossary.md` in the CAS Obsidian vault.

---

## Decision

### 1. Render mode: **Interactive Server** (Blazor 8+ Server interactivity over SignalR) as the default

Every routable Page in `source/FlowHub.Web/` declares `@rendermode InteractiveServer` unless there's a documented reason to use a different mode. The Blazor component runs **in the ASP.NET Core process**, UI events flow over a long-lived SignalR circuit, and DOM diffs are pushed back to the browser.

**Per-Page exceptions allowed but rare:**

| Page type | Render mode | Why |
|---|---|---|
| Login / OIDC callback | **Static SSR** | No interactivity, no client state, must work before circuit established |
| Health / status (`/_status`) | **Static SSR** | Plain text/HTML, scrapeable, no interactivity needed |
| Everything else | **Interactive Server** | Default — see below |

**WebAssembly / Auto are explicitly out of scope for Block 2.** They can be revisited later if a specific component justifies the WASM runtime cost (e.g. an offline-capable Capture composer, a heavy in-browser editor). Adding them later is cheap because Blazor 8+ allows per-component render-mode declarations.

### 2. The Web UI does **not** consume FlowHub's REST API

Because Interactive Server components run in-process, the Web UI `@inject`s application services (`CaptureService`, `SkillRegistry`, `IntegrationHealth`, …) and calls them **directly**. No HTTP, no JSON serialization, no fetch, no controller round-trip for the UI's own data.

The REST API (built in Block 3) exists for **non-UI consumers**:

- `FlowHub.Telegram` (bot, separate process)
- External integrations / automation
- CLI tools, future mobile clients
- Webhook receivers from upstream services

This means: the Block 3 *Services* assignment is **not** "build the API the frontend needs". It's "build the API the *other clients* need." That distinction shapes how we model the API's resource design.

### 3. Authentication: **OIDC against the existing homelab Authentik instance**

FlowHub is a new client of the user's existing **Authentik** SSO (already operated in the homelab — see `homelab/_uncategorized/Authentik.md`). ASP.NET Core's `Microsoft.AspNetCore.Authentication.OpenIdConnect` provider is wired in `Program.cs`; tokens land in a cookie that the SignalR circuit can read.

Rejected: **ASP.NET Core Identity (local user store)** — would create a second user database to keep in sync, contradicts the homelab "one IdP for all services" pattern, and adds Block 5 deployment complexity (secrets, password reset flows, MFA bootstrapping) that Authentik already solves.

The OIDC client registration in Authentik is a **runtime config concern**, not a code concern. Client ID, client secret, and issuer URL all come from environment variables (`Auth__OIDC__*`) per the 12-factor rule already in `CLAUDE.md`.

### 4. The Web UI is itself a **Channel**

Per the Glossary, a `Channel` is an inbound source of Captures. The Web UI is one of them. Concretely:

- A `WebChannel` exists alongside `TelegramChannel`.
- The dashboard has a **persistent quick-add field** (always-visible, in the `MudAppBar` or just below it) — paste a URL, type a quote, drop an image — *Enter* sends it to `CaptureService.Submit(new RawCapture(source: WebChannel, content: …))`.
- A dedicated `/captures/new` Page exists for the longer-form variant: image upload, multi-line text, manual category override before classification.
- Both routes flow through the **same** `CaptureService.Submit(...)` entry point. The channel is metadata on the Capture, not a code path.

This satisfies the Moodle Lernziel *"Quarkus für Web-Formulare nutzen"* (translated to our stack: ASP.NET Core / Blazor for web forms) without inventing a contrived form just for the assignment.

### 5. Initial Page inventory (directional, refined in `/ui-brainstorm`)

| # | Path | Purpose | Render mode |
|---|---|---|---|
| 1 | `/` | Dashboard — recent Captures, Skill health, Integration health, quick-add field | Interactive Server |
| 2 | `/captures` | Captures list — filter, search, sort, lifecycle filter (raw / classified / orphan / unhandled) | Interactive Server |
| 3 | `/captures/{id}` | Capture detail — content preview, classification, override category, retry routing | Interactive Server |
| 4 | `/captures/new` | Long-form capture submission (Web Channel) | Interactive Server |
| 5 | `/skills` | Skill list with status, recent activity, success/failure counts | Interactive Server |
| 6 | `/integrations` | Integration list with health, last successful write per integration | Interactive Server |
| 7 | `/auth/login`, `/auth/callback` | OIDC redirect endpoints (Authentik) | Static SSR |

**Not in scope for Block 2 Nachbereitung** (parked for later blocks):
- Settings / preferences page
- Skill suggestion review queue (for unhandled Captures)
- Audit log viewer
- Multi-user / RBAC

Each Page above gets its own `/ui-brainstorm` → `/ui-flow` → `/ui-build` → `/ui-review` cycle in the order listed. Pages 1, 4, and 2 are the **MVP path** that should be implementable end-to-end against Faker test data within the 26 h Block 2 budget; pages 3, 5, 6 are stretch.

### 6. Test data: Bogus (the .NET equivalent of Faker)

Stub services in `source/FlowHub.Web/Stubs/` return Bogus-generated Captures, Skills, and Integrations. The same stubs are used by bUnit tests so component behaviour can be asserted against deterministic seeded data. In Block 3 the stubs are replaced with real `CaptureService` calls; in Block 4 the in-memory store is replaced with EF Core + PostgreSQL. The component code does not change — only the DI registration.

---

## Alternatives Considered

### A. Blazor WebAssembly (CSR) only

- ❌ Forces a REST API to exist *for the UI's sake*, which inverts the design intent (the API should serve external clients, not the UI).
- ❌ First-load WASM runtime download (~1–3 MB) on a single-user dashboard is a poor trade.
- ❌ Loses in-process service injection — every read becomes a fetch + serialize + deserialize round-trip for a UI that has zero scaling pressure.
- ❌ Live Capture stream from the server requires SSE/WebSockets anyway → effectively re-implements what SignalR gives you for free.

### B. Static SSR + JavaScript sprinkles (htmx-style, or Blazor Static SSR with form posts)

- ❌ Captures/dashboard are inherently *live* — the operator wants new Captures to appear without refreshing. Static SSR forces full page reloads or polling.
- ❌ The Moodle Lernziele explicitly call out applying CSR *and* SSR concepts. Going pure-Static loses half the conceptual coverage.
- ❌ Loses the "no REST API for UI" benefit because every form post still goes through HTTP routing.

### C. Interactive Auto (Server-then-WASM swap)

- ⏸ Reasonable but premature. Adds complexity (component runs in two contexts, must be tested in both, no in-process service injection on the WASM side) for a single-user dashboard where the WASM upgrade has no measurable benefit.
- ⏸ Can be adopted later per-component if a specific case justifies it (e.g. an offline draft composer). Per-component render modes are a Blazor 8+ feature.

### D. ASP.NET Core Identity (local user store)

- ❌ Second user database to keep in sync with the homelab IdP.
- ❌ Contradicts the established homelab "one IdP, many services" pattern.
- ❌ Adds Block 5 deployment work (password reset flows, MFA, account lockout, secret rotation) that Authentik already solves.

### E. No auth at all (network-level isolation only)

- ❌ Even in a homelab, FlowHub will eventually be reachable via the Cloudflare tunnel / reverse proxy. App-level auth is the only honest answer.
- ❌ Fails the Block 5 *Deployment* expectations and the *Validierung* grading dimension.

---

## Consequences

### Positive

- **No HTTP layer for UI data flow.** Less code, less serialization, less to test, better diagnostics (server logs see the full call stack including the Blazor event handler).
- **Live Capture stream is free** — when a new Capture lands (from any Channel), the server already knows about it and can push a UI patch over the open SignalR circuit. No polling, no SSE, no extra plumbing.
- **REST API stays focused** on its real consumers (Telegram, integrations, automation) — better resource design, clearer versioning story.
- **Auth is solved at the platform layer** — Authentik handles MFA, password reset, account lifecycle.
- **Block 3 (Services) is conceptually cleaner** — the API exists for external clients, not as scaffolding for the UI's own data needs.

### Negative

- **Always-on connection requirement.** A dropped SignalR circuit means lost UI state. Mitigation: use `PersistentComponentState` for anything that must survive a reconnect; show a clear reconnect indicator.
- **Server-side state per active user.** Fine for single-operator FlowHub; would need rethinking at hundreds of concurrent users.
- **Blazor Server has a wider attack surface during the SignalR circuit** — the server holds component state that's tied to the user's session. Mitigation: standard ASP.NET Core authorization on every event handler.
- **WASM offline mode is not on the table** without revisiting this decision.
- **bUnit tests run components in isolation**, but won't catch SignalR-specific issues like state-after-reconnect. E2E tests in Block 5 will need to cover that.

### Neutral

- The UI's render mode is explicit per Page — adding a WASM component later is a single attribute change plus the WASM project wiring.
- The decision is reversible at the per-Page level. If `/captures/new` later needs offline drafting, it can become Interactive Auto without affecting the rest of the app.

---

## Implementation Notes (for Block 2 Nachbereitung)

The implementation work this ADR unblocks:

1. **Scaffold `source/FlowHub.Web/`** — empty today except `.gitkeep`. Add Blazor App template (Interactive Server), wire MudBlazor, wire OIDC against Authentik (env-var driven), add `MudLayout` with `MudAppBar` quick-add field placeholder.
2. **Define stub services** in `source/FlowHub.Web/Stubs/` returning Bogus-generated Captures/Skills/Integrations. These implement interfaces that will live in `FlowHub.Core` so Block 3 can swap real implementations in.
3. **Per-page work** via the mandatory UI workflow: `/ui-brainstorm` → `/ui-flow` → `/ui-build` → `/ui-review`, in the MVP order: Dashboard → New Capture → Captures list. Stretch: Capture detail → Skills → Integrations.
4. **Component tests** via bUnit against the Bogus stubs.
5. **No auth wiring against a real Authentik client** in Block 2 — a placeholder OIDC config + a dev-mode bypass is enough; the real client registration happens in Block 5 (Deployment) when the service is exposed.

Each numbered item above will be its own short brainstorm → plan → implementation cycle. This ADR is the umbrella decision; it does not replace the per-feature planning.

---

## References

- Moodle: *W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26: Projektarbeit: Frontend* — assignment text
- `CLAUDE.md` — repo conventions (Blazor + MudBlazor lock-in, hexagonal architecture, MudBlazor component preferences)
- `.ai/cas-instructions.md` — block schedule and grading dimensions
- Vault: `Projektarbeit/Idee FlowHub.md` — original concept and management summary
- Vault: `Projektarbeit/Glossary.md` — Capture, Channel, Skill, Integration, Render Mode definitions
- Vault: `Projektarbeit/Skills.md` — running list of Skill ideas and example Captures per category
- Vault: `homelab/_uncategorized/Authentik.md` — existing SSO infrastructure
- Microsoft Learn: [Blazor render modes (.NET 8+)](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
