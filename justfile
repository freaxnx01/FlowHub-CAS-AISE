# FlowHub — common dev tasks (just recipes)
#
# `just` with no args prints the recipe list grouped by section.
#
# Requires just >= 1.20. Install:
#   Linux:   sudo apt install just  (or: cargo install just)
#   macOS:   brew install just
#   Windows: winget install Casey.Just  (or: scoop install just)

set windows-shell := ["pwsh", "-NoLogo", "-NonInteractive", "-Command"]
set dotenv-load
set positional-arguments

# ── Project Configuration ────────────────────────────────────────────────────

sln                := "FlowHub.slnx"
web_project        := "source/FlowHub.Web"
web_url            := "http://localhost:5070"
aiping_project     := "tools/FlowHub.AiPing"
props_file         := "Directory.Build.props"
pdf_tool_dir       := "tools/md-to-pdf"
pdf_renderer       := pdf_tool_dir + "/render.mjs"
projbeschr_md      := "docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md"
projbeschr_pdf     := "docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.pdf"

video_dir          := "video"
video_piper        := justfile_directory() / "video/tools/piper/piper"
video_model        := justfile_directory() / "video/tools/voices/en_US-amy-medium.onnx"
video_ffmpeg_dir   := justfile_directory() / "video/tools/ffmpeg"

compose            := "docker compose"

# ── Default ──────────────────────────────────────────────────────────────────

# Show this help (`just` with no args)
default:
    @just --list --unsorted

# ── Build & run ──────────────────────────────────────────────────────────────

# Run FlowHub.Web on http://localhost:5070 (no hot reload)
[group('build')]
run:
    cd {{web_project}} && \
        ASPNETCORE_URLS={{web_url}} \
        ASPNETCORE_ENVIRONMENT=Development \
        dotnet run --no-launch-profile

# Run FlowHub.Web with hot reload (dotnet watch)
[group('build')]
watch:
    cd {{web_project}} && \
        ASPNETCORE_URLS={{web_url}} \
        ASPNETCORE_ENVIRONMENT=Development \
        dotnet watch run --no-launch-profile

# Build the full solution
[group('build')]
build:
    dotnet build {{sln}}

# Restore NuGet packages
[group('build')]
restore:
    dotnet restore {{sln}}

# Apply dotnet format
[group('build')]
format:
    dotnet format {{sln}}

# ── Testing ──────────────────────────────────────────────────────────────────

# Run all tests except [Category=AI], [Category=BetaSmoke], [Category=E2E]
[group('test')]
test:
    dotnet test {{sln}} --no-build --filter "Category!=AI&Category!=BetaSmoke&Category!=E2E"

# Run backend unit + skill contract tests (Persistence + Skills + Skills.ContractTests)
[group('test')]
test-unit:
    dotnet test tests/FlowHub.Persistence.Tests
    dotnet test tests/FlowHub.Skills.Tests
    dotnet test tests/FlowHub.Skills.ContractTests --filter "Category=SkillContract"

# Alias for test-unit (legacy name)
[group('test')]
test-backend: test-unit

# Run frontend (bUnit) component tests
[group('test')]
test-frontend:
    dotnet test tests/FlowHub.Web.ComponentTests

# Run component tests in watch mode
[group('test')]
test-watch:
    dotnet watch test --project tests/FlowHub.Web.ComponentTests

# Run all tests with coverage
[group('test')]
test-coverage:
    dotnet test {{sln}} --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./coverage
    @echo "Coverage reports (Cobertura) in ./coverage/"

# Start Postgres + FlowHub.Web, wait until ready, run Playwright happy-flow E2E, then stop the server
[group('test')]
test-e2e: db-up db-migrate playwright-install
    #!/usr/bin/env bash
    set -uo pipefail
    echo "==> starting FlowHub.Web at {{web_url}}…"
    mkdir -p .make
    ( cd {{web_project}} && \
        ASPNETCORE_URLS={{web_url}} ASPNETCORE_ENVIRONMENT=Development \
        setsid nohup dotnet run --no-launch-profile > ../../.make/web.log 2>&1 & echo $! > ../../.make/web.pid )
    echo "==> waiting for /health/live…"
    for i in $(seq 1 60); do
        if curl -fsS {{web_url}}/health/live >/dev/null 2>&1; then echo "ready"; break; fi
        sleep 1
        if [ "$i" = 60 ]; then echo "web never became ready"; tail -50 .make/web.log; kill $(cat .make/web.pid) 2>/dev/null || true; exit 1; fi
    done
    echo "==> running E2E tests"
    FLOWHUB_E2E_BASE_URL={{web_url}} dotnet test tests/FlowHub.Web.E2ETests --filter "Category=E2E"
    status=$?
    echo "==> stopping FlowHub.Web (pid=$(cat .make/web.pid))"
    kill $(cat .make/web.pid) 2>/dev/null || true
    rm -f .make/web.pid
    exit $status

# Run backend + frontend tests, then start Postgres + FlowHub.Web and open it in Microsoft Edge
[group('test')]
test-all: test-backend test-frontend db-up db-migrate
    #!/usr/bin/env bash
    set -uo pipefail
    mkdir -p .make
    echo "==> starting FlowHub.Web at {{web_url}}…"
    ( cd {{web_project}} && \
        ASPNETCORE_URLS={{web_url}} ASPNETCORE_ENVIRONMENT=Development \
        setsid nohup dotnet run --no-launch-profile > ../../.make/web.log 2>&1 & echo $! > ../../.make/web.pid )
    for i in $(seq 1 60); do
        if curl -fsS {{web_url}}/health/live >/dev/null 2>&1; then echo "ready (pid=$(cat .make/web.pid))"; break; fi
        sleep 1
        if [ "$i" = 60 ]; then echo "web never became ready"; tail -50 .make/web.log; exit 1; fi
    done
    echo "==> opening {{web_url}} in Microsoft Edge"
    ( command -v microsoft-edge >/dev/null 2>&1 && microsoft-edge {{web_url}} >/dev/null 2>&1 & ) || \
     ( command -v microsoft-edge-stable >/dev/null 2>&1 && microsoft-edge-stable {{web_url}} >/dev/null 2>&1 & ) || \
     ( command -v xdg-open >/dev/null 2>&1 && xdg-open {{web_url}} >/dev/null 2>&1 & ) || \
     echo "  (no Edge / xdg-open found — open {{web_url}} manually)"
    echo "==> server kept running. Stop with: kill \$(cat .make/web.pid)"

# Install Playwright browser binaries (one-time setup)
[group('test')]
playwright-install:
    dotnet build tests/FlowHub.Web.E2ETests -c Debug
    pwsh tests/FlowHub.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium 2>/dev/null || \
        tests/FlowHub.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium

# Run live integration tests against real AI providers (requires Ai__*__ApiKey env)
[group('test')]
test-ai:
    {{ _passbolt_exec }} dotnet test tests/FlowHub.AI.IntegrationTests --filter "Category=AI"

# Run WireMock-based wire-contract tests for Vikunja + Wallabag (offline, no secrets)
[group('test')]
test-contract:
    dotnet test tests/FlowHub.Skills.ContractTests --filter "Category=SkillContract"

# Run live skill-integration tests against flowhub-test-services (Vikunja + Wallabag on CT 128 — see docs/runbooks/test-services.md)
[group('test')]
test-services:
    #!/usr/bin/env bash
    set -uo pipefail
    if WB_EXPORT=$(tools/wallabag-token.sh --export 2>/tmp/wallabag-token.err); then
        echo "==> Wallabag OAuth2 access_token fetched (expires in 3600s)"
    else
        echo "==> Wallabag OAuth2 token fetch failed — live Wallabag test will skip"
        cat /tmp/wallabag-token.err >&2 || true
        WB_EXPORT=""
    fi
    eval "$WB_EXPORT"
    set -a; [ -f .env ] && . ./.env; set +a
    if command -v passbolt >/dev/null 2>&1; then
        exec passbolt exec -- dotnet test tests/FlowHub.Skills.IntegrationTests --filter "Category=BetaSmoke"
    else
        exec dotnet test tests/FlowHub.Skills.IntegrationTests --filter "Category=BetaSmoke"
    fi

# Alias for test-services (legacy name)
[group('test')]
test-beta: test-services

# Verify FlowHub's resolved config still catches the known-bad quality cases
# (pulls canonical fixtures from agent-pipeline at a pinned ref). See issue #94.
[group('test')]
verify-gates:
    bash tools/verify-gates.sh

# ── Database ─────────────────────────────────────────────────────────────────

# Start PostgreSQL in Docker (detached, waits until healthy)
[group('database')]
db-up:
    {{compose}} up postgres -d --wait

# Verify the PostgreSQL connection (host:port reachable + SELECT 1)
[group('database')]
db-ping:
    #!/usr/bin/env bash
    set -uo pipefail
    HOST=${PGHOST:-localhost}; PORT=${PGPORT:-5432}; DB=${PGDATABASE:-flowhub}; USER=${PGUSER:-flowhub}; PASS=${PGPASSWORD:-dev-secret}
    echo "==> ping postgres at $HOST:$PORT (db=$DB user=$USER)"
    if command -v pg_isready >/dev/null 2>&1; then
        pg_isready -h "$HOST" -p "$PORT" -d "$DB" -U "$USER" || exit $?
    else
        ( exec 3<>/dev/tcp/$HOST/$PORT ) 2>/dev/null && echo "tcp: ok" || { echo "tcp: FAIL ($HOST:$PORT unreachable)"; exit 1; }
    fi
    if {{compose}} ps --status running postgres >/dev/null 2>&1 && [ -n "$({{compose}} ps -q postgres)" ]; then
        {{compose}} exec -T -e PGPASSWORD="$PASS" postgres psql -h localhost -U "$USER" -d "$DB" -tAc "select 'ok'" \
            | grep -q '^ok$' && echo "psql: SELECT 1 ok" || { echo "psql: FAIL"; exit 1; }
    elif command -v psql >/dev/null 2>&1; then
        PGPASSWORD="$PASS" psql -h "$HOST" -p "$PORT" -U "$USER" -d "$DB" -tAc "select 'ok'" \
            | grep -q '^ok$' && echo "psql: SELECT 1 ok" || { echo "psql: FAIL"; exit 1; }
    else
        echo "psql: skipped (no docker compose container, no host psql)"
    fi

# Apply EF Core migrations against the Docker PostgreSQL
[group('database')]
db-migrate:
    ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret" \
        dotnet ef database update \
        --project source/FlowHub.Persistence \
        --startup-project source/FlowHub.Web

# Apply EF Core migrations to the local database (requires running PostgreSQL)
[group('database')]
migrate:
    dotnet ef database update \
        --project source/FlowHub.Persistence \
        --startup-project source/FlowHub.Web

# Verify the RabbitMQ connection (AMQP TCP + management /api/overview)
[group('database')]
rabbit-ping:
    #!/usr/bin/env bash
    set -uo pipefail
    HOST=${Bus__RabbitMq__Host:-localhost}; PORT=${Bus__RabbitMq__Port:-5672}
    MGMT_PORT=${Bus__RabbitMq__ManagementPort:-15672}
    USER=${Bus__RabbitMq__Username:-flowhub}; PASS=${Bus__RabbitMq__Password:-dev-secret}
    echo "==> ping rabbitmq at $HOST:$PORT (user=$USER)"
    ( exec 3<>/dev/tcp/$HOST/$PORT ) 2>/dev/null && echo "amqp tcp: ok" || { echo "amqp tcp: FAIL ($HOST:$PORT unreachable)"; exit 1; }
    if command -v curl >/dev/null 2>&1; then
        HTTP=$(curl -s -o /dev/null -w "%{http_code}" -u "$USER:$PASS" "http://$HOST:$MGMT_PORT/api/overview" || echo 000)
        if [ "$HTTP" = "200" ]; then
            echo "mgmt api: HTTP 200 ok"
        elif [ "$HTTP" = "401" ]; then
            echo "mgmt api: HTTP 401 (TCP ok, but Bus__RabbitMq__Username/Password rejected)"; exit 1
        elif [ "$HTTP" = "000" ]; then
            echo "mgmt api: skipped (port $MGMT_PORT unreachable — management plugin may be off)"
        else
            echo "mgmt api: HTTP $HTTP (unexpected)"; exit 1
        fi
    else
        echo "mgmt api: skipped (no curl on host)"
    fi

# ── AI provider smoke tools ──────────────────────────────────────────────────

# Smoke-test the configured AI provider with a tiny chat call (env: Ai__Provider, Ai__<Provider>__ApiKey)
[group('ai')]
ai-ping:
    {{ _passbolt_exec }} dotnet run --project {{aiping_project}} -- ping

# Run IClassifier against TEXT. Usage: just ai-classify "todo: buy milk"
[group('ai')]
ai-classify *text:
    {{ _passbolt_exec }} dotnet run --project {{aiping_project}} -- classify {{text}}

# Run IEmbeddingService against TEXT (env: Embeddings__ApiKey, Embeddings__Model). Usage: just ai-embed "hello"
[group('ai')]
ai-embed *text:
    {{ _passbolt_exec }} dotnet run --project {{aiping_project}} -- embed {{text}}

# Helper: re-source .env then wrap with `passbolt exec` if available (resolves passbolt:// secret refs)
_passbolt_exec := 'bash -c "set -a; [ -f .env ] && . ./.env; set +a; if command -v passbolt >/dev/null 2>&1; then exec passbolt exec -- \"$@\"; else exec \"$@\"; fi" --'

# ── Docker (Compose) ─────────────────────────────────────────────────────────

# Run docker-compose in foreground (Ctrl+C to stop)
[group('docker')]
docker-run:
    {{compose}} up --build

# Start docker-compose in background
[group('docker')]
up:
    {{compose}} up -d --build

# Stop docker-compose
[group('docker')]
down:
    {{compose}} down

# Follow container logs
[group('docker')]
logs:
    {{compose}} logs -f

# Rebuild and restart
[group('docker')]
rebuild: down up

# Boot full prod compose stack and smoke-test health, /metrics, capture submit + embedding round-trip
[group('docker')]
smoke-prod:
    #!/usr/bin/env bash
    set -uo pipefail
    echo "==> [1/6] docker compose up --build (detached, --wait until healthy)"
    set -a; [ -f .env ] && . ./.env; set +a
    if command -v passbolt >/dev/null 2>&1; then
        passbolt exec -- {{compose}} up --build -d --wait
    else
        {{compose}} up --build -d --wait
    fi
    echo "==> [2/6] verifying flowhub.migrations exited 0"
    MIG_STATUS=$({{compose}} ps -a --format '{{{{.Service}} {{{{.ExitCode}}' | awk '$1=="flowhub.migrations"{print $2; exit}')
    if [ "$MIG_STATUS" = "0" ]; then echo "    migrations: exit 0"; else echo "    FAIL: migrations exit=$MIG_STATUS"; exit 1; fi
    WEB_CID=$({{compose}} ps -q flowhub.web)
    if [ -z "$WEB_CID" ]; then echo "FAIL: flowhub.web container not running"; exit 1; fi
    CURL="docker run --rm --network container:$WEB_CID curlimages/curl:8.10.1 -fsS --max-time 10"
    echo "==> [3/6] GET /health/live"
    $CURL http://localhost:5070/health/live > /dev/null && echo "    /health/live: 200" || { echo "    FAIL: /health/live"; exit 1; }
    echo "==> [4/6] GET /metrics — expect dotnet_* and http_* series"
    METRICS=$(mktemp); $CURL http://localhost:5070/metrics > $METRICS || { echo "    FAIL: /metrics"; rm -f $METRICS; exit 1; }
    grep -q "^dotnet_" $METRICS && echo "    dotnet_* series: ok" || { echo "    FAIL: no dotnet_* metrics"; rm -f $METRICS; exit 1; }
    grep -q "^http_" $METRICS && echo "    http_* series:   ok" || { echo "    FAIL: no http_* metrics"; rm -f $METRICS; exit 1; }
    rm -f $METRICS
    echo "==> [5/6] POST /api/v1/captures (URL capture for embedding round-trip)"
    BODY=$($CURL -X POST -H "Content-Type: application/json" \
        -d '{"content":"https://en.wikipedia.org/wiki/Hexagonal_architecture","source":"Api"}' \
        http://localhost:5070/api/v1/captures)
    CAPTURE_ID=$(echo "$BODY" | sed -n 's/.*"id":"\([0-9a-fA-F-]\{36\}\)".*/\1/p')
    if [ -z "$CAPTURE_ID" ]; then echo "    FAIL: no id in response: $BODY"; exit 1; fi
    echo "    captured id: $CAPTURE_ID"
    echo "==> [6/6] polling Captures.Embedding (up to 30s)"
    if [ -z "${EMBEDDINGS__APIKEY:-}" ] && [ -z "${Embeddings__ApiKey:-}" ]; then
        echo "    skipped — EMBEDDINGS__APIKEY not set in env / .env (embedding consumer no-ops)"
    else
        for i in $(seq 1 30); do
            HAS=$({{compose}} exec -T postgres psql -U flowhub -d flowhub -tAc \
                "SELECT \"Embedding\" IS NOT NULL FROM \"Captures\" WHERE \"Id\"='$CAPTURE_ID'" 2>/dev/null | tr -d ' ')
            if [ "$HAS" = "t" ]; then echo "    embedding column populated after ~${i}s"; break; fi
            sleep 1
            if [ "$i" -eq 30 ]; then echo "    FAIL: embedding still NULL after 30s — check CaptureEmbeddingConsumer logs"; exit 1; fi
        done
    fi
    echo "==> smoke OK — stack left running. Tear down with: just smoke-down"

# Stop the prod compose stack started by smoke-prod (preserves volumes)
[group('docker')]
smoke-down:
    {{compose}} down

# ── Quality ──────────────────────────────────────────────────────────────────

# Check code formatting
[group('quality')]
lint:
    dotnet format --verify-no-changes

# Check for outdated packages
[group('quality')]
outdated:
    dotnet list {{sln}} package --outdated

# Check for vulnerable packages (fail on high/critical)
[group('quality')]
vuln:
    dotnet list {{sln}} package --vulnerable --include-transitive

# ── Versioning (single source of truth: Directory.Build.props → <Version>) ───

# Show current version
[group('version')]
[unix]
version:
    @sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1

[group('version')]
[windows]
version:
    Write-Output ([xml](Get-Content {{props_file}})).Project.PropertyGroup.Version

# Set version explicitly (usage: just version-set 1.2.3)
[group('version')]
[unix]
version-set v:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>{{v}}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → {{v}}"

[group('version')]
[windows]
version-set v:
    #!/usr/bin/env pwsh
    $ErrorActionPreference = 'Stop'
    $xml = [xml](Get-Content {{props_file}})
    $cur = $xml.Project.PropertyGroup.Version
    (Get-Content {{props_file}} -Raw) -replace "<Version>$cur</Version>", "<Version>{{v}}</Version>" | Set-Content {{props_file}} -NoNewline
    Write-Output "Version: $cur -> {{v}}"

# Bump major version (1.2.3 → 2.0.0)
[group('version')]
[unix]
bump-major:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    new=$((major + 1)).0.0
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

[group('version')]
[windows]
bump-major:
    #!/usr/bin/env pwsh
    $ErrorActionPreference = 'Stop'
    $xml = [xml](Get-Content {{props_file}})
    $v = [version]$xml.Project.PropertyGroup.Version
    $new = "$($v.Major + 1).0.0"
    (Get-Content {{props_file}} -Raw) -replace "<Version>$v</Version>", "<Version>$new</Version>" | Set-Content {{props_file}} -NoNewline
    Write-Output "Version: $v -> $new"

# Bump minor version (1.2.3 → 1.3.0)
[group('version')]
[unix]
bump-minor:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    minor=$(echo "$cur" | cut -d. -f2)
    new=${major}.$((minor + 1)).0
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

[group('version')]
[windows]
bump-minor:
    #!/usr/bin/env pwsh
    $ErrorActionPreference = 'Stop'
    $xml = [xml](Get-Content {{props_file}})
    $v = [version]$xml.Project.PropertyGroup.Version
    $new = "$($v.Major).$($v.Minor + 1).0"
    (Get-Content {{props_file}} -Raw) -replace "<Version>$v</Version>", "<Version>$new</Version>" | Set-Content {{props_file}} -NoNewline
    Write-Output "Version: $v -> $new"

# Bump patch version (1.2.3 → 1.2.4)
[group('version')]
[unix]
bump-patch:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    minor=$(echo "$cur" | cut -d. -f2)
    patch=$(echo "$cur" | cut -d. -f3)
    new=${major}.${minor}.$((patch + 1))
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

[group('version')]
[windows]
bump-patch:
    #!/usr/bin/env pwsh
    $ErrorActionPreference = 'Stop'
    $xml = [xml](Get-Content {{props_file}})
    $v = [version]$xml.Project.PropertyGroup.Version
    $new = "$($v.Major).$($v.Minor).$($v.Build + 1)"
    (Get-Content {{props_file}} -Raw) -replace "<Version>$v</Version>", "<Version>$new</Version>" | Set-Content {{props_file}} -NoNewline
    Write-Output "Version: $v -> $new"

# Bump version from Conventional Commits via git-cliff (minor/patch only)
[group('version')]
[unix]
bump-auto:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    new=$(git-cliff --bumped-version 2>/dev/null | sed 's/^v//')
    if [ -z "$new" ]; then
        echo "Error: git-cliff did not return a bumped version" >&2
        exit 1
    fi
    cur_major=$(echo "$cur" | cut -d. -f1)
    new_major=$(echo "$new" | cut -d. -f1)
    if [ "$cur_major" != "$new_major" ]; then
        echo "Error: auto-bump suggests major version change ($cur → $new)." >&2
        echo "  Major bumps require explicit action. Run: just bump-major" >&2
        exit 1
    fi
    if [ "$new" = "$cur" ]; then
        echo "Version unchanged: $cur (no bump-worthy commits since last tag)"
        exit 0
    fi
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new} (auto)"

[group('version')]
[windows]
bump-auto:
    #!/usr/bin/env pwsh
    $ErrorActionPreference = 'Stop'
    $xml = [xml](Get-Content {{props_file}})
    $cur = [version]$xml.Project.PropertyGroup.Version
    $bumped = (& git-cliff --bumped-version) -replace '^v',''
    if (-not $bumped) { Write-Error "git-cliff did not return a bumped version"; exit 1 }
    $new = [version]$bumped
    if ($cur.Major -ne $new.Major) {
        Write-Error "auto-bump suggests major version change ($cur -> $new). Run: just bump-major"
        exit 1
    }
    if ($new -eq $cur) {
        Write-Output "Version unchanged: $cur (no bump-worthy commits since last tag)"
        exit 0
    }
    (Get-Content {{props_file}} -Raw) -replace "<Version>$cur</Version>", "<Version>$new</Version>" | Set-Content {{props_file}} -NoNewline
    Write-Output "Version: $cur -> $new (auto)"

# ── Release ──────────────────────────────────────────────────────────────────

# Generate CHANGELOG.md from Conventional Commits (git-cliff)
[group('release')]
changelog:
    git-cliff --output CHANGELOG.md
    @echo "CHANGELOG.md updated."

# Generate user-friendly release notes (RELEASENOTES.md) via Claude Code
[group('release')]
[unix]
release-notes:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    claude -p "/release-notes v${cur}"

# Tag release, regenerate changelog + release-notes, and commit
[group('release')]
[unix]
release:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    if git tag -l "v${cur}" | grep -q .; then
        echo "Error: Tag v${cur} already exists." >&2
        echo "  Already released? Run: just package && just push-release" >&2
        echo "  Need a new version? Run: just bump-patch (or bump-minor / bump-major)" >&2
        exit 1
    fi
    echo "── Releasing v${cur}..."
    git-cliff --tag "v${cur}" --output CHANGELOG.md
    just release-notes
    git add {{props_file}} CHANGELOG.md RELEASENOTES.md
    git commit -m "chore: release v${cur}"
    git tag -a "v${cur}" -m "release: v${cur}"
    echo "── Released v${cur}. Don't forget to push: just push-release"

# Auto-bump from commits, then release
[group('release')]
release-auto: bump-auto release

# Push main branch and current version tag to origin
[group('release')]
[unix]
push-release:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    git push origin main "v${cur}"

# Build distributable artifact (ZIP / tarball / image) and deliver
[group('release')]
package:
    # TODO: implement per-project.
    @echo "package: implement in your project justfile" && exit 1

# ── PDF rendering ────────────────────────────────────────────────────────────

# Install Node deps for the puppeteer-based Markdown→PDF renderer (one-time)
[group('pdf')]
pdf-install:
    cd {{pdf_tool_dir}} && npm install --no-fund --no-audit

# Render an arbitrary Markdown file to PDF. Usage: just pdf docs/foo.md [docs/foo.pdf]
[group('pdf')]
pdf file out='':
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    out_file="{{out}}"
    if [ -z "$out_file" ]; then out_file="${file%.md}.pdf"; fi
    echo "==> rendering {{file}} -> $out_file"
    node {{pdf_renderer}} "{{file}}" "$out_file" --title "$(head -n 1 {{file}} | sed 's/^# *//')"

# Regenerate docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.pdf from the matching .md
[group('pdf')]
pdf-projektbeschreibung:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    node {{pdf_renderer}} {{projbeschr_md}} {{projbeschr_pdf}} --title "FlowHub – Projektbeschreibung"

# Regenerate the as-built Arc42 architecture document PDF
[group('pdf')]
pdf-arc42:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    node {{pdf_renderer}} docs/architektur/FlowHub_Arc42_v2.md docs/architektur/FlowHub_Arc42_v2.pdf --title "FlowHub – Arc42 (as built)"

# Regenerate the KI-Reflexion document PDF
[group('pdf')]
pdf-reflexion:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    node {{pdf_renderer}} docs/reflexion/FlowHub_Reflexion.md docs/reflexion/FlowHub_Reflexion.pdf --title "FlowHub – Reflexion"

# Render SUBMISSION.md → FlowHub_Uebersicht.pdf (Einstieg/Übersicht; links into the GitHub main branch)
[group('pdf')]
pdf-submission:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    node {{pdf_renderer}} SUBMISSION.md FlowHub_Uebersicht.pdf --title "FlowHub — Übersicht & Einreichung"

# Build SUBMISSION-bundle.pdf — SUBMISSION.md + every referenced artefact inlined (safety net)
[group('pdf')]
pdf-submission-bundle:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    tools/submission-bundle.sh tools/build/submission-bundle.md
    node {{pdf_renderer}} tools/build/submission-bundle.md SUBMISSION-bundle.pdf --title "FlowHub — CAS AISE Submission Bundle"

# Render the Marp slide deck to PDF (landscape, NO speaker notes — for the read/upload deck)
[group('pdf')]
pdf-presentation:
    #!/usr/bin/env bash
    set -euo pipefail
    CHROME=$(find "$HOME/.cache/puppeteer" -type f -path '*chrome-linux64/chrome' 2>/dev/null | sort -V | tail -1)
    if [ -z "$CHROME" ]; then echo "ERROR: no puppeteer Chromium found — run 'just pdf-install' first." >&2; exit 1; fi
    cd docs/presentation
    CHROME_PATH="$CHROME" npx --yes @marp-team/marp-cli --html --pdf --allow-local-files flowhub-praesentation.md -o flowhub-praesentation.pdf
    echo "wrote docs/presentation/flowhub-praesentation.pdf (no speaker notes)"

# Assemble the numbered Moodle upload set (00–04) into ./upload/.
# The slide deck LEADS two docs as a mixed landscape/portrait PDF: Teil 1 (Produkt)
# in front of the Projektbeschreibung, Teil 2 (Bauen mit KI) in front of the Reflexion.
[group('pdf')]
package-submission: pdf-submission pdf-arc42 pdf-projektbeschreibung pdf-reflexion pdf-eigenstaendigkeitserklaerung pdf-presentation
    #!/usr/bin/env bash
    set -euo pipefail
    mkdir -p upload tools/build/deck
    rm -f upload/*.pdf tools/build/deck/*.pdf
    # Split the deck at the "Teil 2: Bauen mit KI" divider slide (found dynamically, so it survives slide edits).
    DECK=docs/presentation/flowhub-praesentation.pdf
    N=$(pdfinfo "$DECK" | awk '/^Pages/{print $2}')
    B=0; for i in $(seq 1 "$N"); do if pdftotext -layout -f "$i" -l "$i" "$DECK" - 2>/dev/null | grep -q "Teil 2: Bauen mit KI"; then B=$i; break; fi; done
    [ "$B" -gt 1 ] || { echo "ERROR: 'Teil 2: Bauen mit KI' divider slide not found in $DECK — cannot split the deck." >&2; exit 1; }
    pdfseparate "$DECK" tools/build/deck/pg-%d.pdf
    pdfunite $(for i in $(seq 1 $((B-1))); do echo tools/build/deck/pg-$i.pdf; done) tools/build/deck/part1.pdf
    pdfunite $(for i in $(seq "$B" "$N"); do echo tools/build/deck/pg-$i.pdf; done) tools/build/deck/part2.pdf
    # Assemble the upload set (deck Teil leads each merged doc; text follows).
    cp FlowHub_Uebersicht.pdf                       upload/00_FlowHub_Uebersicht.pdf
    cp docs/architektur/FlowHub_Arc42_v2.pdf        upload/01_FlowHub_Arc42.pdf
    pdfunite tools/build/deck/part1.pdf docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.pdf upload/02_FlowHub_Projektbeschreibung.pdf
    pdfunite tools/build/deck/part2.pdf docs/reflexion/FlowHub_Reflexion.pdf                         upload/03_FlowHub_Reflexion.pdf
    cp "Eigenständigkeitserklärung.pdf"             upload/04_FlowHub_Eigenstaendigkeitserklaerung.pdf
    echo "Upload set ready in ./upload/ (00–04). Sign 04 before uploading."
    echo "02 = Präsentation Teil 1 (Produkt) + Projektbeschreibung · 03 = Präsentation Teil 2 (Bauen mit KI) + Reflexion (mixed landscape/portrait)."
    ls -1 upload/

# Build Eigenständigkeitserklärung.pdf (FFHS Hilfsmittelverzeichnis + signed declaration; compact layout)
[group('pdf')]
pdf-eigenstaendigkeitserklaerung:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{pdf_tool_dir}}/node_modules" ]; then just pdf-install; fi
    node {{pdf_renderer}} docs/submission/eigenstaendigkeitserklaerung.md "Eigenständigkeitserklärung.pdf" --title "FlowHub — Hilfsmittelverzeichnis & Eigenständigkeitserklärung" --compact

# ── Explainer videos (Remotion + Piper TTS) ──────────────────────────────────

# Vendor Piper + English voice + emoji font + static ffmpeg into video/tools/, then npm install (one-time)
[group('video')]
video-setup:
    #!/usr/bin/env bash
    set -euo pipefail
    bash {{video_dir}}/tools/setup.sh
    cd {{video_dir}} && npm install --no-fund --no-audit

# Synthesize English narration with Piper → public/audio/tts/*.wav + src/durations.json
[group('video')]
video-tts:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -x "{{video_piper}}" ] || [ ! -x "{{video_ffmpeg_dir}}/ffprobe" ] || [ ! -s "{{video_model}}" ] || [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    PATH="{{video_ffmpeg_dir}}:$PATH" PIPER_BIN="{{video_piper}}" PIPER_MODEL="{{video_model}}" npm run tts

# Render both MP4s into video/out/ (run video-tts first for real narration)
[group('video')]
video-render:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    PATH="{{video_ffmpeg_dir}}:$PATH" npm run render:users
    PATH="{{video_ffmpeg_dir}}:$PATH" npm run render:technical

# Capture the live VPS demo into video/public/demo/ (screenshots + manifest)
[group('video')]
video-capture:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    CAPTURED_AT="$(date -u +%FT%TZ)" npm run capture:demo

# Render the demo-walkthrough video → video/out/flowhub-demo.en.mp4
[group('video')]
video-demo:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    PATH="{{video_ffmpeg_dir}}:$PATH" npm run render:demo

# Full pipeline: setup if needed → narration → render both videos
[group('video')]
video: video-tts video-render
    @echo "✓ videos rendered to {{video_dir}}/out/"

# Remove rendered MP4s and generated narration wavs
[group('video')]
video-clean:
    rm -rf {{video_dir}}/out {{video_dir}}/public/audio/tts

# ── Cleanup ──────────────────────────────────────────────────────────────────

# Remove build artifacts
[group('cleanup')]
clean:
    dotnet clean {{sln}}
