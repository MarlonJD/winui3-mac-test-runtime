# Downstream Native Visual Parity Audit

This document is the human-readable companion to
[`downstream-native-visual-parity-audit.json`](downstream-native-visual-parity-audit.json).
It records the **measurement baseline** for the eight downstream Windows probe
scenarios so the current native-comparison failure is auditable, reproducible,
and reviewable **without** copying private Windows screenshots or pixel-diff PNGs
into this runtime repository.

- Audit date: `2026-06-08`.
- Reference capture date: `2026-06-06` (downstream Windows onscreen-client WinUI
  probe screenshots, private QA evidence).
- Runtime viewport: `960x640` for every scenario.
- Evidence format: PNG only. Real Windows reference PNGs, macOS runtime PNGs, and
  pixel-diff PNGs/JSON stay in the private QA evidence location
  (`/private/tmp/emsi_qa` or the private QA repository) and are never committed
  here. JPG previews are review-only and must not replace pixel-diff inputs.

Source-level coverage, renderer fidelity, native comparison, and native-quality
promotion stay separate contracts. This audit is a **native visual parity**
artifact; it does not change source-level support claims and does not promote any
`nativeQualityGrade`.

## Baseline Metrics (L0)

Recorded from the `2026-06-06` downstream comparison sweep. All eight scenarios
currently fail the conservative native-comparison contract because the
whole-image changed-pixel percentage is near full-frame, even though MAE and RMSE
are already below the broad thresholds. Lower is better.

| Priority | Scenario | Changed pixels | MAE | RMSE | Threshold status | Ladder |
| ---: | --- | ---: | ---: | ---: | --- | --- |
| 1 | `login-light` | 97.911133% | 7.169520 | 21.312369 | failed (changed > 45%) | L0 |
| 2 | `status-states-light` | 99.999023% | 7.558422 | 23.599508 | failed (changed > 45%) | L0 |
| 3 | `messages-multiline-light` | 99.997721% | 6.792517 | 25.316643 | failed (changed > 45%) | L0 |
| 4 | `shell-staff-light` | 99.999349% | 7.085251 | 26.113236 | failed (changed > 45%) | L0 |
| 5 | `admin-dashboard-light` | 99.998698% | 8.055549 | 27.656882 | failed (changed > 45%) | L0 |
| 6 | `admin-workbench-light` | 99.107585% | 5.898421 | 21.813655 | failed (changed > 45%) | L0 |
| 7 | `command-search-light` | 99.107585% | 5.898421 | 21.813655 | failed (changed > 45%) | L0 |
| 8 | `settings-profile-light` | 98.819987% | 5.835193 | 22.975264 | failed (changed > 45%) | L0 |

Artifact completeness, scenario interactions, image integrity, image size, and
external font provenance all pass for every scenario. The failures are **not**
caused by missing references, missing artifacts, blank output, image-size
mismatches, automation failures, or missing external fonts. The changed-pixel
percentage is near full-frame because background tint, top-left/page alignment,
typography rasterization, control stroke placement, and one-pixel separator
differences are global.

`maxChannelDelta` stays diagnostic at `255`; one high-contrast glyph edge can hit
`255` even when the route is visually close.

## Threshold Ladder

Do not jump directly to perfect parity. Ratchet thresholds only after visual
changes are reviewed and public component fixtures still pass. The
`DownstreamNativeVisualParityAudit.ClassifyLadder` engine (shared by the C# audit
type and the probe sweep's `classify_ladder`) returns the highest level whose
whole-image changed-pixel, MAE, and RMSE bars are all satisfied.

| Ladder | Purpose | Changed pixels | MAE | RMSE |
| --- | --- | ---: | ---: | ---: |
| L0 | Baseline: current failing evidence is complete and reviewable. | document actual | document actual | document actual |
| L1 | Coarse route alignment (remove top-left/full-width/page-frame drift). | <= 90% | <= 12 | <= 36 |
| L2 | Layout and density parity. | <= 70% | <= 10 | <= 32 |
| L3 | Control-family parity. | <= 55% | <= 8 | <= 28 |
| L4 | Conservative native-comparison pass. | <= 45% | <= 8 | <= 28 |
| L5 | Premium production promotion. | <= 35% broad / <= 24% focused | <= 6.5 / <= 5.5 | <= 24 / <= 20 |

Focused routes (command, status, and form: `login-light`,
`status-states-light`, `command-search-light`, `settings-profile-light`) are
graded against the tighter focused L5 bar. Broad shell/list/detail app routes use
the broad L5 bar.

## Threshold Ratchet Policy

No scenario threshold is ratcheted or promoted in this audit without a real
`--require-native-comparison` sweep against Windows reference PNGs. The sanitized
manifest currently records the first deferred ratchet candidate,
`login-light` from L0 toward L1, with status
`deferred-pending-required-native-comparison` and evidence pointer
`private-qa:windows/probe-comparisons/2026-06-08-threshold-ratchet`.

Every future ratchet entry must record the scenario, current ladder, target
ladder, status, reason, and private QA evidence pointer. `thresholdChange` stays
`none` until the required native comparison and manual inspection gates pass.

## Shared Gaps

| Category | Shared gap |
| --- | --- |
| Layout | Mac content starts top-left or uses larger pane/card extents while Windows is narrower, centered, or aligned to WinUI page columns. |
| Typography | Vertical rhythm, line baselines, weight, and compact caption/body spacing differ. |
| Control chrome | TextBox, PasswordBox, Button, CheckBox, InfoBar, ProgressBar, ProgressRing, NavigationViewItem, ListView, and AppBarButton use approximate chrome. |
| Density and spacing | Mac rows, cards, inputs, and buttons occupy more space than Windows. |
| Color/theme | Selection fills, InfoBar severity backgrounds, disabled tracks, pane backgrounds, and subtle surfaces differ. |
| Borders/elevation | Mac adds rounded card borders where Windows uses flatter pane separation. |
| List/detail rendering | Mac boxes the panes, uses stronger selected-row blue, and draws separators differently. |
| Text input rendering | Focus underline, clear button, search glyph, password masking density, caret/selection, and natural field width differ. |
| Command/search rendering | Mac search lacks the native focused underline and trailing clear/search glyph; commands render icon-only. |
| Status/progress rendering | Mac InfoBars use vertical strips and bordered white surfaces; Windows fills severity surfaces and shows a close affordance. |
| Route/content fidelity | Some routes show a different rail selection state between Windows and Mac (Admin, Workbench, Status, Profile, Settings). |

## Route And Selection Tracking

The probe sweep now exports, per scenario, the `expectedRouteAnchor` and the
runtime's `selectedNavigationItem`, and raises a `selectionStateWarning` when the
selected rail item does not match the route's expected anchor. The current
reference-free baseline raises route/selection warnings for the runtime selecting
`Settings` on the status route, `Channels` on the messages route, and `Profile`
on the settings route. These warnings are informational; the full route-selection
audit that reconciles each one against the Windows reference lands in the shell
phase. An empty selection (the signed-out `login-light` route) is not flagged.

## How To Reproduce

The sweep writes its evidence outside this repository. Without the Windows
references it runs the macOS runtime, skips native comparison, and still emits the
metric rollup plumbing, route/selection warnings, and PNG-only evidence-format
checks:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline
```

With the Windows references staged, add the reference directory to produce real
per-scenario `changedPixelPercentage`, `meanAbsoluteError`, `rootMeanSquaredError`,
`maxChannelDelta`, and `ladderLevel` values plus the summary `metricRollup`:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Add `--require-native-comparison` only when the phase is expected to pass the
configured native comparison ladder.

## Manual Inspection Criteria

A scenario cannot be promoted even if metrics pass when any of these are true:

- Wrong route content or wrong selected navigation state versus the native reference.
- Missing user-visible control, status, command, close affordance, glyph, or label.
- Text overlap, clipped critical text, leaked password content, or unreadable line height.
- Control chrome communicates a different state than Windows (selected, focused, disabled, error, warning, success, loading, checked).
- Screenshot was generated without real Windows PNG provenance or with non-PNG replacement evidence.
- Improvement comes only from loosening thresholds or hiding unsupported diagnostics.
