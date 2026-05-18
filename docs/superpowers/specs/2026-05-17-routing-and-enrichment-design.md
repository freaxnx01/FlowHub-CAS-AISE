# Per-Vikunja-Project Routing & Capture Enrichment

**Status:** Draft (approved in brainstorming 2026-05-17)
**Author:** freax (with Claude)
**Related:** `2026-05-03-slice-c-ai-integration-design.md`, `2026-04-09-flowhub-vikunja-skills-design.md`

---

## 1. Problem

Today every Vikunja-bound capture lands in a single hardcoded project (`VikunjaOptions.DefaultProjectId`). The classifier picks the *skill* (Wallabag / Vikunja / Orphan) but not *where inside Vikunja*. There is also no enrichment step — the only model-generated metadata is a 3–8 word title. A capture like

> `"Unix and C are the ultimate computer viruses.", Richard Gabriel`

cannot be routed to a `Quotes` project, and the resulting task lacks any context about the author.

## 2. Goals

- Classifier can route Vikunja captures to a specific project (e.g. `Inbox`, `Quotes`, `Movies`, `Reading`).
- The catalog of routable projects is read live from the Vikunja API and cached.
- A pluggable per-bucket enricher can add a structured description (e.g. AuthorInfo for quotes) before the task is written.
- The Quote/Author example is testable end-to-end via integration + Playwright E2E.
- Best-effort behaviour: classification, catalog lookup, and enrichment failures never block a capture from landing somewhere.

## 3. Non-goals

- Retry loop for enrichment (best-effort only).
- Wikipedia / web-search enrichers — `IEnricher` keeps the door open but only `QuotesEnricher` (LLM-backed) ships in this spec.
- Admin UI for bucket management — the catalog is read-only from Vikunja.
- Per-tag routing — only per-project routing.
- Authorisation/multi-user concerns (`DevAuthHandler` still in effect until Block 5).

## 4. Architecture

### Pipeline

```
Capture
  │
  ▼
[1] AiClassifier   ──► ClassificationResult { tags, matched_skill,
  │                                           project, entities, title }
  │     (falls back to KeywordClassifier on schema error)
  ▼
[2] EnricherDispatcher  ──► picks IEnricher by ClassificationResult.VikunjaProject
  │                          (no-op if no enricher registered for that bucket)
  ▼
[3] VikunjaSkillIntegration  ──► PUT /api/v1/projects/{projectId}/tasks
                                  body = { title, description }
```

Three concerns, three components. Classifier + dispatcher + enrichers all live in **`source/FlowHub.AI`**; the project catalog and the (modified) skill integration stay in **`source/FlowHub.Skills`**. No new csproj.

### New / changed types

| Type | Location | Purpose |
|---|---|---|
| `ClassificationResult` *(extend)* | `FlowHub.Core/Classification` | Add `string? VikunjaProject` (bucket name) and `IReadOnlyDictionary<string,string>? Entities`. |
| `IEnricher` *(new)* | `FlowHub.Core/Classification` | `string BucketName`; `Task<EnrichmentResult?> EnrichAsync(Capture, ClassificationResult, CT)`. |
| `EnrichmentResult` *(new)* | `FlowHub.Core/Classification` | `{ string Description, IReadOnlyDictionary<string,string> Fields }`. |
| `EnricherDispatcher` *(new)* | `FlowHub.AI` | Resolves project to fallback if unknown; resolves `IEnricher` by `BucketName`; swallows enricher exceptions and returns null with reason. |
| `QuotesEnricher` *(new)* | `FlowHub.AI/Enrichers` | Second LLM call producing a 2–3 sentence factual bio of the author. |
| `IVikunjaProjectCatalog` *(new)* | `FlowHub.Core/Skills` | `Task<IReadOnlyDictionary<string,int>> GetAsync(CT)` — bucket name → projectId. |
| `VikunjaProjectCatalog` *(new)* | `FlowHub.Skills/Vikunja` | Live API fetch + TTL cache + fallback to last-known. |
| `VikunjaSkillIntegration` *(modify)* | `FlowHub.Skills/Vikunja` | Resolve project id via catalog; accept description from enrichment. |
| `VikunjaOptions` *(modify)* | `FlowHub.Skills/Vikunja` | Remove `DefaultProjectId`; add `FallbackProject`, `FallbackProjectId`, `Catalog` (refresh/timeout). |
| `AiPrompts` *(modify)* | `FlowHub.AI` | Inject bucket list from catalog; ask model for `project` + `entities`. |
| `AiClassificationResponse` *(modify)* | `FlowHub.AI` | Add `Project` (nullable string) and `Entities` (nullable dict). |
| `CaptureEntity` *(modify)* | `FlowHub.Persistence` | Add `VikunjaProject` (nullable string), EF migration `AddVikunjaProjectToCapture`. |

### DI wiring

```csharp
// AiServiceCollectionExtensions
services.AddSingleton<IEnricher, QuotesEnricher>();
services.AddSingleton<EnricherDispatcher>();

// SkillsServiceCollectionExtensions
services.AddSingleton<IVikunjaProjectCatalog, VikunjaProjectCatalog>();
```

## 5. Data flow — Quote example

Input content: `"Unix and C are the ultimate computer viruses.", Richard Gabriel`

**Step 1 — Classify**

`AiClassifier` pulls the live bucket list (`["Inbox", "Quotes", "Movies", "Reading"]`) from the catalog and renders it into the system prompt. Structured response:

```json
{
  "tags": ["quote", "computing"],
  "matched_skill": "Vikunja",
  "project": "Quotes",
  "title": "Gabriel on Unix and C",
  "entities": {
    "quote": "Unix and C are the ultimate computer viruses.",
    "author": "Richard Gabriel"
  }
}
```

**Step 2 — Dispatch enrichment**

`EnricherDispatcher`:

1. If `VikunjaProject` is not in the catalog → coerce to `FallbackProject` (`"Inbox"`), log `EventId=3011 ClassifierProjectCoerced`.
2. Look up `IEnricher` where `BucketName == VikunjaProject`. Found → `QuotesEnricher`.

`QuotesEnricher` reads `ClassificationResult.Entities["author"]` (`"Richard Gabriel"`) and makes a second LLM call:

```
System: You write a 2–3 sentence factual bio of a public figure for a personal
        knowledge tool. If you don't know the person, reply with an empty string.
        Never invent facts.
User:   Richard Gabriel
```

Returns `EnrichmentResult` whose `Description` is:

```markdown
> "Unix and C are the ultimate computer viruses." — Richard Gabriel

**About Richard Gabriel:** American computer scientist…
```

**Step 3 — Vikunja write**

`VikunjaSkillIntegration` resolves `"Quotes" → 7` via the catalog and posts:

```
PUT /api/v1/projects/7/tasks
{ "title": "Gabriel on Unix and C",
  "description": "<the markdown above>" }
```

## 6. Configuration

```jsonc
"Skills": {
  "Vikunja": {
    "BaseUrl": "https://vikunja.home.freaxnx01.ch",
    "ApiToken": "${VIKUNJA_TOKEN}",
    "FallbackProject": "Inbox",
    "FallbackProjectId": 1,        // used until first catalog fetch succeeds
    "Catalog": {
      "RefreshInterval": "00:05:00",
      "RequestTimeout": "00:00:03"
    }
  }
},
"AI": {
  "Enrichers": {
    "Quotes": { "Enabled": true }
  }
}
```

`DefaultProjectId` is removed; `FallbackProject` + `FallbackProjectId` replace it. The catalog does not block startup — the first classify request triggers the first fetch; before that, only `Inbox` is known.

## 7. Failure handling

| Failure | Behaviour | Log EventId |
|---|---|---|
| Classifier schema violation / exception | Fall back to `KeywordClassifier` (existing). | 3010 (existing) |
| Classifier returns unknown `project` | Coerce to `FallbackProject`. | 3011 |
| Catalog fetch fails on first call | Return `{ FallbackProject: FallbackProjectId }`. | 3020 |
| Catalog fetch fails on refresh | Keep last-known catalog. | 3021 |
| Enricher LLM call fails / times out / empty | Skip enrichment; post task without bio. | 3030 |
| No enricher registered for bucket | Silent no-op; task posted with title only. | — |
| Vikunja write fails | Existing — SkillRun fails, capture → `Failed`. | (existing) |

All new event IDs feed the existing Grafana dashboard.

## 8. Testing

**Unit (xUnit + NSubstitute + FluentAssertions; bUnit for components)**

- `AiClassifierTests` *(extend)*: valid `project` propagated; missing `project` when `matched_skill="Vikunja"` → result has null project (dispatcher coerces); structured-response failures still fall back to keyword classifier. `TestChatClient` from `Microsoft.Extensions.AI` for stubbing.
- `EnricherDispatcherTests`: picks correct enricher by `BucketName`; returns null when no match; coerces unknown project; swallows enricher exception and returns null result with reason.
- `QuotesEnricherTests`: stubbed `IChatClient` returns canned bio → composed description matches snapshot. Empty bio → quote-only description. Exception → propagated.
- `VikunjaProjectCatalogTests`: cache hit within TTL; refresh after TTL; HTTP failure → last-known returned; first-call failure → `{ Fallback: FallbackId }`.
- `VikunjaSkillIntegrationTests` *(extend)*: resolves project id via catalog; passes description when provided; falls back on unknown project name.
- `LifecycleBadgeTests` (bUnit): shows `→ Quotes` chip when `VikunjaProject` is set.

**Integration**

- `ClassifyAndEnrichPipelineTests`: wires real `AiClassifier` + `EnricherDispatcher` + `QuotesEnricher` against a `TestChatClient` returning two scripted responses (classify, then enrich). Asserts final `SkillResult` for the Richard Gabriel quote — including the bio in the description and `projectId=7` in the captured HTTP request.

**E2E (Playwright, headed)**

- Extend the existing full-cycle demo: type the Gabriel quote into Quick Capture; WireMock stubs (a) Vikunja `/projects` catalog, (b) Vikunja `PUT /projects/7/tasks`, (c) the MEAI fake for both LLM calls. Assert the captured `PUT` body contains both the quote and the bio paragraph and that the UI shows `→ Quotes` on the capture row.

The Richard Gabriel fixture is shared between the integration and E2E tests.

## 9. Migration

EF Core migration `AddVikunjaProjectToCapture` adds a nullable `VikunjaProject` (string) column. Backfill is not required — existing rows remain null and the UI treats null as "Inbox or pre-routing".

## 10. Open questions

None blocking. Future work tracked separately:

- Wikipedia-backed `QuotesEnricher` strategy (factual grounding).
- `MoviesEnricher` (TMDB lookup) once Block 4 wires the Movies bucket.
- Per-bucket enabling via the planned admin UI (out of scope this block).
