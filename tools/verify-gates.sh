#!/usr/bin/env bash
#
# verify-gates — prove FlowHub's OWN resolved config catches the known-bad cases.
#
# Central green in agent-pipeline only proves FlowHub is gated if FlowHub uses
# the config unmodified. The moment a threshold is overridden here (or someone
# fat-fingers a rule to `none`), that guarantee breaks silently. This pulls the
# canonical fixtures from agent-pipeline at a PINNED ref into a scratch dir
# *inside* FlowHub's tree (so they inherit FlowHub's Directory.Build.props /
# .editorconfig) and asserts each ENFORCED gate fires.
#
# Phase-1 note (issue #94): CA1502/CA1506 are demoted to warnings here (debt,
# #95/#97), so this checks the gates FlowHub currently ENFORCES: IDE0005 (build
# error) and the method-size check. Re-add CA1502/CA1506 below when Phase-2
# re-promotes them. (The method-size action step is deferred on Linux —
# agent-pipeline#91 — but the check script itself is Linux-safe and verified here.)
set -euo pipefail
IFS=$'\n\t'

# Pinned agent-pipeline ref the canonical fixtures are pulled from — not floating.
REF="c3977e1bd9b190be01b98eb93c260230495cfa1b"
RAW="https://raw.githubusercontent.com/freaxnx01/agent-pipeline/${REF}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRATCH="${ROOT}/.verify-gates"
fail=0

cleanup() { rm -rf "${SCRATCH}"; }
trap cleanup EXIT
rm -rf "${SCRATCH}"
mkdir -p "${SCRATCH}"

fetch() { curl -sSfL --create-dirs -o "${SCRATCH}/$2" "${RAW}/$1"; }
note()  { printf '\n=== %s ===\n' "$1"; }

# --- IDE0005: enforced as an error in FlowHub's .editorconfig ----------------
note "IDE0005 fixture must FAIL the build under FlowHub's resolved config"
fetch "gate-tests/build-failures/UnusedUsing/UnusedUsing.cs"     "ide/UnusedUsing.cs"
fetch "gate-tests/build-failures/UnusedUsing/UnusedUsing.csproj" "ide/UnusedUsing.csproj"
if dotnet build "${SCRATCH}/ide/UnusedUsing.csproj" -c Release --nologo -v q >/dev/null 2>&1; then
  printf '::error::IDE0005 did NOT fail the build — the gate is dead in FlowHub\n'
  fail=1
else
  printf 'OK: IDE0005 fired\n'
fi

# --- Method-size check (Linux-safe; independent of the Metrics.exe generator) -
fetch ".github/actions/dotnet-quality/check-method-size.py" "check-method-size.py"
fetch "gate-tests/metrics-script/over-limit.xml"            "over-limit.xml"
fetch "gate-tests/metrics-script/garbage.xml"               "garbage.xml"

note "method-size over-limit (41 > 40) must exit 1"
if python3 "${SCRATCH}/check-method-size.py" --max 40 "${SCRATCH}/over-limit.xml" >/dev/null 2>&1; then
  printf '::error::method-size over-limit did not trip\n'; fail=1
else
  printf 'OK: method-size over-limit fired\n'
fi

note "method-size zero-methods guard (garbage XML) must exit 1"
if python3 "${SCRATCH}/check-method-size.py" --max 40 "${SCRATCH}/garbage.xml" >/dev/null 2>&1; then
  printf '::error::method-size zero-methods guard did not trip\n'; fail=1
else
  printf 'OK: method-size zero-methods guard fired\n'
fi

note "VERDICT"
if (( fail != 0 )); then
  printf '::error::verify-gates FAILED — at least one enforced gate is dead in FlowHub\n'
  exit 1
fi
printf 'verify-gates PASSED — FlowHub config enforces the checked gates\n'
