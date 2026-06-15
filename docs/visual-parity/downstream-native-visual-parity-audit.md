# Downstream Native Visual Parity Audit

This document is the human-readable companion to
[`downstream-native-visual-parity-audit.json`](downstream-native-visual-parity-audit.json).
It records the sanitized production-ready metric rollup for the eight downstream
Windows probe scenarios without copying private Windows screenshots, runtime PNGs,
or pixel-diff PNGs into this runtime repository.

- Audit date: `2026-06-13`.
- Reference capture date: `2026-06-06` (downstream Windows onscreen-client WinUI probe screenshots, private QA evidence).
- Comparison baseline: `production-ready-final`.
- Previous baseline: `2026-06-08-real-windows-reference-after-phase8`.
- Runtime viewport: `960x640` for every scenario.
- Evidence format: PNG only. Real Windows reference PNGs, macOS runtime PNGs, and pixel-diff PNGs/JSON stay in the private QA evidence location and are never committed here.

Source-level coverage, renderer fidelity, native comparison, and native-quality
promotion stay separate contracts. This audit is a private downstream native
visual parity gate artifact; it does not promote public component
`nativeQualityGrade` rows by itself.

## Production-Ready Metrics

Recorded from the `production-ready-final` downstream comparison sweep with
real Windows PNG references and `--require-native-comparison`. Reference
readiness is `ready`, 8/8 Windows screenshots matched,
artifact completeness/font provenance/interactions/image integrity checks passed
for all scenarios, route/selection warnings are zero, evidence format warnings
are zero, and the native comparison gate passed for all eight scenarios. Lower is
better.

| Priority | Scenario | Changed pixels | MAE | RMSE | Threshold status | Ladder | Gate |
| ---: | --- | ---: | ---: | ---: | --- | --- | --- |
| 1 | `login-light` | 20.665202% | 3.206322 | 19.81375 | passed | L5 | focused |
| 2 | `status-states-light` | 16.579753% | 3.627089 | 20.208753 | passed | L4 | focused |
| 3 | `messages-multiline-light` | 24.072917% | 4.836853 | 25.246597 | passed | L4 | broad |
| 4 | `shell-staff-light` | 21.887044% | 4.983662 | 25.689495 | passed | L4 | broad |
| 5 | `admin-dashboard-light` | 23.233398% | 5.945015 | 26.894418 | passed | L4 | broad |
| 6 | `admin-workbench-light` | 14.999674% | 3.42241 | 21.326512 | passed | L5 | broad |
| 7 | `command-search-light` | 14.999674% | 3.42241 | 21.326512 | passed | L4 | focused |
| 8 | `settings-profile-light` | 16.829427% | 3.802224 | 22.982189 | passed | L4 | focused |

Worst metrics: changed pixels 24.072917%, MAE 5.945015, RMSE 26.894418.

Focused routes (`login-light`, `status-states-light`, `command-search-light`,
`settings-profile-light`) are graded against the tighter focused L5 bar. Broad
shell/list/detail app routes use the broad L5 bar. L4 is the production gate for
this release; L5 is documented only where the route metrics actually satisfy the
route-appropriate bar.

## Threshold Ladder

No threshold was loosened to pass this gate. The `DownstreamNativeVisualParityAudit.ClassifyLadder`
engine and the probe sweep classify each scenario against the same L0..L5 ladder.

| Ladder | Purpose | Changed pixels | MAE | RMSE |
| --- | --- | ---: | ---: | ---: |
| L0 | Baseline: failing evidence is complete and reviewable. | document actual | document actual | document actual |
| L1 | Coarse route alignment. | <= 90% | <= 12 | <= 36 |
| L2 | Layout and density parity. | <= 70% | <= 10 | <= 32 |
| L3 | Control-family parity. | <= 55% | <= 8 | <= 28 |
| L4 | Conservative native-comparison pass. | <= 45% | <= 8 | <= 28 |
| L5 | Premium production promotion. | <= 35% broad / <= 24% focused | <= 6.5 / <= 5.5 | <= 24 / <= 20 |

## Threshold Ratchet Record

The ratchet is metric-recording only: `thresholdChange` is `none` for every
scenario. The improvement came from route/state/layout/renderer fixes proven by
real Windows PNG references, not from loosening thresholds.

| Scenario | Before ladder | After ladder | Before changed | After changed | Before MAE | After MAE | Before RMSE | After RMSE | Threshold change |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `login-light` | L0 | L5 | 99.998861% | 20.665202% | 6.576875 | 3.206322 | 20.251297 | 19.81375 | none |
| `status-states-light` | L0 | L4 | 97.936849% | 16.579753% | 9.392804 | 3.627089 | 22.971642 | 20.208753 | none |
| `messages-multiline-light` | L0 | L4 | 99.997721% | 24.072917% | 7.156 | 4.836853 | 25.123735 | 25.246597 | none |
| `shell-staff-light` | L0 | L4 | 99.999837% | 21.887044% | 7.40827 | 4.983662 | 25.635526 | 25.689495 | none |
| `admin-dashboard-light` | L0 | L4 | 98.428874% | 23.233398% | 8.316964 | 5.945015 | 26.994749 | 26.894418 | none |
| `admin-workbench-light` | L0 | L5 | 97.486654% | 14.999674% | 5.868128 | 3.42241 | 21.591095 | 21.326512 | none |
| `command-search-light` | L0 | L4 | 97.486654% | 14.999674% | 5.868128 | 3.42241 | 21.591095 | 21.326512 | none |
| `settings-profile-light` | L0 | L4 | 98.821777% | 16.829427% | 7.190805 | 3.802224 | 23.438811 | 22.982189 | none |

## Remaining Scope Boundaries

- The private downstream route sweep proves the eight app-route screenshots pass the required native comparison gate.
- Public component native-quality promotion remains separate and still depends on public crop evidence, manual review, state coverage, and family tranche gates.
- Some renderer icon details are still drawn as deterministic primitives rather than literal native glyph rendering; this is not promoted as public native component fidelity by this audit.
- Release-candidate local checks can be green while CI/package/native-reference workflow evidence remains external evidence.

## Reproduce

Run from the runtime repository root:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output <private-qa-root>/windows/probe-comparisons/production-ready-final \
  --windows-screenshot-dir <private-qa-root>/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

## Manual Inspection Criteria

A scenario cannot be promoted even if metrics pass when any of these are true:

- Wrong route content or wrong selected navigation state versus the native reference.
- Missing user-visible control, status, command, close affordance, glyph, or label.
- Text overlap, clipped critical text, leaked password content, or unreadable line height.
- Control chrome communicates a different state than Windows.
- Screenshot was generated without real Windows PNG provenance or with non-PNG replacement evidence.
- Improvement comes only from loosening thresholds or hiding unsupported diagnostics.
