# WinUI3 Mac Test Runtime

Wine-free source-level WinUI 3 compatibility runtime for macOS.

The product goal is full source-level WinUI 3 C# and XAML development from
macOS: developers should be able to build, run, test, inspect, and visually
validate real WinUI 3 app code locally while public `windows-latest` GitHub
Actions runs provide the intended behavioral and visual source of truth from
actual native WinUI fixture apps. The current repository is strongest as a
source-level compatibility harness, catalog, artifact, and release-gate system.
The macOS visual renderer is not yet production-fidelity WinUI; current
`skia-v2` screenshots show usable scaffolding for a narrow public subset and
many simplified or intentionally `not-rendered` controls.

This repository does not run arbitrary Windows binaries, `.msix` packages, or
`.exe` files on macOS. The current runtime runs managed .NET assemblies against
clean-room `Microsoft.UI.Xaml` facade types, hosts the app in a macOS .NET
process, and emits structured artifacts for test inspection.

## Read This First

Current release readiness means **cataloged source-level harness and evidence
readiness for the documented public subset only**.

It does **not** mean full WinUI 3 compatibility. The current catalog has 126
entries: 55 `supported`, 35 `partial`, 31 `planned`, 3 `windows-only`, and 2
`not supported`. That is intentionally transparent: planned, Windows-only,
not-supported, and uncataloged APIs must fail or report diagnostics instead of
silently looking supported.

It also does **not** mean native WinUI visual fidelity. The checked-in public
component-quality dashboard currently tracks 58 public component rows: 51
`usable` source-level harness rows and 7 planned or not-supported
`not-rendered` diagnostic rows. `usable` means recognizable and functionally
testable, not pixel-matched Fluent chrome. The next project phase is renderer
fidelity and deeper automation evidence, not more release-gate expansion.

The checked-in public component-quality dashboard at
`docs/visual-parity/component-quality-dashboard.json` now has zero source-level
harness blocker rows. The rows have native/macOS/diff crop evidence, native
WinUI reference provenance, and manual inspection metadata, while all
`nativeQualityGrade` values remain `not-evaluated`; this is not a
native-quality visual claim.

Use this runtime when your app or fixture stays inside the documented
`supported` and `partial` subset. Do not use it as evidence that arbitrary WinUI
3 apps, templates, visual states, Mica/Acrylic, composition, advanced controls,
Windows binaries, `.msix` packages, or full Fluent pixel parity work on macOS.

## Productization Status

Current productization level is **L2** for the documented public source-level
harness subset. That means the catalog, runner artifacts, public component
evidence, native reference provenance, and checked-in dashboards are ready to
support source-level app and component harness validation for the documented
subset. It does not mean native-quality visual fidelity.

The authoritative level model is
`docs/compatibility/compatibility-levels.md` and
`docs/compatibility/compatibility-levels.json`. The current evidence rollup is
`docs/release/production-evidence-view.md`. State, interaction, and
accessibility gaps are tracked in
`docs/visual-parity/state-coverage-matrix.json`; rows labeled `default-only`
are not native-quality or product-ready state evidence. Each state requirement
also names the strict-sweep component, accessibility, and visual-run artifacts
that `public-product` must validate before release. Family-level native-quality
promotion queues are tracked in
`docs/visual-parity/native-quality-family-tranches.json`; every Milestone C
family remains blocked until its component rows have native-quality inspection
evidence and broader-than-default state coverage. Each family also publishes
its `stateRequirementCount`, `missingStateRequirementCount`, required state
names, and strict-sweep scenario names so Milestone C native-quality queues and
Milestone D state coverage gaps stay joined. The product evidence rollup can be
reproduced locally after producing the strict scenario sweep:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile strict-scenario-sweep --output artifacts/product-evidence/strict-scenario-sweep
PATH="$PWD/tools:$PATH" winui3-mac-runner product-evidence --profile public-product --output artifacts/product-evidence/public-product
```

The clean-checkout local release-candidate command is:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-release-ready-local
```

The script runs the full local gate in order: `dotnet build`, the compiled
MSTest assemblies, strict scenario product evidence, public product evidence,
package dry-run artifacts, `release-check`, and `release-candidate`.

Native-quality labels remain per-component-row labels only. Source-level
production labels, diagnostic exclusions, Windows-only exclusions, and non-goal
exclusions stay separate from native-quality promotion.

## Production Support Policy

The current support claim is limited to the public sanitized component subset
documented in `docs/compatibility/production-component-targets.md`. That subset
is validated as a source-level harness by local strict `skia-v2` runs, scripted
smoke/E2E scenarios, component evidence, and public native WinUI reference
screenshots from `windows-native-screenshot.yml`. It is not validated as
production visual fidelity.

The project does not claim arbitrary WinUI 3 app compatibility. APIs outside
the cataloged subset must remain `planned`, `windows-only`, `not supported`, or
`unknown` until they have fixture coverage, macOS artifact evidence, native
WinUI provenance, and release documentation.

The production support policy, final gate evidence, and residual risks are
tracked in `docs/release/support-policy.md` and
`docs/release/final-production-gate.md`. For the single-page summary of
catalog counts, Ring 0/Ring 1 status, latest recorded workflow IDs, strict
scenario results, and checked-in visual examples, see
`docs/release/production-evidence-view.md`.

The release decision is gated by `winui3-mac-runner release-candidate`, which
aggregates the deterministic local release requirements (126/126 catalog
dispositions, catalog/docs count consistency, zero unknown surfaces, broader-
control honesty, no OS composition claim, gated component-crop drift, the
component-quality dashboard, state coverage matrix, native-quality family
tranche freshness, native reference provenance, release docs, and the
private-name scan) and lists the external workflow requirements (full native
reference capture, full strict scenario sweep, and the package dry run with
`release-check`). The deterministic local gate is expected to pass when the
machine-readable artifacts are current;
external workflow/package evidence is still recorded separately. The exact
support boundary stays source-level WinUI 3 harness readiness for the documented
public subset, not Windows binary execution, arbitrary WinUI 3 compatibility, or
high-fidelity Fluent rendering.

## UI Automation Strategy

UI automation and screenshot capture are core project goals. The production
direction is a two-layer contract:

- Windows reference validation uses FlaUI 5.0 + FlaUI.UIA3 against real native
  WinUI apps whenever Windows UI Automation is available.
- The macOS runtime will expose the same semantic automation contract through
  repo-owned artifacts and adapters: stable automation IDs, names, control
  types or roles, bounding rectangles, state/value export, action dispatch, and
  full-window or element screenshot capture.

The current alpha supports runner-owned scripted interactions and deterministic
`tree.json`, `accessibility.json`, `interactions.json`, screenshot, crop, and
diff artifacts. It does not yet claim full FlaUI/UIA provider compatibility on
macOS. JSON accessibility export is useful evidence, but it is not by itself a
replacement for FlaUI 5.0 + FlaUI.UIA3 API-level automation tests.

## Optional Windows Font A/B Diagnostics

The `skia-v2` renderer can run an optional local A/B visual comparison with
licensed Windows design fonts. This is diagnostic evidence only: do not copy,
commit, package, or redistribute Windows font files in this repository.

Official Microsoft font resources:

- Windows design resources:
  https://learn.microsoft.com/en-us/windows/apps/design/downloads/
- Segoe UI font family reference:
  https://learn.microsoft.com/en-us/typography/font-list/segoe-ui
- Segoe Fluent Icons reference:
  https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-fluent-icons-font
- Segoe MDL2 Assets reference:
  https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-ui-symbol-font

Download the fonts from the Windows design resources page and place the
extracted `.ttf`, `.otf`, or `.ttc` files in a directory outside the repository,
for example:

```sh
mkdir -p "$HOME/winui-font-ab"
```

The current renderer looks for these families, in order:

- Text: `Segoe UI Variable`, then `Segoe UI`, then the platform fallback.
- Symbols: `Segoe Fluent Icons`, then `Segoe MDL2 Assets`, then the text-font
  fallback.

Run the native-backed diagnostic comparison by pointing
`WINUI3_MAC_TEST_FONT_DIRS` at the repo-external font directory:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference docs/visual-parity/examples/component-basic-input-light \
  --output artifacts/native-quality-tranche/font-ab-external-native
```

After the run, check `snapshot.json`, `visual/visual-run.json`, and
`visual/component-evidence.json`. A successful external-font diagnostic run
should report `requestedFamilyAvailable: true` and
`resolvedSource: external-font-directory` for the resolved text and symbol
roles. If a row only improves or passes with repo-external fonts, document that
environmental provenance before using the evidence for native-quality review.

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
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-smoke-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ProductionSmoke.WinUI/ProductionSmoke.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ProductionSmoke.WinUI/scenarios/production-e2e-workbench-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-dark.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ResourceCatalogApp.WinUI/ResourceCatalogApp.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ResourceCatalogApp.WinUI/scenarios/resource-catalog-high-contrast.json --strict-visual
```

## Public Application Corpus

`fixtures/corpus.json` curates a public, clean-room WinUI 3 application corpus
with varied project shapes (single-window, navigation shell, MVVM/settings form,
and resource-heavy). The `ingest` command discovers and classifies the API, XAML
construct, resource, asset, and project-ingestion surface across the corpus into
a deterministic, tracked inventory.

```sh
# Verify the corpus surface against the tracked baseline (CI mode):
dotnet run --project src/WinUI3.MacRunner -- ingest --manifest fixtures/corpus.json --output artifacts/corpus --check

# Refresh the tracked baseline after an intentional corpus change:
dotnet run --project src/WinUI3.MacRunner -- ingest --manifest fixtures/corpus.json --write-baseline
```

Every discovered surface is classified; the tracked unknown report
(`docs/compatibility/corpus-unknown-apis.json`) is empty. See
[`docs/compatibility/corpus.md`](docs/compatibility/corpus.md). The
[`tools/private-name-denylist`](tools/private-name-denylist/README.md) scan keeps
private product names out of the public surface, and both gates run in CI.

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
`docs/examples/consumer-github-actions.yml` for a self-hosted macOS consumer CI
starting point. See `docs/consumption/downstream-windows-apps.md` for why this
tool exists, how a downstream Windows WinUI 3 app can use it, and which tests
should run locally and in CI.

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

`ProductionSmoke.WinUI` is a public Windows-targeted smoke and E2E fixture for
the documented harness component subset. It exercises launch, navigation, form edits,
combo/list selection, status transitions, command invocation, resource-backed
theme styling, and managed popup decision actions without private product data.

Scenario JSON files under each fixture's `scenarios/` directory describe the
strict visual contract for the supported public subset. A scenario can set the
viewport, scale, theme, interaction actions, strict visual mode, and pixel diff
thresholds. The stricter renderer path is opt in:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference ./artifacts/native-reference-import \
  --diff-output ./artifacts/winui3-mac/component-basic-input-light/visual
```

After downloading the public `windows-reference-screenshots` workflow artifact,
normalize and validate the native references before local review:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner native-reference-import \
  --source ./artifacts/windows-reference-screenshots \
  --output ./artifacts/native-reference-import
```

For scenario-driven runs, `--reference` accepts either one
`windows-reference.png` file or the normalized native reference import
directory. When a directory is supplied, the runner resolves the matching
scenario reference from `native-reference-import.json` or adjacent native WinUI
provenance before writing crop and diff artifacts.

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
- `interactions.json`: optional scripted interaction results with selector,
  expected/actual, observed state, and before/after state for state-changing
  actions.
- `snapshot.json` and `screenshots/snapshot.svg`: deterministic nonblank
  snapshot output for smoke and regression tests. Passing `--renderer skia`
  writes `screenshots/snapshot.png` with the Skia renderer.
- `screenshots/mac-runtime.png`: deterministic PNG from `--renderer skia-v2`
  for scenario-driven visual comparison.
- `visual/visual-run.json`: scenario metadata, strict visual status,
  unsupported visual features, and pixel comparison summary.
- `visual/component-evidence.json`: component-level catalog status, presence,
  interaction status, visual grade, known gaps, crop paths, native reference
  provenance, and optional diff metrics when a reference-backed comparison
  supplies them.
- `visual/visual-review.html` and `visual/visual-review.json`: component
  review output that places native WinUI, macOS runtime, and diff crops
  side by side for manual inspection when crop artifacts exist.
- `visual/windows-reference.png`, `visual/mac-runtime.png`,
  `visual/pixel-diff.png`, and `visual/pixel-diff.json`: comparison artifacts
  when a Windows reference PNG is provided.

Wine is optional diagnostic context only. The primary runtime path is a managed
macOS .NET process.

## Visual Parity Evidence

Public visual evidence lives in `docs/visual-parity/`. The checked-in PNG
comparisons are kept in `docs/visual-parity/comparisons.md` as historical
visual-review fixtures, not as the current support or visual-quality grade
source.
The latest full native WinUI reference artifact set comes from public GitHub
Actions run
[`26962358057`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26962358057)
on commit `1a2eb01`, and the final production gate evidence is recorded in
`docs/release/final-production-gate.md`. Synthetic `WindowsNativeProbe` output
remains only as smoke evidence for the harness. Fresh component lab runs publish
`component-evidence.json`; treat that file as the component-level truth for
`good`, `usable`, `weak`, `poor`, or `not-rendered` grades. A whole screenshot
that passes thresholds is smoke evidence, but it is not enough to call every
visible control visually good. Controls that only emit text, show placeholder
chrome, or disappear in macOS screenshots remain `not-rendered` or low-fidelity
and outside any visual fidelity claim until fresh inspected evidence promotes
them.

### Component Parity Evidence

Current component parity status is not read from the checked-in PNG examples.
It is read from freshly generated `component-evidence.json`, strict scenario
results, and the production target inventory:

- Supported Ring 0 and claimed Ring 1 components must have at least `usable`
  component evidence with target layout regions.
- Planned, unsupported, Windows-only, diagnostic-only, weak, poor, or
  `not-rendered` rows remain outside the support claim.
- The `ClaimedSupportedComponentsAreNeverNotRendered` test prevents supported
  or partial component claims from regressing to `not-rendered`.

See `docs/compatibility/component-support.md`,
`docs/compatibility/production-component-targets.md`,
`docs/visual-parity/README.md`, and `docs/release/final-production-gate.md` for
the current evidence tables and interpretation notes.

## Compatibility Status

This runtime is release-gate-ready only for the documented public source-level
subset. The published support surface includes **Level 0: Harness Reliability**,
**Level 1: Core App And XAML
Compatibility**, **Level 2: Layout And Controls Foundation**, **Level 3:
Styling, Resources, And Theme Fidelity**, **Level 4: Data Binding, Commands,
And State**, **Level 5: Input, Accessibility, And Automation**, **Level 6:
Windows Reference Visual Compatibility**, and **Level 7: Release And
Consumption Readiness**. These levels describe the supported harness and
evidence subset; they are not a claim of arbitrary WinUI 3 app compatibility or
native-quality visual rendering.

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
for the material compatibility contract. See
`docs/release/production-readiness.md` for the current production readiness
assessment, known gaps, and production blockers.

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

The `windows-native-screenshot` workflow runs only on GitHub's `windows-latest`
runner and captures client-area PNG reference screenshots from Windows desktop
windows. The current workflow launches public native WinUI fixture projects and
the `WindowsNativeProbe` smoke fixture; local macOS runtime comparison is run
from a developer Mac with `winui3-mac-runner --reference` when visual parity
needs review. Treat `WindowsNativeProbe` references as harness smoke evidence;
public admin and component parity examples must use native WinUI fixture
reference provenance.

The capture tool can be pointed at any Windows desktop app by changing the
window title and command:

```sh
dotnet run --project tools/WindowsWindowCapture/WindowsWindowCapture.csproj -- \
  --title "WinUI3 Mac Test Runtime - shell-light" \
  --output artifacts/windows-synthetic-probe-smoke/shell-light/windows-reference.png \
  --metadata-output artifacts/windows-synthetic-probe-smoke/shell-light/windows-reference.json \
  --reference-source synthetic-probe \
  --client-area \
  -- dotnet run --project fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
```

The workflow uses only generic public fixture content and public GitHub-hosted
Windows runners. It does not require Wine, macOS GitHub-hosted runners, private
repositories, secrets, or private screenshots. Passing the workflow means the
Windows reference capture tier completed; local visual comparison must still
pass before claiming the documented alpha fixture/control subset stayed within
scenario thresholds. Passing either tier is not a claim of arbitrary WinUI 3
pixel compatibility, and visibly weak components must remain labeled `weak` or
`poor`; text-only or absent components must remain labeled `not-rendered` in
component evidence until native WinUI public reference artifacts justify a
stronger grade.

## License

This project is licensed under `LGPL-3.0-or-later`. See `LICENSE` for the GNU
Lesser General Public License text and `COPYING` for the GNU General Public
License text that LGPLv3 extends.
