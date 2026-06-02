# All-Catalog Readiness Audit

Date: 2026-06-02

This is the Phase 6 ("All-126 catalog closure") per-entry production readiness
audit. It drives every catalog entry to an explicit production disposition so
that no entry is left `unknown` or with silent behavior.

The machine-readable, per-entry source of truth is
`docs/compatibility/all-catalog-readiness-audit.json`. It is generated
deterministically from `docs/compatibility/winui-api-compatibility.catalog.json`
and regenerated or verified with:

```bash
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit          # regenerate
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check  # fail on drift
```

Because the audit is derived from the catalog, it cannot drift from the catalog
source of truth: status drives the disposition, `kind|status` drives the primary
blocker, and `area`/`status` drives the owner phase. The
`CompatibilityCatalogReadinessAudit*` tests fail if the checked-in JSON, the
catalog, or the visual readiness inventory disagree.

## Accounting

The audit accounts for all **126/126** catalog entries and leaves **0** entries
without a production disposition.

## Production Dispositions

Every entry receives exactly one production disposition derived from its catalog
status.

| Disposition | Count | From status | Release-candidate gate |
| --- | ---: | --- | --- |
| Source-level production implementation | 55 | `supported` | Implemented support with required evidence. |
| Bounded source-level production implementation | 35 | `partial` | Bounded support with exact limits and required evidence. |
| Production-ready diagnostic exclusion until promoted | 31 | `planned` | Deterministic diagnostic with promotion exit criteria. |
| Production-ready Windows-only exclusion | 3 | `windows-only` | Deterministic Windows-only exclusion with support policy. |
| Production-ready non-goal exclusion | 2 | `not supported` | Deterministic non-goal exclusion with support policy. |

## Owner Phases

Each entry is owned by the phase responsible for its production disposition.
Materials, composition, and motion entries are owned by the high-fidelity phase
regardless of status; everything else is owned by the phase that delivers its
support or by the catalog closure phase.

| Owner phase | Catalog entries |
| --- | ---: |
| Phase 4: Ring 0 Windows chrome completion | 55 |
| Phase 5: Ring 1 E2E visual completion | 34 |
| Phase 6: All-catalog closure | 21 |
| Phase 8: Materials, motion, and high-fidelity polish | 16 |

## Primary Blockers

Each entry maps to a single primary production blocker by `kind|status`. The
totals reproduce the published blocker counts exactly.

| Primary blocker | Catalog entries | Treatment |
| --- | ---: | --- |
| PB-001 | 14 | Planned API entries remain cataloged roadmap diagnostics until promoted. |
| PB-002 | 28 | Source-level nonvisual, parser, project, or resource entries need implementation or bounded support evidence. |
| PB-003 | 62 | Visual API, element, resource, and state entries need component evidence so `not-rendered`, weak, or poor rows are not hidden. |
| PB-004 | 8 | Theme resources, visual states, and template property elements need state/theme/template evidence before promotion. |
| PB-012 | 14 | Planned, Windows-only, and non-goal entries need precise support-policy and exclusion handling. |

## Evidence Profiles

| Disposition | Required evidence |
| --- | --- |
| Source-level production implementation | Tests, fixtures, macOS artifacts, native WinUI reference when visual, interaction/accessibility evidence where applicable, and docs. |
| Bounded source-level production implementation | Same evidence bar plus exact partial boundary wording and diagnostics for missing behavior. |
| Production-ready diagnostic exclusion until promoted | Deterministic diagnostics, docs, owner or roadmap treatment, and promotion exit criteria. |
| Production-ready Windows-only exclusion | Deterministic exclusion, Windows validation evidence where applicable, support-policy wording, and no local macOS support claim. |
| Production-ready non-goal exclusion | Deterministic non-goal diagnostics and support-policy wording. |

## Relationship To Other Artifacts

- `docs/compatibility/visual-readiness-inventory.json` holds the bucket-level
  aggregation (`allCatalogReadinessAudit.auditBuckets`); this audit expands it to
  per-entry rows. The two must agree on counts, dispositions, and blockers.
- `docs/release/production-evidence-view.md` summarizes the audit on the running
  visual readiness dashboard.
- The Phase 9 release candidate gate consumes this audit and fails if any entry
  lacks a production disposition.
