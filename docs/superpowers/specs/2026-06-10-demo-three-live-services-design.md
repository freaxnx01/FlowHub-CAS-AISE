# Design — Demo: three live downstream services (Wallabag + paperless-ngx)

**Date:** 2026-06-10
**Issue:** [#37](https://github.com/freaxnx01/FlowHub-CAS-AISE/issues/37) — Demo @VPS: integrate three live services (Vikunja, paperless-ngx, Wallabag)
**Status:** Approved (brainstorm) → ready for plan

## Goal

Extend the public demo so captures route to **three** live, self-contained
downstream services, completing the "capture → classify → route to the right
tool" showcase:

| Service | Capture type | Status before | After this work |
|---|---|---|---|
| **Vikunja** | task / structured note | live | unchanged |
| **Wallabag** | URL / article | skill exists, config forced empty → stops at Unhandled | **live** |
| **paperless-ngx** | uploaded document (PDF/PNG/JPEG) | none | **live (minimal stack)** |

Each service is self-contained and reset on the demo cycle, mirroring the
existing Vikunja demo (bootstrap sidecar + shared `/bootstrap` env volume +
clear-on-reset).

## Background — how routing works today

1. `CaptureCreated` → **`CaptureEnrichmentConsumer`** classifies `Content`
   (text) via the LLM (`AiClassifier`, `AllowedSkills = {Wallabag, Vikunja, ""}`),
   marks classified, publishes `CaptureClassified{MatchedSkill, …}`.
2. **`SkillRoutingConsumer`** finds the `ISkillIntegration` whose `Name`
   equals `MatchedSkill`; none → capture marked **Unhandled**.
3. Live config is injected at container start from bootstrap-written env files
   on a shared `/bootstrap` volume (see `demo/vikunja/bootstrap.sh`).

Wallabag and Vikunja are already in `AllowedSkills`; the Wallabag skill class
already exists. So this is mostly **infra + one new skill + one routing rule**.

## Part A — Wallabag (enable existing skill)

### Compose
- Add `wallabag` service (single container, sqlite backend) + Traefik route
  `wallabag.demo.flowhub.freaxnx01.ch` (generic labels in
  `demo/docker-compose.yml`, VPS overrides in `demo/docker-compose.vps.yml`).
- Add `flowhub.wallabag-bootstrap` one-shot sidecar mirroring the Vikunja
  bootstrap: wait for API, ensure the OAuth client + demo user exist, write
  `/bootstrap/wallabag.env`.

### Token lifecycle (Decision 1 — **self-refreshing token**)
Wallabag's API is OAuth2; access tokens expire after **1 hour**. The current
skill takes a *static* `ApiToken`, which would silently break the live demo
within an hour (the web app only reads env at startup).

**Decision:** change the Wallabag skill to obtain and cache its own token.
- `WallabagOptions` gains `ClientId`, `ClientSecret`, `Username`, `Password`
  (replacing the static `ApiToken`). `BaseUrl` stays.
- A small token provider performs the OAuth2 password grant against
  `/oauth/v2/token`, caches the `access_token` until shortly before expiry,
  and re-mints on expiry or on a `401`.
- `bootstrap.sh` runs the Wallabag console command to create the OAuth client
  and writes `Skills__Wallabag__{BaseUrl,ClientId,ClientSecret,Username,Password}`
  into `wallabag.env`.

### Reset
Extend `demo/reset/reset.sh` with a best-effort block: read `wallabag.env`,
do a password grant to mint a token, delete all entries. Skipped if the env
file is absent. Account + OAuth client persist (written once by bootstrap).

## Part B — paperless-ngx (minimal, file-driven)

### Minimal footprint
Only **paperless-ngx + Redis**. Redis is a hard requirement; the DB defaults to
sqlite; OCR for PDF/PNG/JPEG is built in. **Tika and Gotenberg are omitted** —
they only add office/email format support, which the demo's upload policy
(`application/pdf`, `image/png`, `image/jpeg`) does not accept. → 2 containers.

### New skill `PaperlessSkillIntegration : ISkillIntegration`
- `Name = "Paperless"`.
- Reads the capture's attachment bytes and POSTs multipart to
  `/api/documents/post_document/` with header `Authorization: Token <token>`.
- Returns the document/task ref as `SkillResult.ExternalRef`.
- Fails (non-success) when the capture has no attachment, or the upload is
  rejected — engaging the existing MassTransit retry → `LifecycleFaultObserver`
  → Unhandled path.
- **Supporting change:** add `IAttachmentStorage.OpenReadAsync(relativePath)`
  + implementation in `FilesystemAttachmentStorage` (today it only Save/Delete).

### Attachment → skill rule (Decision 2 — **has-attachment ⇒ Paperless**)
Attachment captures submit with `content: null`, so text classification cannot
route them.

**Decision:** deterministic rule in `CaptureEnrichmentConsumer` — if the capture
has an attachment, set `MatchedSkill = "Paperless"` and skip the LLM call.
- Carry a `HasAttachment` flag on the `CaptureCreated` event so the consumer
  decides without an extra DB round-trip.
- The LLM `AllowedSkills`/prompt stay unchanged — Paperless is routed by this
  rule, not by the classifier, so binary files incur no LLM cost.

### Bootstrap
One-shot `flowhub.paperless-bootstrap`: auto-create the admin via
`PAPERLESS_ADMIN_USER`/`PAPERLESS_ADMIN_PASSWORD`, POST `/api/token/` to obtain
a long-lived API token, write `Skills__Paperless__{BaseUrl,ApiToken}` into
`/bootstrap/paperless.env`. Traefik route `paperless.demo.flowhub.freaxnx01.ch`.

### Reset
Extend `demo/reset/reset.sh`: read `paperless.env`, bulk-delete all documents
each cycle. Admin account persists.

## Cross-cutting

- **Skill registration:** `SkillsServiceCollectionExtensions.AddFlowHubSkills`
  gains `AddPaperless` (same dormant-if-unconfigured pattern via
  `SkillsRegistrationOutcome`). Wallabag registration updated for the new
  OAuth options.
- **Compose env (`demo/docker-compose.yml`):** stop forcing
  `Skills__Wallabag__*` empty; both Wallabag and Paperless config injected at
  runtime from their bootstrap env files via the entrypoint wrapper (same
  mechanism as Vikunja today). `depends_on` the new bootstrap sidecars.
- **Demo banner** (`Demo__BannerText`) updated to mention all three services
  and the file-upload path at `/captures/new`.
- **Reset cadence:** all three services clear on the **same** existing 15-min
  `flowhub.demo-reset` loop; each block is independently skippable when its
  bootstrap env file is absent. User accounts/projects/clients persist.

## Testing (TDD — write failing tests first)

- **Wallabag token provider:** mints on first use; reuses cached token before
  expiry; re-mints after expiry and on `401`.
- **Wallabag skill:** posts the URL; parses the entry id; rejects non-URL content.
- **Paperless skill:** multipart post shape; parses the returned ref; fails when
  the capture has no attachment.
- **Attachment routing rule:** `CaptureEnrichmentConsumer` routes a capture with
  `HasAttachment = true` to `Paperless` without calling the classifier; text
  captures still flow through the LLM.
- **Full suite** (`dotnet test`) green before completion.

## Out of scope

- Wiring the dead skill-override dropdown on `/captures/new` through
  `SubmitAsync` (pre-existing; note only).
- Tika/Gotenberg (office/email ingestion).
- Embeddings/search over documents.

## Risks / notes

- paperless-ngx OCR is CPU-heavy per upload; acceptable for low-traffic demo
  traffic and small files (≤2 MB), but worth watching on the shared VPS.
- Wallabag client-creation depends on the image's console command surface;
  bootstrap must wait for the app to be migration-ready before invoking it.
