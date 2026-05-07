# CI/CD

FlowHub uses three GitHub Actions workflows.

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

## Local Development

```bash
# Apply migrations to local PostgreSQL (no auto-migration at app startup)
make migrate

# Run app without migration step
make run
```
