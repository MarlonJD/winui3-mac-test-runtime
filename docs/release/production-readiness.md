# Production Readiness Assessment

Date: 2026-06-01

Scope: `winui3-mac-test-runtime` as a public, Wine-free macOS runtime for
source-level WinUI 3 compatibility testing.

## Verdict

The library is **alpha-ready for public compatibility experiments and fixture
validation**, but **not production-ready for general-purpose WinUI 3 application
development**.

The important shift is that the project now has public harness evidence instead
of only intent:

- Local build, test, doctor, runner, strict visual, and Windows probe gates pass.
- Public `windows-native-screenshot.yml` produces Windows-hosted synthetic
  probe screenshots, macOS runtime screenshots, pixel diffs, and
  `visual-run.json`.
- `ComponentParityLab.WinUI` produces `component-evidence.json`, so component
  quality is graded independently from whole-screenshot pass/fail status.
- Public docs now include synthetic-probe-vs-macOS component visual examples,
  labeled as harness evidence rather than native WinUI parity proof.

This is a useful harness foundation, but the current visual evidence is not a
native WinUI reference source of truth. `WindowsNativeProbe` draws synthetic
Windows reference screens instead of running the real WinUI fixture
applications. That makes native WinUI Windows reference capture the first
production blocker.

## What Is Done

### Runtime And Tooling

- Wine-free managed macOS host for facade-backed .NET assemblies.
- `winui3-mac-doctor` and `winui3-mac-runner` source wrappers.
- Packaged runner and compatibility package contracts for the alpha release set.
- Versioned runtime artifacts:
  - `run.json`
  - `tree.json`
  - `accessibility.json`
  - `binding-failures.json`
  - `resource-failures.json`
  - `unsupported-apis.json`
  - `project-ingestion.json`
  - `diagnostics.sarif`
  - `visual-run.json`
  - `component-evidence.json`

### Source-Level WinUI Fixture Support

- Public Windows-targeted WinUI source fixture ingestion through compat shadow
  builds without mutating the source project.
- `UseWinUI`, Windows-targeted TFM, Windows App SDK package references, WinUI
  XAML files, and common project item types are detected and reported.
- Unsupported project features are surfaced through structured diagnostics
  instead of silently passing.

### XAML, Runtime, And Interaction Subset

- Core app types and XAML roots: `Application`, `Window`, `Page`, `Frame`.
- Core XAML constructs: `x:Class`, `x:Name`, `x:Uid`, text content, event
  hookup, basic resource lookup, simple style setter application, and binding
  subset.
- Public control subset for fixture validation: layout containers, text/input
  controls, navigation, command surfaces, list/items controls, progress/status
  controls, icons, and images.
- Scripted interactions for click, focus, text entry, item selection, property
  assertions, navigation selection, and command invocation.
- Deterministic accessibility export for the supported logical tree subset.

### Visual Evidence

- `skia-v2` strict visual scenarios for shell, interactions, control gallery,
  public admin/workbench, and component parity lab categories.
- Public synthetic Windows probe workflow:
  `windows-native-screenshot.yml`.
- Checked-in public visual examples under `docs/visual-parity/examples/`.
- Component parity evidence is separated from whole-image pixel metrics.

### Component Parity Lab

- Public `ComponentParityLab.WinUI` fixture with eight clean-room pages:
  - Basic input
  - Text and forms
  - Collections
  - Dialogs and flyouts
  - Commands and menus
  - Navigation and workbench
  - Status and pickers
  - Layout, media, and resources
- Component inventory metadata in
  `docs/compatibility/winui-component-inventory.json`.
- Scenario requirements with expected catalog status, presence, interaction
  status, minimum visual grade, known gaps, and optional diff metrics.
- Downstream source-audit gaps are explicitly represented:
  `SymbolIcon`, `XamlControlsResources`,
  `ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
  `Style`, `Setter`, `Color`, `SolidColorBrush`, `CornerRadius`,
  `DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
  `CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`,
  `NavigationView.MenuItems`, `NavigationView.PaneFooter`,
  `ToolTipService.SetToolTip`, and `Window.SystemBackdrop / MicaBackdrop`.

## Evidence Available

Latest inspected public component visual workflow:

- Workflow: `windows-native-screenshot.yml`
- Run: `26757799015`
- Code commit inspected by that workflow: `72c3148`
- Documentation commit adding checked-in examples: `e4e746f`
- Reference limitation: this run captured `WindowsNativeProbe` synthetic
  drawings, not native WinUI renders of the fixture projects.

Local verification gates run for the component parity foundation:

- `dotnet build`
- `dotnet test`
- `PATH="$PWD/tools:$PATH" winui3-mac-doctor`
- `winui3-mac-runner` Tiny fixture smoke, default renderer.
- `winui3-mac-runner` Tiny fixture smoke, `--renderer skia`.
- `skia-v2` strict visual scenarios for existing fixture categories.
- All eight component parity lab scenarios.
- `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj
  --configuration Release`
- `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
  --configuration Release`

Checked-in synthetic probe visual example categories:

- `public-admin-workbench-light`
- `component-basic-input-light`
- `component-commands-menus-light`
- `component-layout-media-light`

## What Is Not Done

- No arbitrary Windows binary, `.exe`, `.msix`, or packaged Windows App SDK app
  execution on macOS.
- No real Windows App SDK target execution on macOS.
- No claim of full WinUI 3 API coverage.
- No claim of full Microsoft Learn controls inventory support.
- No pixel-perfect Fluent visual parity.
- No Mica, Acrylic, compositor, system backdrop, shadow, transform, or animation
  rendering parity.
- No full `ControlTemplate`, `DataTemplate`, `VisualStateManager`, transition,
  or theme dictionary implementation.
- No complete keyboard routing, pointer-over, pressed, focus, disabled, or
  accessibility automation behavior.
- No full text stack: advanced typography, IME, selection, caret, wrapping,
  trimming, or font fallback remain incomplete.
- No media, WebView2, ink, or platform integration support.
- No broad downstream production app matrix across many real public WinUI
  applications.
- No long-running performance, memory, concurrency, or flake-rate benchmark
  suite.

## Current Gaps

### Component Quality Gaps

The component lab intentionally records weak and not-rendered components. A
whole screenshot can pass while individual components remain weak.

Known weak examples from checked-in evidence:

- `CommandBar`
- `AppBarButton`
- `AppBarButton.Icon`
- `Grid`
- `Border`
- `FontIcon`
- `Image`

Many planned controls are present only as diagnostic rows and are graded
`not-rendered`. This is correct for alpha evidence, but it blocks production
claims until each component has cataloged behavior, fixture coverage, renderer
support, and native WinUI Windows reference evidence.

### Source Compatibility Gaps

- Complex project files and Windows packaging flows are not production-grade.
- Windows App SDK MSBuild targets are not executed locally.
- Unsupported XAML is diagnosed, but broad source compatibility is not complete.
- `x:Bind`, templates, visual states, richer resource dictionaries, and dynamic
  theme behavior remain planned or partial.

### Visual Fidelity Gaps

- `skia-v2` is deterministic and useful for evidence, but still visibly
  simplified.
- Current checked-in `windows-reference.png` files are synthetic
  `WindowsNativeProbe` screenshots. They are not native WinUI screenshots and
  must not be used to promote component visual grades.
- Text metrics, edge anti-aliasing, Fluent control chrome, focus visuals,
  hover/pressed states, command surfaces, list/detail painters, depth, and
  material effects are not production parity.
- Scenario thresholds are reviewable contracts, not proof of pixel perfection.

### Product And Operations Gaps

- Package release hardening is not yet enough for production adoption.
- Consumer support docs exist, but upgrade, deprecation, compatibility policy,
  and troubleshooting depth are still alpha-level.
- CI has public synthetic Windows probe evidence, but production needs native
  WinUI reference capture, stronger flake tracking, artifact retention policy,
  and reference drift review.
- Security, supply-chain provenance, license review, and release signing are
  not documented as completed production gates.

## Production Blockers

| ID | Severity | Blocker | Why It Blocks Production | Exit Criteria |
| --- | --- | --- | --- | --- |
| PB-000 | Blocking | Native Windows reference source of truth is missing. | Current visual references are synthetic `WindowsNativeProbe` drawings, not real WinUI fixture renders, so visual parity evidence starts from the wrong baseline. | `windows-native-screenshot.yml` builds, launches, drives, and captures the actual public WinUI fixture projects on Windows, records reference provenance, and labels any remaining synthetic probe output as non-parity smoke evidence only. |
| PB-001 | Blocking | Broad WinUI API coverage is incomplete. | Production users will hit uncataloged or planned APIs in normal apps. | Expand the API catalog and diagnostics until common public WinUI apps produce no unknown API usage; unsupported APIs must have explicit status and docs. |
| PB-002 | Blocking | Many controls are `not-rendered` or diagnostic-only. | The Microsoft Learn controls inventory is not yet represented by usable runtime behavior. | Component lab inventory has fixture coverage for the target production subset, with no unexpected `not-rendered` entries for claimed supported controls. |
| PB-003 | Blocking | Weak supported component visuals remain. | `CommandBar`, `AppBarButton`, layout/media primitives, icons, and images are visibly simplified. | Claimed supported components reach at least `usable`, with native WinUI public Windows reference artifacts and reviewed `component-evidence.json`. |
| PB-004 | Blocking | Templates, visual states, and theme dictionaries are incomplete. | Real WinUI apps rely on templates, state groups, and Fluent resources. | Implement and test the production subset of `ControlTemplate`, `DataTemplate`, `VisualStateManager`, `ThemeResource`, and theme dictionaries. |
| PB-005 | Blocking | Fluent materials and composition are not rendered. | Mica, Acrylic, system backdrops, shadows, transforms, and motion are core modern WinUI surfaces. | Add deterministic material/composition modeling or clearly define a production support tier that excludes them. |
| PB-006 | Blocking | Input and accessibility behavior is partial. | Production testing needs reliable keyboard, pointer, focus, automation, and text behavior. | Add broader keyboard routing, pointer state, focus visuals, text editing, and accessibility automation coverage with tests. |
| PB-007 | Blocking | Project ingestion matrix is too narrow. | Real projects vary in MSBuild structure, packaging, assets, resources, and multi-targeting. | Validate against a documented set of public WinUI project shapes and keep failures structured. |
| PB-008 | Blocking | Public visual evidence is still fixture-focused. | Fixture evidence does not prove production app behavior, and current references are synthetic probe captures. | Add a public downstream app corpus or representative clean-room scenarios with native WinUI Windows references and component evidence. |
| PB-009 | Blocking | Performance and reliability are not measured. | Production adoption needs predictable runtime duration, memory use, and flake rate. | Add benchmark and flake tracking gates for runner startup, XAML compile, render, interaction, and artifact generation. |
| PB-010 | Blocking | Release hardening is incomplete. | Production users need stable packages, upgrade policy, rollback path, provenance, and signed/reproducible release artifacts. | Publish a production release checklist covering semver, package signing/provenance, changelog, migration notes, and rollback. |
| PB-011 | Blocking | Security and supply-chain review is incomplete. | The runner builds and executes user source projects locally. | Document threat model, dependency policy, artifact privacy policy, and safe CI usage guidance. |
| PB-012 | Blocking | Documentation still describes alpha limits. | Production users need exact support boundaries and support expectations. | Create production support policy, compatibility tiers, known unsupported list, and issue triage policy. |

## Readiness Gates

### Alpha Gate

Status: **partially met for the current documented alpha harness scope**.

Required evidence:

- Local build/test/doctor/runner gates pass.
- Public synthetic Windows probe workflow passes.
- Component evidence exists for the component parity lab.
- Docs distinguish visual smoke evidence from component-level quality.

Missing evidence:

- Native Windows captures of the actual public WinUI fixture projects.

### Beta Gate

Status: **not met**.

Required evidence:

- No unknown APIs for the selected beta fixture corpus.
- Supported component subset has no `poor` grades and no unexpected
  `not-rendered` grades.
- Weak grades are either fixed or explicitly excluded from beta claims.
- Public downstream app corpus exists.
- Performance and flake metrics are recorded.

### Production Gate

Status: **not met**.

Required evidence:

- Stable package release policy and support policy.
- Security and supply-chain readiness review.
- Broad project ingestion matrix.
- Production compatibility contract with exact supported and unsupported
  behavior.
- Public visual, component, accessibility, input, and performance evidence for
  the claimed production subset.

## Recommended Next Milestones

Detailed execution plan: `docs/plans/2026-06-01-production-readiness-roadmap-plan.md`.

1. Replace synthetic `WindowsNativeProbe` visual references with native WinUI
   Windows captures of the actual public WinUI fixture projects.
2. Move weak checked-in component examples to `usable` after native reference
   evidence exists.
3. Add crop-level or component-region diff metrics so whole-screen thresholds do
   not mask localized regressions.
4. Expand component lab coverage for currently diagnostic controls that are
   likely to matter first: `AutoSuggestBox`, `MenuFlyout`, `DropDownButton`,
   `SplitButton`, `Slider`, `NumberBox`, `PasswordBox`, `DatePicker`, and
   `TabView`.
5. Implement the minimal production subset of templates, visual states, and
   theme dictionaries.
6. Add a public downstream app corpus with source ingestion, interactions,
   visual evidence, and component evidence.
7. Add performance, flake-rate, and artifact-size gates.
8. Write the production release policy: semver, changelog, package provenance,
   rollback, support window, and security reporting.

## Decision Summary

The library is in a good place for an alpha harness: it has public harness
proof, structured artifacts, honest component grades, and a clean path for
measuring progress. Native WinUI reference capture must be fixed before the
visual evidence can support production parity claims.

It should not be marketed as production-ready until the blockers above are
closed or explicitly scoped out in a production compatibility contract.
