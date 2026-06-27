#!/usr/bin/env python3
"""Per-assembly coverage threshold gate for FlowHub CI.

Why this exists
---------------
`dotnet test FlowHub.slnx --collect:"XPlat Code Coverage"` emits one
cobertura XML per test project. Each XML lists the *same* source classes
under inconsistent `filename=` attributes (`Telemetry/X.cs` vs
`FlowHub.Core/Telemetry/X.cs`) depending on which test project produced it.
Naively unioning by filename double-counts files and undercounts coverage.

This tool dedupes by *class name + line number* so a line covered by *any*
test project counts as covered — which is the only meaningful measurement
when different test projects exercise different branches of the same code
(e.g. Persistence.Tests covers the Npgsql branch of FlowHubDbContext via
Testcontainers, Api.IntegrationTests covers the InMemory branch).

Usage
-----
    tools/check-coverage.py \\
        --reports 'coverage/**/coverage.cobertura.xml' \\
        --thresholds coverage.thresholds.json \\
        [--summary $GITHUB_STEP_SUMMARY]

Exits 0 when every configured assembly meets both line and branch
thresholds; non-zero (and prints a per-assembly miss table) otherwise.
"""

from __future__ import annotations

import argparse
import glob
import json
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

# defusedxml hardens against billion-laughs / XXE — cobertura reports come from
# our own test runs in CI, but a compromised dependency could still emit a
# malicious one, so this is defense-in-depth at near-zero cost.
from defusedxml import ElementTree as ET


@dataclass
class AssemblyCoverage:
    name: str
    lines_total: int
    lines_covered: int
    branches_total: int
    branches_covered: int

    @property
    def line_percent(self) -> float:
        return (
            100.0 * self.lines_covered / self.lines_total if self.lines_total else 0.0
        )

    @property
    def branch_percent(self) -> float:
        return (
            100.0 * self.branches_covered / self.branches_total
            if self.branches_total
            else 0.0
        )


def aggregate(report_paths: list[str]) -> dict[str, AssemblyCoverage]:
    """Union line+branch hits across cobertura reports, deduped by class name.

    Cobertura's branch metric lives on each branchy line via
    `condition-coverage="50% (1/2)"`. Decompose that into N "branch slots"
    keyed by (class, line, slot-index) so partial coverage from one report
    can be completed by another report exercising the missing slot.
    """
    lines: dict[str, set[tuple[str, str]]] = defaultdict(set)
    lines_covered: dict[str, set[tuple[str, str]]] = defaultdict(set)
    branches: dict[str, set[tuple[str, str, int]]] = defaultdict(set)
    branches_covered: dict[str, set[tuple[str, str, int]]] = defaultdict(set)

    for path in sorted(report_paths):
        # A truncated / partial cobertura (e.g. a test crash mid-emit) must fail
        # the gate loudly, not blow up with an uncaught ParseError stack trace.
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as err:
            print(
                f"::error file={path}::malformed cobertura XML: {err}", file=sys.stderr
            )
            raise SystemExit(2) from err

        for pkg in root.findall(".//package"):
            asm = pkg.get("name") or ""
            for cls in pkg.findall(".//class"):
                cname = cls.get("name") or "?"
                for line in cls.findall(".//line"):
                    lno = line.get("number") or ""
                    lines[asm].add((cname, lno))
                    if int(line.get("hits", "0")) > 0:
                        lines_covered[asm].add((cname, lno))

                    # cobertura's branch attribute is spelled "True" by current
                    # coverlet but the spec is case-insensitive; older coverlet
                    # versions and competing tools emit "true". Lowercase before
                    # comparing so a future coverlet bump doesn't silently zero
                    # out branch totals (which would make the gate pass vacuously).
                    if (line.get("branch") or "").lower() == "true":
                        cc = line.get("condition-coverage", "")
                        if "(" not in cc:
                            continue
                        # Parse "100% (2/2)" / "50% (1/2)" / "0% (0/2)".
                        cov, tot = cc.split("(", 1)[1].rstrip(")").split("/")
                        for idx in range(int(tot)):
                            branches[asm].add((cname, lno, idx))
                        for idx in range(int(cov)):
                            branches_covered[asm].add((cname, lno, idx))

    return {
        asm: AssemblyCoverage(
            name=asm,
            lines_total=len(lines[asm]),
            lines_covered=len(lines_covered[asm]),
            branches_total=len(branches[asm]),
            branches_covered=len(branches_covered[asm]),
        )
        for asm in lines
    }


def evaluate(
    coverage: dict[str, AssemblyCoverage],
    thresholds: dict[str, dict[str, float]],
) -> tuple[bool, list[str]]:
    """Return (passed, lines-of-report). Misses surface in the report."""
    report_lines: list[str] = []
    report_lines.append("| Assembly | Line | min | Branch | min | |")
    report_lines.append("|---|---:|---:|---:|---:|:--|")
    passed = True

    # Sort assemblies for deterministic output. Include both configured and
    # unconfigured ones so we can spot drift in either direction.
    all_assemblies = sorted(set(coverage) | set(thresholds))
    for asm in all_assemblies:
        cov = coverage.get(asm)
        th = thresholds.get(asm, {})
        min_line = th.get("line")
        min_branch = th.get("branch")
        if cov is None:
            # Threshold configured but no coverage data — usually means the
            # assembly's tests didn't run. Treat as a miss; CI should fail loudly.
            report_lines.append(
                f"| {asm} | n/a | {min_line} | n/a | {min_branch} | ❌ no data |"
            )
            passed = False
            continue

        line_ok = min_line is None or cov.line_percent >= float(min_line)
        branch_ok = min_branch is None or cov.branch_percent >= float(min_branch)
        status = "✅" if line_ok and branch_ok else "❌"
        if not (line_ok and branch_ok):
            passed = False

        line_cell = f"{cov.line_percent:.1f}%"
        branch_cell = f"{cov.branch_percent:.1f}%" if cov.branches_total else "—"
        report_lines.append(
            f"| {asm} | {line_cell} | {min_line if min_line is not None else '—'} "
            f"| {branch_cell} | {min_branch if min_branch is not None else '—'} | {status} |"
        )

    return passed, report_lines


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--reports",
        required=True,
        help="glob matching cobertura XML reports (e.g. 'coverage/**/coverage.cobertura.xml')",
    )
    parser.add_argument(
        "--thresholds",
        required=True,
        type=Path,
        help="JSON file mapping assembly name → {line, branch} percentages",
    )
    parser.add_argument(
        "--summary",
        type=Path,
        default=None,
        help="optional Markdown summary file to append to (e.g. $GITHUB_STEP_SUMMARY)",
    )
    args = parser.parse_args(argv)

    report_paths = glob.glob(args.reports, recursive=True)
    if not report_paths:
        print(f"::error::no cobertura reports matched: {args.reports}", file=sys.stderr)
        return 2

    with args.thresholds.open() as f:
        raw = json.load(f)
    # JSON has no comment syntax; strip `_`-prefixed keys so the threshold file
    # can hold an inline `_comment` block without the tool treating it as an
    # "assembly with no coverage data" miss.
    thresholds = {k: v for k, v in raw.items() if not k.startswith("_")}

    coverage = aggregate(report_paths)
    passed, lines = evaluate(coverage, thresholds)

    header = "## Coverage gate" + ("" if passed else " — FAILED")
    print(header)
    for line in lines:
        print(line)

    if args.summary:
        with args.summary.open("a") as f:
            f.write("\n" + header + "\n")
            for line in lines:
                f.write(line + "\n")
            f.write("\n")

    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
