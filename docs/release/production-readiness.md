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

- Local build, test, doctor, runner, and Windows capture gates pass; native
  WinUI visual comparisons now fail honestly where macOS component visuals are
  text-only or absent.
- Public `windows-native-screenshot.yml` now produces native WinUI Windows
  references for the public admin workbench and component parity lab fixtures,
  with `referenceSource: native-winui` provenance.
- `ComponentParityLab.WinUI` produces `component-evidence.json`, so component
  quality is graded independently from whole-screenshot pass/fail status.
- Public docs now include native-WinUI-vs-macOS component visual examples,
  labeled as failed parity evidence where the macOS renderer is text-only or
  absent.

This closes the native WinUI reference source-of-truth blocker for the public
fixture set: `WindowsNativeProbe` remains only as labeled synthetic smoke
evidence. The next production blockers are the honest comparison failures and
component gaps exposed by the native references.

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
- Public native WinUI Windows reference workflow:
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
- Sanitized production-ring target mapping in
  `docs/compatibility/production-component-targets.md`.
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

Latest inspected public native WinUI visual workflow:

- Workflow: `windows-native-screenshot.yml`
- Run: `26785240127`
- Code commit inspected by that workflow: `61b6ad3`
- Reference source: `native-winui`
- Viewport: `1028x720`
- Capture mode: client area.
- Native reference provenance exists for `public-admin-workbench-light`, all
  light Ring 0 and Ring 1 `ComponentParityLab.WinUI` scenarios,
  `production-smoke-light`, `production-e2e-workbench-light`, and the
  inspected component examples checked into `docs/visual-parity/examples`.
- Synthetic probe smoke artifacts are still produced separately for
  `shell-light`, `interactions-light`, and `control-gallery-light`, but they are
  not parity examples.

Latest inspected native comparison status:

| Scenario | Status | Changed pixels | MAE | RMS | Component evidence |
| --- | --- | ---: | ---: | ---: | --- |
| `public-admin-workbench-light` | failed | 100.00% / threshold 45% | 9.72 | 35.87 | n/a |
| `component-basic-input-light` | failed | 42.07% / threshold 18% | 9.92 | 38.84 | 13 `not-rendered` |
| `component-commands-menus-light` | failed | 40.68% / threshold 24% | 8.45 | 35.23 | 8 `not-rendered` |
| `component-layout-media-light` | failed | 45.83% / threshold 24% | 10.48 | 39.27 | 4 `usable`, 24 `not-rendered` |

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
whole screenshot can pass while individual components remain weak, text-only,
or absent.

Current local macOS screenshot inspection marks these previously over-optimistic
component examples as `not-rendered` because they are text-only or absent:

- `Button`
- `ToggleButton`
- `CheckBox`
- `RadioButton`
- `ComboBox`
- `TextBox`
- `ItemsControl`
- `ListView`
- `CommandBar`
- `AppBarButton`
- `AppBarButton.Icon`
- `NavigationView`
- `NavigationViewItem`
- `NavigationView.MenuItems`
- `NavigationView.PaneFooter`
- `List/details pattern`
- `InfoBar`
- `ProgressBar`
- `ProgressRing`
- `ScrollViewer`
- `Grid`
- `StackPanel`
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
- Current checked-in public admin and component lab `windows-reference.png`
  files are native WinUI fixture screenshots with provenance. They expose
  honest macOS comparison failures and must not be used to promote component
  visual grades until the macOS output improves.
- Text metrics, edge anti-aliasing, Fluent control chrome, focus visuals,
  hover/pressed states, command surfaces, list/detail painters, depth, and
  material effects are not production parity.
- Scenario thresholds are reviewable contracts, not proof of pixel perfection.

### Product And Operations Gaps

- Package release hardening is not yet enough for production adoption.
- Consumer support docs exist, but upgrade, deprecation, compatibility policy,
  and troubleshooting depth are still alpha-level.
- CI has public native WinUI reference capture for the fixture set and separate
  synthetic smoke evidence, but production still needs stronger flake tracking,
  artifact retention policy, and reference drift review.
- Security, supply-chain provenance, license review, and release signing are
  not documented as completed production gates.

## Production Blockers

| ID | Severity | Blocker | Why It Blocks Production | Exit Criteria |
| --- | --- | --- | --- | --- |
| PB-000 | Closed | Native Windows reference source of truth is available for the public fixture set. | Public run `26785240127` captured the actual public WinUI fixture projects plus production smoke scenarios, recorded `native-winui` provenance, and kept synthetic probe output separated as smoke evidence. | Keep this gate closed by preserving provenance, fixture launch coverage, and smoke-only labeling for synthetic output. |
| PB-001 | Blocking | Broad WinUI API coverage is incomplete. | Production users will hit uncataloged or planned APIs in normal apps. | Expand the API catalog and diagnostics until common public WinUI apps produce no unknown API usage; unsupported APIs must have explicit status and docs. |
| PB-002 | Blocking | Many controls are `not-rendered` or diagnostic-only. | The Microsoft Learn controls inventory is not yet represented by usable runtime behavior. | Component lab inventory has fixture coverage for the target production subset, with no unexpected `not-rendered` entries for claimed supported controls. |
| PB-003 | Blocking | Supported component visuals are text-only, absent, or weak. | Local macOS evidence currently marks basic input controls, text input, collection controls, command controls, status controls, and layout/media primitives as `not-rendered`; whole-image passes would hide this without component evidence. | Claimed supported components reach at least `usable`, with native WinUI public Windows reference artifacts and reviewed `component-evidence.json`. |
| PB-004 | Blocking | Templates, visual states, and theme dictionaries are incomplete. | Real WinUI apps rely on templates, state groups, and Fluent resources. | Implement and test the production subset of `ControlTemplate`, `DataTemplate`, `VisualStateManager`, `ThemeResource`, and theme dictionaries. |
| PB-005 | Blocking | Fluent materials and composition are not rendered. | Mica, Acrylic, system backdrops, shadows, transforms, and motion are core modern WinUI surfaces. | Add deterministic material/composition modeling or clearly define a production support tier that excludes them. |
| PB-006 | Blocking | Input and accessibility behavior is partial. | Production testing needs reliable keyboard, pointer, focus, automation, and text behavior. | Add broader keyboard routing, pointer state, focus visuals, text editing, and accessibility automation coverage with tests. |
| PB-007 | Blocking | Project ingestion matrix is too narrow. | Real projects vary in MSBuild structure, packaging, assets, resources, and multi-targeting. | Validate against a documented set of public WinUI project shapes and keep failures structured. |
| PB-008 | Blocking | Public visual evidence is still fixture-focused. | Fixture evidence does not prove production app behavior, even though current fixture references are now native WinUI captures. | Add a public downstream app corpus or representative clean-room scenarios with native WinUI Windows references and component evidence. |
| PB-009 | Blocking | Performance and reliability are not measured. | Production adoption needs predictable runtime duration, memory use, and flake rate. | Add benchmark and flake tracking gates for runner startup, XAML compile, render, interaction, and artifact generation. |
| PB-010 | Blocking | Release hardening is incomplete. | Production users need stable packages, upgrade policy, rollback path, provenance, and signed/reproducible release artifacts. | Publish a production release checklist covering semver, package signing/provenance, changelog, migration notes, and rollback. |
| PB-011 | Blocking | Security and supply-chain review is incomplete. | The runner builds and executes user source projects locally. | Document threat model, dependency policy, artifact privacy policy, and safe CI usage guidance. |
| PB-012 | Blocking | Documentation still describes alpha limits. | Production users need exact support boundaries and support expectations. | Create production support policy, compatibility tiers, known unsupported list, and issue triage policy. |

## Readiness Gates

### Alpha Gate

Status: **partially met for the current documented alpha harness scope**.

Required evidence:

- Local build/test/doctor/runner gates pass.
- Public native WinUI reference workflow passes for the fixture set, and
  synthetic probe smoke remains separate.
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

1. Keep native WinUI Windows reference capture healthy for every public fixture
   scenario and preserve synthetic probe output as smoke-only evidence.
2. Fix checked-in `not-rendered` or weak component examples and move them to
   `usable` only
   after native reference capture and macOS inspection prove the grade is
   honest.
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
