# WinUI3 Mac Premium Native Visual Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the EMSI WinUI3 Mac runtime from source-level downstream XAML coverage to premium production-grade native visual parity against real Windows WinUI screenshots for the eight downstream probe scenarios.

**Architecture:** Keep source-level coverage, renderer fidelity, native comparison, and native-quality promotion as separate contracts. Implement renderer improvements by family first, prove them against public clean-room component fixtures, then ratchet downstream private Windows probe scenarios with PNG evidence stored outside this runtime repository.

**Tech Stack:** .NET 10, MSTest, clean-room `Microsoft.UI.Xaml` facade types, `WinUI3.MacXaml`, `WinUI3.MacRuntime`, `WinUI3.MacRenderer.Skia`, `winui3-mac-runner`, `skia-v2`, downstream Windows WinUI probe screenshots.

---

Date: 2026-06-08

Owner subtree: `tools/winui3-mac-test-runtime`

Downstream validation boundary: `apps/windows` owns the real Windows app, private probe source, Windows runner validation, and screenshot-like QA evidence. Runtime repo changes should be limited to source, tests, docs, and sanitized manifests. Private PNG evidence stays in `/private/tmp/emsi_qa` or the private QA repository, never in this runtime repo.

## Required Guidance And Skills

Read before executing this plan:

- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`
- `/Users/marlonjd/Developer/monorepos/emsi_monorepo/.codex/skills/windows-winui3-design/SKILL.md`
- `README.md`
- `docs/visual-parity/README.md`
- `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`

Use these skills before execution:

- `emsi-workflows:emsi-task-router`
- `emsi-workflows:emsi-native-ui-overlay`
- `emsi-workflows:emsi-verification-gate`
- `superpowers:systematic-debugging`
- `superpowers:test-driven-development`
- `superpowers:subagent-driven-development` or `superpowers:executing-plans`
- `google-eng-practices`
- `windows-winui3-design` through the repo-local skill file above

Creative Production is not required for this execution because the target visual direction is not a new premium brand route. The approved direction is direct native WinUI parity against real Windows reference PNGs.

## Objective

The runtime has now crossed the source-level downstream XAML coverage boundary, but the current native comparison sweep shows that visual fidelity is not production-grade. The next objective is to make the macOS renderer visually credible against real native Windows WinUI screenshots while preserving the runtime's source-level honesty, unsupported diagnostics, and evidence separation.

Success means:

- All eight downstream native comparisons produce complete artifacts and pass staged visual gates.
- Source-level coverage remains separate from native visual parity.
- Public component fixtures prevent one-off tuning to private downstream screenshots.
- Native-quality labels remain blocked until real Windows references pass, manual inspection confirms no material visual regressions, and thresholds are explicitly justified.

## Current Evidence

Downstream comparison sweep:

```sh
/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-06-downstream-probe-onscreen-client-vs-mac-runtime
```

Windows references:

```sh
/private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Summary:

- 8 total scenarios, 0 passed, 8 failed.
- Artifact completeness: 8 passed.
- Scenario interactions: 8 passed.
- Route anchor warnings: 0.
- Image integrity: 8 passed, 0 missing, 0 warnings.
- Image size: 960x640 for every macOS runtime output, no image-size warnings.
- Font provenance: 8 passed with external Segoe UI Variable and Segoe Fluent Icons.
- Windows references: 8 matched.
- Native comparison: 8 failed conservative pixel thresholds.

Pixel metrics:

| Scenario | Changed pixels | MAE | RMSE | Current threshold failure |
| --- | ---: | ---: | ---: | --- |
| `login-light` | 97.911133% | 7.169520 | 21.312369 | Changed pixels exceeds 45%. |
| `shell-staff-light` | 99.999349% | 7.085251 | 26.113236 | Changed pixels exceeds 45%. |
| `messages-multiline-light` | 99.997721% | 6.792517 | 25.316643 | Changed pixels exceeds 45%. |
| `admin-dashboard-light` | 99.998698% | 8.055549 | 27.656882 | Changed pixels exceeds 45%. |
| `admin-workbench-light` | 99.107585% | 5.898421 | 21.813655 | Changed pixels exceeds 45%. |
| `command-search-light` | 99.107585% | 5.898421 | 21.813655 | Changed pixels exceeds 45%. |
| `status-states-light` | 99.999023% | 7.558422 | 23.599508 | Changed pixels exceeds 45%. |
| `settings-profile-light` | 98.819987% | 5.835193 | 22.975264 | Changed pixels exceeds 45%. |

Interpretation:

- Failures are not due to missing references, missing artifacts, blank output, image-size mismatches, scenario automation failures, or missing external fonts.
- MAE and RMSE are already below the broad current thresholds, which suggests broad lightness and content presence are close enough for coarse comparison.
- Changed-pixel percentage is nearly full-frame because background tint, top-left/page alignment, typography rasterization, control stroke placement, and one-pixel separator differences are global.

## Visual Gap Audit

### Shared Gaps Across Scenarios

| Category | Shared gap | Evidence |
| --- | --- | --- |
| Layout | Mac content tends to start at the top-left or uses larger pane/card extents while Windows content is narrower, centered, or aligned to WinUI page columns. | `login-light`, shell/list/detail routes, settings. |
| Typography | Text is present and usually legible, but vertical rhythm, line baselines, weight, and compact caption/body spacing differ. | All eight scenarios. |
| Control chrome | TextBox, PasswordBox, Button, CheckBox, InfoBar, ProgressBar, ProgressRing, NavigationViewItem, ListView, and AppBarButton use approximate chrome. | All scenarios, strongest in login and status states. |
| Density and spacing | Mac rows, cards, text inputs, and buttons often occupy more vertical or horizontal space than Windows. | Login, messages, admin, settings. |
| Color/theme | Accent and severity colors are present, but selection fills, InfoBar severity backgrounds, disabled tracks, pane backgrounds, and subtle surfaces differ. | Shell routes, admin, status. |
| Borders/elevation | Mac adds rounded card borders around list/detail regions where Windows often uses flatter pane separation or a single detail border. | Shell, messages, admin dashboard, workbench, settings. |
| List/detail rendering | Mac boxes the list/detail panes, uses stronger selected-row blue, and draws separators differently. | Shell, messages, admin dashboard, workbench. |
| Text input rendering | Focus underline, clear button, search glyph placement, password masking density, caret/selection, and natural field width differ. | Login, messages, command/search, settings. |
| Command/search rendering | Mac search lacks the native focused underline and trailing clear/search glyph pattern, and command actions render as icon-only instead of native label/chrome. | Admin workbench and command-search. |
| Status/progress rendering | Mac InfoBars use vertical strips and bordered white surfaces; Windows fills severity surfaces and shows close affordance. Progress placement and sizing differ. | Status states and admin status surfaces. |
| Route/content fidelity | Some route screenshots show different rail selection state between Windows and Mac, especially Admin, Workbench, Status, Profile, and Settings routes. | Admin dashboard, workbench, command-search, status, settings. |

### Scenario-Specific Audit

| Scenario | Main visual gaps | Route-specific priority |
| --- | --- | --- |
| `login-light` | Login stack anchored top-left on Mac instead of centered narrow WinUI column; input fields and button are too wide; PasswordBox mask density differs; TextBox focus underline/clear affordance differs; progress bar width differs. | First closure target because it is the smallest route and isolates typography, form control chrome, density, and centering. |
| `shell-staff-light` | NavigationView selected state, icon weight, pane background, list/detail carding, selected row fill, detail border, progress bar width, and page alignment differ. | Shared shell/list/detail baseline after login. |
| `messages-multiline-light` | Conversation list and detail panes are boxed and wider; selected row chrome differs; multiline TextBox height, inner padding, bottom alignment, and send button width differ; InfoBar inside detail uses Mac strip chrome. | TextBox/list/detail family gate. |
| `admin-dashboard-light` | Native success InfoBar is a filled green band while Mac uses bordered strip chrome; list/detail carding and selected row differ; action buttons are placed similarly but density and widths differ; rail selection state differs. | Admin summary with status and list/detail overlap. |
| `admin-workbench-light` | Search field focus, clear/search glyphs, command label/icon behavior, list/detail cards, status InfoBar severity fill, and rail selection differ. | Command/search and workbench gate. |
| `command-search-light` | Same visual frame as workbench; Mac renders search and commands with simplified chrome and icon-only actions. | Dedicated command/search regression scenario. |
| `status-states-light` | InfoBar severity surfaces, close button, icon fill, title/message baselines, progress bar width, and ring placement differ. | Status/progress family gate. |
| `settings-profile-light` | Settings form card radius/stroke differs; input widths, focus underline, clear icon, field vertical rhythm, and button natural width differ; rail selection state differs. | Final form/settings polish gate after core controls. |

## Premium Production Visual Quality Gates

Source-level coverage must remain separate from native visual parity:

- Source-level coverage means XAML compiles, facade semantics exist, interactions pass, tree/accessibility artifacts are complete, and unsupported surfaces remain cataloged.
- Native visual parity means macOS PNG output is compared against real native Windows PNG references, meets staged thresholds, and passes manual visual inspection.
- Native-quality promotion means parity evidence, state coverage, component crop evidence, public clean-room fixtures, and manual inspections all agree. It is not implied by source-level support.

### Always-On Invariants

- `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, and `pixel-diff.json` exist for every required scenario.
- Runtime output dimensions match the Windows reference dimensions. Current target is 960x640.
- `visual-run.json` records renderer, viewport, scale, theme, strict-visual status, and font diagnostics.
- `tree.json`, `accessibility.json`, `interactions.json`, and `run.json` exist for every scenario.
- Scenario interactions pass with zero route-anchor warnings.
- Runtime images are nonblank. Current low-content warning threshold remains 2%.
- External font diagnostics remain explicit. Windows fonts stay outside the repository.
- PNG remains the evidence format. Do not replace reference, runtime, or diff inputs with JPG.
- Evidence screenshots stay in `/private/tmp/emsi_qa` or the private QA repo, not in this runtime repo.

### Threshold Ladder

Do not jump directly to perfect parity. Ratchet thresholds only after visual changes are reviewed and public component fixtures still pass.

| Ladder | Purpose | Whole-image changed pixels | MAE | RMSE | Manual gate |
| --- | --- | ---: | ---: | ---: | --- |
| L0 Baseline | Current failing evidence is complete and reviewable. | Document actual values. | Document actual values. | Document actual values. | Audit triplets and categorize gaps. |
| L1 Coarse route alignment | Remove top-left/full-width/page-frame drift. | <= 90% | <= 12 | <= 36 | Content appears in the same route regions; no missing major surface. |
| L2 Layout and density parity | Align page columns, list/detail widths, row heights, and form natural sizing. | <= 70% | <= 10 | <= 32 | Major controls occupy comparable bounds. |
| L3 Control-family parity | Match core chrome for forms, buttons, list rows, status, progress, and navigation. | <= 55% | <= 8 | <= 28 | Native-visible chrome differences are minor, not structural. |
| L4 Conservative native-comparison pass | Pass the existing downstream comparison contract. | <= 45% | <= 8 | <= 28 | All eight scenarios pass `--require-native-comparison`. |
| L5 Premium production promotion | Promote only after public component crops and private route screenshots agree. | <= 35% for app routes; <= 24% for focused command/status/form routes when achievable. | <= 6.5 broad; <= 5.5 focused. | <= 24 broad; <= 20 focused. | Manual review signs off no route/content mismatch, no missing affordance, no misleading status, no text overlap. |

`maxChannelDelta` should remain diagnostic at 255 for now. One high-contrast glyph edge can hit 255 even when the route is visually close.

### Manual Inspection Criteria

A scenario cannot be promoted even if metrics pass when any of these are true:

- Wrong route content or wrong selected navigation state versus native reference.
- Missing user-visible control, status, command, close affordance, glyph, or label.
- Text overlap, clipped critical text, leaked password content, or unreadable line height.
- Control chrome communicates a different state than Windows, especially selected, focused, disabled, error, warning, success, loading, or checked.
- Screenshot was generated without real Windows PNG provenance or with non-PNG replacement evidence.
- Improvement comes only from loosening thresholds or hiding unsupported diagnostics.

## Scenario Priority Order

1. `login-light`
2. `status-states-light`
3. `messages-multiline-light`
4. `shell-staff-light`
5. `admin-dashboard-light`
6. `admin-workbench-light`
7. `command-search-light`
8. `settings-profile-light`

Rationale:

- `login-light` isolates centering, typography, TextBox, PasswordBox, CheckBox, Button, and ProgressBar without shell complexity.
- `status-states-light` isolates InfoBar and progress rendering, which are visually obvious and shared with admin/workbench.
- `messages-multiline-light` combines multiline TextBox, list/detail, and inner InfoBar.
- `shell-staff-light` locks the reusable NavigationView and read-surface baseline.
- Admin/workbench/search then build on the shell baseline.
- `settings-profile-light` is best as a final form polish scenario because it exercises focused inputs and natural field/button widths after the core control work lands.

## Affected Files And Responsibilities

### Measurement, Sweep, And Evidence

- `tools/winui3-mac-runner-downstream-windows-probe-sweep`: downstream probe sweep, required scenario list, artifact completeness, font provenance, image integrity, native comparison requirement, summary rollup, review page.
- `src/WinUI3.MacRunner/Program.cs`: runner commands for dashboards, visual comparison, product evidence, and future audit helpers.
- `src/WinUI3.MacRuntime/VisualComparisonReport.cs`: reusable comparison reporting if scenario-level aggregate reporting moves from shell script into source.
- `src/WinUI3.MacRuntime/NativeReferenceIntegrity.cs`: native reference sanity checks.
- `src/WinUI3.MacRenderer.Skia/PixelDiff.cs`: pixel metric calculation and threshold status.
- `src/WinUI3.MacRenderer.Skia/RuntimeImageIntegrityAnalyzer.cs`: nonblank and content-density checks.
- `docs/visual-parity/README.md`: evidence boundary and parity process.
- Future sanitized manifest: `docs/visual-parity/downstream-native-visual-parity-audit.json`.
- Future sanitized report: `docs/visual-parity/downstream-native-visual-parity-audit.md`.

### Layout And Tree Semantics

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`: page layout, NavigationView, Grid, StackPanel, Border, ListView, CommandBar, AutoSuggestBox, natural sizes, row/column sizing, padding, layout constraints.
- `src/WinUI3.MacRuntime/UiTree.cs`: deterministic visual tree properties exported from facade controls.
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`: state and role export for controls whose visual parity changes.
- `src/WinUI3.MacRuntime/ElementQuery.cs`: child traversal for command content, list items, and template-generated content.
- `src/WinUI3.MacCompat/XamlFacade.cs`: framework-level layout properties when source semantics need expansion.
- `src/WinUI3.MacCompat/ControlsFacade.cs`: control state properties for TextBox, PasswordBox, ListView, InfoBar, CommandBar, AutoSuggestBox, NavigationView, and progress controls.

### Renderer And Visual System

- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`: main painters for NavigationView, ListView, TextBox, PasswordBox, AutoSuggestBox, Button, CheckBox, InfoBar, ProgressBar, ProgressRing, CommandBar, AppBarButton, Border, and page backgrounds.
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`: reusable Fluent chrome primitives for buttons, inputs, selection, checkbox/radio glyphs, status surfaces, and focus/underline strokes.
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`: light, dark, high-contrast, severity, stroke, surface, typography, and radius tokens.
- `src/WinUI3.MacRenderer.Skia/FontResolver.cs`: Segoe text and symbol font provenance and fallback diagnostics.
- `src/WinUI3.MacRenderer.Skia/ComponentCropper.cs`: component crop evidence for public fixtures.

### Public Fixtures And Gates

- `fixtures/ComponentParityLab.WinUI/Pages/BasicInputPage.xaml`: Button, CheckBox, and related input control fixtures.
- `fixtures/ComponentParityLab.WinUI/Pages/TextFormsPage.xaml`: TextBox, PasswordBox, form states.
- `fixtures/ComponentParityLab.WinUI/Pages/StatusPickersPage.xaml`: InfoBar, ProgressBar, ProgressRing.
- `fixtures/ComponentParityLab.WinUI/Pages/CollectionsPage.xaml`: ListView and item template states.
- `fixtures/ComponentParityLab.WinUI/Pages/CommandsMenusPage.xaml`: CommandBar, AppBarButton, AutoSuggestBox or command/search patterns.
- `fixtures/ComponentParityLab.WinUI/Pages/NavigationWorkbenchPage.xaml`: NavigationView, shell, list/detail.
- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`: public admin workbench scaffold for clean-room route-like validation.
- `fixtures/ProductionSmoke.WinUI/MainWindow.xaml`: route-level production smoke fixture.
- `docs/visual-parity/component-quality-dashboard.json`: public component grade rollup.
- `docs/visual-parity/state-coverage-matrix.json`: state coverage matrix.
- `docs/visual-parity/native-quality-family-tranches.json`: native-quality family queues.
- `docs/visual-parity/public-visual-review-index.json` and `.html`: public review index.

### Tests

- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`: layout, renderer, evidence, dashboard, sweep-script, component crop, parity audit, and product gate tests.
- `tests/WinUI3.MacRuntime.Tests/FontResolverTests.cs`: font selection and provenance.
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`: source XAML support tests when renderer work requires new facade/compiler properties.

## Implementation Phases

### Phase 0: Measurement Baseline And Visual Audit Artifacts

**User-visible goal:** Make the current 8/8 native comparison failure auditable, reproducible, and reviewable without private screenshots in the runtime repo.

**Files likely involved:**

- Create: `docs/visual-parity/downstream-native-visual-parity-audit.json`
- Create: `docs/visual-parity/downstream-native-visual-parity-audit.md`
- Modify: `tools/winui3-mac-runner-downstream-windows-probe-sweep`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Possibly modify: `src/WinUI3.MacRuntime/VisualComparisonReport.cs`

**Failing tests to write first:**

- `DownstreamNativeVisualParityAuditTracksEightScenarioBaseline`
- `DownstreamProbeSweepPublishesNativeComparisonMetricRollup`
- `DownstreamProbeSweepKeepsEvidencePngOnly`
- `DownstreamProbeSweepReportsRouteContentAndSelectionWarnings`

**Implementation approach:**

- Parse each scenario's `pixel-diff.json` into a sanitized rollup containing scenario name, dimensions, changed-pixel percentage, MAE, RMSE, threshold status, artifact status, font provenance status, and image-integrity status.
- Add route/content fields that can be populated from `tree.json` and scenario metadata without copying private text beyond sanitized scenario names.
- Record the shared and route-specific gap categories from this plan in the audit manifest.
- Make the sweep summary link every triplet but keep the actual PNGs in `/private/tmp/emsi_qa`.

**Unsupported/non-goal surfaces:**

- No renderer changes.
- No threshold loosening.
- No native-quality promotion.
- No screenshot copies into `docs/visual-parity/examples` unless the evidence is public and sanitized.

**Verification commands:**

```sh
dotnet test --filter "DownstreamNativeVisualParityAudit|DownstreamProbeSweep"
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

**Expected QA evidence outputs:**

- `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline/summary.json`
- `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline/summary.md`
- `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-downstream-native-parity-baseline/review.html`
- Per-scenario `visual/windows-reference.png`, `visual/mac-runtime.png`, `visual/pixel-diff.png`, and `visual/pixel-diff.json`

**Commit boundary:** Commit only the sanitized audit source, docs, and tests. Suggested message: `test: add downstream native visual parity audit baseline`.

### Phase 1: Typography, Font Metrics, And Text Rendering Parity

**User-visible goal:** Text baselines, weights, line heights, input text positioning, and icon glyph metrics feel native before layout and control chrome are tuned.

**Files likely involved:**

- Modify: `src/WinUI3.MacRenderer.Skia/FontResolver.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Modify: `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- Modify: `tests/WinUI3.MacRuntime.Tests/FontResolverTests.cs`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

**Failing tests to write first:**

- `SkiaV2SnapshotRendererUsesWinUITextMetricsForBodyCaptionAndTitle`
- `SkiaV2SnapshotRendererHonorsSemiBoldInfoBarAndPaneLabels`
- `VisualLayoutEngineUsesMeasuredSegoeTextWidthForNaturalControlSizing`
- `FontResolverKeepsExternalFontProvenanceOutOfRepository`

**Implementation approach:**

- Use `SKFont.Metrics` and `SKFont.MeasureText` for natural text sizes instead of broad string-length estimates where deterministic enough.
- Normalize body, caption, title, icon, and semibold weights to the closest available Segoe UI Variable faces or stable Skia weight settings.
- Align TextBlock and control-label baselines to WinUI-like vertical centers.
- Keep font diagnostics in `visual-run.json`; never copy font files.

**Unsupported/non-goal surfaces:**

- No text selection rendering yet.
- No full DirectWrite-compatible shaping guarantee.
- No checked-in Windows font binaries.

**Verification commands:**

```sh
dotnet test --filter "FontResolver|TextMetrics|SemiBold|NaturalControlSizing"
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-text-metrics
```

**Expected QA evidence outputs:**

- Public clean-room text/forms visual run under `/private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-text-metrics`
- Updated downstream sweep showing lower MAE/RMSE or a documented unchanged result if layout remains the dominant failure

**Commit boundary:** Commit typography and font metric changes separately from layout. Suggested message: `fix: align skia v2 text metrics with WinUI references`.

### Phase 2: Layout Engine Density, Alignment, Padding, And Sizing

**User-visible goal:** Major route regions appear in the same places and sizes as native WinUI, starting with `login-light` and then list/detail shells.

**Files likely involved:**

- Modify: `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- Modify: `src/WinUI3.MacRuntime/UiTree.cs`
- Modify: `src/WinUI3.MacCompat/XamlFacade.cs`
- Modify: `src/WinUI3.MacCompat/ControlsFacade.cs`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Possibly modify: `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`

**Failing tests to write first:**

- `VisualLayoutEngineCentersConstrainedLoginPanel`
- `VisualLayoutEngineUsesNaturalButtonWidthInsteadOfStretchWhenAlignmentIsLeft`
- `VisualLayoutEnginePreservesNativeListDetailColumnWidths`
- `VisualLayoutEngineDoesNotAddCardBoundsToFlatListRegions`
- `VisualLayoutEngineMatchesFocusedTextBoxBoundsFromDownstreamProbe`

**Implementation approach:**

- Trace current layout from `tree.json` and compare bounds with the Windows reference triplets before changing code.
- Tighten `Grid`, `StackPanel`, `Border`, and `ContentControl` alignment behavior so `MaxWidth`, `HorizontalAlignment`, padding, and auto/star sizing produce WinUI-like route geometry.
- Use scenario-level layout assertions for the login form, shell content frame, list column, detail pane, and settings form.
- Keep tests based on clean-room fixtures and sanitized bounds, not private screenshot pixels.

**Unsupported/non-goal surfaces:**

- No full WinUI layout engine.
- No virtualization.
- No Mica/Acrylic composition.
- No arbitrary XAML layout guarantee outside cataloged/facade-supported surfaces.

**Verification commands:**

```sh
dotnet test --filter "VisualLayoutEngine|DownstreamNativeVisualParityAudit"
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-layout-ratchet \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

**Expected QA evidence outputs:**

- `login-light` should approach L1, then L2 if the centered panel and natural widths are correct.
- Shell/list/detail routes should show lower full-frame changed-pixel percentages from region alignment.

**Commit boundary:** Commit layout behavior and tests before control chrome work. Suggested message: `fix: align downstream probe layout density with WinUI`.

### Phase 3: Core Control Chrome Parity

**User-visible goal:** Core controls look native enough that users recognize focused, selected, checked, loading, warning, error, success, and default states.

**Files likely involved:**

- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- Modify: `src/WinUI3.MacRuntime/UiTree.cs`
- Modify: `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- Modify: `src/WinUI3.MacCompat/ControlsFacade.cs`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/BasicInputPage.xaml`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/TextFormsPage.xaml`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/StatusPickersPage.xaml`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

**Failing tests to write first:**

- `SkiaV2SnapshotRendererDrawsFocusedTextBoxUnderlineAndClearButton`
- `SkiaV2SnapshotRendererDrawsPasswordBoxWithNativeMaskSpacing`
- `SkiaV2SnapshotRendererUsesNaturalButtonChromeForDefaultAndPrimary`
- `SkiaV2SnapshotRendererDrawsNativeCheckBoxGlyphAndLabelBaseline`
- `SkiaV2SnapshotRendererDrawsSeverityFilledInfoBarsWithCloseButton`
- `SkiaV2SnapshotRendererAlignsProgressBarAndProgressRingToNativeProbe`

**Implementation approach:**

- Update drawing primitives before per-control painters when the primitive is shared by Button, ToggleButton, TextBox, PasswordBox, and selection rows.
- Match WinUI control heights and corner radii from public component crops before using downstream screenshots as confirmation.
- For InfoBar, implement filled severity background, icon circle/glyph, title/message baseline, close button when `IsClosable` is true, and severity-specific foreground colors.
- For TextBox and AutoSuggestBox, implement focused underline, trailing clear affordance when text is present, search glyph placement, and multiline inner padding.
- For ProgressBar and ProgressRing, align track thickness, radius, and placement to native references while preserving deterministic static rendering.

**Unsupported/non-goal surfaces:**

- No animated indeterminate progress.
- No real secure password storage.
- No full pointer hover/pressed matrix until state scenarios are added.
- No full ControlTemplate parity.

**Verification commands:**

```sh
dotnet test --filter "SkiaV2SnapshotRenderer|FluentControlChrome|InfoBar|Progress|TextBox|PasswordBox|CheckBox"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-core-basic-input
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-core-status
```

**Expected QA evidence outputs:**

- Public component triptychs for basic input, text/forms, and status/pickers.
- Downstream `login-light` and `status-states-light` should reach at least L2, then L3 before later phases.

**Commit boundary:** Commit core control chrome separately from shell/list/detail. Suggested message: `fix: improve native chrome for core WinUI controls`.

### Phase 4: Shell, NavigationView, List, And Detail Parity

**User-visible goal:** Shell routes feel like a native Windows desktop layout rather than a macOS card approximation.

**Files likely involved:**

- Modify: `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- Modify: `src/WinUI3.MacRuntime/ElementQuery.cs`
- Modify: `src/WinUI3.MacCompat/ControlsFacade.cs`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/NavigationWorkbenchPage.xaml`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/CollectionsPage.xaml`
- Modify: `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

**Failing tests to write first:**

- `VisualLayoutEngineMatchesNavigationViewPaneWidthAndFooterAnchor`
- `SkiaV2SnapshotRendererDrawsNavigationViewSelectionLikeWinUI`
- `SkiaV2SnapshotRendererDrawsListViewSelectionWithoutExtraCardChrome`
- `VisualLayoutEngineKeepsDetailPaneBorderFlatAndAligned`
- `DownstreamRouteSelectionAuditMatchesWindowsReferenceState`

**Implementation approach:**

- Treat `NavigationView` selected item, pane width, menu item height, footer placement, and icon alignment as a reusable shell contract.
- Rework ListView painter and layout so selected rows match native background, indicator, separators, and content padding.
- Separate route content rectangles from decorative cards. Only draw card borders where the native reference has them.
- Add a route-selection audit so Admin/Workbench/Status/Profile selection differences are intentional and documented, or fixed to match the Windows reference.

**Unsupported/non-goal surfaces:**

- No full adaptive/compact NavigationView behavior in this phase.
- No virtualization or complex DataTemplate parity.
- No keyboard navigation overhaul unless required by state tests.

**Verification commands:**

```sh
dotnet test --filter "NavigationView|ListView|RouteSelection|VisualLayoutEngine"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-navigation-workbench-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-shell-list-detail
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-shell-list-detail \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

**Expected QA evidence outputs:**

- `messages-multiline-light`, `shell-staff-light`, `admin-dashboard-light`, and `settings-profile-light` should show lower changed-pixel percentages from pane/list/detail alignment.
- Review triplets should show fewer boxed regions where native WinUI is flat.

**Commit boundary:** Commit shell/list/detail changes after public navigation and collection fixtures pass. Suggested message: `fix: align shell list detail rendering with native WinUI`.

### Phase 5: Command, Search, And Admin Workbench Parity

**User-visible goal:** Command/search surfaces match native WinUI command affordances, focused search fields, and workbench action placement.

**Files likely involved:**

- Modify: `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- Modify: `src/WinUI3.MacRuntime/UiTree.cs`
- Modify: `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- Modify: `src/WinUI3.MacCompat/ControlsFacade.cs`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/CommandsMenusPage.xaml`
- Modify: `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Possibly modify: `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`

**Failing tests to write first:**

- `SkiaV2SnapshotRendererDrawsAutoSuggestBoxFocusedUnderlineClearAndQueryIcon`
- `VisualLayoutEnginePlacesCommandBarContentAndPrimaryCommandsLikeWinUI`
- `SkiaV2SnapshotRendererDrawsAppBarButtonLabelWhenDefaultLabelPositionIsRight`
- `SkiaV2SnapshotRendererPreservesRefreshTextCommandInWorkbench`
- `CommandSearchScenarioPassesRouteSelectionAndCommandAudit`

**Implementation approach:**

- Model the downstream search field as `AutoSuggestBox`/TextBox-like command content with native focused underline, clear button, search glyph, and inner padding.
- Update `CommandBar` layout to preserve content-left and command-right behavior with correct reserved overflow spacing.
- Render `AppBarButton` labels, compact icon-only states, and overflow affordance according to `DefaultLabelPosition`.
- Keep command accessibility names and visible labels in sync.

**Unsupported/non-goal surfaces:**

- No real search suggestions popup unless already supported by source fixtures.
- No animated command overflow.
- No full menu/flyout keyboarding overhaul.

**Verification commands:**

```sh
dotnet test --filter "CommandBar|AutoSuggestBox|AppBarButton|CommandSearch"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-command-search
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-command-search \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

**Expected QA evidence outputs:**

- `admin-workbench-light` and `command-search-light` should reach L3 or better.
- Public command/menu component evidence should still show honest known gaps for popup/overflow states.

**Commit boundary:** Commit command/search changes separately. Suggested message: `fix: match WinUI command and search chrome`.

### Phase 6: Theme, Color, Elevation, And Border Polish

**User-visible goal:** The renderer's light theme, severity colors, strokes, surfaces, focus accents, and borders match Windows closely enough that one-pixel differences stop dominating the whole frame.

**Files likely involved:**

- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Modify: `fixtures/ResourceCatalogApp.WinUI/MainWindow.xaml`
- Modify: `fixtures/ComponentParityLab.WinUI/Pages/LayoutMediaPage.xaml`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

**Failing tests to write first:**

- `SkiaV2ThemeMatchesWinUILightSurfaceAndSubtleStrokeTokens`
- `SkiaV2ThemeProvidesSeverityFillAndForegroundTokens`
- `SkiaV2SnapshotRendererUsesFlatPaneBordersWhereNativeReferenceIsFlat`
- `SkiaV2SnapshotRendererDoesNotStackCardChromeAroundListDetailPanes`
- `ResourceCatalogKeepsLightDarkAndHighContrastTokenCoverage`

**Implementation approach:**

- Compare sampled native light-theme colors from the PNG triplets and public component references.
- Calibrate `AppBackground`, `PaneBackground`, `Surface`, `SubtleSurface`, `Stroke`, `SubtleStroke`, `Accent`, `AccentSoft`, and severity fill colors.
- Tune corner radii and stroke widths so TextBox, Button, InfoBar, ListView, detail panes, and settings cards align with native WinUI.
- Keep dark and high-contrast paths explicit; if a light-theme tune would weaken those modes, add theme-specific tokens rather than hidden conditionals in painters.

**Unsupported/non-goal surfaces:**

- No Mica/Acrylic or compositor blur.
- No shadow/elevation claim beyond static Skia approximation.
- No high-contrast promotion without high-contrast reference evidence.

**Verification commands:**

```sh
dotnet test --filter "SkiaV2Theme|ResourceCatalog|Surface|Stroke|Severity"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-light.json \
  --strict-visual \
  --output /private/tmp/emsi_qa/windows/runtime-public-evidence/2026-06-08-theme-light
```

**Expected QA evidence outputs:**

- Full-frame changed-pixel percentages should drop after global surface/stroke changes.
- MAE/RMSE should remain at or below L3 and move toward L5.

**Commit boundary:** Commit token/polish changes after checking public light/dark/high-contrast tests. Suggested message: `fix: calibrate skia v2 WinUI theme tokens`.

### Phase 7: Scenario-By-Scenario Threshold Ratchet

**User-visible goal:** Move from "looks better" to explicit per-scenario gates, with each threshold change justified by evidence.

**Files likely involved:**

- Modify: `tools/winui3-mac-runner-downstream-windows-probe-sweep`
- Modify: `docs/visual-parity/downstream-native-visual-parity-audit.json`
- Modify: `docs/visual-parity/downstream-native-visual-parity-audit.md`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Potential downstream app-owned changes only when executing cross-surface work: `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/*.json`

**Failing tests to write first:**

- `DownstreamNativeVisualParityAuditRequiresThresholdReasonForEveryRatchet`
- `DownstreamProbeSweepCanRequireNativeComparisonByScenario`
- `DownstreamProbeSweepFailsWhenReferenceDimensionsDrift`
- `DownstreamProbeSweepFailsWhenScenarioThresholdIsLoosenedWithoutReason`

**Implementation approach:**

- Ratchet one scenario at a time in the priority order.
- For each scenario, record baseline metrics, new metrics, new threshold, reason, evidence path, and whether the threshold is broad route, focused form, command, or status.
- Keep existing source-level docs unchanged unless a real support boundary changes.
- If downstream scenario JSON thresholds must change, do that as app-owned cross-surface work with private evidence references. The runtime repo should keep only sanitized threshold-policy manifests.

**Unsupported/non-goal surfaces:**

- No hidden global threshold override.
- No loosening thresholds to make failures pass.
- No native-quality claim based only on one downstream route.

**Verification commands:**

```sh
dotnet test --filter "DownstreamNativeVisualParityAudit|Threshold|NativeComparison"
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-threshold-ratchet \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

**Expected QA evidence outputs:**

- One dated sweep per ratcheted scenario group under `/private/tmp/emsi_qa/windows/probe-comparisons/`.
- `summary.json` shows which scenarios pass which ladder level.
- No screenshots or diff PNGs committed to the runtime repo.

**Commit boundary:** Commit threshold policy after metrics prove it and tests enforce rationale. Suggested message: `test: ratchet downstream native visual parity thresholds`.

### Phase 8: Release Gate Integration And Native-Quality Promotion Rules

**User-visible goal:** Make premium native visual parity a release-quality gate without confusing it with source-level coverage or public component readiness.

**Files likely involved:**

- Modify: `src/WinUI3.MacRunner/ReleaseCandidate.cs`
- Modify: `src/WinUI3.MacRuntime/ProductEvidence.cs`
- Modify: `src/WinUI3.MacRuntime/ComponentQualityDashboard.cs`
- Modify: `src/WinUI3.MacRuntime/NativeQualityFamilyTranches.cs`
- Modify: `src/WinUI3.MacRuntime/StateCoverageMatrix.cs`
- Modify: `docs/release/production-evidence-view.md`
- Modify: `docs/release/support-policy.md`
- Modify: `docs/release/final-production-gate.md`
- Modify: `docs/visual-parity/README.md`
- Modify: `docs/visual-parity/component-quality-dashboard.json`
- Modify: `docs/visual-parity/native-quality-family-tranches.json`
- Modify: `docs/visual-parity/state-coverage-matrix.json`
- Modify: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

**Failing tests to write first:**

- `ReleaseCandidateKeepsSourceLevelAndNativeVisualParitySeparate`
- `ReleaseCandidateBlocksNativeQualityPromotionWithoutRealWindowsReferences`
- `ProductEvidenceRequiresDownstreamNativeComparisonWhenPremiumGateEnabled`
- `NativeQualityFamilyTranchesDoNotPromoteDefaultOnlyOrUnreviewedRows`
- `ComponentQualityDashboardCarriesNativeParityBlockersWithoutChangingHarnessGrade`

**Implementation approach:**

- Add a premium-native-visual-parity gate that is opt-in until the threshold ladder reaches L4 for all eight scenarios.
- Require real Windows reference PNG provenance, not synthetic smoke or JPG review images.
- Require public clean-room component evidence to remain current before private downstream screenshots can promote a family.
- Update release docs to state exactly which parity claims are source-level, route-level, component-level, and native-quality.
- Keep `nativeQualityGrade` as `not-evaluated` unless manual inspection and component crop evidence justify promotion.

**Unsupported/non-goal surfaces:**

- No arbitrary WinUI 3 compatibility claim.
- No Windows binary, `.exe`, `.msix`, Wine, WebView2, Mica/Acrylic, or compositor parity claim.
- No release gate that depends on private screenshots in a public repository checkout.

**Verification commands:**

```sh
dotnet test --filter "ReleaseCandidate|ProductEvidence|ComponentQualityDashboard|NativeQualityFamilyTranches|StateCoverageMatrix"
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

**Expected QA evidence outputs:**

- Release docs clearly distinguish source-level readiness from premium native visual parity.
- Premium gate remains blocked until all eight real-reference comparisons pass the configured ladder.
- Public dashboards remain honest about `usable`, `not-rendered`, and `nativeQualityGrade`.

**Commit boundary:** Commit release gate wiring only after all dashboard checks and release-candidate checks pass. Suggested message: `test: gate native visual parity promotion on real references`.

## Risk Controls

- Avoid tuning only to one private screenshot. Every renderer change must first have a public clean-room component or route fixture test.
- Preserve source-level unsupported diagnostics. Renderer improvements must not silently mark unsupported WinUI surfaces as supported.
- Do not claim native-quality promotion until real Windows references pass the configured gates.
- Keep threshold changes explicit, staged, and justified by before/after metrics.
- Keep screenshots, pixel diffs, and manual visual QA evidence out of the runtime repository.
- Keep PNG as the source evidence format.
- Do not commit `.DS_Store`, `shadow-build`, `bin`, `obj`, or generated build outputs.
- Do not run GitHub workflows on non-public repos.
- Do not create, switch, rename, delete, or otherwise perform branch operations.
- If route selection differs between native Windows and Mac, treat it as a product/state fidelity issue until proven to be an intentional downstream probe difference.
- If a renderer fix improves private downstream screenshots but regresses public component crops, stop and adjust the renderer family behavior instead of special-casing the downstream route.
- If three consecutive fix attempts fail for the same family, stop and revisit the renderer architecture for that family before adding more patchy conditions.

## Dependencies And Ownership Boundaries

- Runtime owner: source-level facade/runtime/renderer/evidence/test/docs changes inside `tools/winui3-mac-test-runtime`.
- Windows app owner: downstream probe scenario source, real app route behavior, private Windows screenshot capture, and private QA evidence.
- Public runtime fixtures must remain clean-room and generic.
- Windows references must remain real native WinUI PNGs with provenance.
- Licensed Windows fonts may be used only from repo-external local paths for diagnostics and parity runs.

## Rollback And Recovery

- Revert by phase, not by broad renderer rollback. Each phase has a separate commit boundary.
- If visual drift worsens, keep the failing triplet and audit metrics in `/private/tmp/emsi_qa`, then restore the previous renderer behavior for that family.
- If threshold ratchets are too strict, lower the target ladder for the scenario only with an explicit reason and leave native-quality promotion blocked.
- If public component evidence regresses, block downstream promotion until public fixtures pass again.
- If private evidence accidentally enters the runtime repo, remove it before commit and run the private-name scan before completing the task.

## Final Verification Matrix

Use this complete gate before declaring premium native visual parity ready:

```sh
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --require-native-comparison \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/2026-06-08-premium-native-visual-parity-final \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329
```

Expected final evidence:

- 8/8 downstream native comparisons pass the configured threshold ladder.
- No image integrity warnings.
- No image-size warnings.
- No route-anchor warnings.
- 8/8 interaction summaries pass.
- 8/8 artifact summaries pass.
- 8/8 font provenance summaries pass.
- Public component dashboards remain current and honest.
- Manual inspection records no blocking route, control, text, status, or command gap.

## Execution Prompt

Paste this prompt into a new Codex task to execute the plan:

```text
Execute the plan in `docs/plans/2026-06-08-winui3-mac-premium-native-visual-parity-plan.md` from `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime`.

Use these skills before execution: `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-native-ui-overlay`, `emsi-workflows:emsi-verification-gate`, `superpowers:systematic-debugging`, `superpowers:test-driven-development`, `superpowers:subagent-driven-development` or `superpowers:executing-plans`, `google-eng-practices`, and the repo-local `windows-winui3-design` skill at `/Users/marlonjd/Developer/monorepos/emsi_monorepo/.codex/skills/windows-winui3-design/SKILL.md`.

Read first: `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`, runtime `README.md`, `docs/visual-parity/README.md`, and `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`.

Work phase by phase, writing failing tests before implementation for each phase. Keep source-level coverage separate from native visual parity. Do not create, switch, rename, delete, or otherwise perform branch operations. Do not run GitHub workflows on non-public repos. Keep evidence screenshots and pixel diffs in `/private/tmp/emsi_qa`, not in the runtime repo. Keep PNG evidence as PNG. Do not commit `.DS_Store`, `shadow-build`, `bin`, `obj`, or build outputs.

Use the existing Windows references at `/private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329` and the current comparison baseline at `/private/tmp/emsi_qa/windows/probe-comparisons/2026-06-06-downstream-probe-onscreen-client-vs-mac-runtime`.

Verification should include targeted `dotnet test --filter ...` commands per phase, public clean-room component fixture runs with `winui3-mac-runner run`, and downstream probe sweeps with:

WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" \
  tools/winui3-mac-runner-downstream-windows-probe-sweep \
  --output /private/tmp/emsi_qa/windows/probe-comparisons/<dated-phase-output> \
  --windows-screenshot-dir /private/tmp/emsi_qa/windows/probe-screenshots/2026-06-06-downstream-probe-onscreen-client-20260606-145329

Only add `--require-native-comparison` when the current phase is expected to pass the configured native comparison ladder. After successful verification, inspect the task diff and make task-only commits at the phase commit boundaries when AGENTS.md git guards pass. Do not push unless the current task explicitly asks for push and the push guards pass.
```
