#!/usr/bin/env bash
# demo/reset/reset.sh — wipes user-generated Captures + reseeds a small fixture
# set so the demo dashboard is never empty. Invoked from the container's
# sleep-loop every $RESET_INTERVAL_SECONDS (default 900s = 15 min).
#
# Idempotent: TRUNCATE … CASCADE removes Captures + dependent Tags + SkillRuns;
# seed inserts have explicit UUIDs and are upsert-safe via ON CONFLICT DO NOTHING.

set -euo pipefail

ts() { date -u +%Y-%m-%dT%H:%M:%SZ; }

echo "[$(ts)] demo-reset: starting"

# 1. Truncate Captures (CASCADE wipes Tags + SkillRuns)
PGOPTS="-h ${PGHOST:-postgres} -p ${PGPORT:-5432} -U ${PGUSER:-flowhub} -d ${PGDATABASE:-flowhub}"
export PGPASSWORD
psql $PGOPTS -v ON_ERROR_STOP=1 -c 'TRUNCATE TABLE "Captures" CASCADE;'
echo "[$(ts)] demo-reset: TRUNCATE Captures CASCADE — done"

# 2. Reseed fixture captures so the dashboard has realistic content.
psql $PGOPTS -v ON_ERROR_STOP=1 -f /seed.sql
echo "[$(ts)] demo-reset: seed inserted"

# 3. Purge MassTransit RabbitMQ queues so in-flight events from before the
#    reset don't process against the now-truncated DB.
RABBITMQ_HOST="${RABBITMQ_HOST:-rabbitmq}"
RABBITMQ_USER="${RABBITMQ_USER:-flowhub}"
RABBITMQ_PASSWORD="${RABBITMQ_PASSWORD:-dev-secret}"
RABBITMQ_API="http://${RABBITMQ_HOST}:15672/api"

for queue in capture-enrichment capture-embedding skill-routing lifecycle-fault-observer; do
  curl -fsS -u "${RABBITMQ_USER}:${RABBITMQ_PASSWORD}" \
    -X DELETE "${RABBITMQ_API}/queues/%2F/${queue}/contents" >/dev/null 2>&1 \
    && echo "[$(ts)] demo-reset: purged queue ${queue}" \
    || echo "[$(ts)] demo-reset: queue ${queue} not present (skipped)"
done

# 4. Clear tasks from the demo Vikunja project (best-effort) so routed captures
#    don't accumulate across cycles. The user/project/token/link-share stay put —
#    written once by demo/vikunja/bootstrap.sh into the shared /bootstrap volume.
VIKUNJA_ENV="${VIKUNJA_ENV_FILE:-/bootstrap/vikunja.env}"
if [ -f "${VIKUNJA_ENV}" ]; then
  # shellcheck disable=SC1090
  . "${VIKUNJA_ENV}"
  if [ -n "${VIKUNJA_API_URL:-}" ] && [ -n "${VIKUNJA_TOKEN:-}" ] && [ -n "${VIKUNJA_PROJECT_ID:-}" ]; then
    auth="Authorization: Bearer ${VIKUNJA_TOKEN}"
    # Clear both demo boards: Inbox (todos) and Zitate (quotes).
    for pid in ${VIKUNJA_PROJECT_ID} ${VIKUNJA_ZITATE_PROJECT_ID:-}; do
      [ -n "${pid}" ] || continue
      ids=$(curl -fsS "${VIKUNJA_API_URL}/projects/${pid}/tasks" -H "${auth}" 2>/dev/null \
        | jq -r 'if type=="array" then .[].id else empty end' 2>/dev/null || true)
      count=0
      for id in ${ids}; do
        curl -fsS -X DELETE "${VIKUNJA_API_URL}/tasks/${id}" -H "${auth}" >/dev/null 2>&1 && count=$((count + 1)) || true
      done
      echo "[$(ts)] demo-reset: cleared ${count} Vikunja task(s) from project ${pid}"
    done
  else
    echo "[$(ts)] demo-reset: Vikunja env incomplete — skipping task cleanup"
  fi
else
  echo "[$(ts)] demo-reset: no ${VIKUNJA_ENV} — Vikunja not provisioned, skipping task cleanup"
fi

# 5. Clear Wallabag entries (best-effort) — mints a fresh token via password grant.
WALLABAG_ENV="${WALLABAG_ENV_FILE:-/bootstrap/wallabag.env}"
if [ -f "${WALLABAG_ENV}" ]; then
  # shellcheck disable=SC1090
  . "${WALLABAG_ENV}"
  if [ -n "${WALLABAG_API_URL:-}" ] && [ -n "${WALLABAG_CLIENT_ID:-}" ]; then
    tok=$(curl -fsS -X POST "${WALLABAG_API_URL}/oauth/v2/token" \
      -d "grant_type=password&client_id=${WALLABAG_CLIENT_ID}&client_secret=${WALLABAG_CLIENT_SECRET}&username=${WALLABAG_USER}&password=${WALLABAG_PASSWORD}" \
      2>/dev/null | jq -r '.access_token // empty' || true)
    if [ -n "${tok}" ]; then
      ids=$(curl -fsS "${WALLABAG_API_URL}/api/entries.json?perPage=1000" -H "Authorization: Bearer ${tok}" 2>/dev/null \
        | jq -r '._embedded.items[].id' 2>/dev/null || true)
      count=0
      for id in ${ids}; do
        curl -fsS -X DELETE "${WALLABAG_API_URL}/api/entries/${id}.json" -H "Authorization: Bearer ${tok}" >/dev/null 2>&1 && count=$((count + 1)) || true
      done
      echo "[$(ts)] demo-reset: cleared ${count} Wallabag entr(ies)"
    else
      echo "[$(ts)] demo-reset: Wallabag token grant failed — skipping"
    fi
  fi
else
  echo "[$(ts)] demo-reset: no ${WALLABAG_ENV} — skipping Wallabag clear"
fi

# 6. Clear paperless-ngx documents (best-effort) via bulk_edit delete.
PAPERLESS_ENV="${PAPERLESS_ENV_FILE:-/bootstrap/paperless.env}"
if [ -f "${PAPERLESS_ENV}" ]; then
  # shellcheck disable=SC1090
  . "${PAPERLESS_ENV}"
  if [ -n "${PAPERLESS_API_URL:-}" ] && [ -n "${PAPERLESS_TOKEN:-}" ]; then
    auth="Authorization: Token ${PAPERLESS_TOKEN}"
    ids=$(curl -fsS "${PAPERLESS_API_URL}/api/documents/?page_size=1000" -H "${auth}" 2>/dev/null \
      | jq -c '[.results[].id]' 2>/dev/null || echo '[]')
    if [ "${ids}" != "[]" ] && [ -n "${ids}" ]; then
      curl -fsS -X POST "${PAPERLESS_API_URL}/api/documents/bulk_edit/" -H "${auth}" \
        -H 'Content-Type: application/json' \
        -d "{\"documents\":${ids},\"method\":\"delete\",\"parameters\":{}}" >/dev/null 2>&1 \
        && echo "[$(ts)] demo-reset: deleted paperless documents ${ids}" \
        || echo "[$(ts)] demo-reset: paperless bulk delete failed"
    else
      echo "[$(ts)] demo-reset: no paperless documents to clear"
    fi
  fi
else
  echo "[$(ts)] demo-reset: no ${PAPERLESS_ENV} — skipping Paperless clear"
fi

echo "[$(ts)] demo-reset: complete"
