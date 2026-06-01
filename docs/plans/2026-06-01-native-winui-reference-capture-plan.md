# Native WinUI Reference Capture Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `.github/workflows`,
`tools/WindowsWindowCapture`, `fixtures/PublicAdminWorkbench.WinUI`,
`fixtures/ComponentParityLab.WinUI`, `src/WinUI3.MacRuntime`,
`src/WinUI3.MacRunner`, `docs/visual-parity`, `docs/compatibility`,
`docs/release`, `README.md`

## Goal

Replace the current synthetic `WindowsNativeProbe` visual baseline with real
native WinUI Windows reference captures for the public WinUI fixture projects,
then refresh README/example evidence from those native references and compare
the macOS runtime against the corrected baseline.

The first deliverable is not renderer parity. The first deliverable is a
trustworthy source of truth: `windows-reference.png` and its metadata must say
whether the image came from an actual WinUI fixture app or from the synthetic
probe. Component grades, README examples, and production readiness claims must
not improve until native WinUI provenance exists.

## Current Finding

`windows-native-screenshot.yml` currently builds `WindowsWindowCapture` and
`WindowsNativeProbe`, then captures `WindowsNativeProbe` for every scenario. That
means all checked-in `windows-reference.png` examples are synthetic WinForms/GDI
drawings, not native WinUI renders of `PublicAdminWorkbench.WinUI` or
`ComponentParityLab.WinUI`.

This invalidates any visual conclusion that depends on native WinUI reference
behavior. It remains useful as capture-harness smoke evidence only.

## Assumptions

- Public `windows-latest` runners can build and launch unpackaged WinUI 3
  fixture apps with `Microsoft.WindowsAppSDK`.
- If direct `dotnet run` does not launch the unpackaged fixture on
  `windows-latest`, the implementation will add the smallest public, documented
  Windows App SDK bootstrap or project property needed for CI launch.
- Native fixture windows can accept a scenario path or scenario name, set a
  stable window title, navigate to the requested page, apply the requested light
  theme, and reach a deterministic state before capture.
- `WindowsNativeProbe` remains in the repository as a labeled synthetic smoke
  fallback, but it is no longer used for production visual evidence.
- No private repositories, private screenshots, private product names, secrets,
  proprietary content, or copied WinUI Gallery fixture content may be introduced.

## Scope

- Add native scenario launch support to public WinUI fixtures.
- Update the Windows reference workflow so native WinUI fixture apps are the
  default reference source for public WinUI scenarios.
- Add reference provenance metadata to captured artifacts.
- Run the public workflow, download artifacts, inspect native references and
  diffs, and update README/example docs from those public artifacts.
- Produce an honest status table after comparison, including weak or failed
  components.

## Non-Goals

- Do not make broad renderer changes while fixing the reference source.
- Do not tune thresholds before inspecting native reference artifacts.
- Do not claim `CommandBar`, `MenuBar`, `CommandBarFlyout`, `MenuFlyout`, Mica,
  Acrylic, templates, theme dictionaries, or full Fluent states are supported
  unless component evidence and native references prove it.
- Do not remove existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current
  Skia, `skia-v2`, existing fixtures, public admin/workbench ingestion, or
  `WindowsNativeProbe` smoke capability.

## Implementation Steps

### Step 1: Define Reference Provenance

- Extend the Windows reference metadata written next to `windows-reference.png`
  to include:
  - `schemaVersion`;
  - `referenceSource`: `native-winui` or `synthetic-probe`;
  - `fixtureProjectPath`;
  - `scenarioPath`;
  - `scenarioName`;
  - `commitSha`;
  - `workflowRunId`;
  - `runnerImage`;
  - `viewport`, `scale`, and `theme`;
  - `windowTitle`;
  - `captureMode`;
  - captured image dimensions.
- Ensure `visual-run.json` or the copied visual artifact folder preserves this
  reference metadata so later docs cannot treat a synthetic image as native.
- Add validation that production/example updates fail or stay labeled synthetic
  when `referenceSource != native-winui`.

Exit criteria:

- Every reference artifact folder includes machine-readable provenance.
- Synthetic probe output is explicitly labeled `synthetic-probe`.

### Step 2: Make Public WinUI Fixtures Scenario-Launchable

- Update `PublicAdminWorkbench.WinUI` and `ComponentParityLab.WinUI` so native
  Windows launch accepts `--scenario <path>` or `--scenario-name <name>`.
- Set the native window title to
  `WinUI3 Mac Test Runtime - <scenario-name>` so `WindowsWindowCapture` can find
  the correct window.
- Call `Activate()` after creating the main window.
- Apply scenario startup state:
  - `public-admin-workbench-light`: navigate/select the review queue and apply
    the same public state used by the macOS scenario.
  - component lab pages: navigate to the page implied by scenario name, including
    `component-commands-menus-light` showing the actual native command/menu page.
- Keep this launch helper small and fixture-local unless duplication becomes
  meaningful.

Exit criteria:

- On Windows, each native fixture can launch to the requested scenario with the
  expected title and visible page.

### Step 3: Split Native Reference Capture From Synthetic Smoke

- Update `.github/workflows/windows-native-screenshot.yml` so the primary
  `windows-reference` job builds and captures the actual public WinUI fixture
  projects for:
  - `public-admin-workbench-light`;
  - all eight `ComponentParityLab.WinUI` scenarios.
- Keep the existing synthetic probe flow only as an optional or separate smoke
  job/artifact named as synthetic output.
- For older Mac-only fixtures that do not have native WinUI equivalents
  (`SampleAdminShell.MacTest`, `InteractionBindingApp.MacTest`,
  `ControlGallery.MacTest`), either:
  - keep them synthetic and labeled `synthetic-probe`, or
  - exclude them from native production visual claims until a native WinUI
    fixture exists.
- Upload native references and provenance separately from synthetic smoke
  artifacts so docs can select the correct source.

Exit criteria:

- Public WinUI scenarios produce `native-winui` reference artifacts.
- Mac-only fixture categories are not accidentally represented as native WinUI
  evidence.

### Step 4: Run Native Reference Audit

- Trigger `windows-native-screenshot.yml` on the public repository.
- Wait for completion.
- Download:
  - `windows-reference.png`;
  - `windows-reference.json` or provenance metadata;
  - `mac-runtime.png`;
  - `pixel-diff.png`;
  - `pixel-diff.json`;
  - `visual-run.json`;
  - `component-evidence.json`.
- Inspect at least:
  - `public-admin-workbench-light`;
  - `component-basic-input-light`;
  - `component-commands-menus-light`;
  - `component-layout-media-light`.
- If native comparison fails because the macOS runtime is weaker than native
  WinUI, keep the artifacts and record the status. Do not hide failure by
  loosening thresholds without a documented visual review.

Exit criteria:

- The team has public native reference artifacts and a first honest comparison
  result.

### Step 5: Replace README And Example Evidence

- Replace checked-in example folders under `docs/visual-parity/examples/` only
  from public native WinUI artifacts.
- Include or link provenance metadata next to each refreshed example.
- Update `README.md` and `docs/visual-parity/README.md` tables so the first
  column says `Native WinUI Windows reference` only for artifacts whose metadata
  says `referenceSource: native-winui`.
- Keep any remaining synthetic screenshots in a clearly labeled smoke-only
  section, or remove them from visual parity examples.

Exit criteria:

- README and visual parity examples no longer show synthetic probe screenshots
  as the main Windows reference for public WinUI scenarios.

### Step 6: Publish The New Status

- Update `docs/release/production-readiness.md`,
  `docs/compatibility/matrix.md`, and `docs/compatibility/component-support.md`
  with the native comparison result.
- Keep weak, poor, failed, or not-rendered components labeled honestly.
- Specifically call out what native WinUI shows for command/menu scenarios:
  `CommandBar`, `CommandBar.Content`, `AppBarButton.Icon`,
  `CommandBarFlyout`, `MenuFlyout`, `MenuBar`, and context menu patterns.
- Decide whether `PB-000` is closed. It closes only when native reference
  provenance exists for the public WinUI scenarios used in production claims.

Exit criteria:

- The repository states the real current status against native WinUI references,
  not against synthetic probe drawings.

## Verification Gates

Run locally before committing implementation work:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
```

Run on the public repository after workflow changes:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-reference-screenshots --dir ./artifacts/github-windows-reference-screenshots
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

Inspect the downloaded PNG and JSON artifacts before updating README/examples or
claiming status.

## Risks

- Native WinUI unpackaged launch may fail on `windows-latest`.
  Mitigation: add the smallest documented Windows App SDK bootstrap or project
  property needed by the public fixture.
- Native WinUI screenshots may reveal larger gaps than the synthetic probe.
  Mitigation: record the result honestly and keep renderer work as a later
  milestone.
- Workflow artifacts may mix synthetic and native outputs.
  Mitigation: require `referenceSource` metadata and fail doc/example promotion
  when provenance is missing.
- Scenario page selection may differ between native fixture and macOS runner.
  Mitigation: derive both from the same scenario names and add title/page
  assertions in workflow logs or metadata.

## Rollback Or Recovery

- If native capture is unstable, keep synthetic capture as a smoke-only fallback
  while the native launch issue is fixed.
- If the workflow fails before uploading all artifacts, keep the last successful
  public artifacts unchanged and document the failure mode.
- If a native reference reveals weak parity, do not revert the reference fix;
  update docs/status and create focused renderer/component follow-up tasks.

## Affected Files And Docs

- `.github/workflows/windows-native-screenshot.yml`
- `tools/WindowsWindowCapture/Program.cs`
- `fixtures/PublicAdminWorkbench.WinUI/App.xaml.cs`
- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml.cs`
- `fixtures/ComponentParityLab.WinUI/App.xaml.cs`
- `fixtures/ComponentParityLab.WinUI/MainWindow.xaml.cs`
- `fixtures/ComponentParityLab.WinUI/Pages/*`
- `src/WinUI3.MacRuntime`
- `src/WinUI3.MacRunner`
- `docs/architecture/artifacts.md`
- `docs/visual-parity/README.md`
- `docs/visual-parity/examples/*`
- `docs/compatibility/matrix.md`
- `docs/compatibility/component-support.md`
- `docs/release/production-readiness.md`
- `README.md`

## Execution Prompt

```text
/goal Use $google-eng-practices and $windows-winui3-design and implement docs/plans/2026-06-01-native-winui-reference-capture-plan.md in the public MarlonJD/winui3-mac-test-runtime repository.

First fix the reference source of truth. Replace the current synthetic WindowsNativeProbe visual baseline with native WinUI Windows captures of the actual public WinUI fixture projects for PublicAdminWorkbench.WinUI and ComponentParityLab.WinUI. Do not make broad renderer changes, do not tune thresholds to hide failures, and do not promote any component visual grade until native-winui reference provenance exists.

Implement reference provenance metadata with referenceSource native-winui or synthetic-probe, fixture project path, scenario path/name, commit SHA, workflow run, runner image, viewport, scale, theme, title, capture mode, and dimensions. Make the public WinUI fixtures launchable by scenario, set the stable capture title, activate the window, and navigate to the requested page/state. Update windows-native-screenshot.yml so public WinUI scenarios capture native WinUI fixtures by default and keep WindowsNativeProbe only as labeled smoke evidence.

After the workflow change, run targeted local checks from the plan, trigger windows-native-screenshot.yml on the public repository, wait for completion, download artifacts, and inspect windows-reference.png, provenance JSON, mac-runtime.png, pixel-diff.png, pixel-diff.json, visual-run.json, and component-evidence.json for public-admin-workbench-light, component-basic-input-light, component-commands-menus-light, and component-layout-media-light. If comparison fails against native WinUI, preserve the artifacts and record the honest status instead of loosening thresholds without review.

Then update README.md, docs/visual-parity/README.md, docs/visual-parity/examples, docs/release/production-readiness.md, docs/compatibility/matrix.md, and docs/compatibility/component-support.md from the inspected public native artifacts. Only label an example as Native WinUI Windows reference when provenance says referenceSource is native-winui; keep synthetic outputs labeled smoke-only or remove them from parity examples. Final handoff must summarize the native reference provenance status, comparison metrics, weak/poor/not-rendered components, and whether PB-000 is closed.

Preserve the Wine-free macOS runtime, winui3-mac-doctor, winui3-mac-runner, SVG, current Skia, skia-v2, existing fixtures, public admin/workbench source ingestion, and ComponentParityLab.WinUI foundation. Do not use private repositories, private screenshots, private product names, secrets, proprietary fixture content, or copied WinUI Gallery fixture content. Keep identifiers, source comments, and canonical docs in English.

At the end of each completed milestone, run the relevant verification gate, commit only that milestone's relevant files with author marlonjd <burak.karahan@mail.ru> using a Conventional Commit message, and push immediately before starting the next milestone.
```
