# Direct WinUI App Project Runtime Ingestion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `winui3-mac-test-runtime` accept a real WinUI Windows application project directly and render/test it on macOS without requiring downstream apps to add a custom probe or host project.

**Architecture:** The runtime owns project ingestion, source-level host generation, Windows-only boundary isolation, XAML/page selection, deterministic scenario execution, and screenshot artifacts. A user should be able to point `WinUI3.MacRunner` at a real `*.csproj` such as `MeetingChallenge.Windows.csproj`; the runner creates a temporary macOS-compatible source-level harness outside the app repo, compiles supported WinUI source/XAML against the clean-room facades, runs the selected route/page, and emits tree/accessibility/screenshot evidence. This is not Windows binary execution and not Windows App SDK deployment emulation.

**Tech Stack:** .NET 10, MSBuild project inspection, WinUI facade runtime, `WinUI3.MacRunner`, `WinUI3.MacXaml`, `WinUI3.MacRuntime`, `WinUI3.MacRenderer.Skia`, MSTest, source-level generated temporary host projects.

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
open a real WinUI Windows app project in a source-level test environment.

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
  `visual-run.json`, and `mac-runtime.png`.

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
- Keep private screenshots and pixel diffs outside the repo.
- Produce honest diagnostics for unsupported Windows-only boundaries.

### The Runtime Should Not Claim

- Native execution of `.exe`, `.msix`, or packaged Windows App SDK binaries on
  macOS.
- Full `MeetingChallenge.Windows.csproj` build as a macOS app.
- WinRT storage, PasswordVault, deployment, activation, native UIA provider,
  Mica/Acrylic, Windows shell, or packaged resource behavior.
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
- `tests/WinUI3.MacRunner.Tests/ProjectIngestionTests.cs`
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

### Phase 3: Page Render Mode

Purpose: get the first direct screenshot from a real app project by rendering a
selected production page XAML.

- [ ] Extend scenario support with a direct page selector, for example:

  ```json
  {
    "name": "meetingchallenge-home-page-light",
    "theme": "light",
    "entry": {
      "mode": "page",
      "xaml": "Pages/HomePage.xaml"
    }
  }
  ```

- [ ] Add a failing runner test that uses the public fixture app and expects the
  generated host to render the selected page.
- [ ] Verify RED.
- [ ] Implement the minimum page entry generation.
- [ ] Close only reusable compiler/facade/layout/renderer gaps with separate
  RED/GREEN tests.
- [ ] Run the real EMSI direct page command:

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

### Phase 4: Shell Route Render Mode

Purpose: render the real app shell/window shape, not only a standalone page.

- [ ] Extend scenario support with a shell selector:

  ```json
  {
    "name": "meetingchallenge-shell-home-light",
    "theme": "light",
    "entry": {
      "mode": "window",
      "xaml": "MainWindow.xaml",
      "route": "home",
      "session": "staff"
    }
  }
  ```

- [ ] Add failing tests for shell route generation against the fixture app.
- [ ] Verify RED.
- [ ] Implement generated shell bootstrap that can:
  - instantiate the app window source-level;
  - choose the selected navigation item or content route;
  - inject deterministic state through generated runtime adapters;
  - skip or diagnose Windows-only startup services.
- [ ] Reuse runtime support for `Frame.Navigate`, navigation selection,
  resource dictionaries, `x:Uid`, and page parameters when possible.
- [ ] Run the real EMSI direct shell command:

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

### Phase 5: Windows-Only Boundary Diagnostics

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

### Phase 6: Verification And Documentation

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
  dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
  dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
  dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
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
  - unsupported Windows-only boundary diagnostics;
  - remaining renderer/layout/facade gaps;
  - whether native Windows reference comparison was run or skipped.

Acceptance:

- A user can point the runtime at a real WinUI Windows app project and get a
  macOS source-level runtime screenshot without adding a custom probe project to
  their app.

## Verification Gates

Minimum runtime gates:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
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
| Private screenshots leak into the repo. | Keep screenshot/diff output under `/private/tmp/emsi_qa` or approved QA storage. |
| Visual output is mistaken for native Windows parity. | Run native reference comparison only with explicit external reference evidence and report skipped comparison honestly. |

## Rollback And Recovery

- If project ingestion cannot support direct shell mode yet, keep page mode as
  the first deliverable and document shell mode as the next blocker.
- If EMSI direct ingestion exposes a Windows-only blocker, keep the public
  fixture support and report the exact unsupported boundary.
- If a generated host change affects existing probe runs, revert only the
  generated host integration and keep the inspector/tests.
- If native comparison is unavailable, report it as skipped and leave source-
  level render evidence intact.

## Execution Prompt

Implement the plan saved at `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-direct-winui-app-project-runtime-ingestion-plan.md`.

Work in `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime`. The product goal is direct app project ingestion: users should be able to run `winui3-mac-runner run --project <real WinUI Windows app csproj>` on macOS and get source-level tree/accessibility/screenshot evidence without adding a custom downstream probe or host project. Use `MeetingChallenge.Windows.csproj` only as the downstream validation target. Do not create `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost`; changes should live in `tools/winui3-mac-test-runtime` except for read-only validation against `apps/windows`.

Use `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-verification-gate`, `superpowers:test-driven-development`, and either `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Read `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, `README.md`, `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`, and `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md` before editing.

Implement in phases:

1. Add project inspection for real WinUI app `.csproj` files.
2. Add generated temporary source-level host creation under `/private/tmp`.
3. Add direct page render mode for selected app XAML.
4. Add direct shell/window route render mode.
5. Add precise Windows-only boundary diagnostics.
6. Update docs and run verification.

For every XAML/facade/layout/renderer/runner behavior change, write a failing targeted test first, verify RED, implement the minimum support, then verify GREEN. Do not make the full `MeetingChallenge.Windows.csproj` macOS build the implementation goal. Do not run `.exe` or `.msix`. Do not run GitHub workflows. Do not push. Do not copy private PNG/screenshot/pixel-diff artifacts into the repo. Keep source-level runtime render, direct project ingestion, and native Windows visual parity as separate claims.

Required final direct app ingestion command:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
tools/winui3-mac-runner run \
  --project /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj \
  --renderer skia-v2 \
  --scenario /private/tmp/emsi_qa/windows/mac-runtime-direct/scenarios/meetingchallenge-shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-direct/shell-home-light
```

Required verification before final:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
tools/winui3-mac-runner xaml compile --output /private/tmp/emsi-windows-prod-xaml-generated /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Final response must include: completed phases, whether direct `MeetingChallenge.Windows.csproj` ingestion works or where it is blocked, changed runtime support areas, verification commands and results, screenshot/evidence paths under `/private/tmp/emsi_qa`, remaining runtime/visual differences, and commit hash if a commit was made.
