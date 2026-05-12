# FlowHub — common dev tasks
# Usage: make <target>
#
# `make` with no target prints help.

.DEFAULT_GOAL := help
SHELL := /bin/bash

# Auto-load .env (gitignored). For targets that need secrets, wrap with
# $(SECRET_EXEC) — that re-sources .env into the recipe shell *before* the
# command runs, so Passbolt refs like `KEY=passbolt://<uuid>` get resolved
# by `passbolt exec` (if present). Falls through unchanged when passbolt is
# not installed, so plain real-value .env files keep working.
-include .env
export

SECRET_EXEC = bash -c 'set -a; [ -f .env ] && . ./.env; set +a; \
	if command -v passbolt >/dev/null 2>&1; then exec passbolt exec -- "$$@"; \
	else exec "$$@"; fi' --

.PHONY: help run watch build test test-backend test-frontend test-e2e test-all test-ai test-beta test-watch playwright-install restore clean format db-up db-ping db-migrate migrate ai-ping ai-classify ai-embed smoke-prod smoke-down

SOLUTION       := FlowHub.slnx
WEB_PROJECT    := source/FlowHub.Web
WEB_URL        := http://localhost:5070
AIPING_PROJECT := tools/FlowHub.AiPing
TEXT           ?=

help: ## Show this help
	@grep -hE '^[a-zA-Z0-9_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| sort \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

run: ## Run FlowHub.Web on http://localhost:5070 (no hot reload)
	cd $(WEB_PROJECT) && \
		ASPNETCORE_URLS=$(WEB_URL) \
		ASPNETCORE_ENVIRONMENT=Development \
		dotnet run --no-launch-profile

watch: ## Run FlowHub.Web with hot reload (dotnet watch)
	cd $(WEB_PROJECT) && \
		ASPNETCORE_URLS=$(WEB_URL) \
		ASPNETCORE_ENVIRONMENT=Development \
		dotnet watch run --no-launch-profile

build: ## Build the full solution
	dotnet build $(SOLUTION)

test: ## Run all tests except [Category=AI], [Category=BetaSmoke], [Category=E2E]
	dotnet test $(SOLUTION) --no-build --filter "Category!=AI&Category!=BetaSmoke&Category!=E2E"

test-backend: ## Run backend unit + skill contract tests (Persistence + Skills + Skills.ContractTests), no live integration, no E2E
	dotnet test tests/FlowHub.Persistence.Tests
	dotnet test tests/FlowHub.Skills.Tests
	dotnet test tests/FlowHub.Skills.ContractTests --filter "Category=SkillContract"

test-frontend: ## Run frontend (bUnit) component tests
	dotnet test tests/FlowHub.Web.ComponentTests

test-e2e: ## Start Postgres + FlowHub.Web, wait until ready, run Playwright happy-flow E2E, then stop the server
	@$(MAKE) db-up
	@$(MAKE) db-migrate
	@$(MAKE) playwright-install
	@echo "==> starting FlowHub.Web at $(WEB_URL)…"
	@mkdir -p .make
	@cd $(WEB_PROJECT) && \
		ASPNETCORE_URLS=$(WEB_URL) ASPNETCORE_ENVIRONMENT=Development \
		setsid nohup dotnet run --no-launch-profile > ../../.make/web.log 2>&1 & echo $$! > ../../.make/web.pid
	@echo "==> waiting for /health/live…"
	@for i in $$(seq 1 60); do \
		if curl -fsS $(WEB_URL)/health/live >/dev/null 2>&1; then echo "ready"; break; fi; \
		sleep 1; \
		if [ $$i = 60 ]; then echo "web never became ready"; tail -50 .make/web.log; kill $$(cat .make/web.pid) 2>/dev/null || true; exit 1; fi; \
	done
	@echo "==> running E2E tests"
	-FLOWHUB_E2E_BASE_URL=$(WEB_URL) dotnet test tests/FlowHub.Web.E2ETests --filter "Category=E2E"; \
		status=$$?; \
		echo "==> stopping FlowHub.Web (pid=$$(cat .make/web.pid))"; \
		kill $$(cat .make/web.pid) 2>/dev/null || true; \
		rm -f .make/web.pid; \
		exit $$status

test-all: ## Run backend + frontend tests, then start Postgres + FlowHub.Web and open it in Microsoft Edge
	@$(MAKE) test-backend
	@$(MAKE) test-frontend
	@$(MAKE) db-up
	@$(MAKE) db-migrate
	@mkdir -p .make
	@echo "==> starting FlowHub.Web at $(WEB_URL)…"
	@cd $(WEB_PROJECT) && \
		ASPNETCORE_URLS=$(WEB_URL) ASPNETCORE_ENVIRONMENT=Development \
		setsid nohup dotnet run --no-launch-profile > ../../.make/web.log 2>&1 & echo $$! > ../../.make/web.pid
	@for i in $$(seq 1 60); do \
		if curl -fsS $(WEB_URL)/health/live >/dev/null 2>&1; then echo "ready (pid=$$(cat .make/web.pid))"; break; fi; \
		sleep 1; \
		if [ $$i = 60 ]; then echo "web never became ready"; tail -50 .make/web.log; exit 1; fi; \
	done
	@echo "==> opening $(WEB_URL) in Microsoft Edge"
	@( command -v microsoft-edge >/dev/null 2>&1 && microsoft-edge $(WEB_URL) >/dev/null 2>&1 & ) || \
	 ( command -v microsoft-edge-stable >/dev/null 2>&1 && microsoft-edge-stable $(WEB_URL) >/dev/null 2>&1 & ) || \
	 ( command -v xdg-open >/dev/null 2>&1 && xdg-open $(WEB_URL) >/dev/null 2>&1 & ) || \
	 echo "  (no Edge / xdg-open found — open $(WEB_URL) manually)"
	@echo "==> server kept running. Stop with: kill \$$(cat .make/web.pid)"

playwright-install: ## Install Playwright browser binaries (one-time setup)
	dotnet build tests/FlowHub.Web.E2ETests -c Debug
	pwsh tests/FlowHub.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium 2>/dev/null || \
		tests/FlowHub.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium

test-ai: ## Run live integration tests against real AI providers (requires Ai__*__ApiKey env)
	dotnet test tests/FlowHub.AI.IntegrationTests --filter "Category=AI"

test-beta: ## Run live Beta-smoke tests against real Wallabag + Vikunja (requires Skills__*__ApiToken env)
	dotnet test tests/FlowHub.Skills.IntegrationTests --filter "Category=BetaSmoke"

test-watch: ## Run component tests in watch mode
	dotnet watch test --project tests/FlowHub.Web.ComponentTests

restore: ## Restore NuGet packages
	dotnet restore $(SOLUTION)

clean: ## Remove build artifacts
	dotnet clean $(SOLUTION)

format: ## Apply dotnet format
	dotnet format $(SOLUTION)

db-up: ## Start PostgreSQL in Docker (detached, waits until healthy)
	docker compose up postgres -d --wait

db-ping: ## Verify the PostgreSQL connection (host:port reachable + SELECT 1)
	@HOST=$${PGHOST:-localhost}; PORT=$${PGPORT:-5432}; DB=$${PGDATABASE:-flowhub}; USER=$${PGUSER:-flowhub}; PASS=$${PGPASSWORD:-dev-secret}; \
		echo "==> ping postgres at $$HOST:$$PORT (db=$$DB user=$$USER)"; \
		if command -v pg_isready >/dev/null 2>&1; then \
			pg_isready -h $$HOST -p $$PORT -d $$DB -U $$USER || exit $$?; \
		else \
			( exec 3<>/dev/tcp/$$HOST/$$PORT ) 2>/dev/null && echo "tcp: ok" || { echo "tcp: FAIL ($$HOST:$$PORT unreachable)"; exit 1; }; \
		fi; \
		if docker compose ps --status running postgres >/dev/null 2>&1 && [ -n "$$(docker compose ps -q postgres)" ]; then \
			docker compose exec -T -e PGPASSWORD=$$PASS postgres psql -h localhost -U $$USER -d $$DB -tAc "select 'ok'" \
				| grep -q '^ok$$' && echo "psql: SELECT 1 ok" || { echo "psql: FAIL"; exit 1; }; \
		elif command -v psql >/dev/null 2>&1; then \
			PGPASSWORD=$$PASS psql -h $$HOST -p $$PORT -U $$USER -d $$DB -tAc "select 'ok'" \
				| grep -q '^ok$$' && echo "psql: SELECT 1 ok" || { echo "psql: FAIL"; exit 1; }; \
		else \
			echo "psql: skipped (no docker compose container, no host psql)"; \
		fi

db-migrate: ## Apply EF Core migrations against the Docker PostgreSQL
	ConnectionStrings__Default="Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret" \
		dotnet ef database update \
		--project source/FlowHub.Persistence \
		--startup-project source/FlowHub.Web

migrate: ## Apply EF Core migrations to the local database (requires running PostgreSQL)
	dotnet ef database update \
		--project source/FlowHub.Persistence \
		--startup-project source/FlowHub.Web

ai-ping: ## Smoke-test the configured AI provider with a tiny chat call (env: Ai__Provider, Ai__<Provider>__ApiKey)
	$(SECRET_EXEC) dotnet run --project $(AIPING_PROJECT) -- ping

ai-classify: ## Run IClassifier against TEXT (default: URL + todo samples). Usage: make ai-classify TEXT="todo: buy milk"
	$(SECRET_EXEC) dotnet run --project $(AIPING_PROJECT) -- classify $(TEXT)

ai-embed: ## Run IEmbeddingService against TEXT (env: Embeddings__ApiKey, Embeddings__Model). Usage: make ai-embed TEXT="hello"
	$(SECRET_EXEC) dotnet run --project $(AIPING_PROJECT) -- embed $(TEXT)

smoke-prod: ## Boot full prod compose stack and smoke-test health, /metrics, capture submit + embedding round-trip
	@echo "==> [1/6] docker compose up --build (detached, --wait until healthy)"
	$(SECRET_EXEC) docker compose up --build -d --wait
	@echo "==> [2/6] verifying flowhub.migrations exited 0"
	@MIG_STATUS=$$(docker compose ps -a --format '{{.Service}} {{.ExitCode}}' | awk '$$1=="flowhub.migrations"{print $$2; exit}'); \
		if [ "$$MIG_STATUS" = "0" ]; then echo "    migrations: exit 0"; else echo "    FAIL: migrations exit=$$MIG_STATUS"; exit 1; fi
	@WEB_CID=$$(docker compose ps -q flowhub.web); \
		if [ -z "$$WEB_CID" ]; then echo "FAIL: flowhub.web container not running"; exit 1; fi; \
		CURL="docker run --rm --network container:$$WEB_CID curlimages/curl:8.10.1 -fsS --max-time 10"; \
		echo "==> [3/6] GET /health/live"; \
		$$CURL http://localhost:5070/health/live > /dev/null && echo "    /health/live: 200" || { echo "    FAIL: /health/live"; exit 1; }; \
		echo "==> [4/6] GET /metrics — expect dotnet_* and http_* series"; \
		METRICS=$$(mktemp); $$CURL http://localhost:5070/metrics > $$METRICS || { echo "    FAIL: /metrics"; rm -f $$METRICS; exit 1; }; \
		grep -q "^dotnet_" $$METRICS && echo "    dotnet_* series: ok" || { echo "    FAIL: no dotnet_* metrics"; rm -f $$METRICS; exit 1; }; \
		grep -q "^http_" $$METRICS && echo "    http_* series:   ok" || { echo "    FAIL: no http_* metrics"; rm -f $$METRICS; exit 1; }; \
		rm -f $$METRICS; \
		echo "==> [5/6] POST /api/v1/captures (URL capture for embedding round-trip)"; \
		BODY=$$($$CURL -X POST -H "Content-Type: application/json" \
			-d '{"content":"https://en.wikipedia.org/wiki/Hexagonal_architecture","source":"Api"}' \
			http://localhost:5070/api/v1/captures); \
		CAPTURE_ID=$$(echo "$$BODY" | sed -n 's/.*"id":"\([0-9a-fA-F-]\{36\}\)".*/\1/p'); \
		if [ -z "$$CAPTURE_ID" ]; then echo "    FAIL: no id in response: $$BODY"; exit 1; fi; \
		echo "    captured id: $$CAPTURE_ID"; \
		echo "==> [6/6] polling Captures.Embedding (up to 30s)"; \
		if [ -z "$$EMBEDDINGS__APIKEY" ] && [ -z "$$Embeddings__ApiKey" ]; then \
			echo "    skipped — EMBEDDINGS__APIKEY not set in env / .env (embedding consumer no-ops)"; \
		else \
			for i in $$(seq 1 30); do \
				HAS=$$(docker compose exec -T postgres psql -U flowhub -d flowhub -tAc \
					"SELECT \"Embedding\" IS NOT NULL FROM \"Captures\" WHERE \"Id\"='$$CAPTURE_ID'" 2>/dev/null | tr -d ' '); \
				if [ "$$HAS" = "t" ]; then echo "    embedding column populated after ~$${i}s"; break; fi; \
				sleep 1; \
				if [ $$i -eq 30 ]; then echo "    FAIL: embedding still NULL after 30s — check CaptureEmbeddingConsumer logs"; exit 1; fi; \
			done; \
		fi
	@echo "==> smoke OK — stack left running. Tear down with: make smoke-down"

smoke-down: ## Stop the prod compose stack started by smoke-prod (preserves volumes)
	docker compose down
