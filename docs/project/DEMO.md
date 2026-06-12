# FlowHub Public Demo

A fully open, rate-limited, self-resetting FlowHub instance — designed so the operator pays **€4.50/mo** for the VPS and **€0** for AI inference, while still giving a visitor a working "submit Capture → see it classified by AI" round-trip.

- **URL (live):** <https://demo.flowhub.freaxnx01.ch>
- **Status:** **Live** on VPS-DE (IONOS) — valid Let's Encrypt cert, OpenRouter Gemma classification active, data resets every 15 min.
- **Note:** the live demo tracks `main`, which includes enhancements made **after** the graded submission tag `v0.1.0` (e.g. citation enrichment, one-click example chips, live Vikunja routing). The graded submission is the `v0.1.0` stand, not the live demo.
- **Full runbook:** [`docs/runbooks/public-demo.md`](../runbooks/public-demo.md)

## Posture at a glance

| Concern | Stance |
|---|---|
| Hosting | VPS-DE (existing Hetzner-class VM, Traefik-fronted) |
| Auth | None — fully open; `DemoAuthHandler` auto-signs every request |
| Rate limit | Traefik `RateLimit` per source IP: `average=10/min, burst=20` |
| Data lifetime | 15-min sliding window — `flowhub.demo-reset` sidecar truncates Captures + reseeds 6 fixture rows |
| AI provider | `google/gemma-4-31b-it:free` on OpenRouter, dedicated key with **$1/mo hard cap** |
| AI fallback | Automatic — `KeywordClassifier` takes over when Gemma 429s or quota exhausts |
| Embeddings | **Disabled** in demo profile — `/api/v1/captures/search` returns 503 ProblemDetails (transparent posture, not hidden) |
| Skill writes | **Vikunja: live** — `todo:` captures route to a self-contained demo Vikunja and appear as tasks (auto-provisioned, public read-only link-share, tasks cleared each reset). **Wallabag: disabled** — URL captures stop at `Unhandled`. |
| Observability | Prometheus + Grafana not exposed publicly — internal metrics only |

## Repo layout

```
demo/
├── docker-compose.yml      ← overlay layered on root docker-compose.yml
├── docker-compose.vps.yml  ← VPS-DE Traefik label alignment (web-secure/default/web)
├── .env.example            ← only Ai__OpenRouter__ApiKey is operator-supplied
├── reset/
│   ├── Dockerfile          ← Alpine + postgresql-client + bash + curl + jq
│   ├── reset.sh            ← sleep-loop every RESET_INTERVAL_SECONDS; truncates Captures,
│   │                          reseeds fixtures, purges queues, clears demo Vikunja tasks
│   └── seed.sql            ← fixture captures spanning every LifecycleStage
└── vikunja/
    ├── Dockerfile          ← Alpine + curl + jq one-shot
    └── bootstrap.sh        ← provisions demo user / Inbox project / long-lived token /
                               public read-only link-share → writes /bootstrap/vikunja.env

source/FlowHub.Web/Components/Layout/MainLayout.razor
    └── reads Demo:Mode + Demo:BannerText → renders an info-filled MudAlert
        banner above page content when the demo overlay is active.

docs/runbooks/public-demo.md   ← full design + operator playbook
```

## Quick deploy (on the VPS-DE host)

```bash
git clone https://github.com/freaxnx01/FlowHub-CAS-AISE.git
cd FlowHub-CAS-AISE
cp demo/.env.example .env
# Edit .env → set Ai__OpenRouter__ApiKey (openrouter.ai/keys, $1/mo cap)
#            and TRAEFIK_NETWORK=web (the VPS-DE Traefik's external proxy network)

# demo/docker-compose.vps.yml aligns the Traefik labels with the VPS-DE host
# (entrypoint web-secure, certresolver default, network web).
docker compose -f docker-compose.yml -f demo/docker-compose.yml -f demo/docker-compose.vps.yml up --build -d --wait
```

Cloudflare DNS entry (handled by the `homelab-service-routing` skill once you're ready):

| Record | Type | Target | Proxy |
|---|---|---|---|
| `demo.flowhub` | `A` | `<VPS-DE public IPv4>` | 🟠 Proxied |

## What you still need to do

| Step | Where | Notes |
|---|---|---|
| Create dedicated OpenRouter key | <https://openrouter.ai/keys> | Name `flowhub-demo`, set $1.00 credit cap, store in Passbolt |
| Add Cloudflare DNS record | Cloudflare dashboard or `homelab-service-routing` skill | A-record above |
| Deploy on VPS-DE | `docker compose ... up` per the runbook | First boot pulls the bundled Chromium-less images + builds the reset sidecar |
| Verify rate limit | `for i in {1..25}; do curl -s -o /dev/null -w "%{http_code}\n" https://demo.flowhub.freaxnx01.ch/; done` | Expect 200s rolling into 429s after the burst is exhausted |
| Verify reset | Wait 15 min, check `docker compose logs flowhub.demo-reset` | Should see `demo-reset: complete` log line each cycle |

## Overrides

The compose overlay reads a few env vars from `.env`:

| Var | Default | Purpose |
|---|---|---|
| `Ai__OpenRouter__ApiKey` | *required* | Demo-only OpenRouter key (rotation-safe, $1 cap) |
| `Ai__OpenRouter__Model` | `google/gemma-4-31b-it:free` | Swap if a better free model lands |
| `RESET_INTERVAL_SECONDS` | `900` | 15 min — drop to `300` for noisy demos, raise to `3600` for slower rotation |
| `TRAEFIK_NETWORK` | `traefik_public` | The Docker network the VPS-DE Traefik dispatcher is attached to |
| `POSTGRES_PASSWORD` / `RABBITMQ_*` | `demo-secret-rotate-me` | Demo-local creds, no production secrets reused |

The overlay forces `Embeddings__*`, `Skills__Vikunja__*`, `Skills__Wallabag__*`, and `Auth__OIDC__*` to empty strings regardless of `.env` contents — the demo cannot accidentally hit a real backend even if you copy your dev `.env` over by mistake.

## Operator runbook (excerpts)

| Symptom | Action |
|---|---|
| `429` from Traefik | A single IP exceeded burst — middleware is doing its job, no action needed |
| All classifications show `Keyword` fallback | OpenRouter quota exhausted; check OpenRouter dashboard; demo keeps serving via keyword routing |
| Dashboard empty mid-cycle | Reset just ran — fixture seed is back; wait or trigger `docker compose exec flowhub.demo-reset /reset.sh` |
| Inappropriate user content visible | At most 15 min lifetime; force-reset with the same exec command |
| Take demo offline | `docker compose -f docker-compose.yml -f demo/docker-compose.yml down` |

Full incident playbook + topology diagram: [`docs/runbooks/public-demo.md`](../runbooks/public-demo.md).

## Cost ceiling

- **VPS-DE:** €4.50/mo (existing — no incremental cost)
- **OpenRouter:** $0 expected (Gemma 4 31B free tier), **$1/mo hard cap** as worst case
- **Cloudflare:** $0 (free tier sufficient for the proxied A-record + DDoS protection)
- **Net incremental:** ~$0/mo on top of the homelab budget you already pay
