---
tags:
  - claude-generated
updated: 2026-04-10
---

# FlowHub Glossary

Project-specific vocabulary for FlowHub. Use these terms consistently in **code, REST API, UI strings, the thesis, and vault notes**.

Languages may be mixed (English + German). Where a German term already exists in the vault, the mapping is noted so both can coexist without drift.

This is a living document — extend it whenever a new domain term lands.

---

## Domain

### Capture

The central FlowHub noun. A single piece of incoming content from any channel — URL, text, image, file reference, voice memo, etc. **Untyped at arrival**; gets classified and routed downstream.

- **German equivalent in vault notes:** *Infoschnipsel* (e.g. *"Alle Infoschnipsel welche über Signal reinkommen"*).
- **Code:** `Capture`, `CaptureService`, `ICaptureClassifier`, `ICaptureRouter`
- **API:** `POST /captures`, `GET /captures/{id}`
- **DB:** `captures` table
- **Verb form:** *"FlowHub captured a URL from Telegram"*

#### Capture lifecycle

| # | Stage | Meaning |
|---|---|---|
| 1 | **raw Capture** | Just arrived, no classification yet |
| 2 | **classified Capture** | AI has assigned a category / target Skill |
| 3 | **routed Capture** | Handed off to a Skill (in-flight, processing) |
| 4 | **completed Capture** | Skill processed + Integration write succeeded (happy terminal state) |
| 5 | **orphan Capture** | Skill or Integration failed during processing |
| 6 | **unhandled Capture** | No matching Skill — triggers a Skill suggestion |

### Channel

An **inbound** source of Captures. Examples: Telegram bot, Signal bot, Email, Web form, direct REST API client.

- **Code:** `IChannel`, `TelegramChannel`, …
- A Capture always has exactly **one** source Channel.

### Skill

A FlowHub processing unit that knows how to handle one category of Capture and route it to one or more Integrations. Examples: `BookSkill`, `MovieSkill`, `ArticleSkill`, `QuoteSkill`.

- **Code:** `ISkill`, `BookSkill`
- Each Skill declares: the categories it handles, the Integrations it writes to, and any Enrichment it performs.
- See also `Projektarbeit/Skills.md` for the running list of skill ideas.

### Integration

A **downstream** third-party service that FlowHub writes Captures into via that service's API. Examples: Wallabag (read-later), Wekan (kanban), Vikunja (tasks/lists), Paperless-ngx (DMS), Obsidian (markdown via git).

- **Code:** `IIntegration`, `WallabagIntegration`
- An Integration is an **output target**, not a Channel. Don't confuse the two.

### Enrichment

Optional additional data a Skill fetches or computes before handing a Capture off to an Integration. Examples: `BookSkill` enriches a book URL with author + ISBN; `QuoteSkill` enriches a quote with author bio.

---

## Frontend (Blazor + MudBlazor)

### Page

A **routable** Blazor component declared with `@page "/route"`. Has a URL. Top-level navigable view.

### Component

A **reusable** Blazor building block with **no** `@page` directive. Lives in `Components/` or `Shared/`. Composed into Pages and other Components.

### Layout

The structural shell of the app: `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent`. Holds navigation chrome — never content.

### Card

A bounded UI block displaying a single discrete piece of info, typically rendered with `MudCard`. Used for: one Capture summary, one Skill status, one Integration health indicator.

### Widget

A larger composite dashboard unit. May contain multiple Cards. Self-contained and (often) interactive. Example: a *"Recent Captures"* widget on the dashboard Page that contains many Capture Cards.

### Render Mode

Blazor 8+ per-component rendering strategy. Four options:

- **Static SSR** — server renders HTML, no interactivity
- **Interactive Server** — server renders, UI events flow over SignalR (FlowHub's default)
- **Interactive WebAssembly** — runtime ships to browser
- **Interactive Auto** — starts as Server, swaps to WASM after first load

FlowHub default: **Interactive Server** — operator-facing dashboard, server-side state, no need to ship a runtime, no REST hop for the UI's own data.

---

## Avoid

These terms are out of vocabulary. Use the suggested replacement instead.

| ❌ Don't use | ✅ Use instead | Why |
|---|---|---|
| Screen | **Page** | UX/mobile-app term, ambiguous in Blazor |
| View | **Page** or **Component** | MVC heritage, conflicts with Blazor's component model |
| Tile | **Card** | "Card" is the MudBlazor-native term |
| Snippet *(for incoming data)* | **Capture** | "Snippet" reads as *code snippet* in English |
| Item *(for incoming data)* | **Capture** | Too generic — every system has items |
| Note *(for incoming data)* | **Capture** | Implies user-authored text, not auto-ingested URL/image |

---

## See also

- [[Repository]] — pointer to the FlowHub code repository
- [[Idee FlowHub]] — original concept and management summary
- [[Skills]] — running list of Skill ideas and example Captures per category
- `Knowledge/Akronyme.md` — general CAS vocabulary (acronyms, not project-specific)
