
# FlowHub - Management Summary

## Problem

Fragmentierte Workflows: Signal Notes → manuell sortieren → Copy-Paste in verschiedene Tools (Todoist, Read-Later, Kanban). Zeitverschwendung durch Kontext-Switching.

## Lösung

**FlowHub**: Intelligenter Integration-Hub, der bestehende Self-Hosted Services orchestriert. AI kategorisiert Input automatisch und routet an den richtigen Service.

## Architektur

**Core (Quarkus/Java):**

- API Gateway & Orchestrator
- AI Processing (Ollama - lokal, kostenlos)
- Integration Hub mit REST Clients
- Passbolt für Credentials

**Integriert:** Wallabag (Read-Later) | Wekan (Kanban) | n8n (Workflows) | Paperless-ngx | Obsidian (Doku)

## Workflow-Beispiel

```
Signal: "Inception - rewatch"
→ FlowHub empfängt
→ AI: "Movie"
→ Wekan: Card erstellt in "Movies → To Watch"
```

## MVP (Phase 1)

Signal Input → AI Categorization → Wallabag/Wekan Integration → Simple UI

## Evolution

Block 1-2: Modularer Monolith | Block 3: Microservices | Block 4: RAG (Homelab-Doku)

## CAS-Fit

✅ Verteilte Architektur | ✅ AI-Integration | ✅ Microservice-Evolution | ✅ Praxisrelevant

## Value

Automatisiert 80% der manuellen Workflow-Arbeit. Kostenlos (~$0-5). Demonstriert Enterprise-Integration-Patterns.