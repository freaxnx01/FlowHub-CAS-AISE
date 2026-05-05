# Beta MVP — Operator Acceptance Runbook (Task 21)

- **Source:** `docs/superpowers/plans/2026-05-04-beta-mvp.md` §"Task 21"
- **Goal:** Validate the Beta MVP slice end-to-end against the real homelab Wallabag + Vikunja, then push the branch.
- **Branch:** `feat/beta-mvp` (28 commits ahead of `main`)
- **Pre-state:** `make build` clean, `make test` 138/138 green. All operator-facing changes already committed.

---

## What this runbook does

Walks through the seven-step demo path from the spec's *"Demo path (acceptance)"* section, plus the live Beta-smoke test run and the final push. Without this validation the Beta MVP slice is technically complete but architecturally unverified.

**Time budget:** 30–60 min depending on how many test captures you make.

---

## Step 21.1 — Set user secrets

```bash
cd source/FlowHub.Web

dotnet user-secrets set "Ai:Provider" "Anthropic"
dotnet user-secrets set "Ai:Anthropic:ApiKey" "<sk-ant-…>"
dotnet user-secrets set "Skills:Wallabag:BaseUrl" "https://wallabag.home.freaxnx01.ch"
dotnet user-secrets set "Skills:Wallabag:ApiToken" "<wallabag-pat>"
dotnet user-secrets set "Skills:Vikunja:BaseUrl" "https://vikunja.home.freaxnx01.ch"
dotnet user-secrets set "Skills:Vikunja:ApiToken" "<vikunja-pat>"
dotnet user-secrets set "Skills:Vikunja:DefaultProjectId" "42"

cd ../..
```

**Where to get the tokens:**

- Anthropic API key: console.anthropic.com → API keys
- Wallabag PAT: Wallabag UI → `/developer/new-token` (or "Site → Developer → My applications → Create a new client" depending on Wallabag version)
- Vikunja API token: Vikunja UI → Settings → API Tokens → New token (give it `tasks:write` scope minimum)
- Vikunja project id: open the target Vikunja project in the UI; the project id is in the URL (`/projects/<id>`). The plan example uses `42` — substitute your real one.

**Sanity check after setting:**

```bash
cd source/FlowHub.Web
dotnet user-secrets list
cd ../..
```

You should see all 7 keys listed. Tokens are visible in plaintext (that's how user-secrets works); they live under `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`.

---

## Step 21.2 — Boot and verify EventIds

```bash
make run > /tmp/flowhub-boot.log 2>&1 &
sleep 8
grep -E "EventId.*(3020|4020|5010|5011)" /tmp/flowhub-boot.log
```

**Expected stdout** (each on its own line):

- `EventId 3020 — AI provider registered (provider=Anthropic, model=claude-haiku-4-5-20251001)`
- `EventId 4020 — Skill registered (skill=Wallabag)`
- `EventId 4020 — Skill registered (skill=Vikunja)`
- `EventId 5010 — Applying EF Core migrations…`
- `EventId 5011 — EF Core migrations up-to-date.`

If any of those is **missing**, stop the app (`pkill -f FlowHub.Web`) and check:

| Missing EventId | Likely cause | Fix |
|---|---|---|
| `3020` | `Ai:Provider` or `Ai:Anthropic:ApiKey` not set | re-run Step 21.1; `dotnet user-secrets list` to verify |
| `4020` (Wallabag) | `Skills:Wallabag:BaseUrl` or `:ApiToken` not set | re-run Step 21.1; expect `4021 SkillNotConfigured` instead in this case |
| `4020` (Vikunja) | `Skills:Vikunja:BaseUrl`, `:ApiToken`, or `:DefaultProjectId` missing/zero | re-run Step 21.1; `:DefaultProjectId` must be `> 0` |
| `5010`/`5011` | `MigrationRunner` IHostedService not started — usually means `AddFlowHubPersistence` isn't wired in `Program.cs` | git log shows it landed in commit `27c9946`; if branch is broken, `git diff main..HEAD -- source/FlowHub.Web/Program.cs` |

Leave the app running for the next step (don't kill the background process yet).

---

## Step 21.3 — Browser: empty Recent Captures

Open `http://localhost:5070/` in your browser. **Recent Captures grid should be empty** (the SQLite DB at `source/FlowHub.Web/flowhub.db` is fresh on first boot, *unless* it survived from earlier smoke tests).

For a strictly-clean start (optional):

```bash
pkill -f FlowHub.Web
rm -f source/FlowHub.Web/flowhub.db
make run > /tmp/flowhub-boot.log 2>&1 &
sleep 8
```

You can leave older captures in place — the demo only needs to add three new ones, and the matrix is identifiable by content + time.

---

## Step 21.4 — URL capture → Wallabag

In the AppBar **Quick-Capture** field (top of every page), paste:

```
https://en.wikipedia.org/wiki/Hexagonal_architecture
```

Submit (Enter or the submit button).

**Expected within ~3 seconds** in the Recent Captures grid:

| Field | Expected value |
|---|---|
| Stage | `Completed` |
| Skill | `Wallabag` |
| Title | `Hexagonal architecture` (or close — the AI's call; could be "Hexagonal architecture (software)" or similar) |
| Content | the URL |

**Cross-check:** open Wallabag's homelab UI → "Unread" view. The new entry should be there with the article title and content scraped.

**If Stage stuck at `Raw` for > 10s:** AI classifier is not firing. Check `/tmp/flowhub-boot.log` for `EventId 3010 AiClassifierFellBackToKeyword` (means AI errored, fell back to deterministic) — the deterministic `KeywordClassifier` should still match URLs to Wallabag, so Stage should still progress.

**If Stage `Unhandled`:** integration call failed. Click into the capture (`/captures/{id}`) — the Capture Detail page now surfaces the `FailureReason` (per the final-review fixup in commit `c5340e9`). Common causes:

- 401 Unauthorized → token wrong or expired
- 503/timeout → Wallabag homelab not reachable from the dev box
- *Schema mismatch* (`Wallabag response did not include an 'id' field`) → unusual Wallabag version

---

## Step 21.5 — Todo capture → Vikunja

Quick-capture:

```
todo: review Block 4 prep tomorrow
```

**Expected within ~3 s:**

| Field | Expected value |
|---|---|
| Stage | `Completed` |
| Skill | `Vikunja` |
| Title | `Review Block 4 prep tomorrow` (or similar — AI may capitalize/normalize) |
| ExternalRef | a numeric id (Vikunja task id) |

**Cross-check:** open Vikunja's project (the one matching `Skills:Vikunja:DefaultProjectId` from Step 21.1) → the new task should be visible.

The `todo:` prefix is what the `KeywordClassifier` and `AiClassifier` use to route to Vikunja. The `Title` is what the classifier produces; if it's null (e.g. AI fallback), the integration falls back to the truncated content (first 120 chars).

---

## Step 21.6 — Nonsense capture → Orphan

Quick-capture:

```
asdfqwerty
```

**Expected within ~3 s:**

| Field | Expected value |
|---|---|
| Stage | `Orphan` |
| Skill | `—` (none matched) |
| FailureReason | something like `"no skill matched"` or AI's explanation |

The Dashboard's **"Needs Attention"** card should increment by 1.

This validates the Orphan path: AI classifier returned empty `MatchedSkill`, `CaptureEnrichmentConsumer` saw the empty match, called `MarkOrphanAsync`. The capture never reached `SkillRoutingConsumer`.

---

## Step 21.7 — Restart, state survives

```bash
pkill -f FlowHub.Web
make run > /tmp/flowhub-boot.log 2>&1 &
sleep 8
```

Browser → `http://localhost:5070/`. **All three captures from steps 21.4–21.6 should still be visible** in the Recent Captures grid with their stages intact. This is the core persistence validation — without EF Core / SQLite, the in-memory stub would lose them all.

The `flowhub.db` SQLite file at `source/FlowHub.Web/flowhub.db` is the durable state. To inspect:

```bash
sqlite3 source/FlowHub.Web/flowhub.db "SELECT Id, Stage, Source, Title, ExternalRef FROM Captures ORDER BY CreatedAt DESC LIMIT 10;"
```

Stop the app for the next step:

```bash
pkill -f FlowHub.Web
```

---

## Step 21.8 — Run live Beta tests (`make test-beta`)

Export the same secrets as env vars (the trait-gated tests resolve them via `Environment.GetEnvironmentVariable`):

```bash
export Skills__Wallabag__BaseUrl=https://wallabag.home.freaxnx01.ch
export Skills__Wallabag__ApiToken=<wallabag-pat>
export Skills__Vikunja__BaseUrl=https://vikunja.home.freaxnx01.ch
export Skills__Vikunja__ApiToken=<vikunja-pat>
export Skills__Vikunja__DefaultProjectId=42

make test-beta
```

**Expected:** 2 tests pass (`WallabagLiveTests.HandleAsync_LiveWallabag_PostsUrlAndReturnsExternalRef`, `VikunjaLiveTests.HandleAsync_LiveVikunja_PutsTaskAndReturnsExternalRef`).

**Side effect:** 2 new entries appear in Wallabag and Vikunja (a Wikipedia article and a `todo: FlowHub Beta smoke test <ISO timestamp>` task). These are not cleaned up; manually delete them if you want a clean slate.

If the tests fail with `Skip:` messages, the env vars aren't being read — check the underscore convention (`Skills__Wallabag__BaseUrl` with **double underscores**, not single).

---

## Step 21.9 — Final default suite

```bash
make build && make test
```

**Expected:** clean build (warnings-as-errors), 138/138 tests pass under `Category!=AI&Category!=BetaSmoke`.

This is your gate for Step 21.10. If anything is red here, **don't push** — fix or roll back first.

---

## Step 21.10 — Push (final step)

Only after Steps 21.1–21.9 confirm green:

```bash
git push origin feat/beta-mvp
```

After push, decide:

- **Open a PR to `main`** for visibility (recommended — gives a place to attach the demo evidence).
- **Or merge directly** to `main` if no PR review is needed (single-user homelab project; PR may be ceremony).

Either way, document the homelab demo evidence in the PR description or in a follow-up commit on `main`:

- Screenshots of the three captures in Recent Captures (URL → Completed/Wallabag, todo → Completed/Vikunja, nonsense → Orphan)
- Screenshot or `sqlite3` dump showing rows survived restart
- The Wallabag entry id + Vikunja task id (matches `ExternalRef` columns)

This evidence is what feeds the *"Test-Ergebnisse dokumentiert (3)"* and *"Sub-Systeme als unabhängige Container deploybar (5)"* rubric items in Block 5's submission PDF.

---

## Cleanup (optional)

After the demo, you can:

```bash
rm source/FlowHub.Web/flowhub.db   # discard the demo SQLite DB
unset Skills__Wallabag__BaseUrl Skills__Wallabag__ApiToken \
      Skills__Vikunja__BaseUrl Skills__Vikunja__ApiToken Skills__Vikunja__DefaultProjectId
```

User-secrets persist on the dev machine (`~/.microsoft/usersecrets/`) — leave them in place; they're per-project and harmless.

---

## What this validates (rubric mapping)

- **Block 3 — KI-Werkzeug-Nutzung (12 pts):** the URL/todo flow exercises the production-runtime AI classifier; `make test-beta` evidence + screenshots strengthen the Block 5 submission section.
- **Block 4 — Sub-Systeme als unabhängige Container deploybar (5 pts):** Wallabag + Vikunja are external homelab services; FlowHub talks to them via HTTP. Validates the integration boundary.
- **Block 5 — Intelligente Services mit KI (6 pts):** AI classifier in the request path with graceful fallback (per ADR 0004) — exercised live.
- **Cross-block — Demo path (acceptance):** the seven steps above are the exact gating criteria the brainstorm spec called out (`docs/superpowers/specs/2026-05-04-beta-mvp-design.md` §"Demo path (acceptance)").

---

## References

- Brainstorm spec: `docs/superpowers/specs/2026-05-04-beta-mvp-design.md`
- Plan: `docs/superpowers/plans/2026-05-04-beta-mvp.md` (Task 21 is the last task)
- ADR 0005 — Persistence: `docs/adr/0005-persistence.md`
- ai-usage retrospective: `docs/ai-usage.md` §"Block 4 prep — Beta MVP"
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
