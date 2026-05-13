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

echo "[$(ts)] demo-reset: complete"
