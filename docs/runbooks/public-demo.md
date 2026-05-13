# Public Demo Environment

A fully open, rate-limited, self-resetting FlowHub instance at <https://demo.flowhub.freaxnx01.ch>. Designed so the operator pays €4.50/mo for the VPS and **€0** for AI inference, while still giving a visitor a working "click → see Capture classified by AI" round-trip.

## Posture

| Concern | Stance |
|---|---|
| Hosting | VPS-DE (existing Hetzner-class VM, Traefik-fronted) |
| Public URL | `https://demo.flowhub.freaxnx01.ch` (Cloudflare DNS → VPS-DE Traefik) |
| Auth | None — fully open; `Auth__OIDC__*` unset → `DemoAuthHandler` auto-signs every request |
| Rate limit | Traefik `RateLimit` middleware on the demo route: `average=10/min, burst=20, period=1m` per source IP |
| Data lifetime | All Captures + Tags + SkillRuns truncated every 15 minutes by `flowhub.demo-reset` sidecar; small fixture set reseeded |
| AI provider | OpenRouter `google/gemma-4-31b-it:free` only. KeywordClassifier auto-fallback on 429/error. `KeywordClassifier` is also the demo's safety net if the daily quota runs out. |
| Embeddings | **Disabled** — `Embeddings__ApiKey` unset → `AiEmbeddingService` is null → consumer no-ops → `Captures.Embedding` stays NULL. `GET /api/v1/captures/search` returns 503 ProblemDetails with an explanatory body (transparent about what's wired vs not). |
| Skill integrations | **Disabled** — `Skills__Vikunja__*` and `Skills__Wallabag__*` unset → captures stop at `Classified` stage and surface the `MatchedSkill` on the dashboard but never write to anyone's real Vikunja/Wallabag. |
| Observability | Prometheus + Grafana **not exposed publicly** — only `flowhub.web` + `postgres` + `rabbitmq` + `flowhub.demo-reset` go through the demo compose overlay. Operator metrics still scrapable via the VPS internal network. |

## Topology

```
                                    ┌────────────────────────────┐
                                    │ Cloudflare DNS              │
                                    │ demo.flowhub.freaxnx01.ch  │
                                    │ A → <VPS-DE public IP>     │
                                    └─────────────┬──────────────┘
                                                  │ :443 (TLS)
                                                  ▼
                            ┌─────────────────────────────────────┐
                            │ VPS-DE Traefik                       │
                            │ - LetsEncrypt cert resolver          │
                            │ - middlewares: rate-limit, headers   │
                            │ - rule: Host(demo.flowhub.…)         │
                            └─────────────┬───────────────────────┘
                                          │ :5070 (HTTP, in-container)
                                          ▼
   ┌──────────────────────────────────────────────────────────────────┐
   │ Docker network: demo_default                                      │
   │                                                                   │
   │  ┌────────────┐    ┌──────────┐    ┌──────────┐   ┌─────────────┐ │
   │  │flowhub.web │───▶│ postgres │◀───│flowhub.   │   │ flowhub.    │ │
   │  │            │    │(pgvector)│    │demo-reset │   │ migrations  │ │
   │  │            │    │          │    │(*/15 min) │   │ (init only) │ │
   │  └─────┬──────┘    └──────────┘    └─────┬─────┘   └─────────────┘ │
   │        │                                  │                         │
   │        ▼                                  ▼                         │
   │  ┌──────────┐                       ┌──────────┐                    │
   │  │ rabbitmq │◀──────────────────────│ rabbitmq │                    │
   │  │          │   purge_queue every    │ (purge)  │                    │
   │  │          │   15 min               │          │                    │
   │  └──────────┘                       └──────────┘                    │
   └──────────────────────────────────────────────────────────────────┘
```

## Files

- `demo/docker-compose.yml` — overlay layered on top of the root `docker-compose.yml` via `docker compose -f docker-compose.yml -f demo/docker-compose.yml up`. Adds Traefik labels to `flowhub.web`, adds the `flowhub.demo-reset` sidecar, suppresses the public ports on `postgres` / `rabbitmq` / `prometheus` / `grafana`.
- `demo/.env.example` — demo-only env vars; **no real Skills__*, no Embeddings**.
- `demo/reset/Dockerfile` + `demo/reset/reset.sh` — Alpine image with `postgresql-client` + `bash`. Sleep-loop runs `reset.sh` every 900 s. Script TRUNCATEs `Captures` (CASCADE removes Tags + SkillRuns) and reseeds a fixture set of ~6 example captures spanning Wallabag / Vikunja / Orphan / Unhandled stages so the dashboard is never empty.

## Deploy

On the VPS-DE host:

```bash
git clone https://github.com/freaxnx01/FlowHub-CAS-AISE.git
cd FlowHub-CAS-AISE
cp demo/.env.example .env
# Edit .env → set Ai__OpenRouter__ApiKey to the demo-only OpenRouter key
# (the one with a $1/mo hard cap configured in the OpenRouter dashboard)

docker compose -f docker-compose.yml -f demo/docker-compose.yml up --build -d --wait
```

Cloudflare DNS entry (via `homelab-service-routing` skill):
- Type: `A`
- Name: `demo.flowhub`
- Target: VPS-DE public IPv4
- Proxy: 🟠 Proxied (HTTPS termination by Cloudflare → origin TLS by Traefik)

## OpenRouter key hygiene

The demo runs on a **dedicated OpenRouter key**, never the developer key:

1. <https://openrouter.ai/keys> → "Create Key"
2. Name: `flowhub-demo`
3. **Credit limit: $1.00** (hard cap — exhausted key returns 402 → AiClassifier catches → KeywordClassifier fallback kicks in)
4. Rotate quarterly or after suspicious activity.

## Reset cadence

Default: 900 s (15 min). To change:

```yaml
# demo/docker-compose.yml
services:
  flowhub.demo-reset:
    environment:
      RESET_INTERVAL_SECONDS: "900"  # adjust here
```

## Operator runbook

| Symptom | Action |
|---|---|
| `/api/v1/captures` returns 429 from Traefik | Rate-limit middleware fired — a single IP exceeded 20-req burst. No action needed. |
| All classifications show as `Keyword` fallback | OpenRouter daily quota exhausted OR Gemma model unavailable. Check OpenRouter dashboard; demo continues serving via KeywordClassifier. |
| Dashboard is empty mid-cycle | Reset just ran. Wait < 15 min; fixture seed will appear on the next reset, or trigger an immediate reset via `docker compose exec flowhub.demo-reset /reset.sh`. |
| Inappropriate user content visible | At most 15 min lifetime. To purge immediately: `docker compose exec flowhub.demo-reset /reset.sh`. |
| Need to take the demo offline | `docker compose -f docker-compose.yml -f demo/docker-compose.yml down`. Cloudflare DNS can stay; visitors get 522 Origin Unreachable. |

## Out of scope

- Multi-region failover
- WAF beyond Cloudflare's free tier + Traefik rate-limit
- Persistent demo-user accounts
- Captures persisting across resets

## References

- `docker-compose.yml` (production stack reused by overlay)
- `homelab-service-routing` skill (Cloudflare DNS workflow)
- `docs/runbooks/test-services.md` (sibling environment — internal test, not public)
- ROADMAP.md "Additional AI Providers" (future swap to self-hosted Gemma if quota becomes painful)
