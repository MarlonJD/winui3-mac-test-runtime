# Consumer Quick Start

This guide is for public projects that want source-level WinUI 3 smoke and
visual checks on macOS. The runtime does not run Windows binaries, `.msix`
packages, or arbitrary `.exe` files.

The current Level 0 through Level 7 surface is release-gate-ready for the
documented public source-level harness subset. Treat the compatibility matrix,
support policy, API catalog, and renderer-fidelity notes as the boundary for
what can be used locally today.

## Requirements

- .NET SDK `10.0.x`.
- A public fixture project that compiles managed .NET code against the
  documented `Microsoft.UI.Xaml` facade subset.
- Generic fixture content that can be published in CI artifacts.
- Scenario JSON files for strict visual runs when pixel comparison matters.

## Install The Runner

For package-based consumers, install the tool into a repo-local tool directory:

```sh
dotnet tool install MarlonJD.WinUI3.MacRunner \
  --version 0.1.0-alpha.1 \
  --tool-path ./.tools
```

Run doctor through the packaged tool:

```sh
./.tools/winui3-mac-runner doctor
```

Source checkouts of this repository can continue to use the wrapper scripts:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-doctor
```

## Add A Public Fixture Project

Keep the fixture narrow and realistic. It should represent a workflow your app
needs to smoke test, but it must avoid private names, secrets, proprietary
screenshots, and private repositories.

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

Use only controls, XAML constructs, bindings, interactions, and renderer
features listed as `supported` or `partial` in
`docs/compatibility/matrix.md` and
`docs/compatibility/winui-api-compatibility.catalog.json`. `planned`,
`windows-only`, `not supported`, and uncataloged `unknown` usage is reported as
a strict compatibility gap.

## Run Smoke And Strict Visual Checks

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/PublicWinUIFixture/PublicWinUIFixture.csproj

./.tools/winui3-mac-runner run \
  --project ./tests/PublicWinUIFixture/PublicWinUIFixture.csproj \
  --renderer skia-v2 \
  --scenario ./tests/PublicWinUIFixture/scenarios/light.json \
  --strict-visual
```

When a Windows reference PNG is available, add `--reference` and
`--diff-output`:

```sh
./.tools/winui3-mac-runner run \
  --project ./tests/PublicWinUIFixture/PublicWinUIFixture.csproj \
  --renderer skia-v2 \
  --scenario ./tests/PublicWinUIFixture/scenarios/light.json \
  --strict-visual \
  --reference ./artifacts/windows-reference/light/windows-reference.png \
  --diff-output ./artifacts/winui3-mac-visual/light
```

## Direct WinUI App Project Ingestion

You do not have to create a probe or copy production XAML into a test host. The
`--project` argument can point at a real WinUI Windows app `.csproj` (for
example one that targets `net10.0-windows10.0.19041.0`, sets `<UseWinUI>true`,
references `Microsoft.WindowsAppSDK`, and packages as MSIX). The runner inspects
the project, generates a temporary source-level host under
`/private/tmp/winui3-mac-test-runtime/generated-hosts/`, renders the scenario
entry, and runs the scenario automation. It never mutates or builds the original
Windows project, and it never runs the `.exe` or `.msix`.

The scenario file selects the entry surface and the integration actions:

```jsonc
{
  "name": "shell-home-light",
  "theme": "light",
  "entry": { "mode": "window", "xaml": "MainWindow.xaml", "route": "home" },
  "automation": [
    { "type": "assertAccessibilityState", "target": "automationId=shell-nav-home", "key": "selected", "parameter": "true" },
    { "type": "selectNavigation", "target": "automationId=shell-nav-messages" },
    { "type": "waitForIdle" }
  ],
  "visual": { "capture": true, "renderer": "skia-v2" }
}
```

```sh
./.tools/winui3-mac-runner run \
  --project ./src/MyApp.Windows/MyApp.Windows.csproj \
  --renderer skia-v2 \
  --scenario ./scenarios/shell-home-light.json \
  --output ./artifacts/mac-runtime-direct/shell-home-light
```

Use `entry.mode` `page` with `entry.xaml` set to a `Pages/*.xaml` file to render
a single page, or `window` with an optional `route`/`session` to render the app
shell. Direct ingestion writes `tree.json`, `accessibility.json`,
`interactions.json`, `visual/mac-runtime.png`, and `project-ingestion.json`.

`project-ingestion.json` includes `windowsOnlyBoundaries`: honest, non-blocking
diagnostics for Windows-only behavior the runner did not execute, such as
`Windows.Storage.ApplicationData`, `Windows.Security.Credentials.PasswordVault`,
`MicaBackdrop`/`SystemBackdrop`, packaged MSIX activation, and Windows App SDK
deployment references. `blocksRender` stays `false`, so a supported page/window
surface still renders while the report names what was skipped. These are
diagnostics, not a claim that the boundary behavior runs or matches Windows.

## CI

The updated default PR strategy is documented in
`docs/architecture/ci-strategy.md`: portable headless on `ubuntu-latest`,
Windows native reference on `windows-latest`, and macOS only for
local/manual/scheduled or release validation.

Use `docs/examples/consumer-github-actions.yml` only as a manual/self-hosted
macOS validation sample. The same commands can also be run directly on a local
developer Mac. The sample installs the packaged runner, runs doctor, executes a
managed fixture, and uploads strict visual artifacts.

For downstream Windows WinUI 3 app adoption, including why this tool exists and
which test tiers should run, see
`docs/consumption/downstream-windows-apps.md`.

## Operational Safety

The runner builds and executes source projects; it is not a sandbox. Run
private or untrusted app fixtures in isolated local or private CI workspaces,
avoid granting unnecessary secrets, and review screenshots, tree JSON,
accessibility JSON, SARIF, and logs before publishing artifacts. The full
threat model, artifact privacy policy, dependency policy, and release gates are
tracked in `docs/security/threat-model.md` and
`docs/release/release-gates.md`.

## Troubleshooting

- Strict visual failure: inspect `visual/visual-run.json`,
  `visual/pixel-diff.json`, `visual/windows-reference.png`,
  `visual/mac-runtime.png`, and `visual/pixel-diff.png`.
- XAML compiler gap: inspect the `XAML1001` through `XAML1006` diagnostics and
  compare the construct with the compatibility matrix before expanding the
  fixture.
- Renderer or API catalog gap: inspect `unsupported-apis.json` and
  `diagnostics.sarif` for `WINUI3MAC003`. Status values such as `planned`,
  `windows-only`, `not supported`, and `unknown` identify whether the API is a
  roadmap target, Windows validation target, explicit non-goal, or uncataloged
  gap.
- Binding or resource gap: inspect `binding-failures.json`,
  `resource-failures.json`, and SARIF rules `WINUI3MAC001` and
  `WINUI3MAC002`.
- Windows-only boundary in a direct app project: inspect
  `project-ingestion.json` `windowsOnlyBoundaries`. Each entry names the
  boundary category, the cataloged `api`/`status`, the `filePath`/`line`, and a
  `reason`. These never block rendering (`blocksRender` is `false`); they tell
  you which Windows-only behavior (storage, credentials, packaged activation,
  system backdrops, Windows App SDK deployment) was skipped on macOS.
- CI environment drift: compare `visual-run.json` runner metadata, rerun the
  public Windows reference workflow, and inspect the uploaded reference/runtime
  PNGs before changing thresholds.
- Packaged tool cannot find .NET on macOS: ensure the .NET SDK host location is
  discoverable. Homebrew installations may need `DOTNET_ROOT` exported to the
  `dotnet --info` base path's `libexec` root before invoking
  `winui3-mac-runner`.

## Known Limits

- No Windows binary, `.msix`, Wine, broad Mica/Acrylic rendering, compositor
  effects, or arbitrary WinUI 3 pixel parity claim in the current production
  subset.
- Mica, Acrylic, system backdrops, compositor concepts, shadows, transforms,
  motion, focus visuals, high contrast, reduced motion, and Fluent interaction
  states are compatibility targets tracked by the catalog and material contract.
- Controls, properties, actions, and XAML constructs are supported only to the
  extent documented in the compatibility matrix.
- Scenario thresholds are part of the public contract and must stay explicit in
  scenario JSON files.
