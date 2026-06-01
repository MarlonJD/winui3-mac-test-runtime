# Production Windows Component Completion Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `docs/compatibility`, `docs/release`,
`docs/visual-parity`, `src/WinUI3.MacCompat`, `src/WinUI3.MacRuntime`,
`src/WinUI3.MacXaml`, `src/WinUI3.MacRenderer.Skia`, `src/WinUI3.MacRunner`,
`fixtures`, `tests`, `.github/workflows`

## Goal

Move the public `winui3-mac-test-runtime` from an alpha fixture-validation
harness to a production-capable source-level WinUI test runtime for the
operator's production Windows application component subset.

The production target is not "all WinUI 3". The production target is:

- every WinUI component used by the operator's production Windows application is
  cataloged without committing private names, screenshots, source, secrets, or
  proprietary data;
- every claimed supported component in that subset has public clean-room fixture
  coverage, native WinUI Windows reference provenance, macOS renderer coverage,
  interaction coverage, accessibility export coverage, and smoke or E2E test
  coverage;
- unsupported or deferred WinUI behavior fails loudly through structured
  diagnostics instead of being mistaken for working parity;
- no component visual grade is promoted by changing thresholds or hiding failed
  evidence.

## Current Evidence Baseline

The native Windows reference source-of-truth blocker is closed for the public
fixture set:

- Public workflow: `windows-native-screenshot.yml`
- Inspected run: `26777029415`
- Inspected commit: `95e8d7d`
- Reference source: `native-winui`
- Runner image: `win25 20260525.149.1`
- Viewport: `1028x720`
- Capture mode: client area

The current production blocker is no longer "we do not know what native WinUI
shows". The blocker is "the macOS runtime does not render or interact with
enough production components yet".

Latest checked-in component inventory status:

| Visual evidence grade | Count |
| --- | ---: |
| `usable` | 8 |
| `not-rendered` | 90 |

Latest inspected native comparison examples:

| Scenario | Native comparison | Component evidence |
| --- | --- | --- |
| `public-admin-workbench-light` | Failed: `100.00%` changed pixels, MAE `9.72`, RMS `35.87`. | Public admin/workbench still diverges heavily from native WinUI. |
| `component-basic-input-light` | Failed: `42.07%` changed pixels over the `18%` threshold, MAE `9.92`, RMS `38.84`. | 13 `not-rendered`. |
| `component-commands-menus-light` | Failed: `40.68%` changed pixels over the `24%` threshold, MAE `8.45`, RMS `35.23`. | 8 `not-rendered`. |
| `component-layout-media-light` | Failed: `45.83%` changed pixels over the `24%` threshold, MAE `10.48`, RMS `39.27`. | 4 `usable`, 24 `not-rendered`. |

This evidence must stay honest. These failures are useful production planning
data, not noise to threshold away.

## Assumptions

- The macOS runtime remains Wine-free and source-level only.
- The public repository cannot contain private repositories, private screenshots,
  private product names, private API names, secrets, production customer data, or
  proprietary fixture content.
- The operator can provide a private component usage inventory out of band. The
  repository will commit only a sanitized public mapping from required WinUI
  component families to clean-room fixture scenarios.
- Native WinUI Windows references come from public GitHub Actions Windows runs
  that launch public clean-room fixture projects.
- `WindowsNativeProbe` remains smoke-only evidence and cannot satisfy a native
  WinUI production reference requirement.
- Production smoke and E2E maturity means deterministic local runs, public CI
  runs, structured artifacts, and clear failure diagnostics. It does not require
  running the private production application inside this public repository.

## Scope

- Component inventory, support contracts, public clean-room fixture coverage,
  renderer completion, interaction scripting, accessibility export, native
  reference evidence, smoke tests, E2E-style scenario tests, CI gates, and docs.
- The first production ring is the operator production Windows app component
  subset, represented only through sanitized public component categories and
  clean-room fixture scenarios.
- The second ring is the broader WinUI controls inventory already represented by
  `ComponentParityLab.WinUI`.

## Non-Goals

- Do not add Wine, arbitrary Windows binary execution, `.exe` execution, `.msix`
  execution, or real Windows App SDK execution on macOS.
- Do not use private downstream source code, private screenshots, private
  product names, private fixture content, secrets, or copied WinUI Gallery
  content.
- Do not claim complete WinUI 3, Windows App SDK, WebView2, media playback, ink,
  full compositor, Mica, Acrylic, or pixel-perfect Fluent parity unless public
  tests and native references prove the claim.
- Do not make broad renderer rewrites before the production component inventory
  and fixture requirements are locked.
- Do not loosen thresholds to convert failed native comparisons into passing
  evidence.
- Do not promote `weak`, `poor`, or `not-rendered` component grades until
  public native-WinUI provenance exists and macOS output has been inspected.

## Production Gaps To Close

### Gap 1: Sanitized Production Component Inventory

There is no committed, sanitized inventory that maps the operator's production
Windows application component usage to public fixture scenarios.

Exit criteria:

- Add `docs/compatibility/production-component-targets.md` or equivalent
  sanitized inventory doc.
- Each required component family has:
  - component name;
  - current catalog status;
  - public fixture scenario;
  - required interaction coverage;
  - required visual grade;
  - accessibility requirements;
  - smoke or E2E coverage target;
  - explicit production priority.
- The inventory contains no private app names, source paths, screenshots,
  secrets, private data, or proprietary identifiers.

### Gap 2: Core Control Chrome Is Missing Or Text-Only

Production-critical input controls currently have behavior metadata but weak,
text-only, or absent macOS visuals.

Priority components:

- `Button`
- `ToggleButton`
- `CheckBox`
- `RadioButton`
- `TextBox`
- `ComboBox`
- `Slider`
- `ToggleSwitch`
- `PasswordBox`
- `NumberBox`
- `AutoSuggestBox`

Exit criteria:

- Each required control has a facade behavior test, layout test, renderer test,
  public fixture scenario, component evidence entry, and native WinUI reference.
- Default, hover, pressed, focused, disabled, checked, selected, placeholder, and
  invalid states are represented where applicable.
- Component grade is at least `usable` for production-ring controls.

### Gap 3: Command, Menu, And Flyout Surfaces Are Not Production-Ready

Command-heavy Windows apps depend on native command surfaces. The current
`CommandBar` and `AppBarButton` evidence is mostly logical behavior, not native
visual or popup parity.

Priority components:

- `CommandBar`
- `CommandBar.Content`
- `AppBarButton`
- `AppBarButton.Icon`
- `MenuBar`
- `MenuFlyout`
- `CommandBarFlyout`
- `Context menu pattern`
- `DropDownButton`
- `SplitButton`
- `ToggleSplitButton`
- `ToolTip`
- `ToolTipService.SetToolTip`

Exit criteria:

- Public fixtures show visible command chrome, icon slots, labels, overflow or
  disabled states when required, and menu/flyout open states.
- Scenario scripts can open commands, invoke menu items, dismiss flyouts, and
  assert resulting state.
- Keyboard accelerators and access-key metadata are exported for supported
  command paths.

### Gap 4: Navigation, Lists, And Workbench Patterns Need Real Layout Parity

The public admin/workbench comparison currently fails heavily. Production cannot
depend on list/detail screens until navigation, list selection, pane sizing, and
detail layout are stable.

Priority components and patterns:

- `NavigationView`
- `NavigationViewItem`
- `NavigationView.MenuItems`
- `NavigationView.PaneFooter`
- `Frame`
- `Page`
- `ListView`
- `ItemsControl`
- `ItemsRepeater`
- `GridView`
- `DataTemplate`
- `ListView.ItemTemplate`
- `ItemsControl.ItemTemplate`
- list/details pattern
- search/filter/empty state pattern

Exit criteria:

- Workbench fixture renders a recognizable navigation pane, queue/list region,
  detail region, command region, status region, and selected state.
- Selection, navigation, filtering, and detail updates are scriptable and
  verified through artifacts.
- List and item-template visuals are at least `usable` for production-ring
  scenarios.

### Gap 5: Layout, Resources, Theme, And Text Stack Are Too Narrow

Current layout/media/resource evidence still has many `not-rendered` entries.
Production screens will not be stable without theme-aware resources and
deterministic layout.

Priority areas:

- `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl`
- `StaticResource`, `ThemeResource`, `ResourceDictionary.ThemeDictionaries`
- `Style`, `Setter`, `XamlControlsResources`
- `Color`, `SolidColorBrush`, `CornerRadius`
- typography, wrapping, trimming, alignment, padding, margin, spacing
- light, dark, and high-contrast themes
- scale factors and window resizing

Exit criteria:

- Fixture screenshots stay stable at production viewport sizes and scale factors.
- Theme changes affect supported brushes and resources without manual forks.
- Missing resources and unsupported setters produce strict diagnostics.
- Text does not collapse, overlap, or become invisible in production-ring
  fixture scenarios.

### Gap 6: Status, Progress, Dialog, And Picker Workflows Are Partial

Smoke and E2E tests need realistic state transitions, decisions, validation, and
progress feedback.

Priority components:

- `InfoBar`
- `ProgressBar`
- `ProgressRing`
- `ContentDialog`
- `Flyout`
- `TeachingTip`
- `DatePicker`
- `TimePicker`
- `CalendarDatePicker`
- `PersonPicture`
- loading, empty, error, denied, disabled, success, warning states

Exit criteria:

- Scenario scripts can trigger and assert dialog/flyout/status lifecycles.
- State meaning is not color-only and appears in accessibility artifacts.
- Supported states have deterministic visual and interaction evidence.

### Gap 7: Smoke And E2E Harness Maturity Is Not Enough Yet

The runner has scripted actions, but production needs a stable workflow for
launch, navigation, interactions, assertions, artifacts, and failure triage.

Exit criteria:

- Add a documented production smoke suite:
  - app launch;
  - navigation;
  - primary command invocation;
  - form entry;
  - list selection;
  - status/error display;
  - dialog or flyout open/close;
  - artifact generation.
- Add a documented production E2E suite:
  - multi-step workbench flow;
  - data-bound form flow;
  - command/menu flow;
  - list/detail flow;
  - theme/scale/resizing flow.
- Every action emits structured pass/fail details with element names, expected
  state, actual state, and screenshot/artifact links.
- Flaky waits are replaced with deterministic idle, layout, binding, and
  render-completion gates.

### Gap 8: Production Operations Are Not Closed

The project still needs production release hardening beyond component parity.

Exit criteria:

- Production compatibility contract is explicit.
- Package release policy, versioning policy, rollback policy, and support policy
  are documented.
- Security and supply-chain review covers local source builds, dependency
  policy, artifact privacy, and safe CI usage.
- Performance and flake metrics are recorded for runner startup, XAML compile,
  scenario execution, rendering, and artifact generation.

## Production Component Rings

### Ring 0: Production Smoke Foundation

These must be finished first because nearly every production smoke or E2E path
depends on them:

| Area | Required coverage |
| --- | --- |
| App shell | `Application`, `Window`, `Page`, `Frame`, stable title, activation, deterministic startup. |
| Layout | `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl`, sizing, padding, margin, alignment. |
| Text | `TextBlock`, text wrapping, trimming, alignment, opacity, enabled/disabled state. |
| Basic commands | `Button`, `AppBarButton`, `CommandBar`, click, command, enabled state. |
| Forms | `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, validation and focus states. |
| Workbench | `NavigationView`, `NavigationViewItem`, `ListView`, selected state, list/detail updates. |
| Status | `InfoBar`, `ProgressBar`, `ProgressRing`, loading, warning, error, success states. |
| Artifacts | `tree.json`, `accessibility.json`, `visual-run.json`, `component-evidence.json`, pixel diff artifacts. |

### Ring 1: Production E2E Enablers

These unlock realistic desktop user flows:

| Area | Required coverage |
| --- | --- |
| Menus and flyouts | `MenuBar`, `MenuFlyout`, `CommandBarFlyout`, `Context menu pattern`, `DropDownButton`, `SplitButton`. |
| Rich form input | `PasswordBox`, `NumberBox`, `AutoSuggestBox`, `Slider`, `ToggleSwitch`. |
| Dialog decisions | `ContentDialog`, `Flyout`, `TeachingTip`, `ToolTip`. |
| Templates and collections | `DataTemplate`, `ItemsControl.ItemTemplate`, `ListView.ItemTemplate`, `ItemsRepeater`, `GridView`. |
| Theme and resources | `ThemeResource`, `StaticResource`, `Style`, `Setter`, theme dictionaries, light/dark/high contrast. |
| Keyboard and accessibility | Tab order, accelerators, focus visuals, roles, names, checked/selected/value states. |

### Ring 2: Production Polish And Broader Compatibility

These should be handled after Ring 0 and Ring 1 are stable:

| Area | Required coverage |
| --- | --- |
| Advanced controls | `TabView`, `TreeView`, `BreadcrumbBar`, `Expander`, `RatingControl`, `PersonPicture`, `ColorPicker`, date/time controls. |
| Materials and depth | Mica/Acrylic/system backdrop support tier, shadows, transforms, reduced motion. |
| Media and platform integration | `MediaPlayerElement`, `WebView2`, ink, clipboard, launcher, platform APIs, if explicitly in production scope. |
| Performance | Benchmarks, memory tracking, artifact size budgets, flake-rate dashboards. |

## Implementation Milestones

### Milestone 0: Lock The Sanitized Production Inventory

Goal: define the production component target without leaking private content.

Steps:

- Create a sanitized production component target document in
  `docs/compatibility/`.
- Map every operator-required component family to a public clean-room fixture
  scenario or a new scenario to be added.
- Add a `productionPriority` field or equivalent table that separates Ring 0,
  Ring 1, Ring 2, and explicit non-goals.
- Add a private-content safety checklist to the plan or compatibility docs.
- Do not inspect or commit private screenshots, source files, product names, or
  production data.

Verification gate:

```sh
git diff --check
rg -n "<operator-private-name-denylist-regex>" .
```

Milestone exit criteria:

- Every production-ring component has an owner scenario and verification target.
- The committed inventory is sanitized and public.
- No code changes are required in this milestone unless docs generation needs a
  small helper.

Commit:

- `docs(compatibility): define production component targets`

### Milestone 1: Add Fixture Coverage For Missing Production Components

Goal: make every Ring 0 and Ring 1 component visible in public fixtures on real
Windows before renderer work begins.

Steps:

- Extend `ComponentParityLab.WinUI` with clean-room pages or states for missing
  production components.
- Add scenario files for default, focused, disabled, selected, checked, invalid,
  loading, open-popup, and command-invoked states where applicable.
- Ensure fixture windows launch by scenario name, set the stable capture title,
  activate the window, and navigate to the requested page/state.
- Update component inventory metadata with presence, interaction requirement,
  expected minimum grade, and known gaps.
- Keep public admin/workbench fixture coverage aligned with realistic
  production workbench patterns, but with generic public data.

Verification gate:

```sh
dotnet build fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj
dotnet build fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
```

Workflow gate:

- Run `windows-native-screenshot.yml` only after the local fixture scenarios
  launch and write complete artifacts.
- Download and inspect native Windows references for newly visible fixture
  states.

Milestone exit criteria:

- Real Windows captures prove the fixture pages show the expected native WinUI
  components.
- Missing macOS renderer output remains marked honestly as `not-rendered`.

Commit:

- `test(fixtures): cover production component targets`

### Milestone 2: Finish Ring 0 Renderer And Layout Support

Goal: make the production smoke foundation visually recognizable and stable.

Steps:

- Implement or complete `skia-v2` painters for Ring 0 components:
  `Button`, `AppBarButton`, `CommandBar`, `TextBox`, `ComboBox`, `CheckBox`,
  `RadioButton`, `NavigationView`, `NavigationViewItem`, `ListView`,
  `ItemsControl`, `InfoBar`, `ProgressBar`, `ProgressRing`, `Grid`,
  `StackPanel`, `Border`, `ScrollViewer`, `Image`, and `FontIcon`.
- Preserve current SVG and current Skia behavior while improving `skia-v2`.
- Add layout measurements for pane/list/detail surfaces, command bars, form
  rows, status banners, and scrollable content.
- Add renderer-gap diagnostics for unsupported visual features.
- Add component-region evidence so a whole-screen threshold cannot hide a
  missing control.

Verification gate:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-status-pickers-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
```

Milestone exit criteria:

- Ring 0 production components are no longer text-only or absent in macOS
  output.
- Ring 0 components reach at least `usable` only after native reference
  provenance and artifact inspection support the grade.
- No broad threshold loosening.

Commit:

- `feat(renderer): render production smoke components`

### Milestone 3: Finish Ring 1 Interactions, Menus, And Flyouts

Goal: make command-heavy and form-heavy production flows testable end to end.

Steps:

- Add facade/runtime support for missing Ring 1 controls where production
  scenarios require them.
- Implement menu/flyout open, item invocation, dismissal, and disabled-state
  behavior in the scenario action model.
- Implement keyboard focus traversal, accelerator routing, text input, selection
  changes, combo popup selection, and scroll actions for supported controls.
- Export structured interaction reports with expected and actual state.
- Add accessibility metadata for roles, names, values, checked state, selected
  state, expanded/collapsed state, and enabled state.

Verification gate:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-dialogs-flyouts-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-text-forms-light.json --strict-visual
```

Milestone exit criteria:

- Ring 1 smoke scenarios can open commands, menus, flyouts, dialogs, and form
  controls, then assert resulting state.
- Accessibility artifacts are complete enough for smoke and E2E assertions.
- Any unsupported menu/flyout control remains explicitly planned or excluded.

Commit:

- `feat(runtime): support production command interactions`

### Milestone 4: Finish Templates, Resources, Themes, And State Styling

Goal: make production fixtures resilient to real WinUI styling patterns.

Steps:

- Implement the documented subset of `DataTemplate`, `ControlTemplate`,
  `VisualStateManager`, `StaticResource`, `ThemeResource`,
  `ResourceDictionary.ThemeDictionaries`, `Style`, and `Setter` required by the
  production component target.
- Add supported brushes, colors, corner radius, thickness, padding, typography,
  opacity, visibility, and disabled/selected/focused state setters.
- Add light, dark, and high-contrast scenario coverage.
- Keep unsupported templates and resources strict and diagnostic-rich.

Verification gate:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-dark.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-high-contrast.json --strict-visual
```

Milestone exit criteria:

- Theme and style behavior is deterministic across light, dark, and high
  contrast.
- Resource and template gaps are strict diagnostics, not silent visual loss.
- Production-ring list and form templates reach the required evidence grade.

Commit:

- `feat(xaml): support production theme resources`

### Milestone 5: Add Production Smoke And E2E Scenario Suites

Goal: make the runtime mature enough to test real desktop workflows represented
by public clean-room scenarios.

Steps:

- Add `fixtures/ProductionSmoke.WinUI` or extend existing public fixtures with a
  smoke suite that covers launch, navigation, commands, forms, lists, status,
  dialogs, theme, scale, and artifact generation.
- Add E2E-style scenario JSON files for:
  - workbench navigation and selection;
  - command/menu invocation;
  - form edit and validation;
  - list/detail update;
  - loading/error/success state transition;
  - dialog/flyout decision;
  - theme and scale regression.
- Add deterministic `waitForIdle`, `waitForLayout`, `waitForBinding`, and
  `waitForRender` gates if they do not already exist.
- Ensure each E2E scenario exports enough artifacts to debug failures without
  needing private app access.

Verification gate:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
```

Milestone exit criteria:

- Production smoke and E2E public scenarios pass locally.
- Failures produce actionable JSON artifacts and PNGs.
- Scenario scripts are documented for downstream consumers.

Commit:

- `test(e2e): add production smoke scenarios`

### Milestone 6: Refresh Native References And Promote Only Proven Grades

Goal: update public evidence from real native WinUI references after the local
runtime can render and test the production component subset.

Steps:

- Run local gates first. Do not trigger the Windows workflow until local macOS
  artifacts show meaningful component output.
- Trigger `windows-native-screenshot.yml` on the public repository.
- Download artifacts and inspect:
  - `windows-reference.png`;
  - provenance JSON;
  - `mac-runtime.png`;
  - `pixel-diff.png`;
  - `pixel-diff.json`;
  - `visual-run.json`;
  - `component-evidence.json`.
- Inspect all Ring 0 and Ring 1 scenarios, including:
  - `public-admin-workbench-light`;
  - `component-basic-input-light`;
  - `component-text-forms-light`;
  - `component-collections-light`;
  - `component-dialogs-flyouts-light`;
  - `component-commands-menus-light`;
  - `component-navigation-workbench-light`;
  - `component-status-pickers-light`;
  - `component-layout-media-light`;
  - production smoke and E2E scenarios.
- Promote component grades only when `referenceSource` is `native-winui` and
  inspected macOS output supports the grade.

Verification gate:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot-production-components
```

Milestone exit criteria:

- Native WinUI provenance exists for every production-ring visual claim.
- Component evidence has no unexpected `not-rendered` grades in Ring 0.
- Ring 1 unsupported behavior is either fixed, explicitly excluded, or left as a
  documented production blocker.
- Synthetic probe evidence remains smoke-only.

Commit:

- `docs(visual): publish production component evidence`

### Milestone 7: Close Production Readiness Docs And Release Gates

Goal: make the production claim precise, supportable, and auditable.

Steps:

- Update `README.md`, `docs/visual-parity/README.md`,
  `docs/release/production-readiness.md`, `docs/compatibility/matrix.md`, and
  `docs/compatibility/component-support.md`.
- Add or update:
  - production support policy;
  - compatibility tiers;
  - known unsupported list;
  - smoke and E2E usage guide;
  - artifact inspection guide;
  - release checklist;
  - security and supply-chain notes;
  - flake and performance reporting.
- Keep docs clear that the support claim applies to the public sanitized
  production component subset, not arbitrary WinUI 3 applications.

Verification gate:

```sh
dotnet build
dotnet test
git diff --check
rg -n "<operator-private-name-denylist-regex>" .
```

Milestone exit criteria:

- Production readiness blockers PB-001 through PB-012 are closed, scoped out in
  a documented support tier, or remain explicitly open with owner and exit
  criteria.
- PB-000 remains closed because native WinUI reference provenance is preserved.
- The final handoff lists provenance status, comparison metrics,
  weak/poor/not-rendered components, smoke/E2E status, and residual risks.

Commit:

- `docs(release): define production readiness status`

## Verification Policy

Use this policy for every milestone:

- Run the smallest local check that proves the edited behavior.
- Run `dotnet build` and `dotnet test` before any production evidence or release
  docs milestone.
- Run `winui3-mac-doctor` whenever runner, package, fixture, or user-facing
  setup behavior changes.
- Run strict visual scenarios for every fixture page touched.
- Trigger `windows-native-screenshot.yml` only after local artifacts are
  meaningful enough to inspect.
- Download and inspect workflow artifacts before changing docs or grades.
- Preserve failed artifacts and record honest status when native comparison
  fails.
- Do not tune thresholds without a documented before/after artifact review.
- Commit and push only the milestone's relevant files before starting the next
  milestone.

## Production Definition Of Done

The project is production-ready for the operator production Windows component
subset only when all of the following are true:

- The sanitized production component inventory is committed and current.
- Every Ring 0 component has public fixture coverage, native WinUI provenance,
  macOS renderer coverage, interaction coverage, accessibility coverage, and
  smoke coverage.
- Ring 0 has no unexpected `not-rendered`, `poor`, or unreviewed `weak`
  component grades.
- Ring 1 components needed for production E2E flows are either implemented and
  tested or explicitly excluded from the production support contract.
- Public smoke and E2E scenario suites run locally and in CI with structured
  artifacts.
- Native Windows reference artifacts exist for every promoted visual claim.
- Synthetic probe output is labeled smoke-only and is not used as parity
  evidence.
- Performance, flake, package, security, and support policies are documented.
- Public docs state the exact supported subset and do not imply arbitrary WinUI
  3 or Windows binary compatibility.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Private product details leak into public docs or fixtures. | Commit only sanitized component categories and clean-room fixture data; run the operator denylist before every milestone commit. |
| The production component list grows into "all WinUI 3". | Use Ring 0 and Ring 1 as the production contract; move broader controls to Ring 2 unless they are proven production requirements. |
| Whole-screenshot metrics hide missing components. | Use `component-evidence.json`, component-region evidence, and manual artifact inspection before grade promotion. |
| Threshold changes hide regressions. | Require documented artifact review and keep failed evidence when native comparison fails. |
| Fixture behavior diverges from real WinUI. | Native Windows references must launch the public fixture project and record `referenceSource: native-winui`. |
| Renderer work breaks existing SVG/current Skia paths. | Keep changes scoped to documented paths and run existing smoke gates. |
| Flaky waits make E2E tests unreliable. | Add deterministic idle/layout/binding/render gates before expanding E2E coverage. |
| Material/compositor features are too expensive for the first production gate. | Scope them explicitly as unsupported, approximated, or Ring 2 unless the production inventory requires them. |

## Rollback And Recovery

- If a renderer milestone regresses existing scenarios, revert or isolate only
  that milestone's relevant files and keep previous evidence intact.
- If native workflow capture fails because of runner drift, preserve local
  artifacts and do not promote grades until a successful native run is
  inspected.
- If private content is detected, remove it before commit and replace it with
  generic fixture content.
- If a Ring 1 component cannot be completed without broad architecture changes,
  document it as a production blocker or explicitly exclude it from the first
  production support tier.
- If smoke or E2E scenarios become flaky, stop adding scenarios and fix the
  deterministic wait or interaction primitive first.

## Affected Files And Docs

Likely affected areas:

- `README.md`
- `docs/compatibility/component-support.md`
- `docs/compatibility/matrix.md`
- `docs/compatibility/winui-component-inventory.json`
- `docs/compatibility/production-component-targets.md`
- `docs/release/production-readiness.md`
- `docs/visual-parity/README.md`
- `docs/visual-parity/examples/`
- `.github/workflows/windows-native-screenshot.yml`
- `fixtures/ComponentParityLab.WinUI/`
- `fixtures/PublicAdminWorkbench.WinUI/`
- `fixtures/ProductionSmoke.WinUI/`
- `src/WinUI3.MacCompat/`
- `src/WinUI3.MacRuntime/`
- `src/WinUI3.MacXaml/`
- `src/WinUI3.MacRenderer.Skia/`
- `src/WinUI3.MacRunner/`
- `tests/`

## Execution Prompt

Use `$google-eng-practices` and `$windows-winui3-design` to execute `docs/plans/2026-06-01-production-windows-component-completion-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. The objective is to make the Wine-free macOS source-level WinUI runtime production-capable for the operator's production Windows application component subset, represented only through sanitized public component categories and clean-room public fixtures.

Do not use private repositories, private screenshots, private source code, private product names, secrets, proprietary fixture content, copied WinUI Gallery content, or private data. Keep identifiers, source comments, and canonical docs in English. Preserve `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current Skia, `skia-v2`, existing fixtures, public admin/workbench source ingestion, `ComponentParityLab.WinUI`, native WinUI reference provenance, and `WindowsNativeProbe` as labeled smoke-only evidence.

Start with Milestone 0: create the sanitized production component target inventory under `docs/compatibility/`, mapping every operator-required component family to a public clean-room fixture scenario, current catalog status, required interaction coverage, required visual grade, accessibility requirement, smoke or E2E target, and production priority. Do not commit private names or private evidence. Run `git diff --check` and `rg -n "<operator-private-name-denylist-regex>" .` with the operator-provided denylist before committing. Commit only the milestone's relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately.

Then continue milestone by milestone. For each milestone, make only the relevant fixture, runtime, renderer, test, workflow, or documentation changes; run the verification gate listed in the plan; inspect generated artifacts; preserve honest failures; do not loosen thresholds to hide native comparison failures; do not promote component grades until the artifact provenance says `referenceSource: native-winui` and the macOS output has been inspected. Trigger `windows-native-screenshot.yml` only after local artifacts show meaningful component output, then download and inspect `windows-reference.png`, provenance JSON, `mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`, `visual-run.json`, and `component-evidence.json` for the production-ring scenarios.

At the end of each completed milestone, commit only that milestone's relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately before starting the next milestone. Final handoff must summarize native reference provenance status, comparison metrics, weak/poor/not-rendered components, production smoke status, E2E status, remaining production blockers, and whether PB-000 remains closed.
