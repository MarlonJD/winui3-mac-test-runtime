# Windows Reference Pixel Fidelity Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `src/WinUI3.MacRuntime`, `src/WinUI3.MacRenderer.Skia`, `tools/WindowsWindowCapture`, `fixtures`, `tests`, `.github/workflows`

## Goal

Make the project produce pixel-meaningful visual test artifacts by treating
real Windows screenshots from public `windows-latest` GitHub Actions runs as
the reference output for supported fixture screens.

The macOS-managed runtime remains Wine-free, but it must no longer be only a
structural preview path. For every supported fixture, the runtime should emit a
deterministic PNG that can be compared against the Windows reference screenshot
with explicit pass/fail thresholds and reviewable diff artifacts.

The project should still avoid blanket marketing claims that it is a complete
WinUI 3 implementation. The stronger product claim is narrower and testable:
for the documented supported fixture/control subset, the tool produces real
Windows reference screenshots, macOS-runtime screenshots, pixel diff artifacts,
and fail-fast diagnostics for unsupported visual behavior.

## Success Criteria

- A public GitHub Actions workflow captures real Windows screenshots for
  generic fixture apps on `windows-latest`.
- The same fixture state can be rendered through the macOS-managed runtime into
  a deterministic PNG with the same viewport, theme, scale, and scenario name.
- The comparison step emits:
  - `windows-reference.png`
  - `mac-runtime.png`
  - `pixel-diff.png`
  - `pixel-diff.json`
  - `visual-run.json`
- Pixel comparison has documented thresholds and fails CI when the supported
  fixture output drifts beyond those thresholds.
- Unsupported controls, properties, resources, theme values, layout behaviors,
  or interactions do not silently render as successful approximations. They are
  recorded in `unsupported-apis.json`, `visual-run.json`, and the process exits
  non-zero when the scenario is configured as strict.
- The repository remains free of private product names, private screenshots,
  private repository references, or proprietary fixture content.

## Assumptions

- `windows-latest` is available for public GitHub-hosted workflow runs and is
  acceptable as the reference screenshot environment.
- Public fixture apps are intentionally generic but must be realistic enough to
  exercise app-shell, navigation, form, list, text, image placeholder, theme,
  resource, and interaction states.
- Exact cross-platform font rasterization may differ. The plan handles that by
  recording metrics and thresholds, not by ignoring visible drift.
- The runtime can start strict: it may support fewer controls initially, but
  every supported screen must be honest about unsupported pieces.
- Existing artifacts (`run.json`, `tree.json`, `snapshot.json`,
  `unsupported-apis.json`, and `diagnostics.sarif`) should evolve rather than
  being replaced.

## Non-Goals

- Running Windows binaries on macOS.
- Requiring Wine in the primary path.
- Storing private application screenshots in this public repository.
- Using private repositories, secrets, or organization infrastructure for the
  public reference workflow.
- Claiming arbitrary third-party WinUI 3 apps are pixel-compatible before they
  are covered by reference scenarios.

## Architecture

### Reference Pipeline

1. Build a public fixture app on `windows-latest`.
2. Launch the fixture with a named scenario, viewport, theme, and scale.
3. Capture the real Windows desktop window through `tools/WindowsWindowCapture`.
4. Upload `windows-reference.png` and `windows-reference.json`.
5. Render the same scenario through `winui3-mac-runner` with the same options.
6. Compare the two PNGs with the pixel comparison tool.
7. Upload all images, JSON metadata, and the diff summary as workflow artifacts.

### Artifact Contract

`visual-run.json` should include:

- schema version;
- fixture name;
- scenario name;
- runner OS and OS image;
- renderer name and version;
- viewport width and height;
- scale factor;
- theme;
- reference image path;
- runtime image path;
- diff image path;
- threshold configuration;
- comparison metrics;
- unsupported visual features;
- pass/fail status.

`pixel-diff.json` should include:

- image dimensions;
- changed pixel count;
- changed pixel percentage;
- max channel delta;
- mean absolute error;
- root mean squared error;
- bounding box of changed pixels;
- threshold values;
- pass/fail status.

### Strict Supported-Subset Rule

For fixture scenarios marked `strictVisual=true`, the runner must fail when:

- a control type in the scenario has no painter;
- a required property has no exported value;
- a resource or theme lookup is missing;
- a layout panel cannot produce stable arranged rectangles;
- interaction state differs from the scenario definition;
- the pixel diff exceeds thresholds.

This rule makes the project useful as a test tool without pretending to support
the whole WinUI 3 API surface.

## Implementation Phases

### Phase 1: Scenario Contract And CLI Options

- Add a fixture scenario format, for example
  `fixtures/<FixtureName>/scenarios/*.json`.
- Include scenario fields for viewport, scale, theme, startup route,
  interactions, strict visual mode, and thresholds.
- Add runner options:
  `--scenario <path>`, `--renderer skia-v2`, `--viewport <width>x<height>`,
  `--scale <number>`, `--theme light|dark`, `--strict-visual`,
  `--reference <path>`, and `--diff-output <dir>`.
- Preserve existing `winui3-mac-runner run` defaults.

Verification:

- Unit tests for scenario parsing, CLI validation, and backward-compatible
  defaults.
- Existing smoke commands continue to work.

### Phase 2: Visual Tree And Layout Export

- Add a deterministic measure/arrange layer for the documented supported
  controls.
- Export arranged rectangles, desired sizes, actual sizes, margins, padding,
  alignment, visibility, font properties, colors, border values, resource
  resolution, and interaction state in `tree.json`.
- Add unsupported visual diagnostics whenever a required visual property cannot
  be exported.

Verification:

- Runtime tests assert stable layout rectangles for app shell, list, form, text,
  and button fixtures.
- Strict visual scenarios fail when an unsupported required property is present.

### Phase 3: Skia V2 Painter Coverage

- Add `skia-v2` as an explicit renderer path.
- Implement painters for the first reliable supported surface:
  `Window`, `Page`, `Grid`, `StackPanel`, `Border`, `TextBlock`, `Button`,
  `TextBox`, `Frame`, `NavigationView`, `NavigationViewItem`, `ListView`,
  `FontIcon`, and image placeholders.
- Make text layout deterministic enough for comparison by recording font
  fallback and measuring line wrapping consistently.
- Emit `mac-runtime.png` and renderer metadata.

Verification:

- Tests verify PNG dimensions, nonblank pixels, deterministic rerender hashes,
  and expected unsupported diagnostics.

### Phase 4: Pixel Diff Tooling

- Add a small image comparison tool or runtime service based on SkiaSharp.
- Emit `pixel-diff.json` and `pixel-diff.png`.
- Support strict thresholds per scenario:
  changed pixel percentage, max channel delta, mean absolute error, and root
  mean squared error.
- Return a non-zero exit code when strict comparison fails.

Verification:

- Tests for identical images, tolerated changes, dimension mismatch, and
  threshold failure.

### Phase 5: Public Windows Reference Workflow

- Extend `.github/workflows/windows-native-screenshot.yml` or add a focused
  visual workflow that runs on `windows-latest`.
- Build and capture each generic public scenario.
- Generate matching macOS-runtime screenshots where the runner can execute on
  the workflow host, or split the reference and macOS comparison into separate
  jobs with downloaded artifacts.
- Upload all images and JSON artifacts.
- Fail the workflow only for strict public scenarios whose visual contract is
  expected to pass.

Verification:

- Trigger the workflow manually.
- Wait for completion.
- Download artifacts.
- Inspect at least one reference PNG, runtime PNG, and diff PNG.

### Phase 6: Documentation And Compatibility Matrix

- Update `README.md`, `docs/architecture/artifacts.md`, and
  `docs/compatibility/matrix.md`.
- Document the Windows-reference source-of-truth model.
- Document how to add a new public scenario and baseline.
- Document what a passing pixel diff means and what it does not mean.
- Keep public docs explicit that unsupported behavior must be reported, not
  silently approximated.

Verification:

- Documentation examples run locally or in CI.
- Private-name denylist scan is clean.

## Verification Gates

Run before committing implementation work:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
rg -n "<private-name-denylist-regex>" .
```

Run for the public Windows reference check:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

Inspect downloaded PNG artifacts before final handoff.

## Risks And Mitigations

- Risk: Font and OS image changes create noise.
  Mitigation: record OS image, font metadata, and comparison thresholds in
  artifacts; keep scenario thresholds explicit and reviewable.
- Risk: The renderer passes by approximating unsupported UI.
  Mitigation: strict visual mode fails on missing painters, missing properties,
  unresolved resources, and unsupported interactions.
- Risk: The scope expands into a full WinUI implementation.
  Mitigation: define scenario-level supported contracts and add coverage only
  when a real fixture needs it.
- Risk: Public fixtures are too toy-like to prove value.
  Mitigation: make fixtures generic but realistic: shell navigation, account
  footer, dense text, forms, lists, disabled states, dark theme, and image
  placeholders.

## Rollback Or Recovery

- Keep the existing SVG and current Skia renderer paths until `skia-v2` passes
  strict public scenarios.
- If pixel comparison is flaky, continue uploading reference/runtime/diff
  artifacts while marking the strict gate as experimental for that scenario.
- If a Windows runner image changes, update artifact metadata and thresholds in
  the same reviewed change.

## Affected Files And Docs

- `src/WinUI3.MacRunner/Program.cs`
- `src/WinUI3.MacRuntime/*`
- `src/WinUI3.MacRenderer.Skia/*`
- `src/WinUI3.MacCompat/*`
- `tests/WinUI3.MacRuntime.Tests/*`
- `tests/WinUI3.MacXaml.Tests/*`
- `fixtures/*`
- `tools/WindowsWindowCapture/*`
- `.github/workflows/windows-native-screenshot.yml`
- `README.md`
- `docs/architecture/artifacts.md`
- `docs/compatibility/matrix.md`

## Execution Prompt

Use `$google-eng-practices` and implement the plan in `docs/plans/2026-06-01-windows-reference-pixel-fidelity-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. The product goal is reliable pixel-level visual testing for the documented supported fixture/control subset, with real Windows screenshots from public `windows-latest` GitHub Actions runs as the reference source of truth. Keep the macOS-managed runtime Wine-free. Do not use private repositories, private screenshots, private product names, secrets, or proprietary fixture content. Keep identifiers, comments, and canonical docs in English.

Implement scenario-driven visual testing: scenario JSON files, CLI options for `--scenario`, `--renderer skia-v2`, `--viewport`, `--scale`, `--theme`, `--strict-visual`, `--reference`, and `--diff-output`; deterministic visual tree/layout export; `skia-v2` painters for the supported public fixture subset; strict unsupported-feature diagnostics; pixel comparison artifacts (`windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`, and `visual-run.json`); realistic generic public fixtures; and a public `windows-latest` workflow that captures Windows reference screenshots and uploads reviewable artifacts. Preserve existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG, and current Skia behavior while adding the stricter path.

Run `dotnet build`, `dotnet test`, `PATH="$PWD/tools:$PATH" winui3-mac-doctor`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual`, `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release`, and `rg -n "<private-name-denylist-regex>" .` with the operator-provided private-name denylist. Trigger the public `windows-native-screenshot` workflow on GitHub Actions, wait for it to finish, download the screenshot artifacts, and inspect at least one Windows reference PNG, macOS-runtime PNG, and pixel diff PNG before final handoff.

Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately.
