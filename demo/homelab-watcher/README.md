# Homelab "outside watcher" — Uptime Kuma

A second Uptime Kuma that runs in the **homelab** and monitors the **VPS-DE public
demo from the outside**, pushing alerts to the homelab **ntfy**.

## Why

The VPS-DE Kuma (`status.demo.flowhub.freaxnx01.ch`) watches each demo service, but
it runs *on the VPS* — so it cannot alert when the VPS host or its network dies.
That is exactly the failure you must not miss right before an examiner connects.

This watcher closes that gap: it lives off-VPS, so a total VPS/network outage still
reaches your phone. Two layers, each covering the other's blind spot:

| Layer | Runs on | Catches | Alerts via |
|-------|---------|---------|------------|
| VPS Kuma | VPS-DE | per-service failures (app, DB, skill targets, LLM) | ntfy |
| **This watcher** | **homelab** | **whole-VPS / network death** | **homelab ntfy** |

> Scope: this watcher assumes the homelab is the more stable side (you'd notice if
> your own homelab were down). If you ever want alerting that survives the homelab
> being down too, add a free external check (healthchecks.io / UptimeRobot) as a
> third layer.

## Deployed instance (2026-07-05)

Provisioned in the homelab as a dedicated LXC (kept separate from the existing
`uptimekuma1` at `.156`, which watches homelab services):

| | |
|---|---|
| Proxmox node | `odroid-plus-pve` |
| LXC | `140` (`flowhub-watcher`), unprivileged, `nesting=1,keyctl=1`, `onboot=1` |
| IP | `192.168.1.148/24` |
| UI (LAN) | `https://status.home.freaxnx01.ch` (Local-only) or `http://192.168.1.148:3011` |
| Compose | `/opt/flowhub-watcher/docker-compose.yml` inside the LXC |
| Routing | dispatcher `dynamic.yml` HTTP route → `http://192.168.1.148:3011`; PiHole `status.home → 192.168.1.162`; no Cloudflare record |

**Still operator-facing:** create the Kuma admin account, add the monitors, and
wire ntfy (below) — Kuma stores these in its SQLite volume, not declaratively.

## Deploy

1. Copy this `docker-compose.yml` into the target LXC's docker stack (e.g. merge
   the `flowhub-watcher` service into `~/mydocker/docker-compose.yml`, or run it
   standalone from this folder).
2. Confirm `COMPOSE_PROJECT_NAME=mydocker` is set in that LXC's `.env`
   (homelab-service-routing skill, Critical Rule 2 — prevents orphaned volumes).
3. `docker compose up -d`
4. Reach the UI at `http://<lxc-ip>:3011` (fallback port) and create the admin
   account. Optionally expose it at `status.home.freaxnx01.ch` — see **Routing**.

## Monitors (configured 2026-07-05)

Six HTTP(s) monitors, all probing the **public** VPS endpoints from outside
(interval 60 s, 3 retries, ntfy attached). Set up via `uptime-kuma-api`:

| Monitor | URL | Accepted |
|---------|-----|----------|
| FlowHub demo — liveness | `https://demo.flowhub.freaxnx01.ch/health/live` | 200–299 |
| FlowHub demo — web app (root) | `https://demo.flowhub.freaxnx01.ch/` | 200–299 |
| FlowHub demo — status page | `https://status.demo.flowhub.freaxnx01.ch/status/flowhub-demo` | 200–299 |
| Vikunja (demo skill target) | `https://vikunja.demo.flowhub.freaxnx01.ch` | 200–399 |
| Wallabag (demo skill target) | `https://wallabag.demo.flowhub.freaxnx01.ch` | 200–399 |
| Paperless (demo skill target) | `https://paperless.demo.flowhub.freaxnx01.ch` | 200–399 |

> **Note:** `/health/ready` is **404 at the public edge** (readiness is internal-only),
> so the second monitor checks the app **root** (`/`) instead — that's what the
> examiner actually loads. Admin creds for the Kuma UI are in **Passbolt**
> ("FlowHub demo watcher — Uptime Kuma (homelab .148)").

## ntfy notifications (configured)

Kuma pushes to homelab ntfy, **urgent priority** (breaks through DND):

- **Server URL:** `https://ntfy.home.freaxnx01.ch`
- **Topic:** `flowhub-demo-status` — granted **anonymous write-only** on the ntfy
  server (mirrors the app's existing `flowhub-demo-captures` topic), so Kuma needs
  no token. **Subscribe as the `freax` ntfy user to read it** (server is `deny-all`).
- Verified end-to-end 2026-07-05: a forced DOWN monitor delivered a priority-5
  `Down [Uptime-Kuma]` push to the topic.

To reproduce/repair the monitors + notification, the `uptime-kuma-api` scripts used
here are ephemeral; re-run against `http://192.168.1.148:3011` with the Passbolt
admin creds, or add them via the UI (Settings → Notifications → type `ntfy`).

## Routing (optional — expose the watcher UI at status.home.freaxnx01.ch)

The watcher works without this (it only needs outbound). To view its UI on a clean
hostname, use the **homelab-service-routing** skill, **Local-only** access mode
(no Cloudflare record — you don't need this reachable from the internet):

1. **Dispatcher** (`~/projects/mydocker-compose/services/traefik/traefik/dynamic.yml`):
   add a TCP-passthrough route `status.home.freaxnx01.ch` → `<lxc-ip>:443`
   (requires the local-Traefik labels in `docker-compose.yml` uncommented), commit,
   push, pull on dispatcher LXC 110.
2. **PiHole**: add `192.168.1.162 status.home.freaxnx01.ch` to the shared
   `common/pihole/custom.list`; restart both PiHole containers.
3. **Cloudflare**: none (Local-only).

## Pre-exam check

~30 min before an exam window: open this watcher, confirm all monitors green, and
confirm the ntfy test alert reached your phone. This watcher is what tells you if
the VPS is unreachable *before* the examiner discovers it.
