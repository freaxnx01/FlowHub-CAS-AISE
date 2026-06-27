"""Unit tests for tools/check-coverage.py.

Run with `python3 -m unittest tools.test_check_coverage` from repo root, or
`python3 tools/test_check_coverage.py` directly. CI invokes the latter.
"""

from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


def _load_module():
    """Load `check-coverage.py` (hyphen in filename, can't import directly).

    Register the module in sys.modules *before* exec — @dataclass needs to
    look up the owning module's __dict__ to walk the class's bases.
    """
    here = Path(__file__).parent
    spec = importlib.util.spec_from_file_location(
        "check_coverage", here / "check-coverage.py"
    )
    module = importlib.util.module_from_spec(spec)
    sys.modules["check_coverage"] = module
    spec.loader.exec_module(module)
    return module


cc = _load_module()


# Two cobertura snippets that intentionally use *different* `filename=` formats
# for the same class. Aggregation by filename would double-count; aggregation
# by class name (which the tool does) collapses them correctly.
REPORT_NPGSQL = """\
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.5" branch-rate="0.5" version="1.9">
  <packages>
    <package name="FlowHub.Persistence" line-rate="0.5" branch-rate="0.5">
      <classes>
        <class name="FlowHub.Persistence.FlowHubDbContext" filename="FlowHub.Persistence/FlowHubDbContext.cs">
          <lines>
            <line number="20" hits="1" branch="False" />
            <line number="22" hits="1" branch="False" />
            <line number="28" hits="0" branch="True" condition-coverage="0% (0/2)" />
            <line number="30" hits="0" branch="False" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
"""

REPORT_INMEMORY = """\
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.5" branch-rate="0.5" version="1.9">
  <packages>
    <package name="FlowHub.Persistence" line-rate="0.5" branch-rate="0.5">
      <classes>
        <class name="FlowHub.Persistence.FlowHubDbContext" filename="FlowHubDbContext.cs">
          <lines>
            <line number="20" hits="0" branch="False" />
            <line number="22" hits="0" branch="False" />
            <line number="28" hits="1" branch="True" condition-coverage="100% (2/2)" />
            <line number="30" hits="1" branch="False" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
"""


class AggregateTests(unittest.TestCase):
    def test_union_across_reports_with_inconsistent_filenames(self):
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / "a.xml").write_text(REPORT_NPGSQL)
            (Path(d) / "b.xml").write_text(REPORT_INMEMORY)
            result = cc.aggregate([str(Path(d) / "a.xml"), str(Path(d) / "b.xml")])

        # Class is FlowHub.Persistence.FlowHubDbContext, 4 lines total.
        # Report A covers 20+22; report B covers 28+30 → union = all 4.
        # Line %: 100 (4/4). Branch %: 100 (2/2 from report B).
        asm = result["FlowHub.Persistence"]
        self.assertEqual(asm.lines_total, 4)
        self.assertEqual(asm.lines_covered, 4)
        self.assertEqual(asm.line_percent, 100.0)
        self.assertEqual(asm.branches_total, 2)
        self.assertEqual(asm.branches_covered, 2)

    def test_single_report_branch_partial_coverage(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "only.xml"
            p.write_text(REPORT_NPGSQL)
            result = cc.aggregate([str(p)])

        asm = result["FlowHub.Persistence"]
        self.assertEqual(asm.lines_covered, 2)  # 20, 22
        self.assertEqual(asm.lines_total, 4)
        self.assertEqual(asm.branches_covered, 0)  # 0/2 on line 28
        self.assertEqual(asm.branches_total, 2)


class EvaluateTests(unittest.TestCase):
    def _cov(self, line_total=10, line_covered=10, branch_total=4, branch_covered=4):
        return {
            "FlowHub.Demo": cc.AssemblyCoverage(
                name="FlowHub.Demo",
                lines_total=line_total,
                lines_covered=line_covered,
                branches_total=branch_total,
                branches_covered=branch_covered,
            )
        }

    def test_passes_when_above_thresholds(self):
        passed, _ = cc.evaluate(
            self._cov(),
            {"FlowHub.Demo": {"line": 95, "branch": 80}},
        )
        self.assertTrue(passed)

    def test_fails_when_line_below_threshold(self):
        passed, lines = cc.evaluate(
            self._cov(line_covered=8),
            {"FlowHub.Demo": {"line": 95, "branch": 80}},
        )
        self.assertFalse(passed)
        self.assertTrue(any("❌" in line for line in lines))

    def test_fails_when_branch_below_threshold(self):
        passed, _ = cc.evaluate(
            self._cov(branch_covered=2),
            {"FlowHub.Demo": {"line": 95, "branch": 80}},
        )
        self.assertFalse(passed)

    def test_missing_threshold_only_treated_as_passthrough(self):
        # No `branch` threshold configured → branch % is reported but not enforced.
        passed, _ = cc.evaluate(
            self._cov(branch_covered=0),
            {"FlowHub.Demo": {"line": 95}},
        )
        self.assertTrue(passed)

    def test_threshold_for_missing_assembly_fails(self):
        passed, lines = cc.evaluate(
            {},  # no coverage data at all
            {"FlowHub.Missing": {"line": 80}},
        )
        self.assertFalse(passed)
        self.assertTrue(any("no data" in line for line in lines))


class CliTests(unittest.TestCase):
    """End-to-end via subprocess — checks the script returns the right exit code."""

    def _run(self, reports_glob: str, thresholds: dict) -> subprocess.CompletedProcess:
        here = Path(__file__).parent
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False) as f:
            json.dump(thresholds, f)
            thresholds_path = f.name
        return subprocess.run(
            [
                sys.executable,
                str(here / "check-coverage.py"),
                "--reports",
                reports_glob,
                "--thresholds",
                thresholds_path,
            ],
            capture_output=True,
            text=True,
        )

    def test_exits_zero_when_thresholds_met(self):
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / "a.xml").write_text(REPORT_NPGSQL)
            (Path(d) / "b.xml").write_text(REPORT_INMEMORY)
            r = self._run(
                str(Path(d) / "*.xml"),
                {"FlowHub.Persistence": {"line": 95, "branch": 95}},
            )
        self.assertEqual(r.returncode, 0, msg=r.stdout + r.stderr)

    def test_exits_nonzero_when_threshold_missed(self):
        with tempfile.TemporaryDirectory() as d:
            # Single report — 50% line coverage on FlowHubDbContext.
            (Path(d) / "only.xml").write_text(REPORT_NPGSQL)
            r = self._run(str(Path(d) / "*.xml"), {"FlowHub.Persistence": {"line": 95}})
        self.assertEqual(r.returncode, 1, msg=r.stdout + r.stderr)
        self.assertIn("FAILED", r.stdout)

    def test_exits_nonzero_when_no_reports_found(self):
        with tempfile.TemporaryDirectory() as d:
            r = self._run(str(Path(d) / "*.xml"), {})
        self.assertEqual(r.returncode, 2)
        self.assertIn("no cobertura reports matched", r.stderr)

    def test_exits_nonzero_with_clean_error_when_xml_is_malformed(self):
        # A truncated cobertura (mid-emit test crash) must fail loudly with a
        # workflow-annotated `::error::` line and exit 2, not blow up with an
        # uncaught ParseError stack trace.
        with tempfile.TemporaryDirectory() as d:
            (Path(d) / "broken.xml").write_text(
                "<coverage><packages><pack"
            )  # truncated
            r = self._run(str(Path(d) / "*.xml"), {"FlowHub.Persistence": {"line": 95}})
        self.assertEqual(r.returncode, 2)
        self.assertIn("malformed cobertura XML", r.stderr)


class BranchAttributeCaseInsensitivityTests(unittest.TestCase):
    """Some coverlet builds / competing tools emit `branch="true"` instead of
    `"True"`. The tool must accept either — a silent case mismatch would zero
    out all branch totals and let the gate pass vacuously on a regression."""

    LOWERCASE_BRANCH = """\
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="1" branch-rate="0.5" version="1.9">
  <packages><package name="FlowHub.Persistence" line-rate="1" branch-rate="0.5">
    <classes><class name="X" filename="X.cs">
      <lines>
        <line number="10" hits="1" branch="true" condition-coverage="50% (1/2)" />
      </lines>
    </class></classes>
  </package></packages>
</coverage>
"""

    def test_lowercase_branch_attribute_is_recognised(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "lower.xml"
            p.write_text(self.LOWERCASE_BRANCH)
            asm = cc.aggregate([str(p)])["FlowHub.Persistence"]
        self.assertEqual(asm.branches_total, 2, 'branch="true" (lowercase) must count')
        self.assertEqual(asm.branches_covered, 1)


if __name__ == "__main__":
    unittest.main(verbosity=2)
