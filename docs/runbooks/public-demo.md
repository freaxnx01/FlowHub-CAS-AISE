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
| Embeddings | **Disabled** — `Embeddings__*` unset → `Captures.Embedding` stays NULL and `GET /api/v1/captures/search` returns 503 ProblemDetails (transparent posture). The semantic-search feature is demonstrated via the API contract + integration tests + ADR 0006, not run live on the public demo (a self-hosted embedder was trialled but pulled — it didn't earn its VPS memory / ranking quality). |
| Skill integrations | **Vikunja: live** — a self-contained demo Vikunja (sqlite) is provisioned at deploy; `Skills__Vikunja__*` are injected at runtime from the bootstrap env file, so `todo:` captures route to real tasks on a public **writable** link-share (visitors can tick tasks done; wiped every reset). **Wallabag: disabled** — `Skills__Wallabag__*` unset → URL captures stop at `Unhandled`. See "Demo Vikunja" below. |
| Observability | Prometheus + Grafana **not exposed publicly** — operator metrics stay scrapable via the VPS internal network. A lightweight **Uptime Kuma** instance *is* exposed (`status.demo.flowhub.freaxnx01.ch`) as the public-facing uptime monitor + status page — see [§ Uptime monitoring](#uptime-monitoring). |

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

- `demo/docker-compose.yml` — overlay layered on top of the root `docker-compose.yml` via `docker compose -f docker-compose.yml -f demo/docker-compose.yml up`. Adds Traefik labels to `flowhub.web`, adds the `flowhub.demo-reset` sidecar + the live Vikunja stack (`flowhub.vikunja-init`, `vikunja`, `flowhub.vikunja-bootstrap`), suppresses the public ports on `postgres` / `rabbitmq` / `prometheus` / `grafana`.
- `demo/docker-compose.vps.yml` — VPS-DE Traefik label alignment (entrypoint `web-secure`, certresolver `default`, network `web`) for both `flowhub.web` and `vikunja`.
- `demo/.env.example` — demo-only env vars; **no real Skills__*, no Embeddings**.
- `demo/reset/Dockerfile` + `demo/reset/reset.sh` — Alpine image with `postgresql-client` + `bash` + `curl` + `jq`. Sleep-loop runs `reset.sh` every 900 s. Script TRUNCATEs `Captures` (CASCADE removes Tags + SkillRuns), reseeds the fixture set, purges RabbitMQ queues, and clears the demo Vikunja project's tasks (best-effort, using the bootstrap-written creds).
- `demo/vikunja/Dockerfile` + `demo/vikunja/bootstrap.sh` — Alpine + `curl` + `jq` one-shot. Provisions the demo user, an `Inbox` project, a long-lived token, and public link-shares — the Inbox board writable (`right=1`) so visitors can tick todos, the Zitate board read-only (`right=0`); writes `/bootstrap/vikunja.env` (shared volume) consumed by `flowhub.web` and the reset sidecar.

## Deploy

On the VPS-DE host:

```bash
git clone https://github.com/freaxnx01/FlowHub-CAS-AISE.git
cd FlowHub-CAS-AISE
cp demo/.env.example .env
# Edit .env → set Ai__OpenRouter__ApiKey to the demo-only OpenRouter key
# (the one with a $1/mo hard cap configured in the OpenRouter dashboard)

# On VPS-DE, include the vps overlay (web-secure / certresolver default / network web):
docker compose -f docker-compose.yml -f demo/docker-compose.yml -f demo/docker-compose.vps.yml up --build -d
```

Cloudflare DNS entries (via `homelab-service-routing` skill) — `A` records to the VPS-DE public IPv4, not proxied (Traefik terminates TLS):
- `demo.flowhub.freaxnx01.ch` — the FlowHub demo
- `vikunja.demo.flowhub.freaxnx01.ch` — the demo Vikunja board (public writable share for todos; resets every 15 min)

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

## Demo Vikunja (live skill routing)

So that `todo:` captures route somewhere a visitor can *see*, the demo overlay ships a
self-contained Vikunja, provisioned automatically at deploy. Startup order (enforced via
`depends_on` conditions):

```
flowhub.vikunja-init   chown the named volume to uid 1000 (Vikunja runs as 1000;
  (one-shot)           a fresh named volume is root-owned → "permission denied" otherwise)
        ▼
vikunja                sqlite-backed unified image (vikunja/vikunja, port 3456),
                       internal + on the Traefik network as vikunja.demo.flowhub.freaxnx01.ch
        ▼
flowhub.vikunja-bootstrap   register demo user → login (long token) → ensure "Inbox"
  (one-shot)                project → ensure public link-shares (Inbox writable, Zitate read-only) → write
                            /bootstrap/vikunja.env (shared volume)
        ▼
flowhub.web            entrypoint sources /bootstrap/vikunja.env, so Skills__Vikunja__*
                       (BaseUrl, ApiToken, FallbackProjectId) + Demo__Vikunja__ShareUrl are
                       set on the dotnet process → the Vikunja skill registers and the banner
                       renders a "View routed tasks in Vikunja" link to the public share.
```

Each reset cycle, `reset.sh` clears the demo project's tasks via the Vikunja API (the user /
project / token / link-share stay put, so the share URL and FlowHub's config stay valid).

Notes:
- **Token lifetime.** The injected token is a long-lived login JWT (`VIKUNJA_SERVICE_JWTTTLLONG`
  default = 2 592 000 s ≈ 30 days). `VIKUNJA_SERVICE_JWTSECRET` is pinned so tokens survive
  container restarts. A `docker compose up --build` re-runs the bootstrap and refreshes
  everything — redeploy at least monthly to keep the token fresh.
- **Share hash** is stable while the `vikunja-db` volume persists; if the volume is wiped the
  bootstrap mints a new share and the banner link auto-updates from the regenerated env file.
- **Registration stays enabled** (the bootstrap needs it, idempotently). The demo Vikunja holds
  no real data and resets, so this is acceptable; the public reaches it only through the scoped link-shares.
- **Env knobs** (all defaulted, override in `.env`): `VIKUNJA_IMAGE`, `VIKUNJA_PUBLIC_URL`,
  `VIKUNJA_JWT_SECRET`, `VIKUNJA_DEMO_USER`, `VIKUNJA_DEMO_PASSWORD`, `VIKUNJA_DEMO_PROJECT`.

Cloudflare DNS for the Vikunja host (added via `homelab-service-routing`): `A` record
`vikunja.demo.flowhub.freaxnx01.ch → <VPS-DE public IPv4>`, **not** proxied (Traefik terminates TLS).

## Operator runbook

| Symptom | Action |
|---|---|
| `/api/v1/captures` returns 429 from Traefik | Rate-limit middleware fired — a single IP exceeded 20-req burst. No action needed. |
| All classifications show as `Keyword` fallback | OpenRouter daily quota exhausted OR Gemma model unavailable. Check OpenRouter dashboard; demo continues serving via KeywordClassifier. |
| `todo:` captures stop at `Classified`, not `Routed` | Vikunja skill not active. Check `docker logs flowhub-demo-flowhub.vikunja-bootstrap-1` (did it write `/bootstrap/vikunja.env`?) and the web log for `Skill registered (skill=Vikunja)`. Re-run: `docker compose … up -d --force-recreate flowhub.vikunja-bootstrap flowhub.web`. |
| "View routed tasks in Vikunja" link 404s / share empty | Vikunja volume was wiped → stale share hash in a long-lived page. Reload the demo to pick up the new banner link, or re-run the bootstrap. |
| Vikunja board accumulates tasks | Reset sidecar can't reach Vikunja — check `docker logs flowhub-demo-flowhub.demo-reset-1` for the `cleared N Vikunja task(s)` line. |
| Dashboard is empty mid-cycle | Reset just ran. Wait < 15 min; fixture seed will appear on the next reset, or trigger an immediate reset via `docker compose exec flowhub.demo-reset /reset.sh`. |
| Inappropriate user content visible | At most 15 min lifetime. To purge immediately: `docker compose exec flowhub.demo-reset /reset.sh`. |
| Need to take the demo offline | `docker compose -f docker-compose.yml -f demo/docker-compose.yml down`. Cloudflare DNS can stay; visitors get 522 Origin Unreachable. |
| Status page / a monitor shows down | Open `status.demo.flowhub.freaxnx01.ch`. App down → check `flowhub.web` logs + `restart` policy. LLM monitor down → OpenRouter outage/quota; demo keeps serving via KeywordClassifier (expected degradation, not an outage). |

## Uptime monitoring

A self-hosted **Uptime Kuma** (`louislam/uptime-kuma`) ships in the demo overlay as the public-facing monitor for the Block 5 *Monitoring & Observability* learning objective. The internal Prometheus/Grafana stack covers deep metrics; Kuma covers **black-box reachability + a public status page** that needs no login to view.

**DNS (one-time):** add `status.demo.flowhub.freaxnx01.ch` → VPS-DE public IP (Cloudflare, proxied) via the `homelab-service-routing` skill, same as the other demo subdomains.

**First-boot setup** (Kuma stores monitors in its own SQLite volume `uptime-kuma-data`, configured via the UI — not declaratively):

1. Open `https://status.demo.flowhub.freaxnx01.ch`, create the admin account.
2. Add these monitors:

   | Monitor | Type | Target | Notes |
   |---|---|---|---|
   | FlowHub app | HTTP(s) – keyword | `https://demo.flowhub.freaxnx01.ch/health/live` | expect `Healthy` |
   | FlowHub metrics | HTTP(s) – keyword | `https://demo.flowhub.freaxnx01.ch/metrics` | expect `dotnet` — a runtime metric, present on every scrape; confirms the OTel→Prometheus pipeline is alive end-to-end, not just that the app responds |
   | LLM reachability | HTTP(s) | `https://openrouter.ai/api/v1/models` | provider up? (a down LLM is graceful-degradation, not an app outage) |
   | Vikunja | HTTP(s) | `https://vikunja.demo.flowhub.freaxnx01.ch` | skill target |
   | Wallabag | HTTP(s) | `https://wallabag.demo.flowhub.freaxnx01.ch` | skill target |
   | Paperless | HTTP(s) | `https://paperless.demo.flowhub.freaxnx01.ch` | skill target |

3. Create a **public status page** bundling these monitors — link it from the demo banner / submission as the live monitoring artifact.

> The LLM monitor pings the provider directly because FlowHub has no dedicated LLM health endpoint yet; adding an AI `IHealthCheck` (so the provider also surfaces in `/health`) is a tracked follow-up.

## Out of scope

- Multi-region failover
- WAF beyond Cloudflare's free tier + Traefik rate-limit
- Persistent demo-user accounts
- Captures persisting across resets

## References

- `docker-compose.yml` (production stack reused by overlay)
- `homelab-service-routing` skill (Cloudflare DNS workflow)
- `docs/runbooks/test-services.md` (sibling environment — internal test, not public)
- docs/project/ROADMAP.md "Additional AI Providers" (future swap to self-hosted Gemma if quota becomes painful)
