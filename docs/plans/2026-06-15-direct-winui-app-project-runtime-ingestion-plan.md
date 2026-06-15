# Direct WinUI App Project Runtime Ingestion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `winui3-mac-test-runtime` accept a real WinUI Windows application project directly and render, automate, and integration-test it on macOS without requiring downstream apps to add a custom probe or host project.

**Architecture:** The runtime owns project ingestion, source-level host generation, Windows-only boundary isolation, XAML/page selection, deterministic scenario execution, semantic automation, and screenshot artifacts. A user should be able to point `WinUI3.MacRunner` at a real `*.csproj` such as `MeetingChallenge.Windows.csproj`; the runner creates a temporary macOS-compatible source-level harness outside the app repo, compiles supported WinUI source/XAML against the clean-room facades, runs the selected route/page, executes integration-test actions, and emits tree/accessibility/interactions/screenshot evidence. Native Windows remains the reference tier through FlaUI 5.0 + UIA3 and Windows capture tooling; macOS exposes a compatible semantic automation contract over runtime artifacts, not a native Windows UIA provider.

**Tech Stack:** .NET 10, MSBuild project inspection, WinUI facade runtime, `WinUI3.MacRunner`, `WinUI3.MacXaml`, `WinUI3.MacRuntime`, `WinUI3.MacRenderer.Skia`, MSTest, source-level generated temporary host projects, runner interaction scripts, `tree.json`, `accessibility.json`, `interactions.json`, Windows `FlaUI 5.0 + FlaUI.UIA3`, and `tools/WindowsWindowCapture`.

---

Date: 2026-06-15

Owner subtree: `tools/winui3-mac-test-runtime`

Downstream validation target: `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`

Status: active

Supersedes: `docs/plans/2026-06-15-meetingchallenge-production-source-runtime-host-plan.md`

Related plans:

- `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`
- `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`

## Objective

The product should not require every Windows app to create a
`MacRuntimeProbe` or app-owned test host. The runtime itself should be able to
open a real WinUI Windows app project in a source-level test environment, run
scripted UI/integration tests against it, and produce automation artifacts that
can be compared with native Windows UI Automation evidence.

For EMSI, the target command shape should become:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
tools/winui3-mac-runner run \
  --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
  --renderer skia-v2 \
  --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
```

If the direct project contains Windows-only app bootstrap, package deployment,
WinRT storage, PasswordVault, Mica, or shell-only behavior, the runner should
not ask the downstream app to create a probe. Instead it should:

- inspect the project;
- identify renderable WinUI source/XAML surfaces;
- generate a temporary source-level host in `/private/tmp`;
- isolate or explicitly diagnose Windows-only pieces;
- render the selected shell/page route through the runtime;
- produce `tree.json`, `accessibility.json`, `unsupported-apis.json`,
  `interactions.json`, `visual-run.json`, and `mac-runtime.png`.

## Automation And Integration Testing Target

Direct app ingestion is only half the product. The complete target is:

```text
real WinUI app .csproj
  -> runtime-owned temporary source-level host on macOS
  -> semantic automation scenario
  -> tree/accessibility/interactions/screenshot artifacts
  -> optional native Windows FlaUI/UIA3 reference run for the same scenario
```

### Native Windows Reference Tools

Use these for real Windows behavior:

- `FlaUI 5.0 + FlaUI.UIA3`: native Windows UI Automation reference validation
  against real WinUI windows.
- `tools/WindowsWindowCapture`: existing native Windows process/window capture
  tool for client-area screenshots and reference metadata.
- `.github/workflows/windows-native-screenshot.yml`: public Windows reference
  workflow for native fixture screenshots.
- `.github/workflows/windows-downstream-probe-screenshot.yml`: current EMSI
  downstream probe reference capture; useful precedent, but direct ingestion
  should not depend on downstream probe projects.

### macOS Runtime Automation Tools

Use these locally on macOS:

- `InteractionScriptRunner`: existing semantic action runner.
- `tree.json`: logical tree, stable names, visibility, selected/focused state,
  control properties, and layout metadata.
- `accessibility.json`: role/name/automation ID/label/help/focus/focusable/
  enabled/checked/selected/expanded/value contract.
- `interactions.json`: action results with selector, expected/actual,
  target type, observed state, and before/after state.
- `visual/mac-runtime.png`: screenshot evidence from `skia-v2`.
- future `FlaUI.UIA3-compatible artifact adapter`: repo-owned adapter over
  `tree.json`, `accessibility.json`, and `interactions.json`, not a native macOS
  UIA provider claim.

### Shared Scenario Contract

The scenario format must support both render and integration test intent:

```json
{
  "name": "meetingchallenge-shell-home-light",
  "theme": "light",
  "entry": {
    "mode": "window",
    "xaml": "MainWindow.xaml",
    "route": "home",
    "session": "staff"
  },
  "automation": [
    { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
    { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
    { "type": "waitForIdle" },
    { "type": "assertProperty", "target": "ContentFrame", "key": "CurrentRoute", "parameter": "messages" }
  ],
  "visual": {
    "capture": true,
    "renderer": "skia-v2"
  }
}
```

Existing interaction action types are the starting point: `click`, `focus`,
`typeText`, `selectItem`, `selectNavigation`, `navigateFrame`,
`invokeAccelerator`, `openPopup`, `dismissPopup`, `invokeMenuItem`,
`waitForIdle`, `assertProperty`, and `assertAccessibilityState`.

## Product Boundary

### The Runtime Should Do

- Accept real WinUI app `.csproj` paths in `winui3-mac-runner run --project`.
- Detect Windows app project shape: target framework, `UseWinUI`,
  `WindowsPackageType`, `ApplicationDefinition`, `Page`, `ResourceDictionary`,
  and project references.
- Build a temporary generated source-level host outside the app repository.
- Link or transform supported app XAML and source files into the temporary host.
- Generate deterministic host bootstrap code for macOS runtime execution.
- Let scenarios select an entry window/page/route and provide deterministic
  session/data fixtures where needed.
- Execute integration-test actions through the existing scenario/script
  interaction pipeline.
- Emit automation-ready `tree.json`, `accessibility.json`, and
  `interactions.json` with stable selectors and schema versions.
- Provide a FlaUI/UIA3-compatible artifact adapter layer before making any
  claim that existing Windows UI test code can run unchanged against macOS
  runtime output.
- Keep private screenshots and pixel diffs outside the repo.
- Produce honest diagnostics for unsupported Windows-only boundaries.

### The Runtime Should Not Claim

- Native execution of `.exe`, `.msix`, or packaged Windows App SDK binaries on
  macOS.
- Full `MeetingChallenge.Windows.csproj` build as a macOS app.
- WinRT storage, PasswordVault, deployment, activation, native UIA provider,
  Mica/Acrylic, Windows shell, or packaged resource behavior.
- Arbitrary FlaUI/UIA compatibility on macOS before API-level adapter tests
  exist.
- Full pointer/keyboard/IME behavior parity before those interactions have
  explicit runtime support and tests.
- Native Windows pixel parity without a Windows reference screenshot.

### Downstream Apps Should Not Have To Do

- Create `MeetingChallenge.WinUI.MacRuntimeProbe`-style projects just to get a
  screenshot.
- Copy production XAML into a test harness.
- Add macOS-specific project files for routine runtime rendering.

Downstream apps may still supply optional scenario files, deterministic fixture
data, or adapters when their production code requires private services. Those
inputs should be optional and documented, not the core product path.

## Current EMSI Baseline

Already verified before this plan:

- `tools/winui3-mac-runner xaml compile` succeeds for
  `apps/windows/src/MeetingChallenge.Windows/**/*.xaml`.
- `/private/tmp/emsi-windows-prod-xaml-generated.diagnostics.json` contains
  `[]`.
- The current screenshot is from
  `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe`, which is a
  smoke/probe harness and not the desired product path.
- `MeetingChallenge.Windows.csproj` is a real WinUI Windows app project using:
  - `TargetFramework=net10.0-windows10.0.19041.0`
  - `UseWinUI=true`
  - `WindowsPackageType=MSIX`
  - `Microsoft.WindowsAppSDK`
  - `Microsoft.Windows.SDK.BuildTools`
  - production `MainWindow.xaml`, `Pages/*.xaml`, `Themes/*.xaml`, storage,
    resources, and app bootstrap code.

The direct ingestion path should use this project as a validation target, but
the reusable implementation must live in `tools/winui3-mac-test-runtime`.

## Assumptions And Open Questions

- The runner may generate temporary projects under `/private/tmp` and delete or
  overwrite them between runs.
- Direct ingestion can start with XAML-first rendering and limited code-behind
  execution, then expand toward source-code execution as facades mature.
- Scenarios may provide route selection and deterministic state. This does not
  make the downstream app create a custom host; it only supplies runtime input.
- For code-behind that touches Windows-only APIs, the runner should either
  source-transform the call behind a generated adapter, replace it with a
  diagnostic no-op facade, or stop with a precise unsupported-boundary report.
- Open question: first implementation should prefer `MainWindow.xaml` shell
  rendering with generated route/data adapters, or a direct `Page` render mode
  for `Pages/HomePage.xaml`. The plan starts with both modes in order: page mode
  for RED/GREEN simplicity, then shell route mode.

## Affected Runtime Files

Likely additions:

- `src/WinUI3.MacRunner/ProjectIngestion/WinUIProjectInspector.cs`
- `src/WinUI3.MacRunner/ProjectIngestion/WinUIProjectModel.cs`
- `src/WinUI3.MacRunner/ProjectIngestion/GeneratedHostWriter.cs`
- `src/WinUI3.MacRunner/ProjectIngestion/GeneratedHostOptions.cs`
- `src/WinUI3.MacRunner/ProjectIngestion/WindowsOnlyBoundaryClassifier.cs`
- `src/WinUI3.MacRunner/ProjectIngestion/ScenarioHostOptions.cs`
- `src/WinUI3.MacRunner/Automation/AutomationScenarioModel.cs`
- `src/WinUI3.MacRunner/Automation/AutomationContractReport.cs`
- `src/WinUI3.MacRunner/Automation/FlaUIArtifactAdapter.cs`
- `src/WinUI3.MacRunner/Automation/NativeWindowsAutomationPlan.cs`
- `tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj`
- `tests/WinUI3.MacRunner.Tests/ProjectIngestionTests.cs`
- `tests/WinUI3.MacRunner.Tests/AutomationContractTests.cs`
- `tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj`
- `tools/WindowsUiAutomationProbe/Program.cs`
- `fixtures/RealWinUIAppFixture/RealWinUIAppFixture.csproj`
- `fixtures/RealWinUIAppFixture/MainWindow.xaml`
- `fixtures/RealWinUIAppFixture/Pages/HomePage.xaml`
- `fixtures/RealWinUIAppFixture/Themes/Tokens.xaml`
- `fixtures/RealWinUIAppFixture/Themes/Components.xaml`
- `fixtures/RealWinUIAppFixture/scenarios/shell-home-light.json`

Likely modifications:

- `src/WinUI3.MacRunner/Program.cs`
- `src/WinUI3.MacRunner/RunCommand.cs` or equivalent command handling file
- `src/WinUI3.MacRunner/XamlCompileCommand.cs`
- `src/WinUI3.MacXaml/MacXamlCompiler.cs`
- `src/WinUI3.MacCompat/*Facade.cs`
- `src/WinUI3.MacRuntime/*`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- compatibility docs/catalogs only when support status changes
- `docs/architecture/artifacts.md`
- `docs/consumption/quick-start.md`
- `docs/consumption/downstream-windows-apps.md`
- `README.md`

Do not create or modify `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost`.

## Implementation Phases

### Phase 0: Reframe The Gate And Preserve Current Baseline

Purpose: make the goal impossible to confuse with probe screenshots or Windows
binary execution.

- [ ] Read:
  - `README.md`
  - `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`
  - `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`
- [ ] Run the existing XAML compile gate:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  tools/winui3-mac-runner xaml compile \
    --output /private/tmp/emsi-windows-prod-xaml-generated \
    /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
  ```

  Expected current result: exit 0 and diagnostics `[]`.

- [ ] Run the existing smoke probe only as a comparison baseline:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
    --project tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj \
    --renderer skia-v2 \
    --scenario tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/shell-staff-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-probes/shell-staff-light
  ```

  Expected current result: passed, but it remains a probe baseline.

- [ ] Add or update docs wording so the next gate is named
  `direct WinUI app project ingestion`.

Acceptance:

- The plan and execution logs state that the target is `run --project <real app
  csproj>`, not a downstream-created host.

### Phase 1: Project Inspector RED/GREEN

Purpose: teach the runner to identify real WinUI app project shape before
trying to render it.

- [ ] Add a failing test in `tests/WinUI3.MacRunner.Tests` that inspects a
  fixture `.csproj` with `UseWinUI=true`, `WindowsPackageType=MSIX`,
  `ApplicationDefinition`, `Page`, `ResourceDictionary`, and project
  references.
- [ ] If `tests/WinUI3.MacRunner.Tests` does not exist yet, create a minimal
  MSTest project that references `src/WinUI3.MacRunner` before adding the first
  failing test. Keep the test project focused on runner/project-ingestion and
  automation contract behavior; do not fold these tests into renderer or XAML
  compiler suites.
- [ ] Verify RED: the inspector type or behavior does not exist.
- [ ] Implement `WinUIProjectInspector` and `WinUIProjectModel` with:
  - project path;
  - root directory;
  - target framework;
  - `UseWinUI`;
  - `WindowsPackageType`;
  - package references;
  - project references;
  - app XAML;
  - page XAML files;
  - resource dictionary XAML files;
  - content assets.
- [ ] Verify GREEN with the fixture.
- [ ] Add a second test against the real EMSI project path when present:
  `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`.
  If absent in another checkout, skip with a clear message rather than failing.

Acceptance:

- The runner can classify a real WinUI app project without building it.
- No downstream app changes are needed.

### Phase 2: Generated Temporary Host RED/GREEN

Purpose: generate a macOS source-level harness inside `/private/tmp` from a
real app project.

- [ ] Add a failing test for `GeneratedHostWriter` that writes a temporary
  project containing:
  - a generated `App.xaml`;
  - a generated bootstrap `App.xaml.cs`;
  - links to selected source project XAML files;
  - references to runtime facade assemblies;
  - no writes into the source app directory.
- [ ] Verify RED.
- [ ] Implement generated host writing under:

  `/private/tmp/winui3-mac-test-runtime/generated-hosts/<stable-project-id>/`

- [ ] Ensure output paths are deterministic enough for debugging but safe to
  overwrite.
- [ ] Verify GREEN.

Acceptance:

- Direct app ingestion creates a temporary host without asking the app repo for
  a probe project.

### Phase 3: Shared Automation Scenario Contract

Purpose: make render scenarios and integration-test scenarios one contract
instead of separate one-off formats.

- [x] Add a failing test in `tests/WinUI3.MacRunner.Tests` that parses a direct
  app scenario containing `entry`, `automation`, and `visual` sections.
- [x] Verify RED.
- [x] Implement `AutomationScenarioModel` that maps scenario `automation`
  entries to existing `InteractionAction` values without losing selector kind,
  target, key, modifiers, page type, or parameter.
- [x] Extend runner command handling so scenario `automation` actions are
  executed exactly like `--script` actions after the direct app host is loaded.
- [x] Verify `interactions.json` is emitted for scenario automation, not only
  for explicit `--script`.
- [x] Add documentation that the shared scenario is the public integration-test
  input for both macOS runtime evidence and optional native Windows reference
  validation.

Acceptance:

- A direct app scenario can describe route/page setup, UI automation actions,
  assertions, and visual capture in one file.
- Existing `InteractionScriptRunner` remains the execution engine for macOS
  runtime actions.

2026-06-15 Track B update:

- Added direct scenario `entry`, `automation`, and `visual` parsing to the
  shared `VisualScenario` contract while preserving existing `interactions`
  scenarios.
- Scenario `automation` actions now run through `InteractionScriptRunner` and
  emit `interactions.json`; the runtime accepts scenario contract aliases such
  as `selected=true` while keeping semantic action results explicit.

### Phase 4: Page Render And Interaction Mode

Purpose: get the first direct screenshot from a real app project by rendering a
selected production page XAML and running integration assertions against it.

- [x] Extend scenario support with a direct page selector, for example:

  ```json
  {
    "name": "meetingchallenge-home-page-light",
    "theme": "light",
    "entry": {
      "mode": "page",
      "xaml": "Pages/HomePage.xaml"
    },
    "automation": [
      { "type": "assertAccessibilityState", "target": "automationId=home-title", "key": "role", "parameter": "text" },
      { "type": "waitForIdle" }
    ],
    "visual": {
      "capture": true,
      "renderer": "skia-v2"
    }
  }
  ```

- [x] Add a failing runner test that uses the public fixture app and expects the
  generated host to render the selected page.
- [x] Verify RED.
- [x] Implement the minimum page entry generation.
- [x] Run scenario automation and assert that `interactions.json` records passed
  assertions for the selected page.
- [x] Close only reusable compiler/facade/layout/renderer gaps with separate
  RED/GREEN tests.
- [x] Run the real EMSI direct page command:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  tools/winui3-mac-runner run \
    --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
    --renderer skia-v2 \
    --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-home-page-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-direct/home-page-light
  ```

Acceptance:

- A real production `HomePage.xaml` source-level render exists, or the exact
  reusable runtime blocker is documented.
- The same run emits `tree.json`, `accessibility.json`, and `interactions.json`.

2026-06-15 Track B page update:

- Direct page entry generation passed against `Pages/HomePage.xaml` from
  `MeetingChallenge.Windows.csproj`.
- Evidence was emitted under
  `/private/tmp/emsi_qa/windows/mac-runtime-direct/home-page-light`, including
  `tree.json`, `accessibility.json`, `interactions.json`, and
  `visual/mac-runtime.png`.

### Phase 5: Shell Route Render And Integration Mode

Purpose: render the real app shell/window shape and run navigation-level
integration tests, not only a standalone page screenshot.

- [x] Extend scenario support with a shell selector:

  ```json
  {
    "name": "meetingchallenge-shell-home-light",
    "theme": "light",
    "entry": {
      "mode": "window",
      "xaml": "MainWindow.xaml",
      "route": "home",
      "session": "staff"
    },
    "automation": [
      { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
      { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
      { "type": "waitForIdle" },
      { "type": "assertAccessibilityState", "target": "automationId=shell-nav-messages", "key": "selected", "parameter": "true" }
    ],
    "visual": {
      "capture": true,
      "renderer": "skia-v2"
    }
  }
  ```

- [x] Add failing tests for shell route generation against the fixture app.
- [x] Verify RED.
- [x] Implement generated shell bootstrap that can:
  - instantiate the app window source-level;
  - choose the selected navigation item or content route;
  - inject deterministic state through generated runtime adapters;
  - skip or diagnose Windows-only startup services.
- [x] Reuse runtime support for `Frame.Navigate`, navigation selection,
  resource dictionaries, `x:Uid`, and page parameters when possible.
- [x] Run the real EMSI direct shell command:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  tools/winui3-mac-runner run \
    --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
    --renderer skia-v2 \
    --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
  ```

Acceptance:

- The screenshot path is produced by direct ingestion of
  `MeetingChallenge.Windows.csproj`, not by `MeetingChallenge.WinUI.MacRuntimeProbe`.
- The run produces integration-test evidence in `interactions.json`.

2026-06-15 Track B shell update:

- Direct shell/window route generation passed against `MainWindow.xaml` from
  `MeetingChallenge.Windows.csproj`; the generated host is under
  `/private/tmp/winui3-mac-test-runtime/generated-hosts/` and the downstream app
  was not modified.
- Evidence was emitted under
  `/private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light`, including
  `tree.json`, `accessibility.json`, `interactions.json`, and
  `visual/mac-runtime.png`. The five shell automation steps passed in
  `interactions.json`.
- `project-ingestion.json` reports `isShadowBuild=false`, lists selected render
  inputs (`MainWindow.xaml` plus theme dictionaries), and keeps
  `WindowsPackageType=MSIX` as a Windows packaging boundary rather than a macOS
  compile claim.

### Phase 6: FlaUI/UIA3-Compatible Artifact Adapter

Purpose: let integration tests use a Windows-automation-shaped API over macOS
runtime artifacts before claiming any broader FlaUI support.

- [x] Add failing tests for `FlaUIArtifactAdapter` that load
  `tree.json`, `accessibility.json`, and `interactions.json` and expose:
  - lookup by automation ID;
  - lookup by name;
  - role/control-type mapping;
  - enabled/focused/selected/checked/expanded state;
  - value/text state;
  - bounding rectangle/layout state when available;
  - action-result lookup by selector.
- [x] Verify RED.
- [x] Implement the minimum adapter model. It may be runtime-owned and
  FlaUI-shaped without depending on actual FlaUI packages on macOS.
- [x] Add a compatibility report command or artifact section that states which
  FlaUI/UIA concepts are supported by the artifact adapter and which remain
  unsupported.
- [x] Keep wording strict: this is not a native macOS UIA provider.

Acceptance:

- A test can assert UIA-like automation state against macOS runtime artifacts.
- The adapter has explicit unsupported diagnostics for missing UIA concepts.

2026-06-15 Track C update:

- Added a runtime-owned, FlaUI/UIA3-shaped artifact adapter in
  `src/WinUI3.MacRunner/Automation/` (`FlaUIArtifactAdapter`,
  `ArtifactAutomationElement`, `AutomationContractReport`). It loads
  `tree.json` (layout/bounds), `accessibility.json` (required state), and
  `interactions.json` (action results) over a runtime artifact directory.
- Supported FlaUI/UIA-shaped concepts: lookup by automation ID/name/selector,
  control-type mapping (WinUI type preferred, accessibility role fallback),
  `IsEnabled`, `HasKeyboardFocus`, `IsKeyboardFocusable`,
  `SelectionItemPattern.IsSelected`, `TogglePattern.ToggleState`,
  `ExpandCollapsePattern.ExpandCollapseState`, `ValuePattern.Value`, help text,
  `BoundingRectangle` (when layout is present), child/descendant tree
  navigation, and action-result lookup by selector.
- Explicitly unsupported (named in the compatibility report): native macOS UIA
  provider, unchanged FlaUI/UIA3 test execution, pattern method invocation,
  real pointer/keyboard input, UIA event subscriptions, window handles/process
  attachment, live re-query, `TextPattern`, `Grid`/`Table` patterns,
  `RangeValuePattern`, and screenshot capture.
- New CLI command `automation-adapter-report --artifacts <dir> [--output <dir>]`
  emits `automation-adapter-report.json` (compatibility) and
  `automation-parity.json` (macOS-only parity). Verified against
  `/private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light`
  (15 supported / 11 unsupported concepts; 5 actions passed on macOS, Windows
  reference not run). This is an artifact adapter only; it does not run `.exe`,
  `.msix`, GitHub workflows, or a native UIA provider.

### Phase 7: Native Windows FlaUI/UIA3 Reference Probe

Purpose: define and validate the optional Windows reference side for the same
scenario contract.

- [x] Add a Windows-only tool plan or implementation for
  `tools/WindowsUiAutomationProbe`.
- [x] Use `FlaUI 5.0 + FlaUI.UIA3` to launch or attach to a native WinUI app,
  execute the same scenario automation where possible, and emit:
  - `native-automation.json`;
  - `windows-reference.png` when screenshot capture is requested;
  - `windows-reference.json` provenance;
  - failed action diagnostics.
- [x] Reuse `tools/WindowsWindowCapture` for client-area screenshots instead of
  rewriting capture code.
- [x] Add Windows-only tests or workflow checks that build the tool without
  running it on macOS.
- [x] Do not run GitHub workflows during normal local implementation unless the
  user explicitly asks.

Acceptance:

- Native Windows UIA/FlaUI validation is the reference tier for direct app
  scenarios.
- The macOS runtime artifact adapter and native Windows probe speak the same
  scenario vocabulary.

2026-06-15 Track D update:

- Added Windows-only native reference probe project at
  `tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj`. macOS builds
  compile a non-Windows runner that emits skipped diagnostics without launching
  or attaching to `.exe`/`.msix`; Windows-host builds include `FlaUI.Core 5.0.0`
  and `FlaUI.UIA3 5.0.0` for native UIA attach/launch execution.
- Added shared native automation plan/report models under
  `src/WinUI3.MacRunner/Automation/NativeWindowsAutomationPlan.cs`. The probe
  consumes the same scenario `automation` entries used by direct macOS
  ingestion and maps them into native UIA command kinds.
- Supported native action mappings: `click` -> invoke, `focus` -> focus,
  `typeText` -> value set, `selectItem` / `selectNavigation` -> selection or
  invoke, `invokeAccelerator` -> planned keyboard accelerator command,
  `waitForIdle` -> idle settle, and `assertAccessibilityState` -> recorded UIA
  state assertion.
- Unsupported or skipped native actions are reported explicitly:
  `navigateFrame`, `assertProperty`, `openPopup`, `dismissPopup`, and
  `invokeMenuItem`; keyboard accelerator injection is planned but skipped by the
  first native executor until input injection semantics are validated.
- Probe artifacts are separate from macOS runtime artifacts:
  `native-automation.json` (`schemaVersion: 0.1`,
  `referenceSource: native-windows-uia3-flaui`), `windows-reference.json`
  (`referenceSource: native-winui`, either delegated
  `WindowsWindowCapture` provenance or skipped capture provenance), and optional
  `windows-reference.png` when scenario visual capture is requested.
- Verified locally without native Windows execution:
  `dotnet build tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj`,
  `dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "NativeWindowsAutomationPlan|AutomationParity"`,
  and `dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj`.

### Phase 8: Automation Parity Report

Purpose: compare macOS runtime artifact evidence with optional native Windows
automation evidence.

- [x] Add a report builder that consumes:
  - macOS `tree.json`;
  - macOS `accessibility.json`;
  - macOS `interactions.json`;
  - optional Windows `native-automation.json`; *(Track D)*
  - optional Windows screenshot provenance. *(Track D)*
- [x] Report each scenario action as:
  - passed on macOS;
  - failed on macOS;
  - skipped on macOS due to unsupported runtime boundary;
  - passed on Windows reference; *(Track D)*
  - failed on Windows reference; *(Track D)*
  - not run on Windows.
- [x] Keep screenshot diff and automation parity separate.
- [x] Add a public fixture test for the report shape.

2026-06-15 Track C update (macOS-only report shape):

- `AutomationParityReport.FromActions` builds the macOS-only parity report over
  the adapter's loaded artifacts. Each action maps to `PassedOnMac`,
  `FailedOnMac`, or `SkippedOnMac`, and the Windows reference column is fixed at
  `NotRunOnWindows`. The `AutomationParityStatus` vocabulary already includes
  `PassedOnWindows`/`FailedOnWindows` for the Track D native reference tier.
- Parity output is JSON-only (`automation-parity.json`) and stays separate from
  screenshot/pixel-diff evidence. Public fixture tests
  (`AutomationParityReportTests`) lock the report shape and counts.
- Consuming Windows `native-automation.json` and screenshot provenance is left
  to Track D (Native Windows FlaUI/UIA3 Reference Probe).

Acceptance:

- The final direct ingestion run can say not only "screenshot exists" but also
  "these integration actions passed/failed/skipped."

### Phase 9: Windows-Only Boundary Diagnostics

Purpose: make direct ingestion trustworthy by being precise about what is and is
not executed.

- [ ] Add `WindowsOnlyBoundaryClassifier` tests for:
  - `Windows.Storage.ApplicationData`;
  - `Windows.Security.Credentials.PasswordVault`;
  - packaged app activation;
  - `Window.SystemBackdrop` / `MicaBackdrop`;
  - Windows App SDK deployment/package references.
- [ ] Verify RED.
- [ ] Implement diagnostics that appear in `unsupported-apis.json` or a
  dedicated `project-ingestion.json`.
- [ ] Ensure these diagnostics do not block renderable XAML/page output unless
  the selected route truly needs the unsupported behavior.

Acceptance:

- Direct ingestion can say: "I rendered this supported source-level surface; I
  skipped or diagnosed these Windows-only boundaries."

### Phase 10: Verification And Documentation

Purpose: make the feature usable by other WinUI app owners.

- [ ] Update runtime docs:
  - `README.md`;
  - `docs/consumption/quick-start.md`;
  - `docs/consumption/downstream-windows-apps.md`;
  - compatibility docs only if support status changes.
- [ ] Add CLI help examples for direct app project ingestion.
- [ ] Run targeted verification:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp"
  dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "AutomationContract|FlaUIArtifactAdapter|AutomationParity"
  dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
  dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
  dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
  dotnet build tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj
  ```

- [ ] Run the real EMSI direct ingestion gate:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  tools/winui3-mac-runner run \
    --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
    --renderer skia-v2 \
    --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
  ```

- [ ] Report:
  - exact screenshot path;
  - whether it came from direct app project ingestion;
  - integration action pass/fail/skip counts;
  - automation artifact paths;
  - unsupported Windows-only boundary diagnostics;
  - remaining renderer/layout/facade gaps;
  - whether native Windows reference comparison was run or skipped.

Acceptance:

- A user can point the runtime at a real WinUI Windows app project and get a
  macOS source-level runtime screenshot and integration-test evidence without
  adding a custom probe project to their app.

## Execution Tracks

This is too large for one implementation prompt. Execute it in tracks and commit
each completed track when verification passes.

### Track A: Direct Project Ingestion Foundation

Scope:

- Phase 0
- Phase 1
- Phase 2

Done when:

- real WinUI app `.csproj` inspection works;
- generated temporary host creation works;
- no downstream probe/host project is required.

### Track B: Direct Render With Runtime Interactions

Scope:

- Phase 3
- Phase 4
- Phase 5

Done when:

- direct page and shell scenarios can run automation actions;
- `tree.json`, `accessibility.json`, `interactions.json`, and `mac-runtime.png`
  are emitted from direct project ingestion.

### Track C: UIA/FlaUI-Compatible Artifact Adapter

Scope:

- Phase 6
- Phase 8 report shape for macOS-only evidence

Done when:

- runtime artifacts can be queried through a UIA/FlaUI-shaped adapter;
- action/state parity is reportable without claiming native UIA provider
  support on macOS.

### Track D: Native Windows Reference Automation

Scope:

- Phase 7
- optional Windows side of Phase 8

Done when:

- native Windows `FlaUI 5.0 + UIA3` probe can execute the same scenario contract
  where Windows is available;
- Windows screenshot capture still flows through `tools/WindowsWindowCapture`.

### Track E: Documentation And Final Product Gate

Scope:

- Phase 9
- Phase 10

Done when:

- docs explain direct ingestion, integration scripts, artifact adapter limits,
  Windows reference validation, and remaining exclusions;
- EMSI direct app ingestion gate reports screenshot and automation evidence.

## Verification Gates

Minimum runtime gates:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp"
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "AutomationContract|FlaUIArtifactAdapter|AutomationParity"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
dotnet build tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj
```

Production XAML diagnostic gate:

```bash
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Direct real app ingestion gate:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
tools/winui3-mac-runner run \
  --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
  --renderer skia-v2 \
  --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
```

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Direct ingestion accidentally claims Windows binary execution. | Use "source-level runtime render" wording everywhere; never claim `.exe`/`.msix` execution. |
| The generated host becomes app-specific EMSI code hidden in the runtime. | Build project ingestion against public fixtures first, then validate EMSI as a downstream target. |
| Windows-only APIs block all rendering. | Classify boundaries and allow renderable surfaces to proceed with explicit diagnostics. |
| Code-behind support balloons into full WinUI/WinRT emulation. | Start XAML/page-first, add facades only when reusable and test-proven. |
| macOS artifact adapter is mistaken for native UIA provider support. | Use "FlaUI/UIA3-compatible artifact adapter" wording and keep unsupported native provider diagnostics explicit. |
| Integration tests become screenshot-only again. | Treat `interactions.json` and automation parity reports as required evidence beside `mac-runtime.png`. |
| Native Windows automation cannot run locally on macOS. | Build Windows tools locally only when possible; keep actual FlaUI/UIA3 execution optional and Windows-runner scoped. |
| Private screenshots leak into the repo. | Keep screenshot/diff output under `/private/tmp/emsi_qa` or approved QA storage. |
| Visual output is mistaken for native Windows parity. | Run native reference comparison only with explicit external reference evidence and report skipped comparison honestly. |

## Rollback And Recovery

- If project ingestion cannot support direct shell mode yet, keep page mode as
  the first deliverable and document shell mode as the next blocker.
- If automation cannot execute a scenario action yet, emit an explicit skipped
  or unsupported action result instead of hiding the gap behind screenshot
  success.
- If FlaUI/UIA3 adapter work is too large for the direct render track, complete
  Track B first and leave Track C active.
- If EMSI direct ingestion exposes a Windows-only blocker, keep the public
  fixture support and report the exact unsupported boundary.
- If a generated host change affects existing probe runs, revert only the
  generated host integration and keep the inspector/tests.
- If native comparison is unavailable, report it as skipped and leave source-
  level render evidence intact.

## Execution Prompt

Implement Track A from the plan saved at `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Work in `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime`. The overall product goal is direct app project ingestion plus automation/integration testing: users should be able to run `winui3-mac-runner run --project <real WinUI Windows app csproj>` on macOS and get source-level tree/accessibility/interactions/screenshot evidence without adding a custom downstream probe or host project. Track A only covers project inspection and generated temporary host creation. Use `MeetingChallenge.Windows.csproj` only as the downstream validation target. Do not create `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost`; changes should live in `tools/winui3-mac-test-runtime` except for read-only validation against `apps/windows`.

Use `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-verification-gate`, `superpowers:test-driven-development`, and either `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Read `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, `README.md`, `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`, and `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md` before editing.

Implement only these phases:

1. Add project inspection for real WinUI app `.csproj` files.
2. Add generated temporary source-level host creation under `/private/tmp`.
3. Update `project-ingestion.json` documentation for the generated host output.

For every runner/project-ingestion behavior change, write a failing targeted test first, verify RED, implement the minimum support, then verify GREEN. Do not make the full `MeetingChallenge.Windows.csproj` macOS build the implementation goal. Do not run `.exe` or `.msix`. Do not run GitHub workflows. Do not push. Do not copy private PNG/screenshot/pixel-diff artifacts into the repo. Keep source-level runtime render, direct project ingestion, semantic automation, and native Windows visual/UIA parity as separate claims.

Track A validation command shape:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
tools/winui3-mac-runner run \
  --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
  --renderer skia-v2 \
  --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
```

For Track A this command may still fail after generated host creation if page/shell render mode is not implemented yet. If so, report the next blocker as Track B work, not as Track A failure.

Required verification before final:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp"
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
tools/winui3-mac-runner xaml compile --output /private/tmp/emsi-windows-prod-xaml-generated /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Final response must include: completed Track A phases, generated host path, whether real `MeetingChallenge.Windows.csproj` inspection works, whether a generated host is emitted, next Track B blocker if render still fails, verification commands and results, and commit hash if a commit was made.

## Follow-Up Execution Prompts

Use these after Track A is complete.

### Track B Prompt: Direct Render With Runtime Interactions

Implement Track B from `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Build on the completed project inspection/generated host work. Add the shared scenario `entry`, `automation`, and `visual` contract; route scenario automation into `InteractionScriptRunner`; implement direct page render mode; implement direct shell/window route render mode; and produce `tree.json`, `accessibility.json`, `interactions.json`, and `visual/mac-runtime.png` from direct `MeetingChallenge.Windows.csproj` ingestion. Do not create downstream probe or host projects. Use fail-first tests for every runner/runtime behavior change. Do not run GitHub workflows or push. Final evidence must include the direct ingestion output path under `/private/tmp/emsi_qa/windows/mac-runtime-direct`.

Required targeted verification:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "AutomationContract|DirectApp"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "Interaction|Frame|Navigation|Accessibility"
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" tools/winui3-mac-runner run --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj --renderer skia-v2 --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
```

### Track C Prompt: UIA/FlaUI-Compatible Artifact Adapter

Implement Track C from `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Build a runtime-owned FlaUI/UIA3-compatible artifact adapter over macOS `tree.json`, `accessibility.json`, and `interactions.json`. It must support lookup by automation ID/name, control-role mapping, selected/checked/enabled/focused/value state, layout bounds when present, and action-result lookup. It must not claim native macOS UIA provider support. Add fail-first tests and an automation compatibility report that lists supported and unsupported UIA concepts.

Required targeted verification:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "FlaUIArtifactAdapter|AutomationParity"
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
```

### Track D Prompt: Native Windows FlaUI/UIA3 Reference Probe

Implement Track D from `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Add a Windows-only `tools/WindowsUiAutomationProbe` that uses FlaUI 5.0 + FlaUI.UIA3 to execute the shared scenario contract against a native WinUI app when running on Windows. Reuse `tools/WindowsWindowCapture` for screenshots. Emit `native-automation.json`, `windows-reference.json`, optional `windows-reference.png`, and failed action diagnostics. Build the tool locally where possible, but do not run GitHub workflows or push unless explicitly asked.

Required targeted verification:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet build tools/WindowsUiAutomationProbe/WindowsUiAutomationProbe.csproj
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "NativeWindowsAutomationPlan|AutomationParity"
```

### Track E Prompt: Documentation And Final Product Gate

Implement Track E from `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Update README, quick-start, downstream consumption docs, artifact docs, and support-policy wording for direct WinUI app project ingestion plus automation/integration testing. The docs must explain: source-level direct ingestion, generated temporary hosts, scenario automation, `tree.json/accessibility.json/interactions.json`, FlaUI/UIA3-compatible artifact adapter limits, optional native Windows FlaUI reference validation, screenshot/reference boundaries, and unsupported Windows-only APIs. Run the final direct EMSI ingestion gate if Tracks A/B are complete; otherwise document the current blocker without overstating readiness.

Required targeted verification:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp|AutomationContract|FlaUIArtifactAdapter|AutomationParity"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "Interaction|Accessibility|Frame|Navigation|Renderer"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
```
