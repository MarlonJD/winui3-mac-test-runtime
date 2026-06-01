# WinUI3 Mac Test Runtime

Wine-free feasibility harness for running a constrained subset of WinUI-style
C# application code on macOS for automated tests.

This repository does not run arbitrary Windows binaries and does not claim full
WinUI 3 compatibility. The current runtime runs managed .NET assemblies against
clean-room `Microsoft.UI.Xaml` facade types, hosts the app in a macOS .NET
process, and emits structured artifacts for test inspection.

## Smoke Commands

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-light.json --strict-visual
```

## Consumer Quick Start

Public consumer projects can install the packaged runner and execute a managed
fixture without Wine:

```sh
dotnet tool install MarlonJD.WinUI3.MacRunner --version 0.1.0-alpha.1 --tool-path ./.tools
./.tools/winui3-mac-runner doctor
./.tools/winui3-mac-runner run --project ./tests/PublicWinUIFixture/PublicWinUIFixture.csproj
```

See `docs/consumption/quick-start.md` for fixture setup, strict visual commands,
troubleshooting, and known limits. See
`docs/examples/consumer-github-actions.yml` for a public consumer CI starting
point.

## Current Fixtures

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/XamlTinyWinUIApp.MacTest/XamlTinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --script ./fixtures/InteractionBindingApp.MacTest/interactions.json
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-light.json --strict-visual
```

`XamlTinyWinUIApp.MacTest` exercises the Phase 1 XAML compiler path:
`x:Class`, `x:Name`, text content, event hookup, and a simple static resource.

`SampleAdminShell.MacTest` is a headless shell compatibility fixture with a
desktop admin navigation shape. It validates the current `NavigationView`,
`NavigationViewItem`, `Frame`, `Grid`, `Border`, and `FontIcon` facade subset
and exports admin navigation visibility in `tree.json`.

`InteractionBindingApp.MacTest` exercises page navigation, binding refresh,
two-way binding, `INotifyPropertyChanged`, observable item binding, command
execution, button click simulation, focus, text input, item selection, property
assertions, accessibility state export, binding failure export, list/text/image
facade export, and a deterministic snapshot artifact.

`ControlGallery.MacTest` exercises the Level 2 public control subset:
`ScrollViewer`, `ContentControl`, `ItemsControl`, `CheckBox`, `RadioButton`,
`ToggleButton`, `ComboBox`, `ProgressBar`, `ProgressRing`, `InfoBar`,
`CommandBar`, and `AppBarButton`. It also exercises Level 3 style setter
application and the high-contrast `skia-v2` theme scenario.

Scenario JSON files under each fixture's `scenarios/` directory describe the
strict visual contract for the supported public subset. A scenario can set the
viewport, scale, theme, interaction actions, strict visual mode, and pixel diff
thresholds. The stricter renderer path is opt in:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json \
  --viewport 960x640 \
  --scale 1 \
  --theme light \
  --strict-visual \
  --reference ./artifacts/windows-reference-screenshots/shell-light/windows-reference.png \
  --diff-output ./artifacts/windows-native-screenshot/shell-light
```

The runner writes artifacts to `artifacts/winui3-mac/` by default:

- `run.json`: runtime, Wine dependency, project, assembly, and status metadata.
- `tree.json`: logical UI tree exported from the facade-backed app.
- `accessibility.json`: role/name/label tree derived from the logical UI tree.
- `binding-failures.json`: versioned binding failure envelope observed while
  refreshing the tree.
- `resource-failures.json`: versioned static or theme resource lookup miss
  envelope.
- `unsupported-apis.json`: versioned envelope for placeholder facade APIs
  touched by the app.
- `diagnostics.sarif`: warning diagnostics for bindings, resources, and
  unsupported APIs with stable rule IDs.
- `interactions.json`: optional scripted interaction results.
- `snapshot.json` and `screenshots/snapshot.svg`: deterministic nonblank
  snapshot output for smoke and regression tests. Passing `--renderer skia`
  writes `screenshots/snapshot.png` with the Skia renderer.
- `screenshots/mac-runtime.png`: deterministic PNG from `--renderer skia-v2`
  for scenario-driven visual comparison.
- `visual/visual-run.json`: scenario metadata, strict visual status,
  unsupported visual features, and pixel comparison summary.
- `visual/windows-reference.png`, `visual/mac-runtime.png`,
  `visual/pixel-diff.png`, and `visual/pixel-diff.json`: comparison artifacts
  when a Windows reference PNG is provided.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.

## Compatibility Status

This is still a constrained test runtime. It supports a small source-level
WinUI-style subset for automated macOS testing and intentionally does not claim
binary compatibility, arbitrary `.exe` execution, or full WinUI 3 behavior.

The published compatibility claim includes **Level 0: Harness Reliability**,
the documented **Level 1: Core App And XAML Compatibility** subset, the
public-fixture-backed **Level 2: Layout And Controls Foundation** and
**Level 3: Styling, Resources, And Theme Fidelity** subsets, the
fixture-backed **Level 4: Data Binding, Commands, And State** and **Level 5:
Input, Accessibility, And Automation** subsets, **Level 6: Windows Reference
Visual Compatibility** for the public strict fixture categories, and **Level 7:
Release And Consumption Readiness** for package smoke, consumer quick start,
sample CI, release checklist, and known-gap documentation.
See `docs/compatibility/contracts.md` for the public compatibility contract and
`docs/compatibility/matrix.md` for the current supported subset.

## Package Smoke

```sh
dotnet pack src/WinUI3.MacTest.Sdk/WinUI3.MacTest.Sdk.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacCompat/WinUI3.MacCompat.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRuntime/WinUI3.MacRuntime.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacXaml/WinUI3.MacXaml.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRenderer.Skia/WinUI3.MacRenderer.Skia.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRunner/WinUI3.MacRunner.csproj --configuration Release --output ./artifacts/packages
```

`MarlonJD.WinUI3.MacRunner` is packaged as a .NET tool with the
`winui3-mac-runner` command. `winui3-mac-doctor` remains available as a source
checkout wrapper and as `winui3-mac-runner doctor`.

Release readiness evidence and the operator checklist live in
`docs/release/level-7-release-readiness.md`.

## Windows Native Screenshot Harness

The `windows-native-screenshot` workflow runs on GitHub's `windows-latest`
runner and captures client-area PNG reference screenshots from real Windows
desktop windows. A follow-up `macos-latest` job renders the matching public
scenario through `skia-v2`, compares the two PNGs, and uploads reviewable
reference/runtime/diff artifacts for the shell, interaction/binding, and
control-gallery fixture categories.

The capture tool can be pointed at any Windows desktop app by changing the
window title and command:

```sh
dotnet run --project tools/WindowsWindowCapture/WindowsWindowCapture.csproj -- \
  --title "WinUI3 Mac Test Runtime - shell-light" \
  --output artifacts/windows-reference-screenshots/shell-light/windows-reference.png \
  --metadata-output artifacts/windows-reference-screenshots/shell-light/windows-reference.json \
  --client-area \
  -- dotnet run --project fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
```

The workflow uses only generic public fixture content and public GitHub-hosted
runners. It does not require Wine, private repositories, secrets, or private
screenshots. Passing visual comparison means the documented fixture/control
subset stayed within the scenario thresholds; it is not a claim of arbitrary
WinUI 3 pixel compatibility.

## License

This project is licensed under `LGPL-3.0-or-later`. See `LICENSE` for the GNU
Lesser General Public License text and `COPYING` for the GNU General Public
License text that LGPLv3 extends.
