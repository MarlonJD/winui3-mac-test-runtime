# MeetingChallenge Production Source Runtime Host Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move EMSI Windows macOS screenshots from smoke/probe-only pages to a production-source host that renders real `MeetingChallenge.Windows` XAML surfaces through `WinUI3.MacRunner` and `skia-v2`.

**Architecture:** Keep macOS execution source-level and Wine-free. Do not run the Windows `.exe`, packaged app, Windows App SDK deployment, or the full `MeetingChallenge.Windows.csproj` as a native macOS app. Build an app-owned production-source host under `apps/windows/tests` that links real production XAML/resource dictionaries, uses deterministic test services for data/session state, and isolates Windows-only bootstrap/storage/activation behind explicit adapter or exclusion boundaries. Every runtime/facade/layout/renderer gap exposed by this host must be closed fail-first in `tools/winui3-mac-test-runtime`.

**Tech Stack:** .NET 10, WinUI facade runtime, `WinUI3.MacRunner`, `WinUI3.MacXaml`, `WinUI3.MacRuntime`, `WinUI3.MacRenderer.Skia`, MSTest, EMSI `MeetingChallenge.Core`, app-owned Windows test harnesses.

---

Date: 2026-06-15

Owner subtree: `tools/winui3-mac-test-runtime`

Downstream owner: `apps/windows`

Status: active

Related plans:

- `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`
- `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`

## Objective

The current macOS screenshot evidence comes from
`apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe`, which is an
app-owned smoke/probe harness. That is useful, but it is not the requested
target.

The target is a new production-source host that renders real EMSI Windows
production source surfaces through the macOS runtime:

- real `MeetingChallenge.Windows/MainWindow.xaml`;
- real `MeetingChallenge.Windows/Pages/*.xaml`, starting with `HomePage.xaml`;
- real `MeetingChallenge.Windows/Themes/Tokens.xaml` and
  `Themes/Components.xaml`;
- real `MeetingChallenge.Core` view/service contracts where practical;
- deterministic test implementations for auth, read surfaces, product data, and
  admin data;
- explicit exclusions for Windows-only bootstrap, package deployment, WinRT
  storage, PasswordVault, activation, and shell/composition behavior.

The first screenshot label should be:

`MeetingChallenge production-source macOS runtime render`

not:

`MeetingChallenge.WinUI.MacRuntimeProbe smoke screenshot`.

## Correct Boundary

### In Scope

- Create an app-owned production-source host project under
  `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost`.
- Link or otherwise compile real production XAML from
  `apps/windows/src/MeetingChallenge.Windows` instead of copying probe XAML.
- Start with signed-in staff shell + Home/read-surface route.
- Use deterministic, local test services rather than live API calls.
- Add runtime/facade/layout/renderer support in
  `tools/winui3-mac-test-runtime` only when the production-source host exposes a
  reusable runtime gap.
- Produce screenshots and screenshot-like artifacts only under `/private/tmp` or
  the approved QA repository boundary.

### Out Of Scope

- Running `MeetingChallenge.Windows.exe`, `.msix`, or a packaged Windows App SDK
  app on macOS.
- Making `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`
  fully compile and run on macOS as the implementation goal.
- Emulating Windows App SDK deployment, app activation, PasswordVault,
  `Windows.Storage.ApplicationData`, native UI Automation providers, Mica, or
  the Windows shell.
- Copying private PNGs, screenshots, baselines, pixel diffs, or private app data
  into `tools/winui3-mac-test-runtime`.
- Relaxing visual thresholds to hide rendering gaps.
- Claiming native Windows pixel parity without a supplied Windows reference
  screenshot and explicit comparison evidence.

## Current Baseline

Already verified before this plan:

- Production XAML compile gate exits 0 for
  `apps/windows/src/MeetingChallenge.Windows/**/*.xaml`.
- `/private/tmp/emsi-windows-prod-xaml-generated.diagnostics.json` contains
  `[]`.
- `MeetingChallenge.WinUI.MacRuntimeProbe` `shell-staff-light` renders through
  `WinUI3.MacRunner + skia-v2`.
- That rendered image is a smoke/probe screenshot, not the real production-source
  host target.

Relevant current source facts:

- Production app project:
  `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`
  targets `net10.0-windows10.0.19041.0`, `UseWinUI=true`, and
  `WindowsPackageType=MSIX`.
- Current probe project:
  `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj`
  targets `net10.0-windows10.0.19041.0`, `UseWinUI=true`, and
  `WindowsPackageType=None`.
- Production `MainWindow.xaml` already has the desired shell shape:
  `NavigationView`, Home/Channels/Events/Messages/Notifications/Settings/Admin
  items, account footer, logout button, and `ContentFrame`.
- Production `HomePage.xaml` already has the desired Home/read-surface shape:
  `ScrollViewer`, padded `Grid`, `InfoBar`, loading/content/empty/denied/error
  panels, feed controls, and multiline `TextBox` inputs.
- Production code-behind uses Windows-only or runtime-sensitive APIs that must
  be gated or adapted before reuse:
  `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader`,
  `Windows.UI.ViewManagement.AccessibilitySettings`, `MicaBackdrop`,
  `Windows.System.VirtualKey`, `Windows.Storage.ApplicationData`, and
  `Windows.Security.Credentials.PasswordVault`.

## Assumptions And Open Questions

- The production-source host may live in `apps/windows/tests` because it is
  downstream app-owned and can reference production app source paths directly.
- The runtime repo may still own the saved plan because the implementation is
  primarily a `winui3-mac-test-runtime` capability milestone.
- The first route should be signed-in staff Home because the smoke probe already
  provides a visual sanity baseline for the same workflow.
- Production XAML should be linked, not copied. If linked XAML is blocked by
  `x:Class` or generated partial class behavior, document the blocker and use
  the smallest source-level adapter that still proves real production XAML
  ingestion.
- Reusing production code-behind is preferred when the missing surface is a
  reusable runtime/facade gap. Host-owned shims are acceptable only for app
  bootstrap, deterministic data, or Windows-only OS boundaries.
- Open question for execution: whether `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader`
  should be handled by a clean-room facade in `WinUI3.MacCompat` or by a host
  adapter that materializes `x:Uid` strings during scenario setup. The first RED
  compile/runtime failure should decide.

## Affected Files

Likely app-owned additions:

- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/App.xaml`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/App.xaml.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Host/ProductionHostMainWindow.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Host/ProductionHostScenario.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Services/DeterministicAuthService.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Services/DeterministicReadSurfaceService.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Services/DeterministicProductService.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/Services/DeterministicAdminConsoleService.cs`
- `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json`

Likely runtime-owned changes when RED evidence proves a reusable gap:

- `src/WinUI3.MacCompat/*Facade.cs`
- `src/WinUI3.MacXaml/MacXamlCompiler.cs`
- `src/WinUI3.MacRuntime/UiTree.cs`
- `src/WinUI3.MacRuntime/AccessibilityTree.cs`
- `src/WinUI3.MacRuntime/VisualLayoutEngine.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- compatibility/catalog docs only if a support claim changes

Do not edit or copy screenshot artifacts into this repository.

## Implementation Phases

### Phase 0: Production-Source Host Audit And RED Definition

Purpose: define the exact first production-source route and capture the first
real blocker without turning the full Windows app compile into the goal.

- [ ] Read the related plans and guidance:
  - `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`
  - `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`
  - `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`
- [ ] Run the current production XAML diagnostic gate:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  tools/winui3-mac-runner xaml compile \
    --output /private/tmp/emsi-windows-prod-xaml-generated \
    /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
  ```

  Expected current result: exit 0 and diagnostics `[]`.

- [ ] Run the current smoke probe once only as a baseline, not as the target:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
    --project tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj \
    --renderer skia-v2 \
    --scenario tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/shell-staff-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-probes/shell-staff-light
  ```

  Expected current result: passed, but explicitly labeled as smoke/probe.

- [ ] Create a short implementation note in the execution log or plan update
  listing the first production source files to include:
  - `src/MeetingChallenge.Windows/MainWindow.xaml`
  - `src/MeetingChallenge.Windows/Pages/HomePage.xaml`
  - `src/MeetingChallenge.Windows/Themes/Tokens.xaml`
  - `src/MeetingChallenge.Windows/Themes/Components.xaml`
  - `src/MeetingChallenge.Windows/Surfaces/ReadSurfacePageRenderer.cs` if the
    first compile attempt can reuse production code-behind.

Acceptance:

- The first RED target is a missing production-source host or a concrete
  compile/runtime failure from that host.
- No claim is made that the smoke probe is the real app.

### Phase 1: Add The Production-Source Host Skeleton

Purpose: create an app-owned host that uses production source files and can be
run by `WinUI3.MacRunner`.

- [ ] Add
  `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj`
  with:
  - `TargetFramework=net10.0-windows10.0.19041.0`
  - `UseWinUI=true`
  - `WindowsPackageType=None`
  - `EnableDefaultPageItems=false`
  - a reference to `../../src/MeetingChallenge.Core/MeetingChallenge.Core.csproj`
  - linked `Page` items for real production XAML selected in Phase 0
  - no private screenshot or data content
- [ ] Add host-owned `App.xaml` and `App.xaml.cs` that bypass Windows packaged
  activation and constructs the first deterministic host window.
- [ ] Add host-owned scenario parsing for:

  ```json
  {
    "name": "meetingchallenge-production-source-shell-home-light",
    "theme": "light",
    "interactions": [
      { "type": "signIn", "role": "staff" },
      { "type": "selectNavigation", "target": "HomeNavigationItem" }
    ]
  }
  ```

- [ ] Add the scenario file:
  `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json`.
- [ ] Run the production host command and verify RED:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
    --project tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj \
    --renderer skia-v2 \
    --scenario tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light
  ```

  Expected first result: fail for a precise missing project/file/facade/runtime
  reason. Record the exact blocker.

Acceptance:

- The host exists as a real app-owned test project.
- The failure is concrete and useful.
- The host is not a clone of `MeetingChallenge.WinUI.MacRuntimeProbe`.

### Phase 2: Link Real Production XAML Without Copying Probe UI

Purpose: render real production XAML structure, even if deterministic host code
fills data and state.

- [ ] Link real `MainWindow.xaml`, `HomePage.xaml`, `Themes/Tokens.xaml`, and
  `Themes/Components.xaml` into the production host.
- [ ] If `x:Class` requires matching classes, provide the smallest host-owned or
  production-code-behind path that keeps XAML real:
  - preferred: reuse production code-behind after adding missing runtime
    facades/stubs with fail-first tests;
  - acceptable first step: host-owned class in the same production namespace for
    Windows-only bootstrap only, while real XAML remains linked and unmodified.
- [ ] Do not copy `HomeProbePage.xaml` or `MainWindow.xaml` from the probe
  project.
- [ ] Run the host command again and verify the next RED failure.

Acceptance:

- The rendered surface, or the blocker preventing it, comes from real production
  XAML files under `apps/windows/src/MeetingChallenge.Windows`.
- Probe XAML is not the source of the screenshot.

### Phase 3: Add Deterministic Production Host Services

Purpose: let production pages bind realistic EMSI state without live backend,
WinRT storage, or secrets.

- [ ] Add deterministic implementations for the service interfaces needed by
  `MainWindow`, `HomePage`, and `ReadSurfacePageRenderer`.
- [ ] Use realistic but non-private sample state:
  - signed-in staff user;
  - Home read-surface overview with featured title/subtitle, item count, badge
    count;
  - feed snapshot with author/body/meta and enabled actions;
  - admin permission true only where the shell needs the Admin nav visible.
- [ ] Keep all strings public-safe and localizable through the existing resource
  path or a documented host fallback.
- [ ] Add a scenario option for signed-out and signed-in staff, but only
  `shell-home-light` is required for this plan.

Acceptance:

- The host can drive production Home/read-surface UI to a content state without
  a live API.
- No private data, credentials, or screenshot artifacts are committed.

### Phase 4: Close Reusable Runtime Gaps Fail-First

Purpose: fix only the runtime gaps proven by the production-source host.

For each blocker:

- [ ] Add a focused failing test first in the owning runtime test project:
  - XAML compiler gaps: `tests/WinUI3.MacXaml.Tests/MacXamlCompilerTests.cs`
  - facade/tree/layout/renderer gaps:
    `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- [ ] Run the targeted test and verify RED with the expected failure.
- [ ] Implement the minimum runtime support.
- [ ] Run the targeted test and verify GREEN.
- [ ] Re-run the production-source host command.

Likely early gap families:

- `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader` facade or host
  localization adapter.
- `Window.SystemBackdrop` / `MicaBackdrop` explicit no-op boundary for source
  host execution without claiming material rendering.
- `KeyboardAccelerator` / `VirtualKey` behavior if production keyboard shortcut
  setup reaches runtime paths not already covered.
- `Frame.Navigate` parameter propagation and `OnNavigatedTo` timing if
  production pages need it.
- `x:Uid` resource application if production labels are blank in the screenshot.
- layout/export gaps exposed by real `HomePage.xaml`, especially nested grids,
  feed form rows, and multiline input bounds.

Acceptance:

- Every runtime change has RED/GREEN evidence.
- No threshold relaxation is used as a solution.
- Unsupported Windows-only behavior remains explicit and documented.

### Phase 5: Produce The First Production-Source Screenshot

Purpose: create the first non-probe EMSI Windows macOS runtime screenshot.

- [ ] Run:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
  WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
  dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
    --project tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj \
    --renderer skia-v2 \
    --scenario tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json \
    --output /private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light
  ```

- [ ] Inspect:
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/run.json`
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/tree.json`
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/accessibility.json`
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/unsupported-apis.json`
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/visual/visual-run.json`
  - `/private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light/visual/mac-runtime.png`
- [ ] Confirm `unsupported-apis.json` is either empty or contains only
  explicitly documented non-goal Windows-only APIs.
- [ ] Confirm the screenshot visibly comes from production `MainWindow.xaml` and
  `HomePage.xaml`, not probe pages.

Acceptance:

- The screenshot exists at the production-source path.
- The final wording calls it a production-source macOS runtime render.
- The final wording does not call it native Windows output or full app execution.

### Phase 6: Verification, Docs, And Handoff

Purpose: make the result durable and honest.

- [ ] Run targeted runtime verification:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
  dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
  dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
  dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
  ```

- [ ] Run Windows app-owned core checks from `apps/windows` if host services or
  shared core usage changed:

  ```bash
  cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
  dotnet build src/MeetingChallenge.Core/MeetingChallenge.Core.csproj
  dotnet run --project tests/MeetingChallenge.Core.Tests/MeetingChallenge.Core.Tests.csproj
  dotnet run --project tests/MeetingChallenge.SmokeTests/MeetingChallenge.SmokeTests.csproj
  ```

- [ ] Update this plan with:
  - exact production-source screenshot path;
  - exact unsupported API count;
  - remaining renderer/layout/runtime differences;
  - whether native Windows comparison was skipped or run.
- [ ] Keep private PNG/screenshot/pixel-diff files out of the repo.
- [ ] Commit only task-relevant files after verification if commit guards pass.
- [ ] Do not push unless the user explicitly allows it.

Acceptance:

- The user receives a real production-source macOS runtime screenshot path.
- Source-level runtime support, production-source host output, and native
  Windows visual parity remain separate claims.

## Verification Gates

Minimum plan execution gate:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Production-source screenshot gate:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
  --project tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj \
  --renderer skia-v2 \
  --scenario tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light
```

Optional Windows/native reference gate, only when a Windows reference image is
available outside the runtime repo:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
  --project tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj \
  --renderer skia-v2 \
  --scenario tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json \
  --reference /private/tmp/emsi_qa/windows/native-reference/shell-home-light \
  --output /private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light-with-reference
```

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| The host becomes another probe copy. | Link production XAML and reject copied probe pages as the screenshot source. |
| Full Windows app compile becomes the goal again. | Treat full `MeetingChallenge.Windows.csproj` macOS compile failure as a diagnostic only; the host is the target. |
| Windows-only APIs leak into runtime claims. | Gate or stub bootstrap/storage/activation APIs explicitly and keep them out of support claims. |
| Host services diverge from production data contracts. | Implement deterministic services against `MeetingChallenge.Core` interfaces and use realistic public-safe sample states. |
| Renderer changes hide gaps through threshold tweaks. | Keep thresholds fixed; add RED/GREEN tests for each support change. |
| Private screenshot evidence enters the repo. | Store images under `/private/tmp/emsi_qa` or approved QA repo only. |

## Rollback And Recovery

- If the production-source host cannot link real production XAML without a broad
  app refactor, stop and report the exact `x:Class`, generated partial, or
  MSBuild blocker.
- If a runtime support change causes unrelated visual drift, keep the host
  skeleton and revert only that runtime support change; leave the failing test
  or plan note as the next blocker.
- If the host can render only shell but not Home content, keep the shell render
  as partial evidence and list the Home/page blocker separately.
- If native Windows reference comparison cannot run, report it as skipped, not
  failed, and do not claim native parity.

## Execution Prompt

Implement the plan saved at `tools/winui3-mac-test-runtime/docs/plans/2026-06-15-meetingchallenge-production-source-runtime-host-plan.md`.

Work from `/Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime`, but expect to edit both `tools/winui3-mac-test-runtime` and downstream `apps/windows/tests/MeetingChallenge.WinUI.MacRuntimeProductionHost` files. Use `emsi-workflows:emsi-task-router`, `emsi-workflows:emsi-verification-gate`, `superpowers:test-driven-development`, and either `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Read `/Users/marlonjd/Developer/monorepos/emsi_monorepo/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/AGENTS.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/UI_RULES.md`, `/Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/UI_RULES.md`, `README.md`, `docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md`, and `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md` before editing.

Goal: move from `MeetingChallenge.WinUI.MacRuntimeProbe` smoke screenshots to a real production-source macOS runtime host that renders linked `apps/windows/src/MeetingChallenge.Windows` production XAML, starting with signed-in staff shell + Home/read-surface. Do not try to make the full `MeetingChallenge.Windows.csproj` compile/run on macOS. Do not run GitHub workflows. Do not push. Do not copy private PNG/screenshot/pixel-diff artifacts into the repo. Keep source-level runtime support, production-source host screenshot output, and native Windows visual parity as separate claims.

For every XAML/facade/layout/renderer behavior change, write a failing targeted test first, verify RED, implement the minimum support, then verify GREEN. If a failure is only Windows bootstrap/storage/activation/PasswordVault/Mica/native shell behavior, isolate it as an explicit host boundary or non-goal unless it reveals a reusable runtime support gap.

Required target command for the new host:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
  --project tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/MeetingChallenge.WinUI.MacRuntimeProductionHost.csproj \
  --renderer skia-v2 \
  --scenario tests/MeetingChallenge.WinUI.MacRuntimeProductionHost/scenarios/shell-home-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-production-source/shell-home-light
```

Required verification before final:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout|Frame|Resource"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
tools/winui3-mac-runner xaml compile --output /private/tmp/emsi-windows-prod-xaml-generated /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Also run downstream core checks from `apps/windows` if app-owned host services or shared core usage change:

```bash
dotnet build src/MeetingChallenge.Core/MeetingChallenge.Core.csproj
dotnet run --project tests/MeetingChallenge.Core.Tests/MeetingChallenge.Core.Tests.csproj
dotnet run --project tests/MeetingChallenge.SmokeTests/MeetingChallenge.SmokeTests.csproj
```

Final response must include: completed phases, whether the screenshot is production-source host or still blocked, changed runtime/app-owned support areas, verification commands and results, screenshot/evidence paths under `/private/tmp/emsi_qa`, remaining runtime/visual differences, and commit hash if a commit was made.
