# TO-TEST

Manual smoke-test list for the new `make` targets, Playwright E2E, Bruno
collection, and the AiPing console runner. Run from the repo root.

Prereqs once: Docker, .NET 10 SDK, `pwsh` (for `playwright-install`),
Microsoft Edge installed (or `xdg-open` configured) for `just test-all`.

---

## 1. Make targets

### `just`
- [ ] Lists every target including `test-e2e`, `test-backend`, `test-frontend`,
      `test-all`, `playwright-install`, `db-ping`, `ai-ping`, `ai-classify`,
      `ai-embed`.

### `just db-ping`
- [ ] With Postgres compose container running → prints `tcp: ok` and
      `psql: SELECT 1 ok`, exits 0.
- [ ] With Postgres stopped (`docker compose stop postgres`) → prints
      `tcp: FAIL …`, exits non-zero.
- [ ] Overriding env works:
      `PGHOST=127.0.0.1 PGPORT=5432 PGUSER=flowhub PGPASSWORD=dev-secret just db-ping`.

### `just test-backend`
- [ ] Runs `FlowHub.Persistence.Tests`, `FlowHub.Skills.Tests`, and
      `FlowHub.Skills.ContractTests` (Category=SkillContract).
- [ ] Does **not** run `FlowHub.Web.ComponentTests` or any `*IntegrationTests`.
- [ ] Does not require AI / Beta env vars.

### `just test-backend` — Skill contract tests (WireMock.Net)
- [ ] Vikunja contract suite (6 tests) green: happy path, title fallback to
      content, 401 / 500 error mapping, missing-id response, bearer header
      on exact path `/api/v1/projects/{id}/tasks`.
- [ ] Wallabag contract suite (7 tests) green: happy path, non-URL content
      short-circuit (no HTTP call), non-http scheme rejection, 401 / 500
      error mapping, missing-id response, bearer header on exact path
      `/api/entries.json`.
- [ ] Each test class boots its own WireMock server on a random loopback
      port (no fixed port collisions if tests run in parallel).
- [ ] No `Skills__*` env vars required — contract tests run offline.

### `just test-frontend`
- [ ] Runs `FlowHub.Web.ComponentTests` only. Green on a clean checkout.

### `just test-e2e`
- [ ] First run: triggers `db-up`, `db-migrate`, `playwright-install`,
      starts FlowHub.Web, waits for `/health/live`, runs the happy-flow test,
      stops the server. Exit code reflects the test outcome.
- [ ] Subsequent runs reuse the existing Postgres + browser binaries (faster).
- [ ] On test failure, the server is still killed (no orphaned PID in
      `.make/web.pid`).
- [ ] If `pwsh` is not installed, `playwright-install` fails with a clear
      message — install pwsh and retry.

### `just test-all`
- [ ] Runs backend + frontend tests, then starts Postgres and FlowHub.Web in
      the background and opens `http://localhost:5070` in Microsoft Edge.
- [ ] Server keeps running after make exits; `cat .make/web.pid` returns a PID
      and `kill $(cat .make/web.pid)` stops it cleanly.
- [ ] Stopping via Ctrl+C while the server is up: server stays running (it was
      detached). Use the kill command above.
- [ ] If Edge is missing, falls back to `microsoft-edge-stable` or `xdg-open`,
      else prints the manual-open hint.

### `just test`
- [ ] Still works. Excludes `[Category=AI]`, `[Category=BetaSmoke]`,
      `[Category=E2E]`. Does **not** boot Playwright or the web server.

---

## 2. Playwright E2E (`tests/FlowHub.Web.E2ETests`)

Run via `just test-e2e`, or manually with a server already running:

```bash
just watch &  # in another shell, or use `just run`
FLOWHUB_E2E_BASE_URL=http://localhost:5070 dotnet test tests/FlowHub.Web.E2ETests --filter "Category=E2E"
```

- [ ] `HappyFlowTests.QuickCapture_TodoEntry_AppearsInCapturesListAndDetail`:
      - finds the AppBar QuickCapture input (`+ Quick capture: paste URL or type…`)
      - submits `todo: e2e happy-flow <guid>`
      - waits for the `Captured ✓` MudSnackbar
      - navigates to `/captures`
      - finds a table row containing the input text
      - clicks the row and lands on `/captures/{guid}` with the content rendered.
- [ ] Headed mode works: `FLOWHUB_E2E_HEADED=true just test-e2e` opens a visible
      Chromium window.
- [ ] Test is idempotent (each run uses a fresh GUID in the content; previous
      runs don't break filters).

---

## 3. Bruno collection (`bruno/`)

Open the `bruno/` folder in [Bruno](https://www.usebruno.com/), select the
`local` environment, then ensure the app is running (`just run` or
`just watch`). Run requests in this order:

### `system/`
- [ ] `Health Live` → 200, body `"Healthy"`.
- [ ] `Prometheus Metrics` → 200, body starts with `# HELP …`.
- [ ] `OpenAPI Document` → 200, JSON with `openapi` + `paths` keys.

### `captures/`
- [ ] `Submit Capture` (Api source, content `"todo: write Bruno smoke test"`)
      → 201, response body has `id`, `stage: "Raw"`, `source: "Api"`.
      Script stores `id` into `{{captureId}}` env var.
- [ ] `List Captures` → 200 with the submitted capture in `items[]`.
      Try the optional `stage` and `source` filters.
- [ ] `Get Capture by Id` → 200 with the same Capture (uses `{{captureId}}`).
- [ ] `Get Capture by Id` with a random GUID → 404 ProblemDetails
      (type ends with `capture-not-found.md`).
- [ ] `Retry Capture` on a still-`Raw`/`Classified` Capture → 409 (not
      retryable). On an `Orphan` Capture → 202.
- [ ] `Search Captures (semantic)`:
      - With `Embeddings__ApiKey` set → 200 list of similar captures.
      - Without → 503 ProblemDetails ("Semantic search not available").
      - Empty `q` → 400 ValidationProblem.

### `admin/`
- [ ] `Rebuild Embeddings` with `Embeddings__ApiKey` set → 200
      `{ processed, skipped, failed }`.
- [ ] Without `Embeddings__ApiKey` → 503.

---

## 4. AiPing runner (`tools/FlowHub.AiPing`)

- [ ] `just ai-ping` with no env → exits with `FAIL: AI provider not configured`
      and a hint to set `Ai__Provider` + `Ai__<Provider>__ApiKey`.
- [ ] `Ai__Provider=Anthropic Ai__Anthropic__ApiKey=… just ai-ping`
      → prints `OK` plus latency.
- [ ] `Ai__Provider=OpenRouter Ai__OpenRouter__ApiKey=… just ai-ping` → idem.
- [ ] Model override picks up: e.g.
      `Ai__Anthropic__Model=claude-sonnet-4-6 just ai-ping` shows the
      Sonnet model in the config dump.
- [ ] `just ai-classify "todo: buy milk"` → `MatchedSkill = Vikunja`.
- [ ] `just ai-classify "https://en.wikipedia.org/wiki/Hexagonal_architecture"`
      → `MatchedSkill = Wallabag`.
- [ ] `just ai-embed "hello"` with `Embeddings__ApiKey` set → prints
      `dimensions = …` and a numeric preview.
- [ ] `just ai-embed` without `Embeddings__ApiKey` → clean FAIL message.

---

## 5. Production compose stack (`just smoke-prod`)

End-to-end smoke against the real docker-compose stack — the deployment
claim that was never exercised before submission.

- [ ] `just smoke-prod` step [1/6] — `docker compose up --build -d --wait`
      returns 0 (all `depends_on: service_healthy` satisfied,
      `flowhub.migrations` reaches `service_completed_successfully`).
- [ ] Step [2/6] — migrations exit code printed as `0`.
- [ ] Step [3/6] — `/health/live` returns 200 via the curl sidecar on the
      same network namespace as `flowhub.web`.
- [ ] Step [4/6] — `/metrics` body contains both `^dotnet_` and `^http_`
      Prometheus series.
- [ ] Step [5/6] — `POST /api/v1/captures` returns 201 with a Guid `id`.
- [ ] Step [6/6] — with `EMBEDDINGS__APIKEY` set in `.env` (or env):
      `Captures.Embedding` column populates within 30 s (i.e.
      `CaptureEmbeddingConsumer` actually runs against RabbitMQ).
- [ ] Without `EMBEDDINGS__APIKEY`: step [6/6] prints "skipped — …"
      and the smoke still exits 0 (consumer no-ops by design).
- [ ] On any failure the stack is left running for diagnosis (no
      auto-teardown). `just smoke-down` cleanly stops it (volumes preserved).

---

## 6. Regression checks

- [ ] `dotnet build FlowHub.slnx` is green with 0 warnings.
- [ ] `just test` skips E2E (no Playwright browser launch).
- [ ] Existing `just watch` still hot-reloads on .razor / .cs changes.
- [ ] `git status` after `just test-all` shows nothing new outside `.make/`
      (which is gitignored).
