# Level 7 Release Readiness

Level 7 makes the documented public source-level subset consumable by public
projects that accept the compatibility limits. It does not expand the WinUI API
subset by itself; it records package, CI, troubleshooting, and release evidence
contracts for the supported public levels.

Level 0 through Level 7 are production-ready for the documented public subset.
They are not a claim of arbitrary WinUI 3 app compatibility.

For the broader production readiness assessment, including completed work,
known gaps, and production blockers, see `docs/release/production-readiness.md`.

## Published Package Set

Version: `0.1.0-alpha.1`

- `MarlonJD.WinUI3.MacRunner`: .NET tool containing `winui3-mac-runner` and the
  packaged `doctor` command.
- `MarlonJD.WinUI3.MacCompat`: clean-room `Microsoft.UI.Xaml` facade subset.
- `MarlonJD.WinUI3.MacRuntime`: managed macOS host and deterministic artifacts.
- `MarlonJD.WinUI3.MacXaml`: documented public XAML compiler subset.
- `MarlonJD.WinUI3.MacRenderer.Skia`: current Skia and `skia-v2` renderers.
- `MarlonJD.WinUI3.MacTest.Sdk`: MSBuild integration package for compatibility
  test projects.

## Consumer Entry Points

- Quick start: `docs/consumption/quick-start.md`.
- Consumer GitHub Actions example:
  `docs/examples/consumer-github-actions.yml`.
- Compatibility matrix: `docs/compatibility/matrix.md`.
- API catalog: `docs/compatibility/api-catalog.md` and
  `docs/compatibility/winui-api-compatibility.catalog.json`.
- Material/composition contract:
  `docs/compatibility/material-composition.md`.
- Artifact schema contract: `docs/architecture/artifacts.md`.

## Release Checklist

- `dotnet build` passes.
- `dotnet test` passes.
- `winui3-mac-runner benchmark` passes and uploads `benchmark.json` with
  performance and flake metrics.
- `winui3-mac-runner release-check` passes after the package dry run and
  uploads `release-readiness.json`.
- `winui3-mac-runner doctor` or source wrapper `winui3-mac-doctor` passes.
- Managed smoke fixture and current Skia smoke fixture pass.
- `skia-v2` strict visual fixtures pass for shell, interaction/binding, and
  control-gallery categories.
- `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj
  --configuration Release` passes.
- `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
  --configuration Release` passes.
- `dotnet pack` passes for every published package.
- Threat model, release gates, package provenance, and artifact privacy docs
  are current.
- Production support policy is current and `release-check` gated.
- Public `windows-native-screenshot` Windows reference workflow passes for the
  current production subset.
- Review at least one `windows-reference.png`, `mac-runtime.png`, and
  `pixel-diff.png` for every changed fixture category after local macOS
  comparison, including reference provenance when present.
- Treat current `windows-reference.png` files as synthetic `WindowsNativeProbe`
  harness references unless provenance identifies them as native WinUI fixture
  captures.
- Compatibility catalog status counts and diagnostics match the released docs.
- Run the operator-provided private-name denylist scan before publishing.

## Verification Evidence

Latest public native WinUI reference workflow inspected during Level 7
readiness: `windows-native-screenshot` run `26791576394` on commit `cd814a4`.

Latest production gate CI inspected during Level 7 readiness: `ci` run
`26791576401` on commit `cd814a4`.

Local package smoke:

- All published packages packed to `artifacts/packages`.
- A clean ignored sample project installed
  `MarlonJD.WinUI3.MacCompat` from the local package output and built
  successfully.
- The locally packed `MarlonJD.WinUI3.MacRunner` tool installed into the clean
  sample project and `winui3-mac-runner doctor` passed with explicit
  `DOTNET_ROOT` for the Homebrew .NET host.

Inspected categories:

- `shell-light`: `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`.
- `interactions-light`: `windows-reference.png`, `mac-runtime.png`,
  `pixel-diff.png`.
- `control-gallery-light`: `windows-reference.png`, `mac-runtime.png`,
  `pixel-diff.png`.

## Known Gaps

- No Windows binary, `.msix`, arbitrary `.exe`, or Wine execution support.
- Full source-level WinUI 3 development, Windows App SDK API coverage, Fluent
  material, compositor, Mica, Acrylic, and arbitrary pixel parity remain product
  goals, not current production-subset claims.
- Mica, Acrylic, system backdrops, compositor concepts, shadows, transforms,
  motion, focus visuals, theme resources, high contrast, reduced motion, and
  Fluent interaction states are cataloged compatibility targets.
- Unavailable API usage must remain visible through structured artifacts and
  SARIF diagnostics with catalog statuses.
- Scenario thresholds are reviewed compatibility contracts, not hidden global
  tolerances.
- Hosted Windows runner image changes can alter synthetic or native WinUI
  reference screenshots; use workflow metadata and uploaded PNGs to review
  drift before changing thresholds.
- Checked-in `WindowsNativeProbe` reference examples are harness smoke evidence
  only; production visual claims still require inspected native WinUI reference
  provenance for the changed scenario.
