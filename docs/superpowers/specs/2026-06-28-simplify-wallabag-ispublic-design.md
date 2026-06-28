# Design — Simplify `WallabagSkillIntegration.IsPublic`

**Date:** 2026-06-28
**Issue:** #167
**Type:** Refactor / tech-debt (no behaviour change)

---

## Problem

The coverage report's *Risk Hotspots* panel flags
`WallabagSkillIntegration.IsPublic(IPAddress)` with **cyclomatic complexity 38** —
the highest in the codebase. Coverage is already **100 %**, so the C.R.A.P. score
(38) is driven *purely* by branch complexity, not missing tests. The complexity
lives in a single 6-arm tuple-`switch` over the first two IPv4 octets, plus the
preceding IPv6/loopback guard clauses.

This is **security-sensitive code**: `IsPublic` is the per-address half of the
SSRF guard that decides whether FlowHub will fetch a URL into Wallabag. Any
refactor must preserve its exact classification for every address.

## Goal

Reduce the method's cyclomatic complexity by expressing the IPv4 reserved-range
checks as **data** (a CIDR table) iterated by a small helper, instead of as inline
`switch` arms. **No behavioural change.** All existing tests stay green; coverage
stays at 100 %.

## Non-goals

- No change to `IsPubliclyRoutableAsync`, the DNS resolution path, the
  fail-closed behaviour, or any caller.
- No new reserved ranges added or removed — the set of refused addresses is
  identical before and after.
- No public API / signature changes (`IsPublic` stays `private static`).
- No new NuGet packages.

---

## Behaviour contract (must remain identical)

Locked in by `tests/FlowHub.Skills.Tests/Wallabag/WallabagSkillIntegrationTests.cs`.
`IsPublic` returns **`false`** (non-public → refused) for:

| Address / range | Reason | Covering test input |
|---|---|---|
| `127.0.0.0/8` | loopback (`IPAddress.IsLoopback`) | `127.0.0.1`, `2130706433` (decimal → resolves to `127.0.0.1`) |
| `::1` | IPv6 loopback | `[::1]` |
| `fe80::/10` | IPv6 link-local | `[fe80::1]` |
| IPv6 site-local | deprecated site-local | (framework flag) |
| `fc00::/7` | IPv6 unique-local (ULA) | `[fc00::1]` |
| `10.0.0.0/8` | RFC 1918 | `10.0.0.5` |
| `169.254.0.0/16` | link-local incl. `169.254.169.254` cloud-metadata | `169.254.169.254` |
| `172.16.0.0/12` | RFC 1918 | `172.16.0.1` |
| `192.168.0.0/16` | RFC 1918 | `192.168.1.10` |
| `100.64.0.0/10` | CGNAT (RFC 6598) | `100.64.0.1` |
| `0.0.0.0/8` | unspecified | `0.0.0.0` |

Returns **`true`** (public → allowed) for:

| Address | Covering test |
|---|---|
| Globally-routable IPv4 (e.g. `1.1.1.1`) | `HandleAsync_PublicIpLiteral_IsAllowed` |
| Globally-routable IPv6 | `HandleAsync_PublicIpv6Literal_IsAllowed` |

IPv4-mapped IPv6 addresses are unwrapped to IPv4 first (`MapToIPv4`) so a mapped
private address is still caught — preserved exactly.

> **Note on `127.0.0.0/8`:** the IPv4 table intentionally does *not* include a
> `127` arm — `IPAddress.IsLoopback` already catches every `127.x.x.x`, so a
> table entry would be dead code (and would drop coverage below 100 %). Keep the
> existing comment explaining this.

---

## Chosen approach — CIDR table + membership helper

Replace the inline tuple-`switch` with a static table of reserved IPv4 ranges,
each expressed as a network address + prefix length, and test membership with a
small helper.

```csharp
// Reserved / non-routable IPv4 ranges. 127.0.0.0/8 is intentionally absent —
// IPAddress.IsLoopback already covers it (see IsPublic), so an entry here would
// be unreachable.
private static readonly (byte[] Network, int PrefixBits)[] ReservedV4Ranges =
[
    ([10, 0, 0, 0], 8),       // RFC 1918
    ([169, 254, 0, 0], 16),   // link-local incl. 169.254.169.254 metadata
    ([172, 16, 0, 0], 12),    // RFC 1918
    ([192, 168, 0, 0], 16),   // RFC 1918
    ([100, 64, 0, 0], 10),    // CGNAT (RFC 6598)
    ([0, 0, 0, 0], 8),        // unspecified
];

private static bool IsPublic(IPAddress address)
{
    var ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    if (IPAddress.IsLoopback(ip))
    {
        return false;
    }

    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
    {
        return false;
    }

    if (ip.AddressFamily == AddressFamily.InterNetwork)
    {
        var bytes = ip.GetAddressBytes();
        return !Array.Exists(ReservedV4Ranges, range => IsInRange(bytes, range.Network, range.PrefixBits));
    }

    return true; // globally-routable IPv6
}

// Big-endian prefix match: compares the first PrefixBits of two IPv4 byte arrays.
private static bool IsInRange(byte[] address, byte[] network, int prefixBits)
{
    for (var bit = 0; bit < prefixBits; bit++)
    {
        var mask = (byte)(1 << (7 - (bit % 8)));
        if ((address[bit / 8] & mask) != (network[bit / 8] & mask))
        {
            return false;
        }
    }

    return true;
}
```

### Why this lowers complexity

The 6-arm `switch` (each arm a decision point) becomes a single
`Array.Exists(...)` call — the per-range decisions move from control flow into the
`ReservedV4Ranges` data table. `IsInRange` is a single bounded loop with one
`if`. The method-level cyclomatic complexity of `IsPublic` drops from 38 to a
small single-digit number; `IsInRange` is ~3.

### Equivalence argument (why behaviour is identical)

- `10.0.0.0/8` ⇔ old `(10, _) => false`: prefix 8 compares octet 0 only.
- `169.254.0.0/16` ⇔ old `(169, 254) => false`: prefix 16 compares octets 0–1.
- `172.16.0.0/12` ⇔ old `(172, >= 16 and <= 31)`: prefix 12 = octet 0 full +
  top 4 bits of octet 1; `16..31` is exactly `0001_xxxx` ⇒ identical set.
- `192.168.0.0/16` ⇔ old `(192, 168)`.
- `100.64.0.0/10` ⇔ old `(100, >= 64 and <= 127)`: prefix 10 = octet 0 full +
  top 2 bits of octet 1; `64..127` is exactly `01_xxxxxx` ⇒ identical set.
- `0.0.0.0/8` ⇔ old `(0, _)`.

The `default => true` arm becomes `!Array.Exists(...)` returning `true` when no
range matches. Loopback / IPv6 guards are byte-for-byte unchanged.

### Alternatives considered

- **Extract each arm into named `bool` helpers** (`IsRfc1918`, `IsCgnat`, …):
  reads clearly but only *moves* the branches; method complexity drops but total
  branch count is unchanged and it's more code. Rejected — the table is denser
  and makes the range set auditable in one place.
- **`System.Net.IPNetwork` (.NET 8+) `.Contains`**: would remove the hand-rolled
  mask loop, but pulls range parsing into static init and is arguably less
  explicit about the bit math under audit. Viable, but the hand-rolled helper
  keeps the SSRF logic fully visible in the file with zero new surface. Kept as a
  fallback if review prefers it.

---

## Risks

- **Off-by-one in the prefix mask** would silently widen or narrow a range — the
  worst case for an SSRF guard. Mitigated by the existing test matrix (every
  range has a positive in-range case) **plus** a new boundary test per range
  (first address inside, first address just outside) added in the plan.
- Coverage regression if a `switch` arm's branch had unique coverage. Mitigated:
  the table loop + `IsInRange` are fully exercised by the existing inputs; the
  plan verifies `just test-coverage` stays at 100 % line and the per-assembly
  branch floor.

## Acceptance criteria

- [ ] `IsPublic` cyclomatic complexity meaningfully reduced (target: out of the
      Risk Hotspots red zone — single-digit method complexity).
- [ ] SSRF guard semantics identical — every address in the contract table
      classifies the same as before.
- [ ] All existing tests green (`dotnet test FlowHub.slnx`).
- [ ] Coverage preserved: 100 % line, per-assembly branch floor for
      `FlowHub.Skills` still met (`just test-coverage` + `tools/check-coverage.py`).
- [ ] No public API, signature, or behaviour change; no new packages.
