# Coverage gate

CI fails on a per-assembly coverage regression. Tooling lives in
`tools/check-coverage.py`; thresholds in `coverage.thresholds.json` at
the repo root.

## How it works

1. The `Test` step in `.github/workflows/ci.yml` runs the full default
   suite with `--collect:"XPlat Code Coverage"`, producing one
   `coverage.cobertura.xml` per test project under `coverage/`.
2. The `Coverage gate` step calls `tools/check-coverage.py`, which:
   - parses every cobertura report,
   - unions hits across reports keyed by `(class-name, line-number)` —
     different test projects emit different `filename=` formats for the
     same class (`Telemetry/X.cs` vs `FlowHub.Core/Telemetry/X.cs`), so
     filename-keyed merging would double-count,
   - decomposes each branchy line's `condition-coverage="N% (a/b)"` into
     `b` slots so partial branch coverage from one project can be
     completed by another (relevant for `FlowHubDbContext.OnModelCreating`:
     the Npgsql arm is covered by `Persistence.Tests` via Testcontainers,
     the non-Npgsql arm by `Api.IntegrationTests` via InMemory),
   - computes per-assembly line% / branch%,
   - compares to `coverage.thresholds.json`,
   - exits non-zero on any miss; the gate's summary table is also
     appended to `$GITHUB_STEP_SUMMARY`.

## Threshold philosophy

Thresholds are **floors, not targets**. They guard against silent
regression. Raise them upward as new tests land; the gate only fails
when coverage drops *below* the listed value.

Aspirational targets (per [#126](https://github.com/freaxnx01/FlowHub-CAS-AISE/issues/126)):

| Assembly | Line target | Why not 100 % |
|---|---:|---|
| `FlowHub.Core` | 100 % | — |
| `FlowHub.Skills` | 100 % | — |
| `FlowHub.Api` | 100 % | — |
| `FlowHub.AI` | ≥ 99 % | 4 unreachable `_ => throw` arms documented |
| `FlowHub.Web` | ≥ 95 % | literal 100 % on Blazor wiring isn't a useful goal |
| `FlowHub.Persistence` | baseline-or-better | hand-rolled SQL / EF design-time factory excluded |

Initial values reflect what's measured today on `main` with a small
buffer for noise. They will be ratcheted upward as the remaining Web
small-tails and any new Persistence work lands.

## Updating thresholds

```bash
# Reproduce the CI run locally
just test-coverage

# See current numbers
pip install --user defusedxml
python3 tools/check-coverage.py \
    --reports 'coverage/**/coverage.cobertura.xml' \
    --thresholds coverage.thresholds.json
```

If you're adding tests that lift an assembly, edit the corresponding
entry in `coverage.thresholds.json`. The gate runs on every PR, so a
lowered threshold needs a PR-description note explaining why.

## Tool tests

`python3 tools/test_check_coverage.py` — stdlib `unittest`, no extra
dependencies beyond `defusedxml` (already installed by the CI step).
