# Consumer Quick Start

This guide is for public projects that want source-level WinUI 3 smoke and
visual checks on macOS. The runtime does not run Windows binaries, `.msix`
packages, or arbitrary `.exe` files.

The current Level 0 through Level 7 surface is an alpha milestone toward full
source-level WinUI 3 development on macOS. Treat the compatibility matrix and
API catalog as the boundary for what can be used locally today.

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

## CI

Use `docs/examples/consumer-github-actions.yml` as the starting point for public
consumer CI. The sample installs the packaged runner, runs doctor, executes a
managed fixture, and uploads strict visual artifacts.

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
- CI environment drift: compare `visual-run.json` runner metadata, rerun the
  public Windows reference workflow, and inspect the uploaded reference/runtime
  PNGs before changing thresholds.
- Packaged tool cannot find .NET on macOS: ensure the .NET SDK host location is
  discoverable. Homebrew installations may need `DOTNET_ROOT` exported to the
  `dotnet --info` base path's `libexec` root before invoking
  `winui3-mac-runner`.

## Known Limits

- No Windows binary, `.msix`, Wine, broad Mica/Acrylic rendering, compositor
  effects, or arbitrary WinUI 3 pixel parity claim in the current alpha.
- Mica, Acrylic, system backdrops, compositor concepts, shadows, transforms,
  motion, focus visuals, high contrast, reduced motion, and Fluent interaction
  states are compatibility targets tracked by the catalog and material contract.
- Controls, properties, actions, and XAML constructs are supported only to the
  extent documented in the compatibility matrix.
- Scenario thresholds are part of the public contract and must stay explicit in
  scenario JSON files.
