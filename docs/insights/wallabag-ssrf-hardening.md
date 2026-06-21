# Insight — Wallabag SSRF and the three-layer fix

**Context:** a defensive STRIDE/red-team pass over the public demo
(`demo.flowhub.freaxnx01.ch`) surfaced one high-impact finding: a
**server-side request forgery (SSRF)** reachable by any visitor.

## The finding

The demo classifies a URL-shaped Capture as a *Wallabag* skill and hands the
URL to Wallabag, which **fetches it server-side** to scrape the article. The URL
is fully attacker-controlled and was sent with no validation, and the Wallabag
container sat on the same Docker network (`default`) as the data and
observability backends. So a Capture like `http://169.254.169.254/…` (cloud
metadata) or `http://grafana:3000/…` made Wallabag issue requests to internal
services. Read-back was possible by logging into Wallabag with the published
demo credentials.

Why only Wallabag: SSRF needs a component that **dereferences an attacker URL**.
Wallabag's whole purpose is "fetch this URL." Vikunja receives inert task text
and Paperless receives uploaded bytes — neither fetches an attacker-named URL.

## The fix — defence in depth (three layers)

1. **Network egress isolation (primary).** The sensitive backends moved to
   isolated, internet-less Docker networks (`backend`: postgres, rabbitmq,
   prometheus, grafana; `cache`: redis + paperless). The skill targets stay on
   `default`; Wallabag shares **no** network with the backends, so even a
   redirect or DNS-rebind cannot route there. (`demo/docker-compose.yml`)
2. **Host firewall egress (backstop).** Deny the demo bridge subnet egress to
   link-local (`169.254.0.0/16`, incl. the metadata IP) and RFC1918. Closes the
   metadata endpoint and redirect-to-internal regardless of app or network
   layer. *Operator action on the VPS — not in this repo.*
3. **Application guard (defence in depth + clean UX).** `WallabagSkillIntegration`
   now resolves the target host and refuses any non-publicly-routable address
   (loopback / link-local / RFC1918 / CGNAT / ULA) before calling Wallabag.
   This is advisory only — Wallabag re-resolves DNS and may follow redirects —
   so it complements, never replaces, layers 1–2. The host resolver is injected
   so the IP classification is unit-tested deterministically and offline.

## Forward note

The roadmap's *Capture Enrichment* idea (Wikidata / web-search tools) would add
a **second** attacker-URL-fetch path, this time inside FlowHub itself. Carry the
same public-IP guard into that design.
