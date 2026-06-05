# Compatibility Levels

This document defines the productization compatibility levels for the WinUI3
Mac Test Runtime. The source of truth for the level model is
`docs/compatibility/compatibility-levels.json`.

## Current Level

Current level: **L2** for the documented public source-level harness subset.

L2 means supported and partial public rows have source-level app, artifact, and
component evidence with native reference provenance where visual.
L2 does not mean native-quality visual fidelity. Native-quality remains a separate
component-row label that is allowed only for rows promoted through native WinUI
reference crops, macOS runtime crops, component diff crops, and manual review.

## Level Model

| Level | Product meaning | Native-quality claim |
| --- | --- | --- |
| L0 | Runner and artifact harness reliability, including deterministic diagnostics for planned, Windows-only, not-supported, and non-goal surfaces. | Not allowed. |
| L1 | Source-level app startup and XAML ingest through managed macOS execution and compat shadow builds. | Not allowed. |
| L2 | Supported and partial controls render at usable source-level harness quality with component evidence. | Not allowed. |
| L3 | Interaction and accessibility artifacts are strong enough for consumer CI assertions. | Not allowed. |
| L4 | State and theme coverage is explicit for supported control families. | Not allowed. |
| L5 | Selected component rows can be labeled native-quality when their own native-backed evidence supports it. | Allowed only per promoted row. |
| L6 | Consumer package and release readiness evidence is present. | Not allowed by package status alone. |

Levels are product gates, not marketing shortcuts. A release may be L2 for the
source-level harness while only a few individual rows have a native-quality
grade. Package readiness at L6 also does not promote renderer fidelity.

## Catalog Disposition Mapping

| Catalog disposition | Minimum level | Meaning |
| --- | --- | --- |
| `source-level-production-implementation` | L2 | Supported entries have the source-level implementation and evidence required for the documented subset. |
| `bounded-source-level-production-implementation` | L2 | Partial entries have the same evidence bar plus exact boundary wording and diagnostics. |
| `production-ready-diagnostic-exclusion-until-promoted` | L0 | Planned entries are honest diagnostics with an owner and promotion path. |
| `production-ready-windows-only-exclusion` | L0 | Windows-only entries stay excluded from the macOS support claim. |
| `production-ready-non-goal-exclusion` | L0 | Non-goal entries stay explicit non-goals with deterministic diagnostics or docs. |

This keeps source-level production labels separate from native-quality labels:
`supported` and `partial` catalog status can satisfy L2 without implying Fluent
pixel parity, while `planned`, `windows-only`, and `not supported` entries can
be production-ready exclusions without becoming locally supported surfaces.
