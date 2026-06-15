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

There are three supported approaches. Direct app project ingestion is the
preferred path because it does not ask the downstream app to add any project.

### Option 0: Direct App Project Ingestion (No Fixture Required)

Point `--project` at the real WinUI Windows app `.csproj` and supply a scenario
that selects the entry surface. The runner inspects the project, generates a
temporary source-level host under
`/private/tmp/winui3-mac-test-runtime/generated-hosts/`, renders the selected
page or window/route, and runs the scenario automation. The original Windows
project is never mutated or built, and the `.exe`/`.msix` is never executed.

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./apps/windows/MainApp.WinUI/MainApp.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./scenarios/shell-home-light.json \
  --output ./artifacts/mac-runtime-direct/shell-home-light
```

The scenario `entry` selects the surface (`{ "mode": "page", "xaml":
"Pages/HomePage.xaml" }` or `{ "mode": "window", "xaml": "MainWindow.xaml",
"route": "home" }`); see the shared automation scenario contract below. This run
emits `tree.json`, `accessibility.json`, `interactions.json`,
`visual/mac-runtime.png`, and `project-ingestion.json`. The
`project-ingestion.json` `windowsOnlyBoundaries` list is a non-blocking, honest
record of Windows-only behavior the runner did not execute (WinRT storage,
credential lockers, packaged activation, system backdrops, and Windows App SDK
deployment). Use Options A and B only when you also want a smaller, public-safe
fixture checked into a public repository.

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

Run a strict comparison when a Windows reference PNG or normalized native
reference import directory exists:

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/winui-compat/PublicWorkbench.WinUI/PublicWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./tests/winui-compat/PublicWorkbench.WinUI/scenarios/workbench-light.json \
  --strict-visual \
  --reference ./artifacts/native-reference-import \
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

## Full Production XAML Diagnostics

Downstream applications may run full production XAML compile probes against
private app source, but the raw diagnostic output should stay in the app-owned
repository or private QA evidence location when it contains private paths,
route names, product copy, or proprietary page structure.

When a downstream app needs upstream runtime work, share a sanitized gap
summary instead of the raw diagnostics. The public summary should group by
diagnostic code, broad file category, unsupported XAML surface, count, and
current treatment, without absolute paths or product-specific labels. The
runtime-owned baseline for this process is
`docs/compatibility/downstream-production-xaml-gap-summary.json`.

Example private probe:

```sh
winui3-mac-runner xaml compile \
  --output /private/tmp/downstream-prod-xaml-generated \
  ./src/MyApp.Windows/**/*.xaml
```

Keep `/private/tmp/downstream-prod-xaml-generated.diagnostics.json` private
when it includes app-owned paths or source details, then update the sanitized
summary only with public-safe gap families and counts.

## CI Test Tiers

Run tests in four tiers.

| Tier | Runner | Purpose | Required commands |
| --- | --- | --- | --- |
| Fast app tests | macOS or Windows | Normal app logic, view models, services. | App repository `dotnet test` or platform test command. |
| macOS compat smoke | macOS | Prove the public fixture builds and launches through the compatibility runtime. | `winui3-mac-runner doctor` and `winui3-mac-runner run --project ...`. |
| strict macOS visual | macOS | Prove supported controls, interactions, bindings, accessibility export, and renderer diagnostics are stable. | `winui3-mac-runner run --renderer skia-v2 --scenario ... --strict-visual`. |
| Windows UI automation reference | Windows | Drive the real native WinUI app through FlaUI 5.0 + FlaUI.UIA3, capture element state and screenshots, and use that as the automation source of truth. | FlaUI 5.0 + `FlaUI.UIA3` tests against the native app. |
| macOS automation adapter | macOS | Drive the compatibility runtime through the same semantic contract: automation ID, name, role/control type, bounds, state/value, action dispatch, and screenshot or crop capture. | Planned repo-owned FlaUI.UIA3-compatible adapter over runner artifacts. |
| Windows reference parity | Windows plus macOS | Capture a native WinUI Windows reference from the actual app, render macOS runtime, compare PNGs, and upload reviewable artifacts. | Windows screenshot capture job plus macOS diff job. |

The Windows reference tier should be the source of truth for visual claims only
when it captures the actual native WinUI app under test. The macOS tier catches
regressions quickly, but it cannot prove native Windows behavior by itself.
Likewise, current macOS artifacts are an automation boundary, not proof of full
FlaUI/UIA provider compatibility until the adapter has API-level tests.

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
4. Run `winui3-mac-runner native-reference-import --source <downloaded-dir>
   --output artifacts/native-reference-import` to validate and normalize
   downloaded references before review.
5. Run the matching local macOS strict scenario with
   `--reference artifacts/native-reference-import` and `--diff-output`.
6. Inspect `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`,
   `visual-run.json`, and, when available, `component-evidence.json` and
   reference provenance.
7. Record visible gaps as `good`, `usable`, `weak`, `poor`, or `not-rendered`.
8. Do not claim visual parity for weak or poor components.

For downstream Windows probe suites that publish a sweep script, make native
comparison explicit in release or visual-change gates. The runtime-owned
downstream probe sweep uses the normalized import directory and fails when
native comparison is required but any scenario is skipped or failed:

```sh
winui3-mac-runner native-reference-import \
  --source <downloaded-dir> \
  --output artifacts/native-reference-import

winui3-mac-runner-downstream-windows-probe-sweep \
  --output artifacts/winui3-mac/downstream-windows-probe-sweep \
  --reference artifacts/native-reference-import \
  --require-native-comparison
```

If this command reports `native comparison is required`, do not treat a passing
macOS runtime screenshot as visual parity. Add or fix the missing Windows
reference capture first, then rerun the reference-backed sweep.

## GitHub Actions Shape

For private app repositories, keep artifacts inside that repository's CI
retention boundary. Do not upload private screenshots to this public runtime
repository.

The updated default PR shape is documented in
`docs/architecture/ci-strategy.md`: `ubuntu-latest` portable headless for fast
source-level compatibility, `windows-latest` Windows native reference for truth,
and macOS only for local/manual/scheduled or release validation.

The current Mac-local/manual validation shape is:

1. A local developer Mac or self-hosted macOS runner installs
   `MarlonJD.WinUI3.MacRunner`.
2. The local macOS tier runs doctor, smoke, and strict visual scenarios.
3. A Windows job captures native WinUI Windows references for public-safe
   fixtures.
4. The local macOS tier downloads Windows references and runs strict pixel
   comparison.
5. The operator keeps runtime artifacts for review.

Use `docs/examples/consumer-github-actions.yml` only as a starting point for a
manual self-hosted macOS smoke and strict visual tier. Mirror the repository's
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
- Windows-only boundaries in a real app project, reported as non-blocking
  `windowsOnlyBoundaries` diagnostics in `project-ingestion.json` (WinRT
  storage, credential lockers, packaged activation, system backdrops, and
  Windows App SDK deployment) so a supported surface still renders while the run
  states exactly what it skipped.

## What Still Requires Windows

- Actual Windows App SDK packaging and deployment.
- `.msix`, `.exe`, and app lifecycle behavior.
- WinRT storage (`Windows.Storage.ApplicationData`) and credential lockers
  (`Windows.Security.Credentials.PasswordVault`); direct ingestion diagnoses
  these as Windows-only boundaries but does not execute or emulate them.
- Mica, Acrylic, system backdrops, compositor effects, and advanced Fluent
  visual states until the catalog and renderer explicitly support them.
- Native input, IME, pointer, touch, accessibility technology, and windowing
  behavior beyond the current facade subset.
- Native UI Automation provider behavior. The macOS side exposes a FlaUI/UIA3
  *compatible artifact adapter* over `tree.json`/`accessibility.json`/
  `interactions.json`, not a native macOS UIA provider; native FlaUI 5.0 + UIA3
  validation through `tools/WindowsUiAutomationProbe` remains the Windows
  reference tier.
- Final visual acceptance for production UI.
