#!/usr/bin/env bash
# Build the concise, self-contained MAIN reading document of the submission:
# a slim cover + the full Projektbeschreibung (Vision → Architektur → ADRs →
# KI-Reflexion) + a Lernziele-/Rubrik-Abdeckung self-check. ~30 pages.
#
# This is the document an examiner reads end-to-end without browsing GitHub.
# It is distinct from:
#   - SUBMISSION.md / SUBMISSION.pdf — the 7-page hub that links every artefact
#   - SUBMISSION-bundle.pdf — the complete, everything-inlined offline archive
#
# Output is consumed by tools/md-to-pdf/render.mjs.
# Usage: tools/submission-main.sh [<output-md-path>]   (default: tools/build/submission-main.md)

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$ROOT/tools/build/submission-main.md}"
mkdir -p "$(dirname "$OUT")"

# Each entry: <heading depth>|<title>|<path-relative-to-repo-root>
FILES=(
  "1|Projektbeschreibung|docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md"
  "1|Lernziele- & Rubrik-Abdeckung (Self-Check)|docs/lernziele-coverage.md"
)

# Slim cover (this document is meant to be read; keep it short).
cat > "$OUT" <<'COVER'
# FlowHub — CAS AISE Projektarbeit · Hauptdokument

**CAS AI-Assisted Software Engineering (AISE)** · W4B-C-AS001 · ZH-Sa-1 · FS26
**Student:** Andreas Imboden
**Repository:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
**Abgabedatum:** 2026-07-04

> **Lesehinweis.** Dies ist die kompakte, in sich geschlossene Lesefassung der
> Projektarbeit: die vollständige **Projektbeschreibung** (Vision, Stakeholder,
> Funktionsumfang, **Systemarchitektur mit Diagrammen**, Architekturentscheidungen,
> KI-Reflexion, Risiken) und eine **Lernziele-/Rubrik-Abdeckung** als Self-Check.
>
> Für den vollständigen Quellcode und alle Detail-Artefakte (ADR-Volltexte,
> Block-Nachbereitungen, Test-Strategie, Sequenzdiagramme) siehe das Repository.
> Die Einreichungs-Seite `SUBMISSION.md` (separates PDF) verlinkt jedes einzelne
> Artefakt direkt; `SUBMISSION-bundle.pdf` ist das vollständige Offline-Archiv.
COVER

for entry in "${FILES[@]}"; do
  depth="${entry%%|*}"
  rest="${entry#*|}"
  title="${rest%%|*}"
  path="${rest#*|}"

  if [[ ! -f "$ROOT/$path" ]]; then
    echo "submission-main: missing file: $path" >&2
    exit 1
  fi

  {
    printf '\n\n<div style="page-break-before: always;"></div>\n\n'
    printf '%s %s\n\n' "$(printf '#%.0s' $(seq 1 "$depth"))" "$title"
    printf '_Source: `%s`_\n\n' "$path"
    cat "$ROOT/$path"
  } >> "$OUT"
done

echo "submission-main: wrote $OUT ($(wc -l < "$OUT") lines)"
