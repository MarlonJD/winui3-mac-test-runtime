# Downstream Windows Full XAML Coverage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the current WinUI3 Mac Test Runtime gaps that prevent the downstream EMSI Windows production XAML surface from compiling and running as a meaningful source-level macOS test target.

**Architecture:** Treat production XAML coverage as a staged compatibility contract, not a one-shot whitelist expansion. Each promoted surface must move through catalog disposition, XAML compiler ingestion, facade/runtime semantics, layout/tree/accessibility export, renderer evidence where visual, downstream probe coverage, and release/support documentation.

**Tech Stack:** .NET 10, MSTest, clean-room `Microsoft.UI.Xaml` facade types, `WinUI3.MacXaml`, `WinUI3.MacRuntime`, `WinUI3.MacRenderer.Skia`, `winui3-mac-runner`, Windows App SDK source fixtures.

---

Date: 2026-06-06

Owner subtree: `tools/winui3-mac-test-runtime`

Downstream validation boundary: `apps/windows` owns the real Windows app, Windows-runner validation, and private/screenshot-like QA evidence. This plan may reference downstream XAML categories and local evidence paths during execution, but public runtime artifacts must remain sanitized.

## Objective

The current runtime is healthy for the documented public source-level harness subset, but it is not sufficient to test the full downstream EMSI Windows app. The immediate blocker is production XAML ingestion: compiling the downstream production XAML with the current runtime compiler produced 121 diagnostics across page, theme, layout, list/template, command, status, and form surfaces.

This plan documents those gaps and defines the staged work required to make the runtime a credible macOS source-level test target for the downstream Windows production shell.

## Current Evidence

Run from `tools/winui3-mac-test-runtime`:

```sh
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Observed result:

- Exit code: non-zero.
- Diagnostics: `/private/tmp/emsi-windows-prod-xaml-generated.diagnostics.json`.
- Total diagnostics: 121.
- Diagnostic code distribution:
  - `XAML1002`: 83 unsupported XAML properties.
  - `XAML1005`: 31 unsupported attached properties.
  - `XAML1003`: 3 unsupported property elements.
  - `XAML0002`: 2 resource dictionary root handling failures.
  - `XAML1001`: 1 unsupported XAML element.
  - `XAML1006`: 1 unsupported event.

The supplemental downstream macOS runtime probe still passes:

```sh
tools/winui3-mac-runner run \
  --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj \
  --renderer skia-v2 \
  --scenario /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/shell-staff-light.json \
  --strict-visual \
  --output /private/tmp/emsi-windows-macruntime-probe-current
```

That probe is useful but intentionally narrow: signed-out shell visibility, staff sign-in, Home navigation, Admin visibility, and Admin route selection. It does not exercise the production pages, production resource dictionaries, real list templates, command content, password input, or full read-surface/workbench states.

## Gap Inventory

### By Downstream XAML Area

| Area | Diagnostics | Interpretation |
| --- | ---: | --- |
| Admin/workbench page | 27 | Command content, list selection, item templates, layout rows, status/progress sizing, and workbench layout are not production-ingestable. |
| Home/read-surface page | 19 | Shared read-surface layout, text entry, wrapping, and grid row attached properties are not covered. |
| Messages page | 16 | Same shared read-surface gaps plus multiline text entry. |
| Channels page | 13 | Shared read-surface layout and status/progress sizing gaps. |
| Notifications page | 13 | Shared read-surface layout and text/attached property gaps. |
| Settings page | 13 | Shared read-surface layout and text/attached property gaps. |
| Events page | 9 | Shared read-surface layout and status/progress sizing gaps. |
| Login page | 9 | `PasswordBox`, border/card properties, text wrapping, and scroll/grid layout gaps. |
| Theme dictionaries | 2 | Standalone resource dictionaries are not handled as XAML compile inputs without `x:Class`. |

### By Unique Unsupported Surface

| Surface | Count | Current treatment |
| --- | ---: | --- |
| `Grid.RowDefinitions` | 16 | Layout gap; current compiler only supports `Grid.ColumnDefinitions` and `Grid.ColumnSpacing`. |
| `Grid.Padding` | 8 | Layout gap; padding exists on `StackPanel` only, while production XAML uses it broadly. |
| `InfoBar.Grid.Row` | 7 | Attached property gap; compiler supports only `Grid.Column`. |
| `Grid.Grid.Row` | 7 | Attached property gap; same root cause as above. |
| `ScrollViewer.HorizontalScrollBarVisibility` | 7 | ScrollViewer property gap; vertical scrollbar exists, horizontal does not. |
| `ProgressRing.Width` | 7 | Common sizing property gap. |
| `ProgressRing.Height` | 7 | Common sizing property gap. |
| `Grid.MinHeight` | 7 | Common sizing property gap. |
| `Grid.MaxWidth` | 6 | Common sizing property gap. |
| `TextBlock.Grid.Row` | 5 | Attached property gap. |
| `TextBlock.Grid.ColumnSpan` | 5 | Attached property gap; column span semantics missing. |
| `Grid.RowSpacing` | 5 | Layout gap. |
| `StackPanel.Grid.Row` | 4 | Attached property gap. |
| `TextBox.TextWrapping` | 3 | Text stack gap. |
| `TextBox.MinHeight` | 3 | Common sizing property gap. |
| `TextBox.AcceptsReturn` | 3 | Text input behavior gap. |
| `InfoBar.IsClosable` | 2 | Status/control property gap. |
| resource dictionary root without `x:Class` | 2 | XAML compiler input-kind gap. |
| `ListView.ItemTemplate` | 1 | Planned template gap; current catalog treats `ItemsControl.ItemTemplate` as planned, while production uses concrete list templates. |
| `ItemsControl.ItemTemplate` | 1 | Planned template gap. |
| `CommandBar.Content` | 1 | Partial fixture evidence exists in runtime artifacts, but production XAML ingestion still fails. |
| `ListView.SelectionChanged` | 1 | List interaction event gap; `NavigationView.SelectionChanged` is supported, but `ListView.SelectionChanged` is not. |
| `StackPanel.Grid.ColumnSpan` | 1 | Attached property gap. |
| `NavigationView.Grid.Row` | 1 | Attached property gap. |
| `ListView.Grid.Row` | 1 | Attached property gap. |
| `TextBlock.TextWrapping` | 1 | Text stack gap. |
| `ListView.SelectionMode` | 1 | List selection model gap. |
| `ListView.IsItemClickEnabled` | 1 | List item interaction gap. |
| `Grid.SizeChanged` | 1 | Event/layout lifecycle gap; should be explicitly planned or diagnosed, not silently accepted. |
| `CommandBar.DefaultLabelPosition` | 1 | Command chrome property gap. |
| `Border.Padding` | 1 | Layout/card property gap. |
| `Border.MaxWidth` | 1 | Common sizing property gap. |
| `Border.BorderThickness` | 1 | Border visual property gap. |
| `Border.BorderBrush` | 1 | Border visual/resource property gap. |
| `PasswordBox` | 1 | Planned rich form input control gap. |

## Why These Gaps Exist

- The current release claim is the documented public source-level harness subset, not the full downstream production Windows app.
- `src/WinUI3.MacXaml/MacXamlCompiler.cs` has deliberately small allowlists. Examples: `Grid` supports `ColumnDefinitions` and `ColumnSpacing`, but not `RowDefinitions`, `RowSpacing`, `Padding`, `MinHeight`, or `MaxWidth`; attached properties include `Grid.Column`, but not `Grid.Row` or `Grid.ColumnSpan`.
- `src/WinUI3.MacCompat/ControlsFacade.cs` has useful partial controls, but missing or incomplete production semantics for `PasswordBox`, list selection events, item click, selection mode, item templates, horizontal scrolling, broader sizing, and several visual properties.
- Existing plans did include many of these surfaces, but as `planned`, diagnostic, or gap items. `PasswordBox`, `CommandBar.Content`, `ListView.ItemTemplate`, and `ItemsControl.ItemTemplate` appear in prior production/component plans. Layout and text-stack gaps appear under broader layout/resources/text stack readiness, not as completed work.
- The downstream `MeetingChallenge.WinUI.MacRuntimeProbe` intentionally avoids several production XAML features, so it can pass while production XAML ingestion fails.

## Assumptions And Open Questions

- The runtime should remain Wine-free and source-level only.
- The runtime should not execute `.exe`, `.msix`, Windows App SDK targets, or real Windows platform integrations on macOS.
- Public runtime fixtures must remain clean-room and generic. Private product content, private screenshots, secrets, endpoints, customer data, and proprietary names must remain in downstream/private evidence locations.
- The first target is compile-and-run source-level coverage for the downstream production XAML shape, not native-quality visual parity.
- Open question: should production XAML support be proven by a sanitized mirror fixture in this runtime repo, by a downstream app-owned probe fixture, or by both? This plan assumes both: clean-room fixtures for public runtime gates and downstream probes for app-specific validation.
- Open question: should some production-only properties such as `Grid.SizeChanged` stay explicit diagnostics instead of being supported? This plan treats them as a separate decision gate.

## Explicit Out Of Scope

- Pixel-perfect Fluent parity.
- Full WinUI 3 compatibility.
- Windows binary, `.exe`, `.msix`, packaged app, or Wine execution.
- Mica/Acrylic/compositor native rendering parity.
- Full `ControlTemplate`, broad `DataTemplate`, virtualization, and full UIA/FlaUI provider compatibility in the first pass.
- Publishing private downstream screenshots or production app source copies into the public runtime repository.

## Affected Files And Responsibilities

### Runtime Compiler And Catalog

- `src/WinUI3.MacXaml/MacXamlCompiler.cs`: expand supported elements, properties, property elements, events, attached properties, generated assignments, and resource dictionary input handling.
- `docs/compatibility/winui-api-compatibility.catalog.json`: add or promote catalog entries for newly supported public XAML surfaces.
- `docs/compatibility/all-catalog-readiness-audit.json`: regenerate after catalog changes.
- `docs/compatibility/matrix.md`: update support boundaries.
- `docs/compatibility/component-support.md`: update control support wording.

### Runtime Facades And Semantics

- `src/WinUI3.MacCompat/ControlsFacade.cs`: add/expand `PasswordBox`, layout/sizing properties, `ListView` selection and item click properties/events, `CommandBar` chrome properties, `ScrollViewer` horizontal scroll property, `InfoBar.IsClosable`, and template placeholder models.
- `src/WinUI3.MacCompat/XamlFacade.cs`: expand base `FrameworkElement` properties if sizing/padding belongs at the framework layer.
- `src/WinUI3.MacCompat/DataFacade.cs`: include new child/template/content surfaces in logical traversal where needed.
- `src/WinUI3.MacRuntime/UiTree.cs`: export new properties into deterministic `tree.json`.
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`: export semantics for protected password input, list selection, item click targets, status closability, and template-generated items where supported.
- `src/WinUI3.MacRuntime/InteractionScript.cs`: add supported list selection/item click actions only after facade events and tree semantics are stable.

### Layout, Renderer, And Evidence

- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`: implement row definitions, row spacing, row/column spans, sizing constraints, border padding, and template/list layout approximations.
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`: render `PasswordBox`, border brush/thickness/padding, multiline text boxes, command content, list selected/item-click states, status close affordance, and template/list rows at source-level fidelity.
- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`: add reusable drawing helpers only when needed by multiple controls.
- `src/WinUI3.MacRuntime/ComponentEvidence.cs`: keep component evidence honest; do not promote visual grades until crops exist and have been reviewed.
- `docs/visual-parity/component-quality-dashboard.json`: regenerate when public evidence changes.
- `docs/visual-parity/state-coverage-matrix.json`: add state rows for newly claimed states.
- `docs/visual-parity/native-quality-family-tranches.json`: keep native-quality blockers separate from source-level support.

### Fixtures And Tests

- `fixtures/ComponentParityLab.WinUI`: expand public clean-room scenarios for production-like password, command content, list template, row layout, text wrapping, multiline input, and status close affordance.
- `fixtures/PublicAdminWorkbench.WinUI`: add sanitized workbench coverage only for public-safe production patterns.
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`: add failing-then-passing compiler tests for each surface family.
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`: add facade, interaction, tree/accessibility, layout, renderer evidence, catalog, and dashboard tests.
- Downstream `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe`: expand app-owned probe scenarios after runtime support exists.
- Downstream `apps/windows/W1_FIRST_RENDER_EVIDENCE.md` or a new Windows evidence doc: record private/downstream production XAML diagnostic closure without publishing private screenshot artifacts into this runtime repository.

## Implementation Phases

### Phase 0: Baseline Guardrail And Gap Snapshot

Purpose: make the current downstream production XAML failure reproducible, grouped, and impossible to forget.

- [x] Add a runtime-owned public gap manifest, for example `docs/compatibility/downstream-production-xaml-gap-summary.json`, containing sanitized gap families, counts, and current treatment.
- [x] Add a runner command or test helper that can summarize diagnostics by file category and unsupported surface without committing private file paths.
- [x] Add a test in `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs` that asserts the gap summary matches the documented current surface families.
- [x] Add documentation to `docs/consumption/downstream-windows-apps.md` explaining how downstream apps should keep full production XAML diagnostics private while contributing sanitized gap summaries upstream.

Verification:

```sh
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
```

Expected result: tests pass, catalog audit remains current, and the gap summary documents the 35 current unsupported surface families.

### Phase 1: XAML Compiler Foundation For Layout And Common Properties

Purpose: remove the high-volume mechanical blockers before richer controls.

- [ ] Add compiler support for common framework sizing properties used by production pages: `Width`, `Height`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`, and `Padding` where the facade type can safely carry it.
- [ ] Add compiler support for `Grid.RowDefinitions`, `Grid.RowSpacing`, attached `Grid.Row`, and attached `Grid.ColumnSpan`.
- [ ] Add compiler support for `Border.Padding`, `Border.BorderBrush`, `Border.BorderThickness`, and `Border.MaxWidth`.
- [ ] Add compiler support for `ScrollViewer.HorizontalScrollBarVisibility`.
- [ ] Add compiler support for `ProgressRing.Width` and `ProgressRing.Height` through common sizing, not special-case one-off logic.
- [ ] Keep unsupported events such as `Grid.SizeChanged` explicit until Phase 6 decides whether to model them or keep them diagnostic.

Tests:

- [ ] `MacXamlCompilerTests` compiles a `Grid` with `RowDefinitions`, `RowSpacing`, `Grid.Row`, and `Grid.ColumnSpan`.
- [ ] `MacXamlCompilerTests` compiles a `Border` card with padding, brush, thickness, and max width.
- [ ] `MacXamlCompilerTests` compiles a `ScrollViewer` with both horizontal and vertical scrollbar visibility.
- [ ] `MacRuntimeTests` verifies the new layout/sizing properties appear in `tree.json` where visually relevant.

Verification:

```sh
dotnet test --filter MacXamlCompilerTests
dotnet test --filter MacRuntimeTests
```

Expected result: high-volume layout diagnostics are removed without accepting unsupported template or production-only lifecycle events.

### Phase 2: Resource Dictionary And Theme Input Handling

Purpose: compile production resource dictionaries as resource dictionaries, not as page/window roots requiring `x:Class`.

- [ ] Teach `MacXamlCompiler` to accept standalone `ResourceDictionary` roots without `x:Class` when invoked in resource-dictionary mode or when the root is unambiguously a resource dictionary.
- [ ] Preserve strict diagnostics for unsupported resource entries and property setters.
- [ ] Add catalog entries or readiness notes for standalone resource dictionary ingestion.
- [ ] Add tests for `Themes/Tokens.xaml`-style and `Themes/Components.xaml`-style resource dictionary inputs using clean-room fixture resources.

Tests:

- [ ] `MacXamlCompilerTests` compiles a root `ResourceDictionary` without `x:Class`.
- [ ] `MacXamlCompilerTests` rejects unsupported resource child shapes with clear diagnostics.
- [ ] `MacRuntimeTests` verifies `StaticResource` and `ThemeResource` lookup still reports missing resources deterministically.

Verification:

```sh
dotnet test --filter MacXamlCompilerTests
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-light.json \
  --strict-visual \
  --output /private/tmp/winui3-resource-phase2
```

Expected result: standalone theme dictionaries compile in the supported subset and existing resource diagnostics remain strict.

### Phase 3: Text And Form Controls

Purpose: close login and multiline input blockers.

- [ ] Add a `PasswordBox` facade with `Password`, placeholder/header metadata where needed, protected-text accessibility semantics, focus state, and a clear non-goal for real secure storage.
- [ ] Add compiler support for the `PasswordBox` element and its supported properties.
- [ ] Add `TextBlock.TextWrapping`, `TextBox.TextWrapping`, `TextBox.AcceptsReturn`, and `TextBox.MinHeight` support.
- [ ] Add source-level renderer support for masked password content and multiline text box bounds.
- [ ] Add public fixture rows for password default/focused/disabled/reveal-placeholder states without private labels or secrets.

Tests:

- [ ] `MacXamlCompilerTests` compiles a clean-room login panel with `PasswordBox`, wrapped helper text, and multiline `TextBox`.
- [ ] `MacRuntimeTests` verifies protected password text is not exported as plaintext in `tree.json` or `accessibility.json`.
- [ ] `MacRuntimeTests` verifies multiline text box metadata and wrapping state are exported.
- [ ] Component evidence test keeps `PasswordBox` out of `not-rendered` only after visual and accessibility evidence exist.

Verification:

```sh
dotnet test --filter "MacXamlCompilerTests|MacRuntimeTests"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json \
  --strict-visual \
  --output /private/tmp/winui3-text-forms-phase3
```

Expected result: login/form source-level surfaces compile and password content remains protected in artifacts.

### Phase 4: Lists, Selection, And Item Templates

Purpose: close list/detail and Admin workbench blockers without claiming full template parity.

- [ ] Add `ListView.SelectionMode`, `ListView.IsItemClickEnabled`, and `ListView.SelectionChanged` facade/compiler support.
- [ ] Add deterministic `SelectionChanged` invocation when script-driven selection changes.
- [ ] Add bounded `DataTemplate`, `ListView.ItemTemplate`, and `ItemsControl.ItemTemplate` support for simple text/control trees used by clean-room fixtures.
- [ ] Keep unsupported template constructs explicit: bindings beyond the supported subset, converters, visual states, nested complex templates, virtualization, and control templates must still fail with actionable diagnostics.
- [ ] Update `UiTree`, `AccessibilityTree`, and renderer output for template-generated list rows.

Tests:

- [ ] `MacXamlCompilerTests` compiles a simple `ListView.ItemTemplate` with a text/control tree.
- [ ] `MacXamlCompilerTests` rejects unsupported template constructs with `cataloged as planned` or precise unsupported diagnostics.
- [ ] `MacRuntimeTests` verifies selecting a list item updates `SelectedItem`, raises `SelectionChanged`, and exports selected accessibility state.
- [ ] `MacRuntimeTests` verifies `selectItem` or equivalent script actions produce before/after state.

Verification:

```sh
dotnet test --filter "ListView|ItemTemplate|Interaction"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-collections-light.json \
  --strict-visual \
  --output /private/tmp/winui3-collections-phase4
```

Expected result: simple production-like list/detail screens are source-ingestable and scriptable, with explicit diagnostics for unsupported template complexity.

### Phase 5: CommandBar Content And Command Chrome

Purpose: reconcile existing clean-room `CommandBar.Content` evidence with production XAML ingestion.

- [ ] Add compiler support for `CommandBar.Content` property elements and `CommandBar.DefaultLabelPosition`.
- [ ] Confirm `ControlsFacade.CommandBar.Content` is set by generated XAML, not treated as a primary command child.
- [ ] Update `DataFacade`, `ElementQuery`, `UiTree`, `VisualLayoutEngine`, and `SkiaV2SnapshotRenderer` so command content is laid out before primary commands and exported distinctly.
- [ ] Add tests using clean-room command/search content without committing private downstream labels or endpoint names.

Tests:

- [ ] `MacXamlCompilerTests` compiles a `CommandBar` with `CommandBar.Content` and primary commands.
- [ ] `MacRuntimeTests.VisualLayoutEnginePlacesCommandBarContentBeforePrimaryCommands` remains passing and is expanded for generated XAML.
- [ ] Component evidence for command content stays `usable` only with visible crop evidence and documented gaps.

Verification:

```sh
dotnet test --filter "CommandBar|MacXamlCompilerTests"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json \
  --strict-visual \
  --output /private/tmp/winui3-commandbar-phase5
```

Expected result: production command/search layout compiles as source-level XAML and stays visibly inspectable.

### Phase 6: Status, Progress, And Lifecycle Decision Gate

Purpose: support the production status/progress properties that are safe, and explicitly decide unsupported lifecycle events.

- [ ] Add `InfoBar.IsClosable` support and tree/accessibility export.
- [ ] Ensure `ProgressRing` size properties flow through the common sizing implementation from Phase 1.
- [ ] Decide `Grid.SizeChanged`: either model it as a no-op event with clear limitations or keep it as a documented unsupported lifecycle event.
- [ ] If modeled, add handler hookup tests and make clear that real WinUI layout lifecycle timing is not claimed.
- [ ] If not modeled, add an explicit catalog entry and downstream doc note so it is not mistaken for an uncataloged gap.

Tests:

- [ ] `MacXamlCompilerTests` compiles `InfoBar IsClosable`.
- [ ] `MacRuntimeTests` verifies status close affordance metadata appears in artifacts.
- [ ] A dedicated test asserts the chosen `Grid.SizeChanged` behavior.

Verification:

```sh
dotnet test --filter "InfoBar|ProgressRing|SizeChanged"
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-light.json \
  --strict-visual \
  --output /private/tmp/winui3-status-phase6
```

Expected result: status/progress source surfaces compile and the lifecycle boundary is intentional.

### Phase 7: Downstream Probe Expansion

Purpose: prove the runtime now covers the production XAML shape through app-owned probes without copying private app content into the runtime.

- [ ] Expand downstream app-owned macOS runtime probes to cover sanitized route groups: login, read-surface shared page, messages multiline input, admin workbench list/detail, command/search region, and status/error/loading states.
- [ ] Add scenario JSON for each route group with stable automation IDs and deterministic sample data.
- [ ] Keep screenshots and screenshot-like artifacts out of the main runtime repository; use downstream/private QA evidence handling rules.
- [ ] Add a downstream evidence doc update recording before/after diagnostic counts and remaining gaps.

Verification from monorepo root or downstream `apps/windows` as appropriate:

```sh
dotnet build apps/windows/src/MeetingChallenge.Core/MeetingChallenge.Core.csproj
dotnet run --project apps/windows/tests/MeetingChallenge.Core.Tests/MeetingChallenge.Core.Tests.csproj
dotnet run --project apps/windows/tests/MeetingChallenge.SmokeTests/MeetingChallenge.SmokeTests.csproj
```

Supplemental runtime probe command shape:

```sh
cd apps/windows
dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
  --project tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj \
  --renderer skia-v2 \
  --scenario tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/shell-staff-light.json \
  --strict-visual \
  --output artifacts/winui3-mac/meeting-challenge-windows-shell-staff-light
```

Expected result: downstream probe scope increases beyond shell-only while staying source-level and public-safe.

### Phase 8: Release Gate Integration And Support Boundary Update

Purpose: promote only completed surfaces and keep native-quality claims honest.

- [ ] Update `docs/compatibility/matrix.md`, `docs/compatibility/component-support.md`, `docs/compatibility/production-component-targets.md`, and `docs/consumption/downstream-windows-apps.md`.
- [ ] Regenerate catalog audit, component dashboard, state coverage matrix, native-quality family tranches, and visual review index as needed.
- [ ] Run strict scenario sweep and public product evidence.
- [ ] Keep native-quality family tranches blocked unless inspected native-quality evidence exists.
- [ ] Document remaining gaps with exact diagnostics, not broad "unsupported" language.

Verification:

```sh
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
```

Expected result: release evidence distinguishes source-level downstream production XAML coverage from native-quality visual fidelity.

## Verification Gates

Minimum gate per implementation PR:

```sh
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
```

Expanded gate after visual/runtime changes:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-drift-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index --check
```

Downstream private/app-owned gate:

```sh
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo
dotnet build apps/windows/src/MeetingChallenge.Core/MeetingChallenge.Core.csproj
dotnet run --project apps/windows/tests/MeetingChallenge.Core.Tests/MeetingChallenge.Core.Tests.csproj
dotnet run --project apps/windows/tests/MeetingChallenge.SmokeTests/MeetingChallenge.SmokeTests.csproj
```

Production XAML diagnostic closure gate:

```sh
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Expected final result for this plan: zero diagnostics for the intentionally supported production XAML subset, with any remaining unsupported surfaces cataloged, documented, and excluded intentionally.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Whitelist-only fixes create false confidence. | Every supported surface must include facade/runtime behavior, tree/accessibility export where relevant, tests, and docs. |
| Public runtime repo leaks downstream/private content. | Use clean-room fixtures upstream; keep app-owned probes and screenshot evidence downstream/private. |
| Template support balloons into full WinUI template parity. | Support only simple bounded templates first; keep complex templates diagnostic. |
| Layout support diverges from real WinUI. | Treat source-level layout as deterministic scaffold; require Windows reference evidence for visual claims. |
| Native-quality claims creep into source-level coverage. | Keep `nativeQualityGrade` separate and `not-evaluated` until manual native crop inspection supports promotion. |
| Downstream production XAML changes during execution. | Re-run the diagnostic closure gate at each phase and update the sanitized gap summary. |

## Dependencies And Ownership Boundaries

- Runtime owner: compiler, compat facade, renderer, public clean-room fixtures, docs, and runtime release gates.
- Windows app owner: production XAML, app-owned probes, Windows validation, QA screenshots, and milestone evidence.
- Backend/API owner: only needed if expanded downstream probes exercise live API calls; this plan prefers deterministic sample data for macOS runtime probes.
- QA evidence owner: screenshot-like artifacts belong outside this runtime repo according to downstream Windows QA evidence rules.

## Rollback And Recovery

- Revert individual surface promotions by restoring catalog status to `planned` or `partial`, removing claimed fixture evidence, and keeping diagnostics explicit.
- If a renderer promotion causes visual drift, keep compiler/facade support if source-level behavior is correct but demote visual grade and document the renderer blocker.
- If downstream production XAML diagnostics reveal private-only constructs that cannot be mirrored publicly, record a sanitized gap family and keep detailed evidence in downstream/private docs.

## Execution Prompt

Implement the plan saved at `tools/winui3-mac-test-runtime/docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`.

Use these skills before editing: `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-verification-gate`, `superpowers:test-driven-development`, and either `superpowers:subagent-driven-development` for independent phase work or `superpowers:executing-plans` for inline execution.

Read guidance files first: `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, and `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime/README.md`.

Work on the current branch. Do not create, switch, rename, delete, or otherwise perform branch operations unless the user explicitly asks for that branch action. Follow the repo commit policy: after successful verification, commit only task-relevant files when commit guards pass; do not push unless push guards pass and the user allows it.

Start with Phase 0, then execute phases in order. For each phase, write failing tests first, implement the minimal supported surface, run the phase-specific verification commands, update docs/evidence artifacts, and keep unsupported surfaces explicit rather than silently accepted.

Required final verification before claiming completion:

```sh
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix --check
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches --check
tools/winui3-mac-runner xaml compile --output /private/tmp/emsi-windows-prod-xaml-generated /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

If the final production XAML compile still emits diagnostics, report the remaining surfaces grouped by file category and unsupported API/property; do not claim full downstream Windows app coverage.
