# Reference-Matched WinUI Visual Parity Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `src`, `fixtures`, `.github/workflows`

## Goal

Move the project from the current Level 0 through Level 7 alpha foundation to
reference-matched WinUI 3 visual parity for real source-level C# and XAML apps
on macOS. The target behavior is that developers can point the macOS runner at
a real WinUI 3 project, build a Wine-free managed compatibility assembly, run
deterministic scenarios, and compare `mac-runtime.png` against native WinUI
`windows-reference.png` screenshots captured by public Windows GitHub Actions.
Synthetic `WindowsNativeProbe` screenshots may remain as harness smoke evidence,
but they do not prove reference-matched parity.

The product should only claim "nearly the same as Windows" for a scenario after
the public Windows reference, macOS runtime image, and pixel diff pass the
scenario's explicit thresholds.

## Current Level 0-7 Explanation

Levels 0 through 7 are useful but they are not visual parity for arbitrary WinUI
apps.

| Level | What it proved | What it did not prove |
| --- | --- | --- |
| 0 Harness Reliability | `winui3-mac-runner`, doctor, artifacts, SVG/Skia paths, strict visual plumbing. | Real Windows App SDK project ingestion or pixel parity. |
| 1 Core App And XAML Compatibility | Basic `Application`, `Window`, `Page`, `Frame`, resources, startup, and strict XAML diagnostics. | Full WinUI XAML compiler compatibility. |
| 2 Layout And Controls Foundation | A public fixture subset of controls can compile, run, export trees, and render. | Native WinUI templates, full layout semantics, or app-scale controls. |
| 3 Styling, Resources, And Theme Fidelity | Simple resources, style setters, and light/dark/high-contrast renderer themes. | Full `ThemeResource`, merged dictionaries, templates, or material resources. |
| 4 Data Binding, Commands, And State | Basic binding, observable items, and command execution for supported controls. | `x:Bind`, converters, collection views, validation, or full MVVM behavior. |
| 5 Input, Accessibility, And Automation | Scripted actions and accessibility export for the public subset. | Complete keyboard, pointer, hover, pressed, focus, Narrator, or UIA behavior. |
| 6 Windows Reference Visual Compatibility | Public fixture screenshots can be compared by the harness. | Native WinUI fixture screenshots, arbitrary Windows app screenshots, Mica/Acrylic, compositor, or full Fluent states. |
| 7 Release And Consumption Readiness | Packages, quick start docs, smoke checks, and release notes are ready for alpha use. | Production-grade WinUI compatibility. |

The next milestone must stop treating the alpha renderer or synthetic probe
screenshots as a proxy for Windows. It must make native WinUI Windows reference
evidence the gate for every new visual claim.

## Assumptions

- The macOS runtime remains Wine-free and does not execute Windows binaries.
- Native WinUI Windows output is captured only from public GitHub Actions
  workflows or other explicitly provided public, non-secret reference artifacts.
- Public fixtures must not contain private repositories, private screenshots,
  private product names, secrets, or proprietary content.
- Source identifiers, comments, and canonical docs remain English.
- The existing runner, doctor, SVG, current Skia, `skia-v2`, public fixtures,
  and alpha package consumption behavior must keep working.

## Non-Goals

- Do not load `.exe` or `.msix` packages on macOS.
- Do not use Wine, Windows VMs, private screenshots, or proprietary fixture
  content as part of the public repository contract.
- Do not broadly implement controls or visual effects without a catalog entry,
  a fixture, tests, and native WinUI Windows reference evidence.
- Do not claim parity for Mica, Acrylic, compositor effects, shadows,
  transforms, animation, focus visuals, or Fluent interaction states until
  each scenario has passing native WinUI Windows reference comparison artifacts.

## Target Architecture

### 1. Real Project Ingestion

Add a "compat shadow build" path for Windows-targeted WinUI projects:

- Detect `net*-windows*`, `<UseWinUI>true</UseWinUI>`, Windows App SDK package
  references, and XAML items.
- Generate a temporary compatibility project in the runner output directory
  instead of mutating the consumer project.
- Reference `WinUI3.MacCompat` and the macOS XAML compiler.
- Exclude Windows-only generated outputs and bypass Windows App SDK
  `XamlCompiler.exe`.
- Compile the app's C# and XAML source through the clean-room Mac compiler.
- Preserve app namespaces, partial classes, resource dictionaries, and startup
  entry points well enough for source-level scenarios.
- Emit a structured `project-ingestion.json` report with included files,
  excluded Windows-only items, unsupported project properties, and catalog
  statuses.

Success condition: a public WinUI 3 sample project with Windows-targeted TFM
builds through the runner on macOS without requiring `EnableWindowsTargeting`
or executing Windows binaries.

### 2. Strict Compatibility Catalog Gate

Extend the catalog from a seed list into the source of truth for every parser,
facade, runtime, and renderer decision:

- Unknown WinUI APIs, XAML elements, properties, events, resources, visual
  states, materials, and compositor concepts fail strict scenarios.
- Planned or unsupported APIs compile only when explicitly allowed by a
  scenario or diagnostic mode; they never silently render as supported.
- Each promoted entry records the tests, fixture scenario, renderer support,
  and Windows reference artifact category that justified promotion.

Success condition: real app ingestion produces an actionable unsupported
feature report instead of either failing with an opaque MSBuild error or
silently drawing an approximate screen.

### 3. XAML And Resource Compatibility Expansion

Prioritize the constructs needed by real WinUI admin/workbench apps:

- XML namespaces, attached properties, property elements, resource dictionaries,
  merged dictionaries, `StaticResource`, `ThemeResource`, and style inheritance.
- `DataTemplate`, item templates, basic control templates, template bindings,
  visual state groups, and style setters used by built-in controls.
- Binding modes, converters, collection updates, selection state, command state,
  and common `x:Bind` patterns where deterministic clean-room semantics are
  practical.
- Localized resources and `x:Uid` metadata with strict missing-resource
  diagnostics.

Success condition: the public admin/workbench fixture compiles from XAML that
resembles a real WinUI page instead of requiring a hand-built C# approximation.

### 4. Fluent Control Template Parity

Replace generic painters with control-specific Fluent renderers for the most
common shell and admin surfaces:

- `NavigationView`, `CommandBar`, `AppBarButton`, `InfoBar`, `AutoSuggestBox`,
  `ListView`, `ItemsRepeater`, `TextBox`, `Button`, `ToggleButton`,
  `CheckBox`, `RadioButton`, `ComboBox`, `ProgressRing`, `ProgressBar`,
  `ContentDialog`, `TeachingTip`, `MenuFlyout`, `ScrollViewer`, `Grid`, and
  `StackPanel`.
- Implement measure/arrange behavior close enough for reference screenshots:
  padding, min sizes, alignments, list item containers, separators, text
  trimming, density, and selected/focused states.
- Use theme-aware tokens and semantic states instead of hardcoded colors.
- Add scenario coverage for default, hover, pressed, focused, selected,
  disabled, loading, warning, error, success, empty, denied, and offline states.

Success condition: shell, list/detail, command, and form scenarios pass against
Windows references with ratcheted thresholds.

### 5. Material, Composition, And Motion

Implement material and composition in small promoted slices:

- Mica and desktop Acrylic as clean-room approximations with active/inactive,
  light, dark, and high-contrast variants.
- System backdrop object semantics and renderer metadata.
- Shadows, opacity, translation, scale, clip, transform origin, and elevation.
- Visual state transitions and deterministic animation clocks.
- Reduced motion mode that disables or snaps non-essential motion.
- Focus visuals and high-contrast treatment that do not rely on color alone.

Success condition: every material/composition scenario has Windows reference,
macOS runtime, pixel diff, accessibility evidence, and a catalog promotion note.

### 6. Windows Reference Workflow Ratchet

Make the Windows workflow the visual source of truth:

- Expand `.github/workflows/windows-native-screenshot.yml` to enumerate every
  public strict scenario.
- Capture `windows-reference.png` and metadata on `windows-latest`.
- Run macOS `skia-v2` output for the same scenario and viewport.
- Produce `mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`,
  `visual-run.json`, `tree.json`, `accessibility.json`, `diagnostics.sarif`,
  and `unsupported-apis.json`.
- Fail when thresholds regress, unsupported features appear, or reference
  artifacts are missing.
- Ratchet thresholds downward only after reviewing artifact history.

Success condition: visual claims are made per scenario, not per broad level.

### 7. App-Scale Public Fixture Suite

Create public, generic fixtures that resemble real apps without private
content:

- `PublicAdminWorkbench.MacTest`: shell, admin navigation, queue/list, detail,
  command bar, search, decision panel, denied, loading, empty, and error states.
- `MaterialComposition.MacTest`: Mica, Acrylic, high contrast, focus, reduced
  motion, transforms, and shadows.
- `DataTemplateGallery.MacTest`: item templates, collection updates, selection,
  grouping-like layout, empty state, and template resources.
- `FluentStates.MacTest`: default, hover, pressed, disabled, focused, selected,
  success, warning, and error states across core controls.

Success condition: a private app can be evaluated by comparing its unsupported
feature report to these public fixture categories without putting private
content in the repository.

## Implementation Steps

1. Document the new parity ladder.
   - Add docs that separate alpha Levels 0-7 from the next reference-matched
     parity milestones.
   - State clearly that `skia-v2` approximate screenshots are not Windows
     parity unless backed by a reference comparison.

2. Implement compat shadow build discovery.
   - Add project ingestion services and tests around Windows-targeted WinUI
     project files.
   - Emit `project-ingestion.json`.
   - Keep normal existing fixture builds unchanged.

3. Convert one public Windows-targeted fixture through shadow build.
   - Use a generic public admin/workbench app, not private product content.
   - Prove that the runner bypasses Windows `XamlCompiler.exe` and compiles
     through the Mac XAML compiler.

4. Expand XAML support only for constructs used by the fixture.
   - Add failing tests first for each construct.
   - Promote catalog entries only after parser, runtime, renderer, and docs
     agree.

5. Add reference-backed visual scenarios.
   - Add light, dark, high-contrast, compact, medium, and wide scenarios for
     shell and admin workbench surfaces.
   - Capture Windows references through public Actions.
   - Inspect `windows-reference.png`, `mac-runtime.png`, and `pixel-diff.png`.

6. Replace generic painters with targeted Fluent renderers.
   - Start with `NavigationView`, `CommandBar`, `InfoBar`, `AutoSuggestBox`,
     `ListView`, `TextBox`, and `Button`.
   - Add interaction-state fixtures before broadening the control set.

7. Add material/composition slices.
   - Implement Mica first for active/inactive light and dark windows.
   - Add Acrylic only after Mica has tests and reference artifacts.
   - Add high contrast and reduced motion before promoting any material claim.

8. Ratchet thresholds and update release docs.
   - Record artifact run IDs and inspected scenario names.
   - Update compatibility matrix and catalog statuses only for passing
     reference-backed behavior.

## Verification Gates

Run these local checks before handoff for each implementation slice:

```bash
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-high-contrast.json --strict-visual
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
rg -n "<private-name-denylist-regex>" .
```

When visual behavior, scenarios, renderer output, or parity claims change, also
run:

```bash
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

Inspect at least one `windows-reference.png`, `mac-runtime.png`, and
`pixel-diff.png` for every changed fixture category before updating docs or
claiming parity.

## Risks

- Real WinUI XAML has large surface area. Mitigation: promote constructs only
  through catalog entries, tests, and public fixtures.
- Pixel parity can regress through text rendering, font metrics, anti-aliasing,
  and theme differences. Mitigation: store metadata, compare with
  scenario-local thresholds, and ratchet gradually.
- Mica, Acrylic, and compositor behavior cannot be copied from Windows.
  Mitigation: implement clean-room approximations and mark them as
  `visually-approximated` until reference-matched.
- Private apps may expose private names in screenshots or fixtures.
  Mitigation: keep public fixtures generic and run the operator-provided
  private-name scan before handoff.

## Rollback And Recovery

- Keep shadow build output under runner artifacts so consumer projects are not
  mutated.
- Gate new parser and renderer behavior behind tests and catalog status.
- If a renderer slice regresses existing smoke scenarios, revert only that
  slice and keep catalog entries at `planned` or `partial`.
- Preserve existing SVG and current Skia output as smoke paths while `skia-v2`
  carries strict visual parity.

## Affected Files And Docs

- `src/WinUI3.MacRunner/*`
- `src/WinUI3.MacRuntime/*`
- `src/WinUI3.MacXaml/*`
- `src/WinUI3.MacCompat/*`
- `src/WinUI3.MacRenderer.Skia/*`
- `src/Shared/CompatibilityCatalog.cs`
- `fixtures/*`
- `tests/*`
- `.github/workflows/windows-native-screenshot.yml`
- `docs/compatibility/*`
- `docs/architecture/artifacts.md`
- `docs/release/*`

## Execution Prompt

Use `$google-eng-practices` and `$windows-winui3-design` and implement
`docs/plans/2026-06-01-reference-matched-winui-visual-parity-plan.md` in the
public `MarlonJD/winui3-mac-test-runtime` repository. The goal is to move past
the Level 0 through Level 7 alpha milestone toward reference-matched WinUI 3
visual parity: real Windows-targeted WinUI source projects should be ingested
through a Wine-free macOS compat shadow build, unsupported WinUI/XAML/material
features must be diagnosed through the compatibility catalog, and visual parity
claims must be backed by public `windows-reference.png`, `mac-runtime.png`, and
`pixel-diff.png` artifacts from `windows-native-screenshot.yml`.

Start by implementing the smallest end-to-end slice: compat shadow build
discovery for a public Windows-targeted WinUI fixture, `project-ingestion.json`,
strict catalog diagnostics for unsupported project/XAML features, and one
reference-backed public admin/workbench scenario. Preserve existing
`winui3-mac-doctor`, `winui3-mac-runner`, SVG, current Skia, `skia-v2`, and all
existing fixtures. Do not use private repositories, private screenshots, private
product names, secrets, or proprietary fixture content. Keep identifiers,
comments, and canonical docs in English.

Run the local verification gate from the plan: `dotnet build`, `dotnet test`,
`PATH="$PWD/tools:$PATH" winui3-mac-doctor`, the TinyWinUIApp SVG and Skia
runs, strict `skia-v2` runs for `SampleAdminShell`, `InteractionBindingApp`,
and both `ControlGallery` light and high-contrast scenarios, `dotnet build
tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration
Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
--configuration Release`, package smoke commands for changed packages, and
`rg -n "<private-name-denylist-regex>" .` with the operator-provided
private-name denylist. If visual behavior or scenarios change, trigger
`windows-native-screenshot.yml`, wait for it to finish, download artifacts, and
inspect the relevant `windows-reference.png`, `mac-runtime.png`, and
`pixel-diff.png` files before final handoff. Commit only relevant files with
author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message
and push immediately.
