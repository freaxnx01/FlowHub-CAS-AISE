# CI/CD

FlowHub uses three core GitHub Actions workflows.

> **Platform choice — GitHub (not GitLab).** The Block 5 Lernziel names GitHub and the GitLab Agent Platform as the (alternative) hosted CI/CD platforms — it's a GitHub *or* GitLab choice, not both. FlowHub's repository is GitHub-hosted (`github.com/freaxnx01/FlowHub-CAS-AISE`), so CI/CD runs on **GitHub Actions** with GitHub-hosted runners, and **GitHub Copilot** provides inline assistance (see [`ai-usage.md`](ai-usage.md)). The GitLab Agent Platform is GitLab's equivalent of the same capability and is therefore not used.

## Workflows

### `ci.yml` — Continuous Integration

**Triggers:** Every push to any branch; every PR targeting `main`.

**Steps:** restore → build (warnings-as-errors) → test (all projects, XPlat coverage) → upload artifacts.

**Gate:** PR merge to `main` is blocked if CI fails. Configure branch protection in GitHub: Settings → Branches → `main` → Require status checks → select `build-and-test`.

### `release.yml` — Docker Image Release

**Triggers:** Tags matching `v*` (e.g. `v1.0.0`).

**Steps:**
1. Builds the `flowhub.web` Docker image from `source/FlowHub.Web/Dockerfile`.
2. Pushes to GHCR: `ghcr.io/freaxnx01/flowhub-web:vX.Y.Z` and `:latest`.
3. Generates release notes via `git-cliff` from Conventional Commits.
4. Creates a GitHub Release with the changelog section.

**How to trigger a release:**
```bash
git tag v1.0.0 -m "release: v1.0.0 — CAS AISE Projektarbeit Abgabe"
git push origin v1.0.0
```

### `migrations.yml` — EF Core Migrations Bundle

**Triggers:** Push to `main` when files under `source/FlowHub.Persistence/Migrations/` change.

**Steps:** Builds a self-contained `efbundle` native binary (no .NET SDK needed at runtime). Uploads as a workflow artifact retained for 30 days.

**How to apply in production:** Download the artifact, copy to the target host, set `ConnectionStrings__Default` env var, run `./efbundle`.

## Deployment scope — automated vs. manual

Where the automation boundary sits, by design:

| Stage | Automation |
|---|---|
| Build · test · coverage | **Automated** — `ci.yml` on every push and PR |
| Image publishing | **Automated** — `release.yml` builds and pushes the `flowhub.web` image to GHCR on `v*` tags |
| DB migrations bundle | **Automated** — `migrations.yml` produces a runnable `efbundle` artifact (no auto-migrate at app start — 12-Factor XII) |
| Environment rollout | **Manual, deliberate** — the actual deploy is a one-command `docker compose up` runbook ([`runbooks/public-demo.md`](runbooks/public-demo.md)), not an auto-deploy-on-tag job |

**Why image-publishing is the boundary, not auto-deploy:** FlowHub is a single-operator project with no always-on production environment that needs gated, unattended releases. Publishing a versioned, immutable image to GHCR is the automation that adds value; the rollout itself stays a deliberate, documented one-liner. A CD job that deploys to the VPS on release (SSH or webhook) is a sensible future step rather than a current requirement.

## Local Development

```bash
# Apply migrations to local PostgreSQL (no auto-migration at app startup)
just migrate

# Run app without migration step
just run
```
