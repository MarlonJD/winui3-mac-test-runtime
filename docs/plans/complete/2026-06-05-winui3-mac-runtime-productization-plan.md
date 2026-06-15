# WinUI3 Mac Runtime Productization Plan

Date: 2026-06-05

Owner subtree: `tools/winui3-mac-test-runtime`

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> `superpowers:executing-plans` or `superpowers:subagent-driven-development` to
> implement this plan task-by-task. Use `emsi-workflows:emsi-task-router` before
> editing and `emsi-workflows:emsi-verification-gate` before claiming
> completion.

## Objective

Move `winui3-mac-test-runtime` from a source-level WinUI 3 harness with usable
visual scaffolding into a product-grade local macOS development, test,
automation, and evidence runtime for the documented public WinUI 3 subset.

This plan intentionally stops treating progress as one-off component promotion.
The execution model is batch-oriented:

- product contract first;
- family-level renderer primitives second;
- state/theme/interaction matrices third;
- corpus and application-level E2E fourth;
- release, packaging, reliability, and documentation gates last.

Native-quality component promotion remains row-by-row because each row needs its
own evidence, but renderer work should happen by family and product milestone,
not by ad hoc pixel chase.

## Current Baseline

As of the 2026-06-05 HyperlinkButton closure pass:

- The repository has a working source-level compatibility harness for the
  documented public subset.
- `docs/compatibility/all-catalog-readiness-audit.json` accounts for all 126
  catalog entries with 0 unassigned dispositions:
  - 55 `source-level-production-implementation`;
  - 35 `bounded-source-level-production-implementation`;
  - 31 `production-ready-diagnostic-exclusion-until-promoted`;
  - 3 `production-ready-windows-only-exclusion`;
  - 2 `production-ready-non-goal-exclusion`.
- `docs/visual-parity/component-quality-dashboard.json` tracks 58 public
  component rows:
  - 4 `nativeQualityGrade: good`;
  - 54 `nativeQualityGrade: not-evaluated`;
  - 47 `visualGrade: usable`;
  - 7 `visualGrade: not-rendered`.
- Native reference readiness for those 58 rows is structurally ready:
  `missingMacRuntimeCrops=0`, `missingNativeReferenceCrops=0`,
  `missingNativeReferenceProvenance=0`, `missingComponentDiffs=0`, and
  `blockingRowCount=0`.
- The local production E2E workbench can run managed interactions on macOS:
  navigation, text entry, combo selection, list selection, command invocation,
  popup open/dismiss, and status assertions pass.
- Visual output is not product-grade native WinUI fidelity yet. The public admin
  workbench renders recognizably, but native visual comparison still fails.
- Text currently falls back to Helvetica on this host unless repo-external Segoe
  fonts are supplied with `WINUI3_MAC_TEST_FONT_DIRS`.

## Product-Grade Definition

The product is considered production-level for the documented public subset
when all of the following are true:

1. **Clear support contract:** README, support policy, compatibility catalog,
   component support docs, and release docs all state the same boundary.
2. **Source-level app reliability:** public clean-room WinUI-style apps build,
   run, interact, and export artifacts locally without private patches or
   Windows binaries.
3. **Deterministic diagnostics:** unsupported APIs, missing resources, binding
   failures, template gaps, renderer gaps, and native-reference issues fail
   loudly with stable machine-readable codes.
4. **Usable app visuals:** application screenshots look like a coherent product
   test runtime rather than a diagnostic toy, even where native-quality parity is
   not claimed.
5. **Native-quality subset:** promoted controls meet component-level native
   evidence gates across required states, themes, provenance, and manual review.
6. **State coverage:** supported controls are tested in default, disabled,
   focused, hover, pressed, selected, checked, invalid, high-contrast, and
   dark/light variants where applicable.
7. **Automation credibility:** scenario actions and accessibility artifacts are
   stable enough for consumer CI.
8. **Release safety:** build, test, strict scenario sweep, package dry run,
   release-candidate, private-name scan, native reference provenance, and
   artifact drift gates pass from a clean checkout.
9. **Operational confidence:** performance, flake, timeout, artifact retention,
   and failure triage are measured and documented.
10. **No inflated claims:** Windows binary execution, arbitrary WinUI 3
    compatibility, WebView2/media/composition parity, and native Fluent
    pixel-perfect rendering remain out of claim unless evidence is added.

## Architecture

The productization architecture is a set of explicit contracts layered over the
existing runtime:

- **Compatibility contract:** catalog + matrix + docs define supported,
  partial, planned, windows-only, and non-goal behavior.
- **Runtime contract:** facade controls, XAML compiler, layout engine, binding,
  commands, interactions, and accessibility export behave consistently.
- **Renderer contract:** `skia-v2` uses shared Fluent primitives, typography,
  metrics, theme tokens, state renderers, and component crop evidence.
- **Evidence contract:** every scenario emits `run.json`, `tree.json`,
  `accessibility.json`, `interactions.json`, `snapshot.json`, `visual-run.json`,
  `component-evidence.json`, review artifacts, and dashboard/index artifacts.
- **Release contract:** runner commands gate drift, catalog readiness, visual
  evidence, native provenance, package readiness, performance, and privacy.

The key change from the current slow loop is to introduce family-level
milestones with explicit acceptance matrices. A row should only be touched when
it is part of a family milestone or a blocking regression, not because it happens
to be the next visible diff.

## Scope

In scope:

- `src/WinUI3.MacCompat`
- `src/WinUI3.MacRuntime`
- `src/WinUI3.MacRenderer.Skia`
- `src/WinUI3.MacRunner`
- `src/WinUI3.MacXaml`
- `fixtures`
- `tests`
- `docs/compatibility`
- `docs/release`
- `docs/visual-parity`
- `.github/workflows`
- `tools` runner scripts and private-name scan integration

Out of scope:

- Running arbitrary Windows `.exe`, `.msix`, or native Windows binaries on
  macOS.
- Adding Wine as a required runtime path.
- Claiming full WinUI 3, Windows App SDK, WebView2, media, DirectX,
  compositor, Mica/Acrylic, packaged app, or platform integration parity.
- Bundling, copying, committing, packaging, or redistributing proprietary
  Windows fonts.
- Loosening visual thresholds to create a native-quality claim.
- Using private EMSI product names, private screenshots, private data, secrets,
  or private repositories as public evidence.
- Creating, switching, renaming, or deleting branches unless explicitly asked in
  the current task.

## Assumptions And Open Questions

Assumptions:

- The product target is a source-level macOS compatibility runtime and evidence
  tool for public WinUI-style code, not a Windows binary runtime.
- Native Windows references remain the source of truth for visual parity.
- The current 126-entry catalog remains the compatibility accounting baseline.
- The first product-quality milestone should favor stable application-level
  usability and truthful evidence over broad native-quality promotion.
- Repo-external fonts may be used for diagnostics, but default-font behavior must
  remain supported and honestly documented.

Open questions:

- What is the first external-consumer promise: "production source-level harness"
  or "native-quality subset for selected controls"?
- Should product releases include a `CompatibilityLevel` label in package
  metadata, or should that stay docs-only until the state matrix is broader?
- Which macOS and .NET SDK versions are the supported consumer baseline?
- Which native reference workflow cadence is acceptable for product releases:
  every PR, nightly, or release-candidate only?
- Is the public admin workbench the primary product-demo fixture, or should a
  separate "golden consumer app" fixture be created?

## Workstream Overview

The plan has nine workstreams. They should be executed in order, but each
workstream has independently testable exit criteria.

1. **Program Reset And Product Contract**
2. **Artifact And Evidence Infrastructure**
3. **XAML, Resources, Binding, And Diagnostics**
4. **Renderer Design System And Typography**
5. **Control Family Native-Quality Batches**
6. **State, Theme, Interaction, And Accessibility Matrix**
7. **Application Corpus And Product Demo Quality**
8. **Reliability, Performance, Packaging, And CI**
9. **Final Product Gate And Public Claim Update**

## Workstream 1: Program Reset And Product Contract

Goal: stop drifting through one-off fixes and define a product bar that all
future tasks use.

### Phase 1.1: Freeze The Current Truth

Actions:

- Read:
  - `README.md`
  - `docs/release/production-evidence-view.md`
  - `docs/release/support-policy.md`
  - `docs/release/final-production-gate.md`
  - `docs/compatibility/matrix.md`
  - `docs/compatibility/component-support.md`
  - `docs/compatibility/winui-component-inventory.json`
  - `docs/visual-parity/component-quality-dashboard.json`
- Record the current product truth in a new or updated status section:
  - source-level harness is release-evidence-ready;
  - product/native visual fidelity is not complete;
  - only 4 component rows are currently `nativeQualityGrade: good`;
  - public admin/workbench visuals are usable but not native-quality.
- Add a single `Productization Status` section to `README.md` that points to the
  authoritative evidence docs.

Verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter ComponentQualityDashboard
```

Exit criteria:

- Product claim language is consistent across README and release docs.
- No doc says "production visual fidelity" without the native-quality qualifier
  and evidence path.

### Phase 1.2: Define Compatibility Levels

Actions:

- Add a machine-readable compatibility-level document, likely:
  `docs/compatibility/compatibility-levels.json`.
- Add a human-readable companion:
  `docs/compatibility/compatibility-levels.md`.
- Define levels:
  - `L0`: runner/artifact harness reliability;
  - `L1`: source-level app startup and XAML ingest;
  - `L2`: supported controls render at `usable`;
  - `L3`: interaction and accessibility;
  - `L4`: state/theme coverage;
  - `L5`: native-quality selected subset;
  - `L6`: consumer package/release readiness.
- Wire tests that ensure every catalog disposition maps to a compatibility
  level.

Likely files:

- Modify: `docs/compatibility/contracts.md`
- Modify: `docs/compatibility/matrix.md`
- Create: `docs/compatibility/compatibility-levels.json`
- Create: `docs/compatibility/compatibility-levels.md`
- Test: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Compatibility
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
```

Exit criteria:

- Every supported/partial/planned/windows-only/non-goal catalog entry has a
  level-compatible disposition.
- Release docs can summarize the product level without prose-only inference.

## Workstream 2: Artifact And Evidence Infrastructure

Goal: make evidence generation cheap, repeatable, and batch-oriented.

### Phase 2.1: Add A Single Product Evidence Command

Problem:

Current work requires many separate commands and artifact directories. That
encourages small manual loops and stale generated files.

Actions:

- Add a runner command such as:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence \
  --profile public-product \
  --output artifacts/product-evidence/public-product
```

- The command should orchestrate:
  - catalog audit;
  - component-quality dashboard check/build;
  - native-reference readiness check;
  - strict scenario sweep;
  - public admin workbench;
  - production smoke and E2E;
  - visual review index;
  - release-candidate dry run;
  - summary JSON and Markdown report.
- The command should not hide failures. It should emit:
  `product-evidence.json` with per-step status, command, elapsed time, artifact
  paths, and failure reason.

Likely files:

- Modify: `src/WinUI3.MacRunner/Program.cs`
- Create: `src/WinUI3.MacRunner/ProductEvidence.cs`
- Test: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Docs: `docs/release/production-evidence-view.md`

TDD requirements:

- First write tests for:
  - profile parsing;
  - command list generation;
  - failed-step summary;
  - artifact path stability.
- Then implement the command.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter ProductEvidence
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
```

Exit criteria:

- One command can reproduce the product evidence status without manual sequence
  memory.
- The generated report states which failures block source-level release,
  product polish, native-quality promotion, or package publication.

### Phase 2.2: Add Evidence Freshness And Stale Artifact Detection

Actions:

- Track the source inputs for each generated artifact:
  - scenario JSON hash;
  - renderer version;
  - relevant source file hashes;
  - native reference run ID;
  - font diagnostics;
  - command-line options.
- Add freshness checks for:
  - `component-evidence.json`;
  - `visual-review.json/html`;
  - `component-quality-dashboard.json`;
  - `public-visual-review-index.json/html`;
  - `visual-drift-dashboard.json`;
  - product evidence report.
- Fail checks when generated artifacts reference older metrics than the
  underlying run files.

Likely files:

- Modify: `src/WinUI3.MacRuntime/ComponentEvidence.cs`
- Modify: `src/WinUI3.MacRuntime/VisualReviewArtifacts.cs`
- Modify: `src/WinUI3.MacRuntime/VisualReviewIndexArtifacts.cs`
- Modify: `src/WinUI3.MacRuntime/ComponentQualityDashboard.cs`
- Modify: `src/WinUI3.MacRunner/VisualArtifacts.cs`
- Test: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Freshness
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
```

Exit criteria:

- A stale generated metric fails before it reaches final verification.
- The HyperlinkButton-style mismatch between `pixel-diff.json` and
  `visual-drift-dashboard.json` cannot recur silently.

### Phase 2.3: Add Batch Comparison Reports

Actions:

- Add a comparison command:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-compare \
  --before artifacts/baseline \
  --after artifacts/candidate \
  --output artifacts/compare/candidate-vs-baseline
```

- Compare per row:
  - crop status;
  - changed-pixel percentage;
  - MAE;
  - RMS;
  - native/runtime crop size;
  - font diagnostics;
  - native reference provenance.
- Mark rows as:
  - improved;
  - regressed;
  - unchanged;
  - newly passing;
  - newly failing;
  - environment-dependent.

Exit criteria:

- Renderer family work can be evaluated as a batch instead of mentally scanning
  JSON.
- Promotion candidates are generated as a review queue, not guessed.

## Workstream 3: XAML, Resources, Binding, And Diagnostics

Goal: make realistic source-level WinUI apps reliable before chasing final
pixels.

### Phase 3.1: XAML Compiler Gap Closure

Actions:

- Build a failing-test inventory for XAML features used by the public corpus:
  - property elements;
  - attached properties;
  - resource dictionaries;
  - merged dictionaries;
  - theme dictionaries;
  - styles;
  - setters;
  - event handlers;
  - bindings;
  - collection syntax;
  - simple templates as diagnostic exclusions.
- Add one test class per feature family rather than one huge method.
- Keep unsupported templates diagnostic until a specific template support slice
  is planned.

Likely files:

- `src/WinUI3.MacXaml/*`
- `src/WinUI3.MacRuntime/*`
- `tests/WinUI3.MacRuntime.Tests/*`
- `fixtures/ResourceCatalogApp.WinUI/*`
- `fixtures/SettingsFormApp.WinUI/*`

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Xaml
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-dark.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-high-contrast.json --strict-visual
```

Exit criteria:

- Corpus XAML either loads or fails with exact diagnostics.
- No "supported" XAML feature has silent partial behavior.

### Phase 3.2: Binding And State Reliability

Actions:

- Expand binding diagnostics for:
  - missing source property;
  - missing target property;
  - type conversion failure;
  - unsupported binding mode;
  - collection update failure.
- Add tests for:
  - `INotifyPropertyChanged`;
  - observable collection add/remove/reset;
  - two-way text input;
  - command enabled state;
  - selection state.
- Update `interactions.json` artifacts to include before/after values for
  state-changing operations.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Binding
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
```

Exit criteria:

- Production E2E state changes are robust and explain failures.
- Binding failures are actionable without opening renderer screenshots.

## Workstream 4: Renderer Design System And Typography

Goal: move `skia-v2` from hand-tuned painters into a reusable WinUI-like visual
system.

### Phase 4.1: Fluent Token Registry

Actions:

- Centralize tokens for:
  - typography;
  - color;
  - control height;
  - content padding;
  - border thickness;
  - corner radius;
  - disabled opacity;
  - selected/focused/hover/pressed fills;
  - icon sizing;
  - list item metrics;
  - navigation pane metrics.
- Add tests that assert token stability for light, dark, and high-contrast.
- Stop hardcoding offsets in individual renderers when a token exists.

Likely files:

- Modify: `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- Create: `src/WinUI3.MacRenderer.Skia/FluentControlMetrics.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Test: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Fluent
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual
```

Exit criteria:

- Button, dropdown, split, selection, list, navigation, and text controls use
  shared metrics.
- A future one-pixel fix is made in a family primitive, not copied into several
  render methods.

### Phase 4.2: Typography And Font Policy

Actions:

- Define a production font policy:
  - default host fallback is supported;
  - repo-external Segoe font diagnostics are optional;
  - no proprietary fonts are committed;
  - native-quality claims must declare font provenance.
- Add text measurement helpers for:
  - horizontal centering;
  - baseline alignment;
  - leading/trailing trimming;
  - text clipping;
  - state-specific foreground.
- Add tests that compare text bounds for Button, HyperlinkButton, TextBox,
  ComboBox, ListView item, and NavigationView item.

Likely files:

- Modify: `src/WinUI3.MacRenderer.Skia/FontResolver.cs`
- Modify: `src/WinUI3.MacRuntime/SnapshotFontDiagnostics.cs`
- Create: `src/WinUI3.MacRenderer.Skia/FluentTextLayout.cs`
- Modify: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- Test: `tests/WinUI3.MacRuntime.Tests/FontResolverTests.cs`
- Test: `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Docs: `docs/visual-parity/font-resolution-diagnostics.md`

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Font
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Text
find . \( -name '*.ttf' -o -name '*.otf' -o -name '*.ttc' -o -name '*.woff' -o -name '*.woff2' \) -print
```

Exit criteria:

- Text-heavy controls have predictable alignment.
- Environment-dependent visual results are labeled, not promoted by accident.

## Workstream 5: Control Family Native-Quality Batches

Goal: convert slow row-by-row renderer fixes into family milestones with
repeatable acceptance criteria.

### Batch Rules

Every control-family batch must:

- start with manual triptych review;
- write one hypothesis per family, not per row unless needed;
- use TDD before renderer changes;
- run default-font native-backed evidence;
- run repo-external-font diagnostic evidence when text-heavy;
- compare every first-tranche and workbench row touched by the family;
- update inspection metadata only for rows whose evidence supports the grade;
- keep uncertain rows `nativeQualityGrade: not-evaluated`.

### Phase 5.1: Selection Controls Batch

Targets:

- `CheckBox`
- `RadioButton`
- selected/checked states in light, dark, high-contrast, disabled, focused.

Current state:

- `CheckBox` is `good` for the default checked light-state row.
- `RadioButton` is close but remains over threshold in base light evidence.

Actions:

- Add state scenarios if missing:
  - `component-basic-input-checked-light`
  - `component-basic-input-disabled-light`
  - `component-basic-input-focused-light`
  - dark/high-contrast variants where feasible.
- Improve shared selected-ring, tick, disabled opacity, focus ring, and label
  baseline primitives.
- Promote only rows with state-specific evidence.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter RadioButton
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter CheckBox
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/productization/selection-light-native
```

Exit criteria:

- RadioButton either passes threshold or has a documented native-template
  blocker.
- CheckBox does not regress.
- State matrix is broader than default checked/on state.

### Phase 5.2: Button And Link Controls Batch

Targets:

- `Button`
- `RepeatButton`
- `ToggleButton`
- `HyperlinkButton`
- `AppBarButton`
- `CommandBar` button slots.

Actions:

- Create a shared `FluentButtonRenderer` or equivalent primitive set for:
  - default button;
  - repeat button;
  - checked toggle button;
  - command button;
  - app bar icon+label button;
  - hyperlink text button.
- Add state tests for default, disabled, focused, hover, pressed, checked, and
  command-enabled states.
- For icon buttons, prefer deterministic vector fallbacks for common icons and
  real icon font diagnostics when supplied externally.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Button
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/productization/button-family-native
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual --reference docs/visual-parity/examples/component-commands-menus-light --output artifacts/productization/commands-button-family-native
```

Exit criteria:

- Button-family controls are either promoted as a coherent subset or blocked by
  known font/template issues.
- CommandBar/AppBarButton no longer show 100% component crop failures from blank
  or mismatched crop handling.

### Phase 5.3: Dropdown, Combo, Split, Menu Batch

Targets:

- `ComboBox`
- `DropDownButton`
- `SplitButton`
- `ToggleSplitButton`
- `MenuBar`
- `MenuFlyout`
- `CommandBarFlyout`
- context menu target.

Actions:

- Implement shared field/button shell metrics.
- Implement popup host rendering for open-popup scenarios.
- Add light-dismiss, selection, disabled item, and keyboard state diagnostics.
- Capture native references for open popup states where not already present.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter ComboBox
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Flyout
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-open-popup-light.json --strict-visual
```

Exit criteria:

- Closed-state dropdown/split family reaches a coherent visual grade.
- Popup states are either evidence-backed or explicitly blocked by native
  reference/state capture gaps.

### Phase 5.4: Text And Forms Batch

Targets:

- `TextBlock`
- `TextBox`
- `PasswordBox`
- validation visuals;
- placeholder text;
- caret/selection diagnostics.

Actions:

- Add text input state matrix:
  - empty placeholder;
  - focused;
  - typed;
  - invalid;
  - disabled;
  - multiline if supported.
- Implement renderer pieces for:
  - text field border;
  - focus ring;
  - placeholder color;
  - validation stroke;
  - selection/caret representation in deterministic screenshots.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter TextBox
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-focused-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-invalid-light.json --strict-visual
```

Exit criteria:

- Text/form surfaces look product-credible in app screenshots.
- Text field state evidence is not limited to static default state.

### Phase 5.5: Navigation, Lists, And Workbench Batch

Targets:

- `NavigationView`
- `NavigationViewItem`
- `Frame`
- `ListView`
- `ItemsControl`
- workbench shell layouts.

Actions:

- Improve navigation pane metrics, selected indicator, footer/account card,
  content region, list item container, and selected item treatment.
- Add list/detail state tests:
  - selected;
  - empty;
  - multiple items;
  - disabled command;
  - long text clipping.
- Make the public admin workbench the primary visual demo target.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Navigation
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter ListView
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual --reference docs/visual-parity/examples/public-admin-workbench-light --output artifacts/productization/public-admin-workbench-native
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
```

Exit criteria:

- The public admin workbench screenshot is visually credible enough to show as
  the product demo with honest caveats.
- Interaction E2E remains green.

### Phase 5.6: Status, Progress, Rating, And Feedback Batch

Targets:

- `InfoBar`
- `ProgressBar`
- `ProgressRing`
- `RatingControl`
- status-picker scenarios.

Actions:

- Implement severity chrome, progress animation end-state, close/action area
  placeholders, and rating state variants.
- Add loading, success, error, disabled, and high-contrast evidence.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter InfoBar
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Progress
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-loading-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-success-light.json --strict-visual
```

Exit criteria:

- Status/progress visuals are credible in app screenshots.
- Planned picker rows remain honest diagnostics until implemented.

## Workstream 6: State, Theme, Interaction, And Accessibility Matrix

Goal: stop promoting default-state-only evidence as though it represents the
control.

### Phase 6.1: State Matrix Manifest

Actions:

- Add `docs/visual-parity/state-coverage-matrix.json`.
- Track, per component:
  - default;
  - disabled;
  - focused;
  - hover;
  - pressed;
  - checked/unchecked;
  - selected/unselected;
  - invalid;
  - popup open;
  - light/dark/high-contrast;
  - interaction evidence;
  - accessibility evidence.
- Add a dashboard section that shows why a row is `good` but not
  `production-ready`.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter StateCoverage
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
```

Exit criteria:

- No row can be promoted without a visible state coverage status.
- Default-only evidence is explicitly labeled.

### Phase 6.2: Accessibility Contract Upgrade

Actions:

- Define role/name/value/state expectations per supported control.
- Add accessibility checks for:
  - automation ID/name;
  - enabled;
  - focusable/focused;
  - checked;
  - selected;
  - expanded;
  - value;
  - help text;
  - control type.
- Add failure output that points to the exact element path.

Verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter Accessibility
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
```

Exit criteria:

- Interactive controls have accessibility artifacts strong enough for consumer
  CI assertions.

### Phase 6.3: Interaction Command Contract

Actions:

- Expand scenario actions:
  - click;
  - focus;
  - key press;
  - type text;
  - select item;
  - select navigation;
  - scroll;
  - open popup;
  - dismiss popup;
  - assert property;
  - assert accessibility state;
  - wait for idle.
- Add strict failure objects with:
  - step index;
  - selector;
  - expected;
  - actual;
  - observed state;
  - artifact links.

Exit criteria:

- Production E2E failures are self-diagnosing.
- Tests can validate behavior without relying on screenshot interpretation.

## Workstream 7: Application Corpus And Product Demo Quality

Goal: make the product credible to an external user by showing complete app
flows, not only component labs.

### Phase 7.1: Product Demo Fixture

Actions:

- Promote one fixture as the primary product demo:
  `PublicAdminWorkbench.WinUI` or a new `ProductDemoWorkbench.WinUI`.
- Ensure it has:
  - navigation;
  - command bar;
  - forms;
  - list/detail;
  - status feedback;
  - popup/dialog;
  - settings/resource use;
  - light/dark/high-contrast variants.
- Add a README section with honest screenshots:
  - native Windows reference;
  - macOS runtime;
  - diff;
  - current support status.

Verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-deferred-light.json --strict-visual
```

Exit criteria:

- A user can look at one fixture and understand the product value and limits.

### Phase 7.2: Public Corpus Expansion

Actions:

- Expand or refresh the clean-room corpus to include:
  - single-window app;
  - settings form app;
  - navigation/workbench app;
  - resource-heavy app;
  - interaction-heavy app;
  - command/menu app;
  - layout/media diagnostic app.
- Add corpus ingestion checks for unknown APIs and unsupported surfaces.
- Avoid private data and private names.

Verification:

```sh
dotnet run --project src/WinUI3.MacRunner -- ingest --manifest fixtures/corpus.json --output artifacts/corpus --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
```

Exit criteria:

- Corpus coverage represents realistic consumer source code.
- Unknown public surfaces are tracked, not silently ignored.

## Workstream 8: Reliability, Performance, Packaging, And CI

Goal: product-level trust, not just local green tests.

### Phase 8.1: Full Strict Scenario Sweep Command

Actions:

- Add or standardize a sweep command/profile that runs all public scenarios.
- Emit:
  - per-scenario status;
  - elapsed time;
  - artifact path;
  - failure class;
  - flaky retry metadata if retries are used.

Verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep
```

Exit criteria:

- A full sweep is one command, not tribal knowledge.

### Phase 8.2: Performance And Flake Budget

Actions:

- Add benchmarks for:
  - XAML compile;
  - project ingestion;
  - runtime startup;
  - layout;
  - rendering;
  - interaction;
  - visual crop/diff generation.
- Add thresholds and trend docs.
- Track flake rate and timeout causes.

Verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner benchmark --output artifacts/benchmarks/productization
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
```

Exit criteria:

- Product release cannot regress basic performance without visible evidence.

### Phase 8.3: Package And Release Hardening

Actions:

- Verify package commands:
  - `dotnet pack`;
  - tool install smoke;
  - release-check;
  - release-candidate.
- Add package provenance fields and changelog requirements.
- Ensure external consumers get clear quick-start docs.

Verification:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --package-dir artifacts/package-dry-run
PATH="$PWD/tools:$PATH" winui3-mac-runner release-check --package-dir artifacts/package-dry-run
```

Exit criteria:

- Release candidate gate cannot pass if docs, package, catalog, visual evidence,
  or privacy checks are stale.

## Workstream 9: Final Product Gate And Public Claim Update

Goal: graduate from "source-level harness" to a clearly labeled product release.

Actions:

- Run product evidence from a clean checkout.
- Confirm:
  - catalog audit passes;
  - component dashboard passes;
  - native reference readiness passes;
  - full runtime tests pass;
  - strict scenario sweep passes;
  - production E2E passes;
  - product demo renders;
  - package dry run passes;
  - release candidate passes;
  - private-name scan passes;
  - no proprietary fonts are committed;
  - support docs match the evidence.
- Update README and release docs to state the exact product level achieved.
- Keep native-quality rows separate from source-level production readiness.

Verification:

```sh
git status --short
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-reference-readiness --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --package-dir artifacts/package-dry-run
find . \( -name '*.ttf' -o -name '*.otf' -o -name '*.ttc' -o -name '*.woff' -o -name '*.woff2' \) -print
```

Exit criteria:

- Final claim is evidence-backed and narrower than the runtime's future roadmap.
- A user can install/run/use the runtime with correct expectations.
- The product demo and E2E flows are credible.
- Native-quality claims are limited to rows with real evidence.

## Milestone Plan

### Milestone A: Product Contract And Evidence Automation

Target outcome:

- One-command product evidence report.
- Compatibility level model.
- Stale artifact detection.
- README/release docs aligned.

Expected impact:

- Stops manual command drift.
- Gives every future renderer task a clear product status target.

Primary verification:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
```

### Milestone B: Product Demo Quality

Target outcome:

- Public admin/workbench screenshot looks coherent and demo-worthy.
- Production E2E remains green.
- Navigation/list/forms/status/command families have usable app-level polish.

Expected impact:

- External users can understand what the library does without reading a 60-page
  evidence doc.

Primary verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
```

### Milestone C: Family-Level Native-Quality Tranches

Target outcome:

- Selection, button/link, dropdown/menu, text/forms, navigation/list, and
  status/progress families each have their own native-backed tranche.
- Promotions happen as a byproduct of family closure, not as ad hoc tasks.

Expected impact:

- Work accelerates because every renderer primitive improves multiple rows.

Primary verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-compare --before artifacts/baseline --after artifacts/candidate --output artifacts/compare/candidate
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
```

### Milestone D: State And Interaction Coverage

Target outcome:

- Default-only evidence no longer drives product claims.
- State matrix is explicit.
- Interaction and accessibility artifacts cover supported controls.

Expected impact:

- Native-quality and product-quality claims become believable, not just prettier
  screenshots.

### Milestone E: Release-Ready Product Package

Target outcome:

- Clean checkout can run full build/test/product-evidence/release-candidate.
- Consumer docs and package artifacts match support boundaries.

Expected impact:

- Library can be shipped with honest production-level positioning.

## Verification Gates

Use the EMSI verification gate before any completion claim. Minimum gates by
change type:

Renderer-only change:

```sh
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter <FocusedFilter>
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario <scenario> --strict-visual --reference <reference-dir> --output <artifact-dir>
```

Evidence or docs change:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
```

Product milestone:

```sh
git status --short
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-reference-readiness --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --package-dir artifacts/package-dry-run
find . \( -name '*.ttf' -o -name '*.otf' -o -name '*.ttc' -o -name '*.woff' -o -name '*.woff2' \) -print
```

## Risks And Mitigations

Risk: continuing row-by-row work stays too slow.

Mitigation:

- Only execute renderer work inside a family batch.
- Require batch compare reports before promotion decisions.
- Maintain a promotion queue generated from evidence.

Risk: product docs overclaim.

Mitigation:

- Compatibility levels are machine-readable.
- Release docs derive from catalog/dashboard evidence.
- Native-quality and source-level production are separate labels.

Risk: visual threshold tuning hides problems.

Mitigation:

- Do not loosen thresholds as part of this plan.
- Use native/reference/runtime/diff crop inspection.
- Require manual inspection notes for every promotion.

Risk: proprietary Windows fonts enter the repo.

Mitigation:

- Use `WINUI3_MAC_TEST_FONT_DIRS` only for repo-external diagnostics.
- Run font-file scans before completion.
- Keep `resolvedPath` provenance in diagnostics.

Risk: generated artifacts become stale.

Mitigation:

- Add freshness checks.
- Use product-evidence orchestration.
- Avoid hand-editing generated JSON except for known source-owned manifests.

Risk: application demo becomes prettier but less correct.

Mitigation:

- Keep production E2E and interaction assertions green.
- Gate app polish with semantic interaction and accessibility evidence.

Risk: advanced Windows-only surfaces distract from shipping.

Mitigation:

- Keep WebView2, media, DirectX, native composition, packaged apps, and Windows
  OS integrations as explicit exclusions until separately planned.

## Dependencies And Ownership Boundaries

Owned locally:

- `tools/winui3-mac-test-runtime/src/WinUI3.MacCompat`
- `tools/winui3-mac-test-runtime/src/WinUI3.MacRuntime`
- `tools/winui3-mac-test-runtime/src/WinUI3.MacRenderer.Skia`
- `tools/winui3-mac-test-runtime/src/WinUI3.MacRunner`
- `tools/winui3-mac-test-runtime/src/WinUI3.MacXaml`
- `tools/winui3-mac-test-runtime/fixtures`
- `tools/winui3-mac-test-runtime/tests`
- `tools/winui3-mac-test-runtime/docs`

External or cross-boundary:

- Native Windows reference workflows in `.github/workflows`.
- Package publication policy and signing/provenance evidence.
- Licensed Windows fonts, which must remain outside the repository.
- Consumer app usage, which must follow the documented support subset.

## Affected Files Or Docs

Likely code files:

- `src/WinUI3.MacRunner/Program.cs`
- `src/WinUI3.MacRunner/ProductEvidence.cs`
- `src/WinUI3.MacRunner/VisualArtifacts.cs`
- `src/WinUI3.MacRuntime/ComponentEvidence.cs`
- `src/WinUI3.MacRuntime/ComponentQualityDashboard.cs`
- `src/WinUI3.MacRuntime/VisualReviewArtifacts.cs`
- `src/WinUI3.MacRuntime/VisualReviewIndexArtifacts.cs`
- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRuntime/SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `src/WinUI3.MacRenderer.Skia/FontResolver.cs`
- `src/WinUI3.MacXaml/*`
- `src/WinUI3.MacCompat/*`

Likely test files:

- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `tests/WinUI3.MacRuntime.Tests/FontResolverTests.cs`
- new focused test files if the monolithic test file is split.

Likely fixtures:

- `fixtures/PublicAdminWorkbench.WinUI/*`
- `fixtures/ProductionSmoke.WinUI/*`
- `fixtures/ComponentParityLab.WinUI/*`
- `fixtures/ResourceCatalogApp.WinUI/*`
- `fixtures/SettingsFormApp.WinUI/*`
- `fixtures/InteractionBindingApp.MacTest/*`
- `fixtures/corpus.json`

Likely docs/artifacts:

- `README.md`
- `docs/compatibility/*`
- `docs/release/*`
- `docs/visual-parity/*`
- `docs/plans/*`

## Rollback And Recovery

- If a renderer batch regresses product E2E, revert the batch change or split
  the shared primitive by control family.
- If native-backed evidence worsens a promoted row, keep the new artifact for
  diagnosis but do not update canonical docs as success.
- If product evidence orchestration is flaky, keep the individual commands as
  fallback and mark the orchestration failure separately.
- If package or release gates fail, do not weaken the gates; update the package,
  docs, or evidence source.
- If a milestone reveals architecture coupling, stop after three failed fix
  attempts and create a refactor plan instead of adding another patch.
- Do not revert unrelated dirty worktree changes.

## Execution Prompt

Continue from:

```text
docs/plans/2026-06-05-winui3-mac-runtime-productization-plan.md
docs/release/production-evidence-view.md
docs/visual-parity/component-quality-dashboard.json
docs/compatibility/all-catalog-readiness-audit.json
```

Required skills:

```text
superpowers:using-superpowers
superpowers:executing-plans
superpowers:systematic-debugging
superpowers:test-driven-development
superpowers:verification-before-completion
emsi-workflows:emsi-task-router
emsi-workflows:emsi-verification-gate
```

Goal:

Execute Milestone A of the WinUI3 Mac Runtime Productization Plan. Stop doing
one-off component closure as the primary work mode. Build the product evidence
and compatibility-level infrastructure that lets future renderer and runtime
work happen in family batches with one-command verification.

Start with:

1. Confirm current branch without creating, switching, renaming, or deleting
   branches.
2. Confirm dirty worktree and preserve unrelated user changes.
3. Read the plan, README, production evidence view, component dashboard, and
   catalog audit.
4. Add compatibility-level source docs/artifacts.
5. Add product-evidence command scaffolding with tests first.
6. Add stale artifact/freshness checks for at least the visual drift dashboard
   and component dashboard.
7. Run focused tests, full tests, dashboard/catalog checks, and a product
   evidence dry run.

Rules:

- Do not create, switch, rename, or delete branches.
- Do not commit or push unless explicitly asked in the current task.
- Do not loosen visual thresholds.
- Do not bundle, copy, commit, package, or redistribute proprietary Windows
  fonts.
- Keep unsupported, planned, Windows-only, and non-goal surfaces honest.
- Preserve native reference provenance and font diagnostics.
- Keep native-quality and source-level production labels separate.

Verification:

```sh
git status --short
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
find . \( -name '*.ttf' -o -name '*.otf' -o -name '*.ttc' -o -name '*.woff' -o -name '*.woff2' \) -print
```

Expected result:

- Milestone A should produce infrastructure, not broad renderer changes.
- Product evidence status should be reproducible with one command.
- Future control-family work should have a clear batch gate.
- No product visual-fidelity claim should be added unless native-backed evidence
  supports it.
