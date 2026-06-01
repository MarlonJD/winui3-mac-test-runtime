# Downstream Windows App Adoption

This guide explains why the WinUI3 Mac Test Runtime exists, how a downstream
Windows WinUI 3 application can use it, and which tests should run locally and
in CI.

Use this document for private or product-specific Windows app repositories
without copying private app names, private screenshots, secrets, or proprietary
fixture content into this public repository.

## Why This Tool Exists

Many WinUI 3 teams develop on macOS part of the time, but real WinUI 3 apps
normally require Windows App SDK build targets, Windows UI runtime behavior, and
Windows-hosted screenshots for final validation. This tool fills the gap between
ordinary unit tests and full Windows-native verification:

- It gives macOS developers a fast source-level smoke test for WinUI C# and
  XAML before they switch to Windows.
- It ingests Windows-targeted WinUI projects through a Wine-free compat shadow
  build when the project is in the supported subset.
- It reports unsupported project, XAML, resource, binding, visual, and facade
  API usage through versioned artifacts instead of silently passing.
- It produces deterministic macOS runtime artifacts: `run.json`, `tree.json`,
  `accessibility.json`, `unsupported-apis.json`, `project-ingestion.json`,
  `mac-runtime.png`, `visual-run.json`, and optional pixel diffs.
- It keeps real Windows as the source of truth through
  `windows-native-screenshot.yml` and `windows-reference.png` artifacts.

This tool is not a Windows emulator, not a Wine-based app runner, not an `.msix`
or `.exe` compatibility layer, and not a replacement for Windows App SDK tests.
It is an early warning system and evidence collector for source compatibility,
diagnostics, automation, accessibility export, and visual parity work.

## How To Use It In A Windows WinUI 3 App

Use the runtime from the Windows app repository, not by copying private app code
into this public repository.

Recommended repository layout:

```text
apps/windows/MainApp.WinUI/MainApp.WinUI.csproj
tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj
tests/winui-compat/PublicWorkbench.WinUI/scenarios/workbench-light.json
artifacts/
```

The compatibility fixture should be public-safe and deterministic:

- Use generic labels, sample rows, and non-secret data.
- Keep workflows small enough to explain in a screenshot.
- Prefer one scenario per important route or component page.
- Avoid live network calls, real accounts, private product names, and private
  screenshots.
- Keep actual production app tests in the private app repository.

There are two supported fixture styles.

### Option A: Windows-Targeted WinUI Fixture

Use this when the fixture should look like a real WinUI project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
  </ItemGroup>
</Project>
```

The macOS runner detects the Windows-targeted WinUI project, writes
`project-ingestion.json`, excludes Windows-only App SDK build targets, and
builds a generated shadow project against the macOS compatibility facade.

### Option B: Mac Compatibility Fixture

Use this for a smaller smoke fixture that directly references the compatibility
package:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MarlonJD.WinUI3.MacCompat" Version="0.1.0-alpha.1" />
  </ItemGroup>
</Project>
```

This is faster to wire up, but it does not prove that a Windows-targeted WinUI
source project can be ingested through shadow build discovery.

## Scenario JSON

Each scenario should define the route, viewport, theme, interactions, strict
mode, and thresholds:

```json
{
  "schemaVersion": "0.1",
  "fixtureName": "public-workbench",
  "name": "workbench-light",
  "viewport": {
    "width": 1044,
    "height": 720
  },
  "scale": 1.0,
  "theme": "light",
  "strictVisual": true,
  "thresholds": {
    "changedPixelPercentage": 35.0,
    "maxChannelDelta": 255,
    "meanAbsoluteError": 28.0,
    "rootMeanSquaredError": 52.0
  },
  "interactions": [
    {
      "type": "click",
      "target": "ApproveCommand"
    },
    {
      "type": "assertProperty",
      "target": "StatusInfo",
      "key": "Title",
      "parameter": "Approved"
    }
  ]
}
```

Use strict visual thresholds as a documented contract. Do not loosen thresholds
to hide renderer bugs. When a screenshot looks visibly wrong, record that as
`weak` or `poor` parity in docs or `component-evidence.json`.

## Local Test Commands

Install the runner:

```sh
dotnet tool install MarlonJD.WinUI3.MacRunner \
  --version 0.1.0-alpha.1 \
  --tool-path ./.tools
```

Run environment diagnostics:

```sh
./.tools/winui3-mac-runner doctor
```

Run a source-level smoke test:

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj
```

Run a strict macOS visual scenario without a Windows reference:

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./tests/winui-compat/PublicWorkbench.WinUI/scenarios/workbench-light.json \
  --strict-visual
```

Run a strict comparison when a Windows reference PNG exists:

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./tests/winui-compat/PublicWorkbench.WinUI/scenarios/workbench-light.json \
  --strict-visual \
  --reference ./artifacts/windows-reference/workbench-light/windows-reference.png \
  --diff-output ./artifacts/winui3-mac-visual/workbench-light
```

Inspect these files after every failure:

- `run.json`
- `project-ingestion.json`
- `tree.json`
- `accessibility.json`
- `binding-failures.json`
- `resource-failures.json`
- `unsupported-apis.json`
- `diagnostics.sarif`
- `visual-run.json`
- `windows-reference.png`
- `mac-runtime.png`
- `pixel-diff.png`
- `pixel-diff.json`

## CI Test Tiers

Run tests in four tiers.

| Tier | Runner | Purpose | Required commands |
| --- | --- | --- | --- |
| Fast app tests | macOS or Windows | Normal app logic, view models, services. | App repository `dotnet test` or platform test command. |
| macOS compat smoke | macOS | Prove the public fixture builds and launches through the compatibility runtime. | `winui3-mac-runner doctor` and `winui3-mac-runner run --project ...`. |
| strict macOS visual | macOS | Prove supported controls, interactions, bindings, accessibility export, and renderer diagnostics are stable. | `winui3-mac-runner run --renderer skia-v2 --scenario ... --strict-visual`. |
| Windows reference parity | Windows plus macOS | Capture a native WinUI Windows reference from the actual app, render macOS runtime, compare PNGs, and upload reviewable artifacts. | Windows screenshot capture job plus macOS diff job. |

The Windows reference tier should be the source of truth for visual claims only
when it captures the actual native WinUI app under test. The macOS tier catches
regressions quickly, but it cannot prove native Windows behavior by itself.

## Pull Request Gate For A Downstream App

Minimum PR gate:

```sh
dotnet test
./.tools/winui3-mac-runner doctor
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./tests/winui-compat/PublicWorkbench.WinUI/scenarios/workbench-light.json \
  --strict-visual
```

Release or visual-change gate:

1. Run the PR gate.
2. Run the Windows reference workflow.
3. Download the `windows-reference-screenshots` artifact.
4. Run the matching local macOS strict scenario with `--reference` and
   `--diff-output`.
5. Inspect `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`,
   `visual-run.json`, and, when available, `component-evidence.json` and
   reference provenance.
6. Record visible gaps as `good`, `usable`, `weak`, `poor`, or `not-rendered`.
7. Do not claim visual parity for weak or poor components.

## GitHub Actions Shape

For private app repositories, keep artifacts inside that repository's CI
retention boundary. Do not upload private screenshots to this public runtime
repository.

The CI shape is:

1. A local developer Mac or self-hosted macOS runner installs
   `MarlonJD.WinUI3.MacRunner`.
2. The local macOS tier runs doctor, smoke, and strict visual scenarios.
3. A Windows job captures native WinUI Windows references for public-safe
   fixtures.
4. The local macOS tier downloads Windows references and runs strict pixel
   comparison.
5. The operator keeps runtime artifacts for review.

Use `docs/examples/consumer-github-actions.yml` as a starting point for the
self-hosted macOS smoke and strict visual tier. Mirror the repository's
`.github/workflows/windows-native-screenshot.yml` pattern when adding the
Windows reference tier.

## What This Catches

- XAML source constructs outside the supported subset.
- Project properties that cannot be shadow-built on macOS.
- Missing bindings and resources.
- Unsupported facade API usage.
- Accessibility export regressions.
- Interaction script failures.
- Renderer regressions for supported public controls.
- Visual drift against native WinUI Windows screenshots when references are
  provided.

## What Still Requires Windows

- Actual Windows App SDK packaging and deployment.
- `.msix`, `.exe`, and app lifecycle behavior.
- Mica, Acrylic, system backdrops, compositor effects, and advanced Fluent
  visual states until the catalog and renderer explicitly support them.
- Native input, IME, pointer, touch, accessibility technology, and windowing
  behavior beyond the current facade subset.
- Final visual acceptance for production UI.
