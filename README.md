# WinUI3 Mac Test Runtime

Wine-free alpha runtime for source-level WinUI 3 application development on
macOS.

The product goal is full source-level WinUI 3 C# and XAML development from
macOS: developers should be able to build, run, test, inspect, and visually
validate real WinUI 3 app code locally while public `windows-latest` GitHub
Actions runs remain the behavioral and visual source of truth. The current
Level 0 through Level 7 surface is the first alpha milestone toward that goal,
not the final product scope.

This repository does not run arbitrary Windows binaries, `.msix` packages, or
`.exe` files on macOS. The current runtime runs managed .NET assemblies against
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
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-high-contrast.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual
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
point. See `docs/consumption/downstream-windows-apps.md` for why this tool
exists, how a downstream Windows WinUI 3 app can use it, and which tests should
run locally and in CI.

## Current Fixtures

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/XamlTinyWinUIApp.MacTest/XamlTinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --script ./fixtures/InteractionBindingApp.MacTest/interactions.json
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual
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

`PublicAdminWorkbench.WinUI` is a public Windows-targeted WinUI 3 source
fixture. It uses `net10.0-windows10.0.19041.0`, `<UseWinUI>true</UseWinUI>`,
`Microsoft.WindowsAppSDK`, and XAML items like a normal Windows project, but the
macOS runner ingests it through a generated compat shadow build. The original
project is not mutated and the runner writes `project-ingestion.json` with
included files, excluded Windows-only items, catalog statuses, unsupported
project features, and XAML diagnostics.

`ComponentParityLab.WinUI` is a public Windows-targeted component parity lab
fixture with eight clean-room pages: basic input, text/forms, collections,
dialogs/flyouts, commands/menus, navigation/workbench, status/pickers, and
layout/media/resources. Its scenario files declare component requirements,
source-feature requirements, expected catalog status, interaction coverage,
minimum visual grade, and known gaps. The runner writes
`visual/component-evidence.json` so a whole screenshot pass cannot hide weak or
diagnostic-only components.

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
- `project-ingestion.json`: emitted when a Windows-targeted WinUI source
  project is redirected through compat shadow build discovery.
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
- `visual/component-evidence.json`: component-level catalog status, presence,
  interaction status, visual grade, known gaps, and optional diff metrics when a
  reference-backed comparison supplies them.
- `visual/windows-reference.png`, `visual/mac-runtime.png`,
  `visual/pixel-diff.png`, and `visual/pixel-diff.json`: comparison artifacts
  when a Windows reference PNG is provided.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.

## Visual Parity Evidence

Public visual evidence lives in `docs/visual-parity/`. It includes real Windows
reference screenshots, macOS runtime screenshots, pixel-diff images, and
`visual-run.json` metrics from public GitHub Actions runs. Component lab runs
also publish `component-evidence.json`; treat that file as the component-level
truth for `good`, `usable`, `weak`, `poor`, or `not-rendered` grades. A whole
screenshot that passes thresholds is necessary smoke evidence, but it is not
enough to call every visible control visually good.

The current `public-admin-workbench-light` evidence comes from public workflow
run
[`26752174485`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26752174485)
and passed strict comparison:

| Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Windows reference](docs/visual-parity/examples/public-admin-workbench-light/windows-reference.png) | ![macOS runtime](docs/visual-parity/examples/public-admin-workbench-light/mac-runtime.png) | ![Pixel diff](docs/visual-parity/examples/public-admin-workbench-light/pixel-diff.png) |

For this scenario, `16.01%` of pixels changed, `83.99%` were byte-identical,
mean absolute error was `8.50`, and RMS error was `41.09`, all inside the
scenario thresholds. This should be read as **weak visual parity / source
ingestion smoke evidence**, not as a broad component-quality claim. The
matching parts are the Windows-targeted project ingestion path, navigation
shell, selected state, list/detail workbench shape, and command-click
assertion. The visible gaps are still important: exact Fluent control chrome,
command surfaces, InfoBar/list/detail painters, text metrics, focus visuals,
shadows, Mica/Acrylic, and native interaction states are not yet pixel-perfect.

### Component Parity Examples

The component parity lab examples below come from public GitHub Actions run
[`26757799015`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26757799015).
Each row shows the Windows reference first, then the current macOS runtime
rendering from this library, then the pixel diff. These are examples, not broad
parity claims: `component-evidence.json` remains the source of truth for each
component's catalog status, presence, interaction status, visual grade, and
known gaps.

| Scenario | Windows reference | macOS runtime | Pixel diff | Evidence summary |
| --- | --- | --- | --- | --- |
| `component-basic-input-light` | ![Windows basic input component reference](docs/visual-parity/examples/component-basic-input-light/windows-reference.png) | ![macOS basic input component runtime](docs/visual-parity/examples/component-basic-input-light/mac-runtime.png) | ![Basic input pixel diff](docs/visual-parity/examples/component-basic-input-light/pixel-diff.png) | 13 components: 5 `usable`, 8 `not-rendered`; changed pixels `8.07%`, MAE `7.46`, RMS `39.75`. |
| `component-commands-menus-light` | ![Windows commands and menus component reference](docs/visual-parity/examples/component-commands-menus-light/windows-reference.png) | ![macOS commands and menus component runtime](docs/visual-parity/examples/component-commands-menus-light/mac-runtime.png) | ![Commands and menus pixel diff](docs/visual-parity/examples/component-commands-menus-light/pixel-diff.png) | 8 components: 3 `weak`, 5 `not-rendered`; weak items are `CommandBar`, `AppBarButton`, and `AppBarButton.Icon`. |
| `component-layout-media-light` | ![Windows layout and media component reference](docs/visual-parity/examples/component-layout-media-light/windows-reference.png) | ![macOS layout and media component runtime](docs/visual-parity/examples/component-layout-media-light/mac-runtime.png) | ![Layout and media pixel diff](docs/visual-parity/examples/component-layout-media-light/pixel-diff.png) | 28 components/features: 6 `usable`, 4 `weak`, 18 `not-rendered`; weak items include `Grid`, `Border`, `FontIcon`, and `Image`. |

See `docs/visual-parity/README.md` for the current evidence table and
interpretation notes.

## Compatibility Status

This is still an alpha compatibility runtime. The published alpha milestone
includes **Level 0: Harness Reliability**, **Level 1: Core App And XAML
Compatibility**, **Level 2: Layout And Controls Foundation**, **Level 3:
Styling, Resources, And Theme Fidelity**, **Level 4: Data Binding, Commands,
And State**, **Level 5: Input, Accessibility, And Automation**, **Level 6:
Windows Reference Visual Compatibility**, and **Level 7: Release And
Consumption Readiness**. These levels describe the current supported public
subset; they are not a cap on the long-term WinUI 3 source-compatibility goal.

The compatibility catalog in
`docs/compatibility/winui-api-compatibility.catalog.json` is the public seed for
tracking the broader roadmap. It classifies WinUI 3 / Windows App SDK APIs,
XAML constructs, Fluent resources, Mica, Acrylic, system backdrops,
composition/effect concepts, visual states, and animation-related APIs as
`supported`, `partial`, `planned`, `windows-only`, or `not supported`. Unknown
public usage is reported as a catalog gap instead of silently passing.

Mica, Acrylic, compositor effects, shadows, transforms, motion, focus visuals,
theme resources, high contrast, reduced motion, and full Fluent interaction
states are compatibility targets. In this alpha, most material and composition
entries are cataloged and diagnosed as planned rather than broadly rendered.
See `docs/compatibility/contracts.md` for the public compatibility contract and
`docs/compatibility/matrix.md` for the current supported subset. See
`docs/compatibility/api-catalog.md` and
`docs/compatibility/component-support.md` for the readable component-by-component
support matrix, and `docs/compatibility/winui-component-inventory.json` for the
component parity lab inventory. See `docs/compatibility/material-composition.md`
for the material compatibility contract.

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
reference/runtime/diff artifacts for the shell, interaction/binding,
control-gallery, public admin/workbench, and component parity lab fixture
categories.

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
screenshots. Passing visual comparison means the documented alpha
fixture/control subset stayed within the scenario thresholds. The workflow also
includes `public-admin-workbench-light`, which captures a real Windows
reference for the public Windows-targeted admin/workbench source fixture and
compares it with the macOS shadow-build runtime output. Passing the workflow is
not a claim of arbitrary WinUI 3 pixel compatibility, and visibly weak
components must remain labeled `weak` or `poor` in component evidence until
public reference artifacts justify a stronger grade.

## License

This project is licensed under `LGPL-3.0-or-later`. See `LICENSE` for the GNU
Lesser General Public License text and `COPYING` for the GNU General Public
License text that LGPLv3 extends.
