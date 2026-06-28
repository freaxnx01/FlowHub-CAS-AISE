# Implementation Plan — Simplify `WallabagSkillIntegration.IsPublic`

**Date:** 2026-06-28
**Issue:** #167
**Spec:** [`docs/superpowers/specs/2026-06-28-simplify-wallabag-ispublic-design.md`](../specs/2026-06-28-simplify-wallabag-ispublic-design.md)

> Pure refactor (no behaviour change) of an SSRF guard. TDD discipline applies:
> the new boundary tests are written **first** and must pass against the *current*
> implementation (they encode existing behaviour), then the refactor must keep
> them — and all existing tests — green.

---

## Files

| File | Change |
|---|---|
| `source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs` | Replace inline IPv4 `switch` in `IsPublic` with `ReservedV4Ranges` table + `IsInRange` helper |
| `tests/FlowHub.Skills.Tests/Wallabag/WallabagSkillIntegrationTests.cs` | Add CIDR boundary tests (in-range / just-outside) per range |

No other files. No DI, no public surface, no packages.

---

## Task 1 — Add boundary tests that pin the current behaviour (RED → GREEN on current code)

Add to `WallabagSkillIntegrationTests.cs`. These encode the *existing* contract,
so they pass against today's `switch` — they exist to catch any range drift the
refactor might introduce.

For each reserved range, assert the **first in-range** address is refused and the
**first address just outside** is allowed (driven through `HandleAsync` with an
IP-literal URL, the existing test idiom). Use a stub resolver only where a literal
won't parse.

Boundary inputs to cover (refused vs allowed):

- `172.16.0.0/12`: `172.16.0.0` and `172.31.255.255` refused; `172.15.255.255`
  and `172.32.0.0` allowed.
- `100.64.0.0/10`: `100.64.0.0` and `100.127.255.255` refused; `100.63.255.255`
  and `100.128.0.0` allowed.
- `10.0.0.0/8`: `10.255.255.255` refused; `11.0.0.0` allowed.
- `192.168.0.0/16`: `192.168.255.255` refused; `192.169.0.0` and `192.167.255.255`
  allowed.
- `169.254.0.0/16`: `169.254.255.255` refused; `169.253.0.0` / `169.255.0.0`
  allowed.

Reuse the existing `[Theory]/[InlineData]` pattern. For the "allowed" cases,
expect the POST to Wallabag to fire (set up the mock `Expect` and assert
`Success`), mirroring `HandleAsync_PublicIpLiteral_IsAllowed`. For "refused"
cases, expect the `*non-public*` `InvalidOperationException` with zero outbound
calls.

**Gate:** `dotnet test tests/FlowHub.Skills.Tests` — all new + existing tests
green **before** any production change. If a new boundary test fails on current
code, the test encodes a wrong expectation — fix the test, not the implementation.

## Task 2 — Refactor `IsPublic` to the CIDR table

In `source/FlowHub.Skills/Wallabag/WallabagSkillIntegration.cs`:

1. Add the `ReservedV4Ranges` static readonly table exactly as in the spec,
   keeping the per-range comments and the "127.0.0.0/8 intentionally absent"
   note (move the existing comment to the table).
2. Replace the IPv4 tuple-`switch` block with:
   ```csharp
   var bytes = ip.GetAddressBytes();
   return !Array.Exists(ReservedV4Ranges, range => IsInRange(bytes, range.Network, range.PrefixBits));
   ```
3. Add the `IsInRange(byte[] address, byte[] network, int prefixBits)` helper
   (big-endian prefix-bit comparison) as in the spec.
4. Leave the loopback guard, the IPv6 link/site/ULA guard, the `MapToIPv4`
   unwrap, and the IPv6 `return true` tail untouched.

Keep methods small and the SSRF intent obvious in XML-doc / comments.

**Gate:** `dotnet build FlowHub.slnx` clean (warnings are errors).

## Task 3 — Verify behaviour + coverage unchanged

1. `dotnet test FlowHub.slnx` — full suite green (not just the Skills project).
2. `just test-coverage` then
   `python3 tools/check-coverage.py --reports 'coverage/**/coverage.cobertura.xml' --thresholds coverage.thresholds.json`
   — `FlowHub.Skills` still at 100 % line and at/above its branch floor.
3. Sanity-check the Risk Hotspots: regenerate the report
   (`reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:docs/coverage -reporttypes:Html`)
   and confirm `IsPublic` is no longer in the red zone. (Regenerating the
   committed `docs/coverage/` snapshot is optional and can be a separate commit —
   do **not** bundle unrelated report churn into this refactor's diff unless asked.)

**Gate:** all three green; if cyclomatic complexity is still red, iterate on Task 2
(e.g. confirm the lambda isn't inflating the count) — do **not** weaken tests.

## Task 4 — Commit

Conventional Commit:

```
refactor(skills): table-drive IsPublic SSRF range checks (closes #167)

Replace the 6-arm IPv4 tuple-switch in WallabagSkillIntegration.IsPublic with
a static ReservedV4Ranges CIDR table + IsInRange prefix-match helper. Cuts the
method's cyclomatic complexity out of the coverage Risk Hotspots red zone with
no behaviour change. Adds per-range boundary tests. Coverage stays at 100%.
```

---

## Definition of done

- [ ] New boundary tests added and green on both old and new code.
- [ ] `IsPublic` uses the CIDR table; complexity out of the red zone.
- [ ] `dotnet test FlowHub.slnx` fully green.
- [ ] `just test-coverage` → 100 % line, `FlowHub.Skills` branch floor met.
- [ ] No public API / behaviour change, no new packages, warnings-clean build.
