# Route Quote Captures to Vikunja "Zitate" — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the quote routing bucket `Quotes` → `Zitate` across the routing path (enricher, prompt, Skills health-table seed, tests) and fix the demo seed so the public demo shows a quote landing enriched in Vikunja `Zitate`.

**Architecture:** This is a rename refactor of an existing, working pipeline plus one demo-fixture fix. The classifier already routes quote captures to `MatchedSkill="Vikunja"` + a project bucket, and an enricher builds the description. We change the bucket *name* to `Zitate` and update everything that references it. Source + its dependent component tests must move in one commit to keep the build green; the persistence seed, contract/live tests, and demo SQL are independent and get their own tasks.

**Tech Stack:** .NET 10, C#, EF Core (SQLite/Postgres), xUnit + FluentAssertions + NSubstitute + bUnit, WireMock (contract tests). Spec: `docs/superpowers/specs/2026-06-10-zitate-routing-design.md`.

---

## Pre-flight: branch

- [ ] **Step 0: Create a feature branch off `main`**

This worktree (`worktree-misc`) carries unrelated uncommitted changes. Work on a dedicated branch and only stage the files each task names.

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
git checkout -b feat/zitate-routing
```
Expected: `Switched to a new branch 'feat/zitate-routing'`

---

## File Structure

**Source (FlowHub.AI):**
- Rename `source/FlowHub.AI/Enrichers/QuotesEnricher.cs` → `ZitateEnricher.cs` — the enricher; `BucketName => "Zitate"`.
- Rename `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs` → `ZitateEnricherPrompts.cs` — bio-fetch prompt.
- Modify `source/FlowHub.AI/AiServiceCollectionExtensions.cs` — DI registration + comment.
- Modify `source/FlowHub.AI/AiPrompts.cs` — classifier prompt entity example.

**Source (FlowHub.Persistence + Web stubs):**
- Modify `source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs` — `HasData` seed row.
- New migration `source/FlowHub.Persistence/Migrations/*_0010_RenameQuotesSkillToZitate.cs` (+ Designer + snapshot) — EF-generated.
- Modify `source/FlowHub.Web/Stubs/SkillRegistryStub.cs` and `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` — in-memory skill lists.

**Tests:**
- Rename `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs` → `ZitateEnricherTests.cs`.
- Modify `ClassifyAndEnrichPipelineTests.cs`, `EnricherDispatcherTests.cs`, `ClassificationResultTests.cs`, `Ai/AiClassifierTests.cs`, `Ai/AiPromptsTests.cs`, `Shared/LifecycleBadgeTests.cs`, `SmokeTests.cs`, `DashboardCards/SkillHealthCardTests.cs`.
- Modify `tests/FlowHub.Skills.ContractTests/Vikunja/VikunjaRoutingAndEnrichmentContractTests.cs`.
- Modify `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs`.

**Demo:**
- Modify `demo/reset/seed.sql` — row 10 fixture.

---

## Task 1: Rename enricher + bucket in FlowHub.AI and its component tests

This is one atomic commit — renaming the class breaks compilation of the test project until all references move together.

**Files:**
- Rename: `source/FlowHub.AI/Enrichers/QuotesEnricher.cs` → `source/FlowHub.AI/Enrichers/ZitateEnricher.cs`
- Rename: `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs` → `source/FlowHub.AI/Enrichers/ZitateEnricherPrompts.cs`
- Modify: `source/FlowHub.AI/AiServiceCollectionExtensions.cs:69-70`
- Modify: `source/FlowHub.AI/AiPrompts.cs:34`
- Rename + modify: `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs` → `ZitateEnricherTests.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs:190,201`
- Modify: `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs:9`
- Modify: `tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs:49,51`

- [ ] **Step 1: Update the behavior assertion test first (bucket name)**

In `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs`, change the bucket-name test (lines 72-78). Old:

```csharp
    [Fact]
    public async Task BucketName_IsQuotes()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);
        enricher.BucketName.Should().Be("Quotes");
    }
```

New:

```csharp
    [Fact]
    public async Task BucketName_IsZitate()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);
        enricher.BucketName.Should().Be("Zitate");
    }
```

- [ ] **Step 2: Rename the source files (preserve git history)**

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
git mv source/FlowHub.AI/Enrichers/QuotesEnricher.cs source/FlowHub.AI/Enrichers/ZitateEnricher.cs
git mv source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs source/FlowHub.AI/Enrichers/ZitateEnricherPrompts.cs
git mv tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs tests/FlowHub.Web.ComponentTests/Classification/ZitateEnricherTests.cs
```

- [ ] **Step 3: Update `ZitateEnricher.cs`**

In `source/FlowHub.AI/Enrichers/ZitateEnricher.cs` apply these exact replacements:
- Line 9: `public sealed partial class QuotesEnricher : IEnricher` → `public sealed partial class ZitateEnricher : IEnricher`
- Line 12: `private readonly ILogger<QuotesEnricher> _log;` → `private readonly ILogger<ZitateEnricher> _log;`
- Line 14: `public QuotesEnricher(IChatClient chat, ILogger<QuotesEnricher> log)` → `public ZitateEnricher(IChatClient chat, ILogger<ZitateEnricher> log)`
- Line 20: `public string BucketName => "Quotes";` → `public string BucketName => "Zitate";`
- Line 65: `QuotesEnricherPrompts.BuildMessages(author),` → `ZitateEnricherPrompts.BuildMessages(author),`
- Line 82: `Message = "QuotesEnricher bio fetch failed for author='{Author}' (reason={Reason})")]` → `Message = "ZitateEnricher bio fetch failed for author='{Author}' (reason={Reason})")]`

- [ ] **Step 4: Update `ZitateEnricherPrompts.cs`**

In `source/FlowHub.AI/Enrichers/ZitateEnricherPrompts.cs`, line 5:
`internal static class QuotesEnricherPrompts` → `internal static class ZitateEnricherPrompts`

- [ ] **Step 5: Update DI registration**

In `source/FlowHub.AI/AiServiceCollectionExtensions.cs`, lines 69-70. Old:
```csharp
        // QuotesEnricher needs IChatClient — only register when AI is configured.
        services.AddSingleton<IEnricher, QuotesEnricher>();
```
New:
```csharp
        // ZitateEnricher needs IChatClient — only register when AI is configured.
        services.AddSingleton<IEnricher, ZitateEnricher>();
```

- [ ] **Step 6: Update the classifier prompt entity example**

In `source/FlowHub.AI/AiPrompts.cs`, line 34. Old:
```
                Quotes → {"quote": "...", "author": "..."}
```
New:
```
                Zitate → {"quote": "...", "author": "..."}
```
(Leave the entity keys `quote`/`author` — they are field names the enricher reads, not the bucket name. Leave the `Movies → {...}` line below it unchanged.)

- [ ] **Step 7: Update `ZitateEnricherTests.cs` type references**

In `tests/FlowHub.Web.ComponentTests/Classification/ZitateEnricherTests.cs`:
- Line 12: `public class QuotesEnricherTests` → `public class ZitateEnricherTests`
- Line 20: `new(["quote"], "Vikunja", "Gabriel on Unix and C", "Quotes",` → `new(["quote"], "Vikunja", "Gabriel on Unix and C", "Zitate",`
- Lines 35, 48, 65, 76, 92: every `new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance)` → `new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance)` (5 occurrences — use replace-all).

(The `BucketName_IsZitate` test from Step 1 is already correct.)

- [ ] **Step 8: Update `ClassifyAndEnrichPipelineTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs`:
- Line 24: `public async Task RichardGabrielQuote_RoutesToQuotesAndProducesBio()` → `public async Task RichardGabrielQuote_RoutesToZitateAndProducesBio()`
- Line 26 comment: `// Catalog with both Inbox and Quotes` → `// Catalog with both Inbox and Zitate`
- Line 29: `.Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Quotes"] = 7 });` → `... { ["Inbox"] = 1, ["Zitate"] = 7 });`
- Line 45: `project = "Quotes",` → `project = "Zitate",`
- Line 59: `new IEnricher[] { new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance) },` → `new IEnricher[] { new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance) },`
- Line 71: `project.Should().Be("Quotes");` → `project.Should().Be("Zitate");`

- [ ] **Step 9: Update `EnricherDispatcherTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`, replace every `"Quotes"` with `"Zitate"` (lines 31, 35, 39, 64, 70, 75, 77, 85, 91, 96, 98 — use replace-all on the string literal `"Quotes"`). These are the dispatcher's bucket under test; renaming keeps them representing the real bucket.

- [ ] **Step 10: Update `ClassificationResultTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs`:
- Line 23: `new ClassificationResult(["quote"], "Vikunja", "title", "Quotes", entities);` → `... "title", "Zitate", entities);`
- Line 25: `result.VikunjaProject.Should().Be("Quotes");` → `result.VikunjaProject.Should().Be("Zitate");`

- [ ] **Step 11: Update `AiClassifierTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`:
- Line 25: `.Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Quotes"] = 7 });` → `... ["Zitate"] = 7 });`
- Line 190: `project = "Quotes",` → `project = "Zitate",`
- Line 201: `result.VikunjaProject.Should().Be("Quotes");` → `result.VikunjaProject.Should().Be("Zitate");`

- [ ] **Step 12: Update `AiPromptsTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs`, line 9:
`private static readonly string[] DefaultBuckets = ["Inbox", "Quotes"];` → `... = ["Inbox", "Zitate"];`
(The `HasNoGermanRoutingTokens` test only forbids `Ablage`/`Aufgabe`, so `Zitate` is fine.)

- [ ] **Step 13: Update `LifecycleBadgeTests.cs`**

In `tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs`:
- Line 49: `.Add(p => p.VikunjaProject, "Quotes"));` → `.Add(p => p.VikunjaProject, "Zitate"));`
- Line 51: `cut.Markup.Should().Contain("→ Quotes");` → `cut.Markup.Should().Contain("→ Zitate");`

- [ ] **Step 14: Build and run the affected test project**

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
dotnet test tests/FlowHub.Web.ComponentTests
```
Expected: build succeeds (warnings-as-errors clean) and all tests PASS, including `BucketName_IsZitate` and `RichardGabrielQuote_RoutesToZitateAndProducesBio`.

- [ ] **Step 15: Commit**

```bash
git add source/FlowHub.AI tests/FlowHub.Web.ComponentTests
git commit -m "refactor(ai): rename Quotes enricher/bucket to Zitate (#39)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Rename the Skills health-table seed row + DI stubs (+ EF migration)

**Files:**
- Modify: `source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs:22`
- Modify: `source/FlowHub.Web/Stubs/SkillRegistryStub.cs:15`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs:18`
- New (EF-generated): `source/FlowHub.Persistence/Migrations/*_0010_RenameQuotesSkillToZitate.cs` (+ `.Designer.cs` + updated `FlowHubDbContextModelSnapshot.cs`)
- Modify: `tests/FlowHub.Web.ComponentTests/SmokeTests.cs:79,230`
- Modify: `tests/FlowHub.Web.ComponentTests/DashboardCards/SkillHealthCardTests.cs:38,45`

- [ ] **Step 1: Update the dashboard tests first (expect Zitate)**

In `tests/FlowHub.Web.ComponentTests/SmokeTests.cs`:
- Line 79 (`Dashboard_SkillHealth_ShowsSkillNames`): `cut.Markup.Should().Contain("Quotes");` → `cut.Markup.Should().Contain("Zitate");`
- Line 230 (`SkillsPage_ShowsAllSkillsFromRegistry`): `cut.Markup.Should().Contain("Quotes");` → `cut.Markup.Should().Contain("Zitate");`

In `tests/FlowHub.Web.ComponentTests/DashboardCards/SkillHealthCardTests.cs`:
- Line 38: `new("Quotes", HealthStatus.Degraded,  2),` → `new("Zitate", HealthStatus.Degraded,  2),`
- Line 45: `cut.Markup.Should().Contain("Quotes");` → `cut.Markup.Should().Contain("Zitate");`

- [ ] **Step 2: Update the DI stubs**

In `source/FlowHub.Web/Stubs/SkillRegistryStub.cs`, line 15:
`new("Quotes",    HealthStatus.Degraded,  2),` → `new("Zitate",    HealthStatus.Degraded,  2),`

In `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`, line 18:
`["Movies", "Articles", "Books", "Quotes", "Knowledge", "Homelab", "Belege"];` → `["Movies", "Articles", "Books", "Zitate", "Knowledge", "Homelab", "Belege"];`

- [ ] **Step 3: Update the EF seed (`HasData`)**

In `source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs`, line 22:
`            new SkillEntity { Name = "Quotes",    Status = "Degraded", RoutedToday = 0 },` → `            new SkillEntity { Name = "Zitate",    Status = "Degraded", RoutedToday = 0 },`

- [ ] **Step 4: Generate the migration**

EF diffs the model (now seeding `Zitate`) against the last snapshot (seeding `Quotes`) and emits a `DeleteData("Quotes")` + `InsertData("Zitate")` (Name is the PK), and regenerates the model snapshot — no hand-editing.

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
dotnet ef migrations add 0010_RenameQuotesSkillToZitate \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```
Expected: `Done.` and three changed/new files under `source/FlowHub.Persistence/Migrations/` (the migration, its Designer, and the updated `FlowHubDbContextModelSnapshot.cs`).

- [ ] **Step 5: Verify the generated migration content**

Run:
```bash
git diff --stat source/FlowHub.Persistence/Migrations/
grep -rn "Quotes\|Zitate" source/FlowHub.Persistence/Migrations/*0010*.cs
```
Expected: the `Up` deletes the `Quotes` key and inserts `Zitate` (Status `Degraded`); the `Down` restores `Quotes`. If EF instead emitted an `UpdateData`, that is equally valid.

- [ ] **Step 6: Build + run the persistence and component tests**

Run:
```bash
dotnet build source/FlowHub.Persistence
dotnet test tests/FlowHub.Web.ComponentTests
```
Expected: build clean (warnings-as-errors), all tests PASS.

- [ ] **Step 7: Commit**

```bash
git add source/FlowHub.Persistence source/FlowHub.Web/Stubs tests/FlowHub.Web.ComponentTests
git commit -m "refactor(persistence): rename Quotes skill seed to Zitate + migration 0010 (#39)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Update contract + live integration tests

These reference `Quotes` as strings (not the renamed type), so they compile independently of Task 1.

**Files:**
- Modify: `tests/FlowHub.Skills.ContractTests/Vikunja/VikunjaRoutingAndEnrichmentContractTests.cs`
- Modify: `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs`

- [ ] **Step 1: Contract test — rename the bucket + id constant**

In `tests/FlowHub.Skills.ContractTests/Vikunja/VikunjaRoutingAndEnrichmentContractTests.cs`:
- Line 20: `private const int QuotesId = 7;` → `private const int ZitateId = 7;`
- Line 74: catalog body — `{"id":7,"title":"Quotes"}` → `{"id":7,"title":"Zitate"}`. Full new line:
  ```csharp
                .WithBody("""[{"id":1,"title":"Inbox"},{"id":7,"title":"Zitate"},{"id":12,"title":"Movies"}]"""));
  ```
- Line 86: `VikunjaProject: "Quotes",` → `VikunjaProject: "Zitate",`
- Lines 95, 110, 121, 166: every `{QuotesId}` → `{ZitateId}` (replace-all `QuotesId` → `ZitateId` covers lines 20, 95, 110, 121, 166).

- [ ] **Step 2: Live integration test — rename canonical bucket + messages**

In `tests/FlowHub.Skills.IntegrationTests/VikunjaCatalogLiveTests.cs`:
- Line 11 (doc comment): `(Inbox, Quotes, Movies, Ausflugziele)` → `(Inbox, Zitate, Movies, Ausflugziele)`
- Line 14 (doc comment): `an end-to-end Quotes route` → `an end-to-end Zitate route`
- Line 22: `private static readonly string[] CanonicalBuckets = ["Inbox", "Quotes", "Movies", "Ausflugziele"];` → `... ["Inbox", "Zitate", "Movies", "Ausflugziele"];`
- Line 83: `public async Task HandleAsync_LiveVikunja_RoutesQuoteCaptureToQuotesProject()` → `public async Task HandleAsync_LiveVikunja_RoutesQuoteCaptureToZitateProject()`
- Lines 90-91 (comment): `the Quotes bucket isn't provisioned` → `the Zitate bucket isn't provisioned`
- Line 93: `Skip.If(!map.ContainsKey("Quotes"),` → `Skip.If(!map.ContainsKey("Zitate"),`
- Line 94: `"Live Vikunja instance has no 'Quotes' project — see ...` → `"Live Vikunja instance has no 'Zitate' project — see ...`
- Line 111: `VikunjaProject: "Quotes",` → `VikunjaProject: "Zitate",`

> These tests are `[Trait("Category","BetaSmoke")]` / Skip-gated — excluded from CI and skipped without a live Vikunja. The rename documents the **#37 dependency**: the live Vikunja project must be named `Zitate` (or live quotes fall back to Inbox).

- [ ] **Step 3: Build the two test projects**

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
dotnet test tests/FlowHub.Skills.ContractTests
dotnet build tests/FlowHub.Skills.IntegrationTests
```
Expected: contract tests build + PASS; integration project builds clean (its tests Skip without env vars).

- [ ] **Step 4: Commit**

```bash
git add tests/FlowHub.Skills.ContractTests tests/FlowHub.Skills.IntegrationTests
git commit -m "test(skills): rename Quotes routing bucket to Zitate in contract + live tests (#39)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Fix the demo seed fixture

Make the public demo show a quote routed to Vikunja `Zitate` instead of Wallabag.

**Files:**
- Modify: `demo/reset/seed.sql:66-69`

- [ ] **Step 1: Rewrite row 10 (the Austin Freeman quote)**

In `demo/reset/seed.sql`, replace the row-10 block (currently Wallabag). Old:
```sql
  -- 10. free-text quote -> Wallabag, completed (AI-style)
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
   'Save this quote: "Simplicity is the soul of efficiency." — Austin Freeman','Web','Completed', NOW() - INTERVAL '12 minutes',
   'Wallabag','Quote — Austin Freeman','wb-demo-010',NULL,NULL, NULL,NULL,NULL,NULL,NULL);
```
New (route to Vikunja `Zitate`; column order matches the INSERT list `…,"MatchedSkill","Title","ExternalRef","FailureReason","VikunjaProject",…`):
```sql
  -- 10. free-text quote -> Vikunja 'Zitate', completed + enriched (AI-style: classify -> Zitate route)
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
   'Save this quote: "Simplicity is the soul of efficiency." — Austin Freeman','Web','Completed', NOW() - INTERVAL '12 minutes',
   'Vikunja','Quote — Austin Freeman','vk-demo-010',NULL,'Zitate', NULL,NULL,NULL,NULL,NULL);
```
(The `quote` tag for this id at line 83 stays — no change to the `Tags` insert.)

- [ ] **Step 2: Sanity-check the SQL shape**

Confirm the row still has exactly the 15 values matching the INSERT column list and the row terminates the VALUES list with `;`.

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
grep -n "vk-demo-010\|'Zitate'" demo/reset/seed.sql
```
Expected: one line shows `'Vikunja','Quote — Austin Freeman','vk-demo-010',NULL,'Zitate'`.

- [ ] **Step 3: Commit**

```bash
git add demo/reset/seed.sql
git commit -m "feat(demo): route the seed quote fixture to Vikunja 'Zitate' (#39)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Full-suite verification + grep sweep

- [ ] **Step 1: Run the whole solution's test suite**

Run:
```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/misc
dotnet test FlowHub.slnx
```
Expected: build clean, all non-Skip tests PASS. (BetaSmoke/live tests Skip without env vars — that is expected, not a failure.)

- [ ] **Step 2: Grep sweep for stray `Quotes`/`QuotesEnricher` in code**

Run:
```bash
grep -rn "QuotesEnricher\|\"Quotes\"\|→ Quotes" --include=*.cs --include=*.sql source/ tests/ demo/
```
Expected: **no matches** in the routing path. Acceptable remaining matches: generic catalog-parse test data where `Quotes` is an arbitrary project title unrelated to our bucket (e.g. `VikunjaProjectCatalogTests` `title = "Quotes"`) — leave those, they test JSON parsing, not routing. If any *routing/seed/enricher* reference remains, fix it and re-run Step 1.

> Note: `VikunjaProjectCatalogTests.cs` line ~32/40 uses `"Quotes"` purely as a sample project title to test catalog parsing; it is not the routing bucket and is intentionally left unchanged.

- [ ] **Step 3: Optional manual demo check**

If a demo Postgres is up, the reset reseeds `Captures`; the grid/detail for the Austin Freeman quote should show the `→ Zitate` chip. (`EnrichmentDescription` is transient and Vikunja writes are disabled in the public demo, so the bio text is not shown — the routing chip + Title are the intended signal.)

- [ ] **Step 4: Push + open PR**

```bash
git push -u origin feat/zitate-routing
gh pr create --fill --base main
```
Mention in the PR body that this closes #39 (scope: demo seed + naming), and that **live VPS routing remains #37** — the live Vikunja project must be named `Zitate`.

---

## Self-Review (author check)

- **Spec coverage:** Tier-1 (BucketName/prompt/demo seed/affected tests) → Tasks 1 & 4. Tier-2 (class rename, Skills-seed migration, stubs) → Tasks 1 & 2. Live-test alignment + #37 flag → Task 3. ✓
- **Placeholders:** none — every edit shows exact old→new. ✓
- **Type consistency:** `ZitateEnricher`, `ZitateEnricherPrompts`, `BucketName == "Zitate"`, `ZitateId == 7` used consistently across tasks. ✓
- **Migration approach:** EF-generated (HasData edited first, then `migrations add`); snapshot auto-updated; never edits applied `0005`. ✓
