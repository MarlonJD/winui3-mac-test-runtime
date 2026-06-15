# WinUI3 Mac Real Windows Production Visual Parity Plan

Date: 2026-06-08

Owner subtree: `tools/winui3-mac-test-runtime`

Plan status: active implementation plan. Do not implement as part of plan creation.

## Objective

Make `tools/winui3-mac-test-runtime` production-ready for premium native Windows
visual parity using the real downstream Windows WinUI probe PNG references:

```text
/private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

The current real comparison baseline is:

```text
/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8
```

Production-ready means the runtime keeps source-level WinUI compatibility,
renderer/layout fidelity, native visual comparison, and native-quality promotion
as separate contracts, while all eight real downstream probe scenarios pass the
native comparison gate with no route/selection mismatch and with public
clean-room fixture coverage guarding each renderer family.

## Required Guidance And Skills

Read before implementation:

- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/.codex/skills/windows-winui3-design/SKILL.md`
- `README.md`
- `docs/visual-parity/README.md`
- `docs/plans/2026-06-08-winui3-mac-premium-native-visual-parity-plan.md`
- `docs/visual-parity/downstream-native-visual-parity-audit.md`
- `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8/summary.md`
- Per-scenario `pixel-diff.json`, `tree.json`, and the visual PNG triplets under the baseline directory.

Use these skills before execution:

- `emsi-workflows:emsi-task-router`
- `emsi-workflows:emsi-plan-artifact`
- `emsi-workflows:emsi-native-ui-overlay`
- `emsi-workflows:emsi-verification-gate`
- `superpowers:systematic-debugging`
- `superpowers:test-driven-development`
- `google-eng-practices`
- Repo-local `windows-winui3-design` at `/Users/marlonjd/Developer/monorepos/emsi_monorepo/.codex/skills/windows-winui3-design/SKILL.md`

Creative Production is not required for this plan because the target visual
direction is already approved: parity with real native Windows WinUI screenshots,
not a new brand or exploratory visual direction.

## Non-Negotiable Constraints

- Do not create, switch, rename, delete, or otherwise perform branch operations.
- Do not push.
- Do not run GitHub workflows.
- Do not implement runtime code during the planning task.
- Keep QA screenshots, Windows references, macOS runtime PNGs, pixel-diff PNGs,
  and private visual review evidence in `/private/tmp/emsi_qa`, not in this
  runtime repository.
- Keep source-level coverage separate from native visual parity.
- Do not loosen thresholds to make failures pass. Threshold work is a ratchet
  after renderer/layout/state fixes prove real metric improvement.
- Prefer route-state, renderer, layout, and bounds fixes before token polish.
- Write failing tests before implementation in every phase. A phase cannot start
  production code until its RED tests fail for the intended reason.
- Keep public fixture verification separate from private downstream evidence.

## Evidence Boundary

The real Windows PNG references and downstream app screenshots are private QA
evidence. This runtime repo may store sanitized docs, test code, manifests,
commands, and aggregate metrics, but must not copy private PNGs or pixel-diff
images into `docs/visual-parity` or any other runtime repo path.

The real-reference sweep is the native visual parity evidence. The checked-in
component dashboards are public source-level and component-family gates. Passing
one does not imply the other.

## Baseline Diagnosis

The current after-phase8 baseline is evidence-ready but not visually ready:

- 8 scenarios total.
- 0 passed, 8 failed.
- Reference readiness: ready.
- Windows screenshots matched: 8/8.
- Artifact completeness: passed for 8/8.
- External font provenance: passed for 8/8, using Segoe UI Variable and Segoe Fluent Icons from a repo-external font directory.
- Scenario interactions: passed for 8/8.
- Image integrity: passed for 8/8.
- Image size: 960x640 actual and expected for every scenario.
- Evidence format warnings: 0.
- Native comparison: failed, required, 0 passed, 8 failed.
- Worst changed pixels: 99.999837%.
- Worst MAE: 9.392804.
- Worst RMSE: 26.994749.
- Route/selection warnings: 3.

The failure is not missing screenshots, blank output, missing fonts, missing
artifacts, or dimension drift. Each `pixel-diff.json` reports a full-window
960x640 changed bounding box, so the first-order failure is global route,
client-area, root-layout, background, and page-bounds drift. Token tuning alone
cannot fix this baseline.

The visual PNG triplets show the same pattern repeatedly:

- `login-light`: Windows centers a narrow login column; the runtime anchors the
  form at the top-left, widens controls, fills the InfoBar differently, and
  stretches the primary button.
- `shell-staff-light`, `messages-multiline-light`, and `admin-dashboard-light`:
  content exists, but list/detail regions, selected rows, pane boundaries,
  InfoBar surfaces, and button sizing differ structurally.
- `command-search-light`: search and command regions differ in focus underline,
  search/clear glyph placement, command labels, overflow affordance, and rail
  selection.
- `status-states-light`: severity InfoBars are closer than old baselines but
  still differ in layout, icon, close affordance, progress placement, and route
  selection state.
- `settings-profile-light`: form content exists, but the native reference uses a
  flatter bordered form surface while the runtime uses a rounded filled card and
  selects a different rail item.

## Current Scenario Metrics

All values below are from
`/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8/summary.json`
and the per-scenario `visual/pixel-diff.json` files. Lower is better.

| Scenario | Group | Status | Changed pixels | MAE | RMSE | Max delta | Ladder | Route selection |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `login-light.json` | login | failed | 99.998861% | 6.576875 | 20.251297 | 255 | L0 | expected `Login`, selected none, warning none |
| `shell-staff-light.json` | read-surface | failed | 99.999837% | 7.408270 | 25.635526 | 255 | L0 | expected `Home`, selected `HomeNavigationItem`, warning none |
| `messages-multiline-light.json` | messages-multiline | failed | 99.997721% | 7.156000 | 25.123735 | 255 | L0 | expected `Messages`, selected `ChannelsNavigationItem`, warning present |
| `admin-dashboard-light.json` | admin-dashboard | failed | 98.428874% | 8.316964 | 26.994749 | 255 | L0 | expected `Admin`, selected `AdminNavigationItem`, warning none |
| `admin-workbench-light.json` | admin-workbench | failed | 97.486654% | 5.868128 | 21.591095 | 255 | L0 | expected `Admin`, selected `AdminWorkbenchNavigationItem`, warning none |
| `command-search-light.json` | command-search | failed | 97.486654% | 5.868128 | 21.591095 | 255 | L0 | expected `Admin`, selected `AdminWorkbenchNavigationItem`, warning none |
| `status-states-light.json` | status-states | failed | 97.936849% | 9.392804 | 22.971642 | 255 | L0 | expected `Status`, selected `SettingsNavigationItem`, warning present |
| `settings-profile-light.json` | settings-profile | failed | 98.821777% | 7.190805 | 23.438811 | 255 | L0 | expected `Settings`, selected `ProfileNavigationItem`, warning present |

The baseline route-selection warnings are:

| Scenario | Expected route anchor | Reported selected item | Warning |
| --- | --- | --- | --- |
| `messages-multiline-light.json` | `Messages` | `ChannelsNavigationItem` | selected navigation item does not match expected route anchor |
| `status-states-light.json` | `Status` | `SettingsNavigationItem` | selected navigation item does not match expected route anchor |
| `settings-profile-light.json` | `Settings` | `ProfileNavigationItem` | selected navigation item does not match expected route anchor |

The tree exports currently do not expose a reliable `IsSelected` property for
each `NavigationViewItem`; they expose `tag` and `content`. The sweep therefore
needs a testable selected-state contract rather than inference-only route
selection warnings. The visual triplets also show that Windows reference rail
selection can differ from the macOS body route on some shell routes, so Phase 1
must reconcile scenario metadata, Windows reference state, runtime tree state,
and painted rail state before any pixel pass can be trusted.

## Ranked Root-Cause Breakdown

1. Route/selection mismatch
   - Evidence: 3 route-selection warnings in the baseline, affecting Messages,
     Status, and Settings/Profile. Tree exports do not publish per-item
     `IsSelected`, and visual triplets show selected rail state can disagree
     with route body state.
   - Impact: blocks production-readiness even if metrics improve because the
     screenshot can represent the wrong app state.
   - First fix class: explicit route/selection model, tree/accessibility export,
     sweep assertions, and downstream scenario reconciliation.

2. Viewport, client-area, and root-layout alignment
   - Evidence: dimensions match 960x640, but full-frame changed bounding boxes
     remain 960x640 for every scenario. Login runtime content starts at `x=32`
     while Windows centers the column around `x=305`; shell routes place runtime
     content from `x=248` with surfaces that differ from native client geometry.
   - Impact: creates near-100% changed pixels before control-specific fixes can
     matter.
   - First fix class: root background, client origin, page host bounds,
     NavigationView pane width, content frame offsets, scale, and scroll viewer
     ownership.

3. Page container bounds
   - Evidence: runtime login uses a 520px container at the left edge with 456px
     controls; Windows uses a centered, narrower visual column. Shell route
     list/detail panes are wider and boxed differently than Windows.
   - Impact: shifts every downstream control and separator, making full-window
     diff metrics noisy.
   - First fix class: layout tests for route body rectangles, list/detail
     columns, form card bounds, InfoBar widths, and natural button/input sizing.

4. Background, surface, and token drift
   - Evidence: light background, pane background, InfoBar fills, selected-row
     fills, and border strokes differ globally. MAE/RMSE are moderate but
     changed pixels are near-full-frame, which is consistent with subtle
     full-surface color drift.
   - Impact: once geometry is correct, token drift will dominate broad metrics.
   - First fix class: theme tokens only after root geometry and control family
     bounds are stable.

5. Typography and baseline drift
   - Evidence: fonts resolve from external Segoe directories, but line heights,
     title/body/caption baselines, semibold weights, password mask density, and
     compact row text alignment differ.
   - Impact: causes persistent edge and glyph differences after geometry fixes.
   - First fix class: measured Segoe text metrics, WinUI type ramp, baseline
     alignment, and password/text field content positioning.

6. Control chrome drift
   - Evidence: TextBox, PasswordBox, AutoSuggestBox, Button, CheckBox, InfoBar,
     ProgressBar, ProgressRing, CommandBar, AppBarButton, ListView, and
     NavigationViewItem chrome differ from WinUI.
   - Impact: blocks manual native-quality review even if layout metrics improve.
   - First fix class: renderer primitive tests and public component fixture
     coverage by control family.

7. List/detail/card/border drift
   - Evidence: runtime wraps shell/detail areas in rounded cards and stronger
     borders where Windows uses flatter pane separation and simpler bordered
     detail regions. Selected rows are larger and more saturated than Windows.
   - Impact: affects Home, Messages, Admin, Workbench, Command/Search, and
     Settings.
   - First fix class: NavigationView, ListView, Grid/Border, and detail pane
     rendering family work.

8. Command/search/status/form-specific drift
   - Evidence: command search lacks native focused underline and glyph placement;
     AppBarButton label/overflow behavior differs; InfoBars differ by severity;
     settings form uses rounded filled cards and text fields show clear glyphs
     differently from Windows.
   - Impact: focused routes remain visibly wrong after shared shell/layout work.
   - First fix class: route-family phases after shared state, viewport, and page
     alignment are stable.

## Acceptance Criteria For Production-Ready Status

Production-ready status requires all of the following:

- The final real-reference downstream sweep exits successfully with
  `--require-native-comparison`.
- 8/8 scenarios pass native comparison against the real Windows PNG references.
- `referenceReadiness.status == "ready"`.
- Windows screenshots matched: 8/8.
- Artifacts, fonts, interactions, image integrity, and image size all pass.
- Route/selection warnings: 0.
- Evidence format warnings: 0.
- No screenshot or pixel-diff PNG is copied into the runtime repository.
- Public clean-room fixture checks pass for the control families touched by each
  phase.
- Source-level dashboards still pass:
  `component-quality-dashboard`, `state-coverage-matrix`,
  `native-quality-family-tranches`, and `visual-review-index`.
- `release-candidate --skip-private-name-scan` exits successfully and records
  premium downstream native visual parity as satisfied or explicitly attached as
  the final external evidence.
- Manual visual review finds no wrong route, missing control, misleading
  selected/focused/disabled/error/warning/success/loading state, text overlap,
  clipped critical text, password leak, or hidden unsupported diagnostic.
- No threshold is loosened to pass. Any threshold change is a ratchet supported
  by before/after real-reference metrics and manual review.

## Phase Plan Overview

| Phase | Name | Primary blocker removed | Expected ladder movement |
| ---: | --- | --- | --- |
| 0 | Real-reference baseline triage | Evidence and diagnosis reproducibility | Remain L0, but diagnostics become actionable |
| 1 | Route/navigation state fidelity | Wrong or inferred route selection | 3 warnings -> 0 warnings; no pixel claim yet |
| 2 | Viewport, scaling, root layout, and page alignment | Full-frame root/page drift | L0 -> L1 on most routes, login near L2 |
| 3 | Login route parity | Centered auth form and core form controls | `login-light` -> L3/L4 candidate |
| 4 | Status route parity | InfoBar/progress/status state chrome | `status-states-light` -> L3/L4 candidate |
| 5 | Shell/list/detail/messages parity | Shared shell and list/detail geometry | Home/Messages/Admin broad routes -> L2/L3 |
| 6 | Admin/workbench/command-search parity | Command/search/workbench chrome | Admin/workbench/command routes -> L3/L4 |
| 7 | Settings/profile parity | Form card, field, and profile route polish | `settings-profile-light` -> L3/L4 |
| 8 | Threshold ratchet and production gate | Final native comparison and release gate | 8/8 pass L4 minimum; L5 where justified |

## Phase 0: Real-Reference Baseline Triage

Goal: Make the current 8/8 failure reproducible, classified, and ready for
root-cause work without changing renderer behavior.

Files likely involved:

- `tools/winui3-mac-runner-downstream-windows-probe-sweep`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `src/WinUI3.MacRuntime/VisualComparisonReport.cs`
- `src/WinUI3.MacRuntime/NativeReferenceIntegrity.cs`
- `docs/visual-parity/downstream-native-visual-parity-audit.md`
- `docs/visual-parity/downstream-native-visual-parity-audit.json`
- This plan file

Failing tests to write first:

- `DownstreamProbeSweepReadsRealWindowsReferenceAfterPhase8Baseline`
- `DownstreamProbeSweepRollsUpEightScenarioRealReferenceMetrics`
- `DownstreamProbeSweepReportsFullFrameChangedBoundingBox`
- `DownstreamProbeSweepSeparatesSourceCoverageFromNativeComparison`
- `DownstreamProbeSweepKeepsPrivateEvidenceOutOfRepository`

Implementation strategy:

- Add tests that load sanitized copies or test fixtures shaped like the current
  `summary.json` and per-scenario `pixel-diff.json` records.
- Assert the baseline has 8 total, 0 pass, 8 fail, 8/8 Windows screenshots
  matched, reference readiness ready, 3 route-selection warnings, and native
  comparison required and failed.
- Assert each scenario's actual changed-pixel, MAE, RMSE, max-delta, and ladder
  values match this plan's metrics table.
- Do not change rendering, layout, theme, thresholds, or downstream scenario
  behavior in this phase.
- Keep any generated comparison output in `/private/tmp/emsi_qa`.

Public fixture verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase0-real-reference-baseline-triage \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- No required improvement.
- Metrics should reproduce the current L0 baseline within deterministic image
  tolerance.
- Route-selection warnings should remain visible until Phase 1 fixes them.

Commit boundary:

- Commit only tests/docs/diagnostic plumbing.
- Suggested message: `test: capture real windows visual parity baseline`

## Phase 1: Route/Navigation State Fidelity

Goal: Make route state, selected rail state, tree export, accessibility export,
and painted NavigationView selection agree with the real Windows reference or
explicitly flag a downstream reference mismatch before visual comparison can
pass.

Files likely involved:

- `tools/winui3-mac-runner-downstream-windows-probe-sweep`
- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacRuntime/ElementQuery.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/NavigationWorkbenchPage.xaml`
- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Downstream app-owned scenarios only if required: `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/*.json`

Failing tests to write first:

- `NavigationViewItemExportsSelectedStateInTree`
- `NavigationViewItemExportsSelectedStateInAccessibility`
- `DownstreamProbeSweepFailsOnRouteSelectionMismatchWhenNativeComparisonRequired`
- `DownstreamProbeSweepReportsMessagesStatusAndSettingsSelectionWarnings`
- `SkiaV2SnapshotRendererPaintsSelectedNavigationItemFromExplicitState`
- `RouteSelectionAuditReconcilesExpectedAnchorRuntimeTreeAndPaintedState`

Implementation strategy:

- Stop treating selected navigation item as inference-only. Export explicit
  selected state from facade/runtime tree and accessibility artifacts.
- Add a route-selection audit that records expected scenario anchor, runtime
  selected item, painted selected item when available, and Windows reference
  selected anchor when the capture metadata can provide it.
- Fix route selection at the source if the runtime selected item is wrong.
- If the real Windows reference itself is on a different highlighted rail item
  than the scenario body, classify that as a downstream probe/reference setup
  issue and keep native comparison blocked until reconciled.
- Ensure the three current warnings for Messages, Status, and Settings/Profile
  move to zero before Phase 2.
- Do not tune colors, page bounds, or thresholds in this phase.

Public fixture verification:

```sh
dotnet test --filter "NavigationView|RouteSelection|AccessibilityTree|UiTree"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-navigation-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase1-navigation-state
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase1-public-admin-workbench
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase1-route-navigation-state-fidelity \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- Route/selection warnings: 3 -> 0.
- Changed-pixel percentages may remain L0 because geometry is unchanged.
- Manual gate: no route can be promoted while selected state differs from the
  native reference or expected route.

Commit boundary:

- Commit route/selection export, tests, and sweep audit changes before layout.
- Suggested message: `fix: make downstream route selection explicit`

## Phase 2: Viewport, Scaling, Root Layout, And Page Alignment

Goal: Remove full-frame root/client/page drift so controls start from native-like
locations and sizes before route-specific polish.

Files likely involved:

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacCompat/XamlFacade.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs` if new layout properties are required

Failing tests to write first:

- `VisualLayoutEngineUsesNativeClientAreaOriginForProbeViewport`
- `VisualLayoutEngineCentersConstrainedLoginRoutePanel`
- `VisualLayoutEngineUsesNavigationViewContentFrameBoundsFromProbe`
- `VisualLayoutEnginePreservesShellPageInsetsAtNineSixtyBySixForty`
- `VisualLayoutEngineDoesNotStretchNaturalButtonsWhenHorizontalAlignmentIsLeft`
- `PixelDiffBoundingBoxShrinksAfterRootLayoutAlignmentFixture`

Implementation strategy:

- Use the current tree values as RED fixtures: login content starts at `x=32`
  and must move to the native centered column; shell content starts at `x=248`
  and must align to the Windows client/content frame.
- Audit viewport scale, root window background, client area, Frame/Page,
  ScrollViewer, Grid, StackPanel, Border, and NavigationView ownership.
- Fix root alignment and page host bounds before changing any token.
- Normalize natural sizing for buttons and inputs only where WinUI alignment and
  explicit XAML constraints require it.
- Add sanitized route bounds tests from public fixtures; do not test private
  screenshot pixels directly.

Public fixture verification:

```sh
dotnet test --filter "VisualLayoutEngine|ClientArea|Viewport|NaturalButton|PixelDiff"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase2-production-smoke-layout
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-navigation-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase2-navigation-layout
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase2-viewport-root-page-alignment \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- Login changed pixels should fall below L1 (`<= 90%`) and trend toward L2.
- Broad shell routes should show smaller changed bounding boxes or visibly
  aligned content frames.
- MAE/RMSE should not regress materially; if they do, token/background changes
  leaked into the phase and should be backed out or isolated.

Commit boundary:

- Commit root layout, scaling, client-area, and page bounds changes separately.
- Suggested message: `fix: align winui probe viewport and page bounds`

## Phase 3: Login Route Parity

Goal: Make `login-light` a focused form route that can serve as the first
premium route closure: centered column, native TextBox/PasswordBox, CheckBox,
ProgressBar, InfoBar, typography, and natural button sizing.

Files likely involved:

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `src/WinUI3.MacRenderer.Skia/FontResolver.cs`
- `tests/WinUI3.MacRuntime.Tests/FontResolverTests.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/BasicInputPage.xaml`
- `fixtures/ComponentParityLab.WinUI/Pages/TextFormsPage.xaml`

Failing tests to write first:

- `LoginRouteCentersFormColumnAgainstRealReferenceBounds`
- `SkiaV2SnapshotRendererDrawsFocusedTextBoxUnderlineAndClearButton`
- `SkiaV2SnapshotRendererDrawsPasswordBoxWithNativeMaskSpacing`
- `SkiaV2SnapshotRendererDrawsCheckBoxGlyphAndLabelBaselineLikeWinUI`
- `SkiaV2SnapshotRendererUsesNaturalButtonWidthForLoginAction`
- `SkiaV2SnapshotRendererAlignsLoginProgressBarTrackAndFill`
- `SkiaV2SnapshotRendererUsesSegoeBodyAndCaptionBaselinesForLogin`

Implementation strategy:

- Use TDD around public component fixtures first: TextBox, PasswordBox,
  CheckBox, Button, ProgressBar, and InfoBar.
- Then run the real login sweep and compare the route triplet manually.
- Keep all form strings and password evidence safe; never render unmasked
  password text.
- Treat button stretching, field width, focus underline, clear button, mask dot
  spacing, and label baseline as implementation bugs, not threshold issues.
- Keep changes generic to form primitives so Settings/Profile benefits later.

Public fixture verification:

```sh
dotnet test --filter "LoginRoute|TextBox|PasswordBox|CheckBox|ProgressBar|FontResolver|NaturalButton"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase3-basic-input
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-focused-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase3-text-forms-focused
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase3-login-route-parity \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- `login-light`: L0 -> L3 minimum, L4 candidate if changed pixels fall to
  `<= 45%`, MAE `<= 8`, RMSE `<= 28`.
- Other form routes may improve, but do not claim Settings/Profile closure yet.
- No route-selection warning regression.

Commit boundary:

- Commit login/form primitive renderer and tests after public fixtures and the
  real login sweep are reviewed.
- Suggested message: `fix: align login form route with native winui`

## Phase 4: Status Route Parity

Goal: Make `status-states-light` visually faithful for InfoBar severity states,
close affordances, icon treatment, ProgressBar, ProgressRing, and status route
selection.

Files likely involved:

- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/StatusPickersPage.xaml`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Failing tests to write first:

- `SkiaV2SnapshotRendererDrawsInfoBarSeverityFillsFromWinUITokens`
- `SkiaV2SnapshotRendererDrawsInfoBarCloseButtonWhenClosable`
- `SkiaV2SnapshotRendererAlignsInfoBarTitleMessageBaselines`
- `SkiaV2SnapshotRendererDrawsProgressBarTrackAndFillAtNativeThickness`
- `SkiaV2SnapshotRendererDrawsProgressRingAtNativeProbePosition`
- `StatusRouteSelectionMatchesExpectedAnchor`

Implementation strategy:

- Use public status/pickers component scenarios for RED/GREEN before comparing
  private status route screenshots.
- Implement severity fill, foreground, icon, close button, padding, and baseline
  as renderer primitives shared with Admin and Messages InfoBars.
- Fix progress placement and dimensions through layout primitives, not per-route
  pixel offsets.
- Ensure selected route state remains explicit from Phase 1.

Public fixture verification:

```sh
dotnet test --filter "InfoBar|ProgressBar|ProgressRing|StatusRoute|Severity"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase4-status-pickers
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-error-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase4-status-error
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-success-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase4-status-success
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase4-status-route-parity \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- `status-states-light`: L0 -> L3 minimum; L4 if status fill, progress, and
  route selection all align.
- Admin and Messages InfoBar subregions should improve without route-specific
  work.
- Worst MAE should drop below the current `status-states-light` MAE of 9.392804.

Commit boundary:

- Commit status/progress renderer and tests separately.
- Suggested message: `fix: align status info bars and progress chrome`

## Phase 5: Shell/List/Detail/Messages Parity

Goal: Align the shared NavigationView shell, Home read surface, ListView rows,
detail pane boundaries, Messages route, multiline TextBox, and list/detail
density.

Files likely involved:

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/ElementQuery.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/NavigationWorkbenchPage.xaml`
- `fixtures/ComponentParityLab.WinUI/Pages/CollectionsPage.xaml`
- `fixtures/ComponentParityLab.WinUI/Pages/TextFormsPage.xaml`
- `fixtures/ProductionSmoke.WinUI/MainWindow.xaml`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Failing tests to write first:

- `VisualLayoutEngineMatchesNavigationViewPaneWidthFooterAndContentFrame`
- `SkiaV2SnapshotRendererDrawsNavigationViewSelectionLikeWinUI`
- `SkiaV2SnapshotRendererDrawsListViewSelectionWithoutExtraCardChrome`
- `VisualLayoutEngineKeepsDetailPaneFlatBorderAndNativeBounds`
- `SkiaV2SnapshotRendererDrawsMultilineTextBoxWithNativePadding`
- `MessagesRouteUsesMessagesSelectionState`
- `ShellListDetailDoesNotStackRoundedCardsAroundFlatWinUIPanes`

Implementation strategy:

- Treat Home, Messages, Admin dashboard, Workbench, and Settings as consumers of
  one shell/list/detail contract.
- Remove route-level card approximation where the native reference shows a flat
  pane, list, or single bordered detail area.
- Align selected row indicator, fill, row height, content padding, and separator
  behavior in ListView.
- Align NavigationView item icon/text spacing, selection fill, indicator,
  footer location, and pane background.
- For Messages, reuse TextBox/Form primitives from Phase 3 and InfoBar
  primitives from Phase 4.

Public fixture verification:

```sh
dotnet test --filter "NavigationView|ListView|Collections|MultilineTextBox|ShellListDetail|MessagesRoute"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-navigation-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase5-navigation-workbench
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-collections-selected-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase5-collections-selected
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase5-production-smoke-shell
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase5-shell-list-detail-messages \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- `shell-staff-light`, `messages-multiline-light`, and `admin-dashboard-light`
  should move from L0 to L2 or L3.
- `messages-multiline-light` route-selection warning should stay resolved.
- Full-frame changed pixels should decrease because list/detail and pane
  backgrounds no longer differ across whole route regions.

Commit boundary:

- Commit shell/list/detail/messages changes after public navigation, collection,
  and production smoke fixtures pass.
- Suggested message: `fix: align shell list detail and messages routes`

## Phase 6: Admin/Workbench/Command-Search Parity

Goal: Align Admin dashboard, Admin workbench, command search, AutoSuggestBox,
CommandBar, AppBarButton labels, overflow affordance, status surfaces, and
workbench detail panes.

Files likely involved:

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/ElementQuery.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/CommandsMenusPage.xaml`
- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`
- `fixtures/ProductionSmoke.WinUI/MainWindow.xaml`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs` if command properties require compiler/facade support

Failing tests to write first:

- `SkiaV2SnapshotRendererDrawsAutoSuggestBoxFocusedUnderlineClearAndSearchGlyph`
- `VisualLayoutEnginePlacesCommandBarContentAndPrimaryCommandsLikeWinUI`
- `SkiaV2SnapshotRendererDrawsAppBarButtonLabelWhenDefaultLabelPositionIsRight`
- `SkiaV2SnapshotRendererDrawsCommandBarOverflowAffordance`
- `PublicAdminWorkbenchMatchesNativeCommandLayoutBounds`
- `CommandSearchRouteKeepsWorkbenchSelectionAndCommandAuditClean`
- `AdminDashboardUsesSharedInfoBarAndListDetailContracts`

Implementation strategy:

- Build on Phases 4 and 5; do not reintroduce route-specific cards, custom
  status fills, or inferred selection.
- Implement AutoSuggestBox as a TextBox-like search control with native search
  glyph, focused underline, trailing clear affordance, and correct inner
  padding.
- Make CommandBar reserve content-left and command-right regions like WinUI.
- Preserve visible `Refresh` label and overflow affordance where the reference
  shows them.
- Keep accessibility names synchronized with visible command labels.

Public fixture verification:

```sh
dotnet test --filter "CommandBar|AutoSuggestBox|AppBarButton|PublicAdminWorkbench|CommandSearch|AdminDashboard"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase6-commands-menus
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase6-public-admin-workbench
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase6-production-e2e-workbench
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase6-admin-workbench-command-search \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- `admin-dashboard-light`, `admin-workbench-light`, and
  `command-search-light` should reach L3 or L4 candidates.
- Search/command-specific visual diffs should localize to glyph/text edges
  instead of broad command-region geometry.
- Existing Login, Status, and Messages improvements must not regress.

Commit boundary:

- Commit admin/workbench/command-search changes separately.
- Suggested message: `fix: align admin workbench command search parity`

## Phase 7: Settings/Profile Parity

Goal: Close the final form/profile route by aligning settings selection state,
form card bounds, field widths, focused TextBox chrome, clear affordances,
button sizing, typography, and border/surface treatment.

Files likely involved:

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacCompat/ControlsFacade.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/TextFormsPage.xaml`
- `fixtures/ResourceCatalogApp.WinUI/MainWindow.xaml`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Failing tests to write first:

- `SettingsProfileRouteSelectionMatchesExpectedSettingsAnchor`
- `VisualLayoutEngineMatchesSettingsProfileFormBounds`
- `SkiaV2SnapshotRendererUsesFlatSettingsFormBorderWhenNativeReferenceIsFlat`
- `SkiaV2SnapshotRendererDrawsFocusedSettingsTextBoxWithNativeUnderline`
- `SkiaV2SnapshotRendererDrawsTextBoxClearButtonsOnlyWhenNativeStateRequires`
- `VisualLayoutEngineUsesNaturalUpdateProfileButtonWidth`
- `SettingsProfileDoesNotRegressLoginTextFormPrimitives`

Implementation strategy:

- Reuse form primitives from Phase 3 instead of creating settings-only drawing
  paths.
- Reconcile whether the correct production anchor is Settings or Profile; the
  route body says Settings/Profile, but the baseline warns that Profile is
  selected when Settings is expected.
- Match Windows form surface: flatter bordered region, tighter text field
  widths, native focus underline, and natural button width.
- Calibrate any remaining token differences only after geometry and chrome are
  correct.

Public fixture verification:

```sh
dotnet test --filter "SettingsProfile|TextForms|ResourceCatalog|NaturalButton|TextBox"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase7-text-forms
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-focused-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase7-text-forms-focused
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/phase7-resource-catalog-light
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase7-settings-profile-parity \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- `settings-profile-light`: L0 -> L3/L4 candidate.
- Settings/Profile route-selection warning must remain zero.
- Login-focused form primitives must remain at or above their Phase 3 metric
  level.

Commit boundary:

- Commit settings/profile route changes separately.
- Suggested message: `fix: align settings profile route parity`

## Phase 8: Threshold Ratchet And Production Gate

Goal: Convert phase-by-phase improvements into a production gate. Ratchet
thresholds only tighter or more explicit after the real-reference sweep proves
the renderer/layout/state changes. Do not use threshold loosening as a fix.

Files likely involved:

- `tools/winui3-mac-runner-downstream-windows-probe-sweep`
- `src/WinUI3.MacRenderer.Skia/PixelDiff.cs`
- `src/WinUI3.MacRuntime/VisualComparisonReport.cs`
- `src/WinUI3.MacRuntime/NativeReferenceIntegrity.cs`
- `src/WinUI3.MacRunner/ReleaseCandidate.cs`
- `src/WinUI3.MacRuntime/ProductEvidence.cs`
- `src/WinUI3.MacRuntime/ComponentQualityDashboard.cs`
- `src/WinUI3.MacRuntime/NativeQualityFamilyTranches.cs`
- `src/WinUI3.MacRuntime/StateCoverageMatrix.cs`
- `docs/visual-parity/downstream-native-visual-parity-audit.md`
- `docs/visual-parity/downstream-native-visual-parity-audit.json`
- `docs/visual-parity/README.md`
- `docs/release/production-evidence-view.md`
- `docs/release/support-policy.md`
- `docs/release/final-production-gate.md`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Failing tests to write first:

- `DownstreamProbeSweepRequiresNativeComparisonForProductionReadyGate`
- `DownstreamProbeSweepFailsWhenAnyScenarioRemainsBelowL4`
- `DownstreamProbeSweepFailsOnAnyRouteSelectionWarning`
- `DownstreamNativeVisualParityAuditRequiresBeforeAfterMetricsForRatchet`
- `DownstreamNativeVisualParityAuditRejectsThresholdLoosening`
- `ReleaseCandidateKeepsSourceLevelAndNativeVisualParitySeparate`
- `ReleaseCandidateBlocksPremiumParityWithoutRealWindowsReferenceSweep`
- `ComponentQualityDashboardDoesNotPromoteNativeQualityFromPrivateRoutesAlone`

Implementation strategy:

- Define production gate as all eight scenarios passing L4 minimum, with L5
  expected for focused command/status/form routes only when metrics and manual
  review support it.
- Record before/after metrics for every scenario and every ratcheted threshold.
- Fail the production gate on any route-selection warning, missing reference,
  skipped native comparison, JPG pixel evidence, missing PNG artifact, image
  integrity warning, or stale public dashboard.
- Keep release docs explicit that source-level support remains separate from
  premium native visual parity.
- Keep native-quality dashboard rows honest: private downstream route success
  can unblock review, but public component crop evidence and manual inspection
  are still required for `nativeQualityGrade` promotion.

Public fixture verification:

```sh
dotnet test --filter "DownstreamProbeSweep|DownstreamNativeVisualParityAudit|ReleaseCandidate|ProductEvidence|ComponentQualityDashboard|NativeQualityFamilyTranches|StateCoverageMatrix"
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
```

Real-reference downstream sweep command:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/phase8-threshold-ratchet-production-gate \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected metric movement:

- 8/8 scenarios pass L4 minimum.
- Focused routes (`login-light`, `status-states-light`,
  `command-search-light`, `settings-profile-light`) should document whether L5
  is achieved or why L4 is the production gate for this release.
- Worst changed pixels must be below the conservative native comparison
  threshold, not near full-frame.
- Worst MAE/RMSE must remain within the L4 gate.

Commit boundary:

- Commit threshold ratchet, production gate, and docs only after the final gate
  commands pass.
- Suggested message: `test: gate real windows production visual parity`

## Final Production-Ready Gate

Run from `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime`.

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/production-ready-final \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329

PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --skip-private-name-scan --output /private/tmp/emsi_qa/windows/production-ready-release-candidate.json
```

Expected final evidence:

- `/private/tmp/emsi_qa/windows/probe-comparisons/production-ready-final/summary.json`
- `/private/tmp/emsi_qa/windows/probe-comparisons/production-ready-final/summary.md`
- `/private/tmp/emsi_qa/windows/probe-comparisons/production-ready-final/review.html`
- Per-scenario `visual/windows-reference.png`, `visual/mac-runtime.png`,
  `visual/pixel-diff.png`, and `visual/pixel-diff.json` in `/private/tmp/emsi_qa`
- `/private/tmp/emsi_qa/windows/production-ready-release-candidate.json`

## Risks And Mitigations

- Risk: private downstream screenshots encourage one-off renderer hacks.
  Mitigation: each phase requires public clean-room fixture tests before
  downstream sweeps.
- Risk: route selection in the Windows reference and runtime body route disagree.
  Mitigation: Phase 1 blocks promotion until expected route, runtime tree,
  painted selection, and reference metadata are reconciled.
- Risk: full-frame color drift hides layout improvement.
  Mitigation: Phase 2 focuses on bounds first, and Phase 8 records
  before/after metrics instead of relying on a single final value.
- Risk: source-level support claims get confused with visual parity.
  Mitigation: release docs, dashboards, and final gate keep source coverage,
  native comparison, and native-quality promotion separate.
- Risk: threshold ratchet becomes threshold loosening.
  Mitigation: tests reject loosening without evidence, and this plan forbids
  threshold loosening as a fix.
- Risk: public component evidence regresses while private routes improve.
  Mitigation: public fixture verification and dashboard checks are required in
  every implementation phase.
- Risk: screenshots or generated visual evidence get committed to the runtime
  repo.
  Mitigation: output paths stay under `/private/tmp/emsi_qa`, and tests/docs
  should validate PNG evidence boundaries.

## Rollback And Recovery

- Roll back by phase commit boundary.
- If a phase improves private downstream screenshots but regresses public
  component fixtures, revert the phase and redesign the renderer family change.
- If three consecutive fix attempts fail in one family, stop implementation and
  perform a fresh root-cause analysis before trying another patch.
- If route selection remains ambiguous, keep native comparison blocked and fix
  scenario/reference metadata before tuning pixels.
- If any private PNG enters the runtime repo, remove it before commit and rerun
  the private-name and release-candidate checks.

## Execution Prompt

Paste this prompt into a new Codex task to implement the plan:

```text
Execute the plan in `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime/docs/plans/2026-06-08-winui3-mac-real-windows-production-visual-parity-plan.md`.

Use these skills before implementation: `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-native-ui-overlay`, `emsi-workflows:emsi-verification-gate`, `superpowers:systematic-debugging`, `superpowers:test-driven-development`, `google-eng-practices`, and the repo-local `windows-winui3-design` skill at `/Users/marlonjd/Developer/monorepos/emsi_monorepo/.codex/skills/windows-winui3-design/SKILL.md`.

Read first: `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`, runtime `README.md`, `docs/visual-parity/README.md`, `docs/plans/2026-06-08-winui3-mac-premium-native-visual-parity-plan.md`, `docs/visual-parity/downstream-native-visual-parity-audit.md`, `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8/summary.md`, and the per-scenario `pixel-diff.json`, `tree.json`, and visual PNG triplets under `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8`.

Work phase by phase. For every phase, write failing tests first and verify they fail for the intended reason before production code changes. Keep source-level coverage separate from native visual parity. Keep QA screenshots and pixel diffs in `/private/tmp/emsi_qa`, not in the runtime repo. Do not propose or apply threshold loosening as a fix. Prefer route-state, renderer, layout, and page-bound fixes before token polish. Do not create, switch, rename, delete, or otherwise perform branch operations. Do not push. Do not run GitHub workflows.

Use the real Windows references at `/private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329` and compare against the current real baseline at `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-real-windows-reference-after-phase8`.

Run each phase's targeted `dotnet test --filter ...` commands, public fixture verification commands, and real-reference downstream sweep command from the plan. Add `--require-native-comparison` only for gates that are expected to pass. The final production-ready gate must be:

WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/production-ready-final \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329

PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --skip-private-name-scan --output /private/tmp/emsi_qa/windows/production-ready-release-candidate.json

After each successful phase, inspect the task diff and make a task-only commit at the phase commit boundary when the AGENTS.md git guards pass. Do not push unless a later task explicitly requests push and push guards pass.
```
