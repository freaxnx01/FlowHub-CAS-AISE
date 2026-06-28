# Model Routing for the AI-Assisted Dev Workflow

External recommendation saved for reference — a **tiered routing** approach via
**OpenCode** (handles 75+ providers cleanly): pick the model per workflow phase
instead of one model for everything.

| Phase | Model | Why |
|---|---|---|
| Brainstorm + spec + plan | **Opus 4.7 (Max)** | Already paying flat-rate — use the reasoning |
| Implementation (default) | **Qwen3-Coder-Next** via OpenRouter | 256K context fits big plans + code; fast; Apache 2.0 |
| Implementation (EU data needed, work-adjacent) | **Devstral 2** via Mistral | Have credits; EU-hosted |
| Quick fixes / small edits | **DeepSeek V3.2** via OpenRouter | Cheapest competent option |

**Principle:** reasoning-heavy phases (brainstorm / spec / plan) → top-tier
model; bulk implementation → strong open-weights coder with large context;
trivial edits → cheapest competent model. OpenCode does the provider routing.

> Source: chat recommendation (screenshot), saved 2026-06-21. Conceptually related
> to the FlowHub roadmap items *Additional AI Providers* and *LiteLLM Proxy* — but
> this is about the **dev workflow**, not the FlowHub product's runtime providers.
