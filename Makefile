# FlowHub — common dev tasks
# Usage: make <target>
#
# `make` with no target prints help.

.DEFAULT_GOAL := help

# Auto-load .env (gitignored) so ai-* targets and dev runs pick up secrets.
-include .env
export

.PHONY: help run watch build test test-ai test-beta test-watch restore clean format db-up db-migrate migrate ai-ping ai-classify ai-embed

SOLUTION       := FlowHub.slnx
WEB_PROJECT    := source/FlowHub.Web
WEB_URL        := http://localhost:5070
AIPING_PROJECT := tools/FlowHub.AiPing
TEXT           ?=

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
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

test: ## Run all tests except [Category=AI] and [Category=BetaSmoke]
	dotnet test $(SOLUTION) --no-build --filter "Category!=AI&Category!=BetaSmoke"

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
	dotnet run --project $(AIPING_PROJECT) -- ping

ai-classify: ## Run IClassifier against TEXT (default: URL + todo samples). Usage: make ai-classify TEXT="todo: buy milk"
	dotnet run --project $(AIPING_PROJECT) -- classify $(TEXT)

ai-embed: ## Run IEmbeddingService against TEXT (env: Embeddings__ApiKey, Embeddings__Model). Usage: make ai-embed TEXT="hello"
	dotnet run --project $(AIPING_PROJECT) -- embed $(TEXT)
