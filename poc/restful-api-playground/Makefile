.DEFAULT_GOAL := help

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

infra-up: ## Start RabbitMQ
	docker compose up -d

infra-down: ## Stop RabbitMQ
	docker compose down

order-service: ## Run OrderService on :5001 (REST) + :5003 (gRPC)
	dotnet run --project src/OrderService

notification-service: ## Run NotificationService on :5002
	dotnet run --project src/NotificationService

build: ## Build all projects
	dotnet build RestfulApiPlayground.slnx

test: ## Run all tests
	dotnet test RestfulApiPlayground.slnx

restore: ## Restore NuGet packages
	dotnet restore RestfulApiPlayground.slnx

demo: ## POST a test order and GET notifications
	@echo "--- POST /orders ---"
	curl -s -X POST http://localhost:5001/orders \
		-H "Content-Type: application/json" \
		-d '{"item":"Widget","quantity":3,"customer":"Alice"}' | jq .
	@echo ""
	@sleep 2
	@echo "--- GET /notifications ---"
	curl -s http://localhost:5002/notifications | jq .
