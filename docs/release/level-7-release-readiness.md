# Level 7 Release Readiness

Level 7 makes the alpha runtime consumable by public projects that accept the
documented compatibility limits. It does not expand the WinUI API subset by
itself; it records package, CI, troubleshooting, and release evidence contracts
for the supported public alpha levels.

Level 0 through Level 7 are the first alpha milestone toward full source-level
WinUI 3 development on macOS. They are not the final product scope.

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
- `winui3-mac-runner doctor` or source wrapper `winui3-mac-doctor` passes.
- Managed smoke fixture and current Skia smoke fixture pass.
- `skia-v2` strict visual fixtures pass for shell, interaction/binding, and
  control-gallery categories.
- `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj
  --configuration Release` passes.
- `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
  --configuration Release` passes.
- `dotnet pack` passes for every published package.
- Public `windows-native-screenshot` workflow passes.
- Review at least one `windows-reference.png`, `mac-runtime.png`, and
  `pixel-diff.png` for every changed fixture category.
- Compatibility catalog status counts and diagnostics match the released docs.
- Run the operator-provided private-name denylist scan before publishing.

## Verification Evidence

Latest public Windows visual workflow inspected during Level 7 readiness:
`windows-native-screenshot` run `26731192942`.

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
  goals, not current alpha claims.
- Mica, Acrylic, system backdrops, compositor concepts, shadows, transforms,
  motion, focus visuals, theme resources, high contrast, reduced motion, and
  Fluent interaction states are cataloged compatibility targets.
- Unavailable API usage must remain visible through structured artifacts and
  SARIF diagnostics with catalog statuses.
- Scenario thresholds are reviewed compatibility contracts, not hidden global
  tolerances.
- Hosted runner image changes can alter Windows reference screenshots; use
  workflow metadata and uploaded PNGs to review drift before changing
  thresholds.
