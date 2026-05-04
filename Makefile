# FlowHub — common dev tasks
# Usage: make <target>
#
# `make` with no target prints help.

.DEFAULT_GOAL := help
.PHONY: help run watch build test test-ai test-beta test-watch restore clean format

SOLUTION    := FlowHub.slnx
WEB_PROJECT := source/FlowHub.Web
WEB_URL     := http://localhost:5070

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
