#!/usr/bin/env bash
# Build a single Markdown bundle of SUBMISSION.md + all referenced artefacts,
# in TOC order, separated by page breaks. The result is consumed by
# tools/md-to-pdf/render.mjs to produce SUBMISSION-bundle.pdf.
#
# Usage: tools/submission-bundle.sh [<output-md-path>]
# Default output: tools/build/submission-bundle.md
#
# Inclusion list is defined inline below — edit there to add/remove files.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$ROOT/tools/build/submission-bundle.md}"
mkdir -p "$(dirname "$OUT")"

# Each entry: <heading depth>|<title>|<path-relative-to-repo-root>
# - heading depth = number of leading "#" prepended above the included content
# - title         = section title rendered at that heading depth
# - path          = source Markdown file
#
# Files are concatenated in this order, with a page-break separator between
# every entry.
FILES=(
  "0|__cover__|SUBMISSION.md"

  "1|Project description (v4)|docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md"

  "1|Architecture Decision Records|docs/adr/README.md"
  "2|ADR 0001 — Frontend Render Mode & Architecture|docs/adr/0001-frontend-render-mode-and-architecture.md"
  "2|ADR 0002 — Service Architecture & Async Communication|docs/adr/0002-service-architecture-and-async-communication.md"
  "2|ADR 0003 — Async Pipeline|docs/adr/0003-async-pipeline.md"
  "2|ADR 0004 — AI Integration in Services|docs/adr/0004-ai-integration-in-services.md"
  "2|ADR 0005 — Persistence|docs/adr/0005-persistence.md"
  "2|ADR 0006 — Vector Search|docs/adr/0006-vector-search.md"

  "1|Specification & design|docs/spec/system-context.md"
  "2|Use Cases|docs/spec/use-cases.md"
  "2|Non-functional requirements (SMART)|docs/spec/nfa.md"
  "2|Acceptance criteria|docs/spec/acceptance-criteria.md"
  "2|Testing strategy|docs/spec/testing-strategy.md"
  "2|Database model|docs/spec/db-model.md"

  "1|AI usage|docs/ai-usage.md"
  "1|Learnings (personal)|vault/Projektarbeit/Learnings.md"

  "1|Block 1 — Nachbereitung|vault/Blöcke/01 Einführung/01 Einführung - c) Nachbereitung.md"
  "1|Block 2 — Nachbereitung|vault/Blöcke/02 Frontend/02 Frontend - c) Nachbereitung.md"
  "1|Block 3 — Nachbereitung|vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md"
  "1|Block 4 — Nachbereitung|vault/Blöcke/04 Persitence/04 Persitence - c) Nachbereitung.md"
  "1|Block 5 — Nachbereitung|vault/Blöcke/05 Deployment/05 Deployment - c) Nachbereitung.md"

  "1|Per-block insights|docs/insights/block-1.md"
  "2|Block 2 — insights|docs/insights/block-2.md"
  "2|Block 3 — insights|docs/insights/block-3.md"
  "2|Block 4 — insights|docs/insights/block-4.md"
  "2|Block 5 — insights|docs/insights/block-5.md"

  "1|Operations — CI/CD|docs/ci-cd.md"
  "2|Runbook — Authentik OIDC setup|docs/runbooks/authentik-oidc-setup.md"
  "2|Runbook — v0.1.0 final acceptance|docs/runbooks/v0.1.0-final-acceptance.md"
  "2|Runbook — Beta-MVP acceptance (Block 4 milestone, historical)|docs/runbooks/beta-mvp-acceptance.md"
  "2|Runbook — Public demo|docs/runbooks/public-demo.md"
  "2|Runbook — Test services|docs/runbooks/test-services.md"

  "1|Grading rubric|vault/Organisation/Bewertungskriterien.md"
)

: > "$OUT"

for entry in "${FILES[@]}"; do
  depth="${entry%%|*}"
  rest="${entry#*|}"
  title="${rest%%|*}"
  path="${rest#*|}"

  if [[ ! -f "$ROOT/$path" ]]; then
    echo "submission-bundle: missing file: $path" >&2
    exit 1
  fi

  if [[ "$title" == "__cover__" ]]; then
    cat "$ROOT/$path" >> "$OUT"
  else
    {
      printf '\n\n<div style="page-break-before: always;"></div>\n\n'
      printf '%s %s\n\n' "$(printf '#%.0s' $(seq 1 "$depth"))" "$title"
      printf '_Source: `%s`_\n\n' "$path"
      cat "$ROOT/$path"
    } >> "$OUT"
  fi
done

echo "submission-bundle: wrote $OUT ($(wc -l < "$OUT") lines)"
