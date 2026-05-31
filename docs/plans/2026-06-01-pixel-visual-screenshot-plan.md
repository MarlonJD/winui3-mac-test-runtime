# Pixel Visual Screenshot Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `src/WinUI3.MacRuntime`, `src/WinUI3.MacRenderer.Skia`, `tools/WindowsWindowCapture`, `fixtures`, `tests`

## Goal

Move the runtime from structural preview snapshots to pixel-level visual
screenshot artifacts that are useful for automated UI review:

- deterministic macOS-generated PNG snapshots from the managed facade runtime;
- real Windows-hosted reference screenshots from public GitHub Actions fixtures;
- machine-readable pixel comparison artifacts with explicit thresholds;
- documentation that clearly states supported controls and non-goals.

The work must remain Wine-free on the primary path and must not claim full
WinUI 3 compatibility.

## Assumptions

- The immediate target is public, generic fixture coverage. Private application
  screenshots stay out of this repository.
- Pixel fidelity means stable, inspectable screenshots and bounded pixel diffs
  for the supported facade subset. It does not mean complete WinUI rendering
  parity.
- `windows-latest` GitHub-hosted runners are acceptable for public reference
  screenshots.
- The renderer can use SkiaSharp for deterministic offscreen rendering.
- The existing `tree.json`, `run.json`, `snapshot.json`, and screenshot artifact
  contracts should evolve instead of being replaced.

## Non-Goals

- Running arbitrary Windows binaries on macOS.
- Requiring Wine.
- Claiming complete WinUI 3 layout, text, animation, accessibility, or control
  compatibility.
- Capturing or storing screenshots from private applications in this public
  repository.
- Reimplementing the full Windows compositor.

## Scope

### Runtime and Tree Export

- Add a small layout model to the managed facade runtime:
  `desiredSize`, `arrangedRect`, `actualWidth`, `actualHeight`, margins,
  padding, alignment, orientation, and visibility.
- Export visual properties needed by the renderer:
  text, content, foreground, background, border brush, border thickness,
  corner radius, font size, font weight, icon glyphs, item selection, image
  source metadata, and resource lookup status.
- Keep `tree.json` backward-compatible by adding fields rather than renaming
  existing fields.

### Skia Renderer

- Introduce a renderer version such as `skia-v2` while keeping the current
  renderer stable during the transition.
- Build generic painters for the supported subset:
  `Window`, `Page`, `Grid`, `StackPanel`, `Border`, `TextBlock`, `Button`,
  `TextBox`, `Image`, `Frame`, `NavigationView`, `NavigationViewItem`,
  `ListView`, and `FontIcon`.
- Add deterministic render options:
  viewport width/height, scale factor, theme, background color, font fallback,
  and output format.
- Emit `snapshot.json` with renderer version, viewport, scale, theme, image
  dimensions, nonblank status, and optional pixel comparison metadata.

### Pixel Comparison

- Add a small image comparison tool or test helper that compares actual PNGs
  against baselines and writes:
  `pixel-diff.json`, `pixel-diff.png`, and a concise console summary.
- Use thresholds that are strict enough to catch visible regressions but tolerant
  of platform font differences:
  total changed pixels, maximum per-channel delta, mean absolute error, and
  percentage changed.
- Store baselines only for generic public fixtures.

### Public Reference Capture

- Extend the existing `windows-native-screenshot` workflow so it can capture
  public fixture windows as reference images.
- Add an optional workflow job that downloads the macOS/Skia fixture snapshot,
  compares it against the Windows reference where practical, and uploads both
  images plus diff artifacts.
- Keep private application capture instructions out of this repository.

### CLI and Artifacts

- Add runner options:
  `--renderer skia-v2`, `--viewport <width>x<height>`, `--scale <number>`,
  `--theme light|dark`, `--baseline <path>`, and `--diff-output <path>`.
- Keep existing commands working:
  `winui3-mac-doctor` and `winui3-mac-runner run`.
- Document artifact paths in `README.md` and `docs/architecture/artifacts.md`.

### Fixtures and Tests

- Add or extend public fixtures for:
  navigation shell, text-heavy content, form controls, list content, disabled
  states, dark theme, and image placeholders.
- Add unit tests for layout export and renderer selection.
- Add snapshot smoke tests that verify PNG dimensions, nonblank pixels, and
  deterministic output for the same fixture/options.
- Add a public GitHub Actions workflow gate for Windows reference screenshot
  artifacts.

## Implementation Steps

1. Add the renderer options contract.
   - Create immutable render option records in `WinUI3.MacRuntime`.
   - Parse CLI options in `WinUI3.MacRunner`.
   - Extend `snapshot.json` without breaking current fields.
   - Verify with unit tests for CLI parsing and snapshot metadata.

2. Add facade layout export.
   - Implement a simple measure/arrange pass for the supported controls.
   - Export arranged rectangles and visual properties through `tree.json`.
   - Verify with runtime tests over existing fixtures.

3. Implement `skia-v2` painters.
   - Keep control painters small and explicit.
   - Start with `Window`, `Grid`, `StackPanel`, `Border`, `TextBlock`,
     `Button`, `Frame`, and `NavigationView`.
   - Add `ListView`, `TextBox`, `Image`, and `FontIcon` after the shell path is
     stable.
   - Verify generated PNG size, nonblank pixel count, and stable hashes for
     deterministic fixtures.

4. Add pixel comparison artifacts.
   - Add test helper or CLI comparison path for PNG baselines.
   - Emit `pixel-diff.json` and `pixel-diff.png`.
   - Add tests for identical images, small tolerated differences, and failing
     differences.

5. Upgrade public fixtures.
   - Add generic fixture screens that exercise the supported visual subset.
   - Add baselines under a clear public fixture directory.
   - Keep fixture copy generic and product-neutral.

6. Extend Windows reference workflow.
   - Capture the generic public fixture on `windows-latest`.
   - Upload the Windows reference PNG.
   - Upload Skia PNG and pixel diff artifacts when available.
   - Keep the workflow independent from private repositories and secrets.

7. Update docs and compatibility matrix.
   - Document what pixel fidelity means for this project.
   - Document supported controls, artifact paths, CLI examples, and limitations.
   - Keep the README clear that this is not full WinUI 3 compatibility.

## Verification Gates

Run these before committing:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --viewport 1280x800 --scale 1 --theme light
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --viewport 1280x800 --scale 1 --theme light --script ./fixtures/InteractionBindingApp.MacTest/interactions.json
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
rg -n "<private-name-denylist-regex>" .
```

For the Windows reference workflow:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

Inspect downloaded PNG artifacts before final handoff.

## Risks And Mitigations

- Font differences can create noisy diffs.
  Mitigation: document thresholds, include deterministic font fallback, and
  compare structural dimensions separately from raw pixels.
- Scope can drift into full WinUI parity.
  Mitigation: keep the compatibility matrix explicit and limit painters to the
  supported fixture subset.
- Golden images can become brittle.
  Mitigation: keep public fixtures small, review baseline updates carefully, and
  store diff metadata for diagnosis.
- GitHub-hosted Windows image changes can shift pixels.
  Mitigation: pin documented runner expectations where useful and keep
  thresholds visible in `pixel-diff.json`.

## Rollback Or Recovery

- Keep the existing SVG and current Skia renderer paths available until
  `skia-v2` is stable.
- If pixel comparison is flaky, disable only the strict compare gate while still
  uploading PNG artifacts.
- If the Windows workflow changes behavior, keep local macOS Skia snapshot
  smoke tests as the minimum verified path.

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

Use `$google-eng-practices` and implement the plan in `docs/plans/2026-06-01-pixel-visual-screenshot-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. Keep the primary path Wine-free and do not claim full WinUI 3 compatibility. Do not add private product names, private repository references, screenshots, or proprietary fixture content. Keep identifiers, comments, and canonical docs in English.

Implement pixel-level visual screenshot support by adding deterministic render options, facade layout export, a `skia-v2` renderer path with explicit painters for the supported public fixture subset, pixel comparison artifacts (`pixel-diff.json` and `pixel-diff.png`), upgraded public fixtures, and an extended `windows-latest` reference screenshot workflow. Preserve existing `winui3-mac-doctor` and `winui3-mac-runner` commands and keep existing SVG/current Skia behavior working during the transition.

Run `dotnet build`, `dotnet test`, `PATH="$PWD/tools:$PATH" winui3-mac-doctor`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --viewport 1280x800 --scale 1 --theme light`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --viewport 1280x800 --scale 1 --theme light --script ./fixtures/InteractionBindingApp.MacTest/interactions.json`, `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release`, and `rg -n "<private-name-denylist-regex>" .` with the operator-provided private-name denylist. Trigger the public `windows-native-screenshot` workflow on GitHub Actions, wait for it to finish, download the screenshot artifact, and inspect the PNG before final handoff.

Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately.
