# Design — Route enriched Quote captures to Vikunja **Zitate** (#39)

**Date:** 2026-06-10
**Issue:** [#39](https://github.com/freaxnx01/FlowHub-CAS-AISE/issues/39) — "Demo: route enriched Quote captures to Vikunja project 'Zitate'"
**Status:** Approved (brainstorm) → ready for plan

---

## Problem

Captures classified as quotes should route to a dedicated Vikunja project **`Zitate`**, carrying their enriched data — not stop at Wallabag, the Inbox, or Unhandled.

The pipeline already does most of this: the AI classifier dynamically routes quote-type
captures to `MatchedSkill="Vikunja"` + a project bucket, and `QuotesEnricher` builds an
enriched description (`> "quote" — author` plus an AI-fetched author bio). Two gaps remain:

1. **Naming.** Everything in code uses the English `Quotes` (enricher `BucketName`, classifier
   prompt example, Skills health-table seed row). The issue — and the user's live Vikunja
   project — use the German **`Zitate`** (consistent with sibling projects `Bücher`,
   `Reiseliste`).
2. **The demo seed misrepresents the feature.** `demo/reset/seed.sql` row 10 shows a quote
   routed to **Wallabag** (`Completed`, `ExternalRef=wb-demo-010`) — the *pre-enrichment*
   behavior. The public demo never shows a quote landing in a Vikunja `Zitate` project.

## Scope

**In scope (decided):** Demo seed + naming alignment. Full rename `Quotes` → `Zitate` across
the routing path, including class/file renames and the Skills health-table seed (via a new EF
migration). Keep the existing enrichment format.

**Out of scope:**
- **Live VPS routing (#37).** This change makes `Zitate` the canonical bucket; the *live*
  Vikunja project must also be named `Zitate` or live quotes fall back to Inbox. Flagged as a
  dependency of #37 — not claimed working here.
- **Vikunja provisioning sidecar.** No bootstrap that creates the project via API. The repo's
  reset sidecar only truncates+reseeds the FlowHub `Captures` table; Vikunja projects are
  discovered live. The issue's "mirroring the Vikunja Inbox/bootstrap pattern" does not map to
  anything that exists, so it is not built.

## Decisions

| Question | Decision |
|---|---|
| Canonical project name | **`Zitate`** (rename, no `Quotes` alias) |
| Scope of "solve #39" | Demo seed fixture + naming alignment (no live wiring, no provisioning) |
| Enrichment format | Keep current: blockquote + AI-generated `**About <author>:** <bio>` |
| Rename depth | Full — Tier 1 + Tier 2 (class rename + Skills-seed migration) |

## Changes

### Tier 1 — Core (makes #39 correct + demo truthful)

1. `source/FlowHub.AI/Enrichers/QuotesEnricher.cs` — `BucketName`: `"Quotes"` → `"Zitate"`.
2. `source/FlowHub.AI/AiPrompts.cs` — entity example line `Quotes → {"quote": "...", "author": "..."}`
   → `Zitate → {"quote": "...", "author": "..."}`. (Entity *keys* `quote`/`author` stay — they
   are field names the enricher reads, not the bucket name.)
3. `demo/reset/seed.sql` — row 10 (the Austin Freeman quote): change from Wallabag to Vikunja:
   - `MatchedSkill`: `'Wallabag'` → `'Vikunja'`
   - `VikunjaProject`: `NULL` → `'Zitate'`
   - `ExternalRef`: `'wb-demo-010'` → `'vk-demo-010'`
   - `Stage` stays `'Completed'`; Title stays `'Quote — Austin Freeman'`.
   - The `quote` tag on that row stays.
   - Result: the demo Captures grid / detail shows the `→ Zitate` chip (`LifecycleBadge`).
   - Note: `EnrichmentDescription` is a transient field (event-carried, consumed by the Vikunja
     writer), **not** a persisted DB column — the seed cannot show the author-bio text, and the
     public demo disables Vikunja writes anyway. The visible demo signal is the routing chip +
     Title, which is the correct outcome to showcase.
4. Update tests that assert the **quote bucket** specifically, expecting `Zitate`:
   - `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs` (`BucketName_IsQuotes`
     → expects `Zitate`; rename test accordingly)
   - `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`
   - `tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs`
   - `tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs`
   - `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`
   - `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs`
   - `tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs` (`→ Zitate`)
   - Generic catalog-parse test data that uses `"Quotes"` only as an arbitrary project title
     (`VikunjaProjectCatalogTests`, `VikunjaRoutingAndEnrichmentContractTests` catalog stub) is
     **not** about our bucket and stays as-is, except where the routed bucket under test is the
     quote bucket — those (`ContractTests` `VikunjaProject: "Quotes"`) move to `Zitate`.

### Tier 2 — Full consistency

5. Rename class + file `QuotesEnricher` → `ZitateEnricher`, `QuotesEnricherPrompts` →
   `ZitateEnricherPrompts`:
   - `source/FlowHub.AI/Enrichers/QuotesEnricher.cs` → `ZitateEnricher.cs`
   - `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs` → `ZitateEnricherPrompts.cs`
   - `source/FlowHub.AI/AiServiceCollectionExtensions.cs` line 70 DI registration
   - `LoggerMessage` text ("QuotesEnricher bio fetch failed …" → "ZitateEnricher …")
   - Test references to the type name.
6. Rename the **Skills health-table seed row** `Quotes` → `Zitate`:
   - `source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs` `HasData` row
     (`Name = "Quotes"` → `"Zitate"`, keep `Status = "Degraded"`).
   - DI stubs: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` (project list) and
     `source/FlowHub.Web/Stubs/SkillRegistryStub.cs` (`new("Quotes", …)` → `Zitate`).
   - **New EF migration `0010_RenameQuotesSkillToZitate`** generated via `dotnet ef migrations add`
     — updates the seeded row (`UpdateData`/delete+insert as EF emits) and regenerates the model
     snapshot. **Never edit the applied `0005` migration.**
   - Tests asserting the dashboard row: `SmokeTests` (×2 `Contain("Quotes")`),
     `DashboardCards/SkillHealthCardTests` (`new("Quotes", …)` + `Contain("Quotes")`).
7. Live integration test alignment (Skip-gated, harmless when project absent):
   - `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs` — `CanonicalBuckets`
     `"Quotes"` → `"Zitate"`, routed-bucket `VikunjaProject: "Quotes"` → `"Zitate"`, Skip message
     text. This documents the #37 dependency: the live Vikunja project must be named `Zitate`.

## Testing strategy

Per repo testing rules (write/adjust the failing test first):

1. Flip the bucket-name expectations in the Tier-1 tests to `Zitate` → they fail.
2. Apply the `BucketName` + prompt change → they pass.
3. Apply the class/file rename → compile + tests stay green.
4. Add migration `0010`; verify `dotnet build` clean and no model-snapshot drift
   (`dotnet ef migrations add` leaves snapshot consistent).
5. Run full `dotnet test FlowHub.slnx` — all green (excluding the Skip-gated live integration
   tests, which require a real Vikunja).
6. Sanity-check the demo seed: `seed.sql` row 10 yields `→ Zitate` in the Captures grid.

## Risks / notes

- **Live routing regression risk:** if the live Vikunja still has a `Quotes` project (not
  `Zitate`), live quotes route to Inbox. Mitigated by being out of scope + flagged for #37;
  the canonical name is now unambiguously `Zitate`.
- **Migration is additive and reversible** (`Down` restores `Quotes`). Demo reset is unaffected
  (it reseeds `Captures`, not `Skills`).
- No new packages, no target-framework changes, no new patterns.
