# Compatibility Matrix

Status values:

- `supported`: implemented and covered by a fixture or test.
- `partial`: implemented for a constrained headless test subset.
- `planned`: part of the public direction but not implemented yet.
- `windows-only`: valid public API surface that requires real Windows
  validation or OS integration rather than local macOS execution.
- `not supported`: intentionally outside the current public compatibility
  contract.

Compiler and runtime diagnostics may also report `unknown` for public API usage
that is not present in the catalog yet.

## Alpha Compatibility Levels

| Level | Status | Current public contract |
| --- | --- | --- |
| Level 0: Harness Reliability | supported | Managed macOS runner, doctor, SVG, current Skia, `skia-v2`, versioned artifacts, strict visual failures, and public CI workflow wiring. |
| Level 1: Core App And XAML Compatibility | supported | `Application`, `Window`, `Page`, `Frame`, resource dictionaries, startup activation, navigation, strict unsupported XAML diagnostics, and the documented public XAML subset. |
| Level 2: Layout And Controls Foundation | supported | Public fixture subset for `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl`, `ItemsControl`, `TextBlock`, `TextBox`, `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, `Image`, `ListView`, `ProgressRing`, `ProgressBar`, `InfoBar`, `CommandBar`, `AppBarButton`, `NavigationView`, `NavigationViewItem`, `Frame`, and `FontIcon`. |
| Level 3: Styling, Resources, And Theme Fidelity | supported | Resource lookup, style setter application for supported properties, light/dark/high-contrast renderer themes, and strict resource diagnostics for the public subset. |
| Level 4: Data Binding, Commands, And State | supported | One-way and two-way binding for supported properties, `INotifyPropertyChanged`, observable item collections, command execution state, button command invocation, and navigation state for public fixtures. |
| Level 5: Input, Accessibility, And Automation | supported | Scripted click, focus, text entry, item selection, property assertions, navigation selection, frame navigation, accelerator invocation, and deterministic accessibility state export. |
| Level 6: Windows Reference Visual Compatibility | supported | Public `windows-latest` reference workflow and scenario-local pixel thresholds for shell, interaction/binding, control-gallery, and public admin/workbench strict fixture categories. |
| Level 7: Release And Consumption Readiness | supported | Package metadata, pack smoke, consumer quick start, sample consumer CI, release checklist, verification evidence, troubleshooting, and known-gap documentation. |

Levels 0 through 7 are the current alpha milestone. They are not the final
WinUI 3 macOS development scope.

## API Compatibility Catalog

`docs/compatibility/winui-api-compatibility.catalog.json` is the deterministic
catalog seed used by docs, XAML diagnostics, project ingestion, and placeholder
facade runtime diagnostics. The current `0.1` catalog contains 113 entries:

| Status | Count |
| --- | ---: |
| `supported` | 52 |
| `partial` | 28 |
| `planned` | 28 |
| `windows-only` | 3 |
| `not supported` | 2 |

The catalog includes public WinUI 3 / Windows App SDK APIs, XAML constructs,
Fluent theme resources, visual states, Mica, Acrylic, system backdrops,
composition/effect concepts, shadows, transforms, and animation-related APIs.
`supported` and `partial` entries describe the alpha surface. `planned`,
`windows-only`, `not supported`, and `unknown` entries are strict diagnostics
when app code touches unavailable behavior.
See `component-support.md` for a readable component-by-component support table.

## Runtime

| Area | Status | Notes |
| --- | --- | --- |
| Wine-free managed host | supported | Runs facade-backed .NET assemblies on macOS. |
| Windows executable loading | not supported | Binary `.exe` compatibility is out of scope. |
| `run.json` and `tree.json` | supported | Emitted for every runner invocation. |
| `accessibility.json` | supported | Role/name/label/help/focus/enabled/checked/selected/value approximation from the logical tree. |
| `binding-failures.json` | supported | Versioned envelope captures unresolved paths and non-writable targets. |
| `resource-failures.json` | supported | Versioned envelope captures unresolved static and theme resources. |
| `unsupported-apis.json` | supported | Versioned envelope captures clean-room placeholder facade APIs that were touched, with catalog statuses such as `planned`, `windows-only`, `not supported`, or `unknown`. |
| `project-ingestion.json` | supported | Versioned envelope for Windows-targeted WinUI compat shadow builds, including source files, excluded Windows-only items, catalog statuses, unsupported project features, and XAML diagnostics. |
| `diagnostics.sarif` | supported | Warning diagnostics derived from binding, resource, and unavailable API reports with stable `WINUI3MAC001`, `WINUI3MAC002`, and `WINUI3MAC003` rule IDs. |
| Scripted click/focus actions | supported | Name-based interaction script actions. |
| Scripted text entry, item selection, and assertions | supported | `typeText`, `selectItem`, and `assertProperty` actions emit deterministic pass/fail results. |
| Versioned interaction scripts | supported | Script input accepts `schemaVersion: 0.1`; reports emit `schemaVersion: 0.1`. |
| Keyboard accelerators | partial | Headless accelerator model exists; broader routing is planned. |
| Snapshot output | partial | Deterministic SVG fallback and Skia-backed PNG output are available for the supported tree subset. |
| Scenario JSON visual runs | supported | `--scenario`, `--viewport`, `--scale`, `--theme`, `--strict-visual`, `--reference`, and `--diff-output` drive the strict path. |
| Deterministic layout export | partial | `skia-v2` scenarios add arranged rectangles, desired sizes, padding, alignment, and visibility to `tree.json`. |
| Pixel diff artifacts | supported | `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`, and `visual-run.json` are emitted for reference-backed runs. |
| Windows reference source of truth | supported | Public `windows-latest` workflow captures generic reference screenshots and macOS comparison artifacts. |
| Compat shadow build discovery | supported | Public Windows-targeted WinUI projects are redirected to generated macOS compatibility projects without mutating the original project or executing Windows App SDK build targets. |

## XAML

| Construct | Status | Notes |
| --- | --- | --- |
| `x:Class` | supported | Generates a partial class. |
| `Application` XAML root | supported | Generates a partial `Microsoft.UI.Xaml.Application` class for Windows-targeted source fixtures. |
| `x:Name` / `Name` | supported | Generates strongly typed fields. |
| `x:Uid` | supported | Preserved as `FrameworkElement.Uid` for public fixture localization metadata. |
| Text content | supported | Covered for `TextBlock` and `Button`. |
| Event hookup | supported | Covered for routed click and navigation selection events. |
| `{StaticResource ...}` | partial | Simple dictionary lookup with missing-resource reporting. |
| `{ThemeResource ...}` | partial | Simple dictionary lookup with missing-resource reporting. |
| `Style` resources and `Setter` values | supported | Applies supported setter properties from resource dictionaries; unsupported templates remain out of scope. |
| `AutomationProperties.Name` | supported | Exported into `tree.json` and `accessibility.json`. |
| `AutomationProperties.HelpText` | supported | Exported into `tree.json` and `accessibility.json`. |
| `Grid.Column` | supported | Supported attached property for public fixture layout metadata. |
| `{Binding Path}` | supported | One-way and two-way refresh through `BindingOperations`; both `{Binding Title}` and `{Binding Path=Title, Mode=TwoWay}` forms are accepted for supported properties. |
| Unsupported elements and properties | supported | Unsupported elements, properties, property elements, directives, attached properties, and events fail compilation with stable diagnostics. |
| Control templates and materials | planned | Templates, Mica, Acrylic, system backdrops, visual states, and compositor-backed materials are cataloged as roadmap targets and rejected or reported when used. |

## Facade Controls

| Type | Status | Notes |
| --- | --- | --- |
| `Application`, `Window`, `Page` | supported | Basic lifecycle and page activation. |
| `Frame` | supported | Supports `Navigate(Type, object?)` and XAML `Frame.Content` for public shadow-build fixtures. |
| `StackPanel`, `Grid`, `Border` | partial | Logical child containment and deterministic `skia-v2` layout for public scenarios. |
| `TextBlock`, `TextBox`, `Button` | supported | Basic content/text and button click. |
| `ToggleButton`, `CheckBox`, `RadioButton` | supported | Checked state, content, tree export, accessibility roles, layout, and `skia-v2` painters for public fixtures. |
| `ComboBox` | supported | Items, selected item/index, placeholder, tree export, layout, and `skia-v2` painter for public fixtures. |
| `ProgressRing`, `ProgressBar`, `InfoBar` | supported | State/value/severity metadata, tree export, layout, and `skia-v2` painters for public fixtures. |
| `CommandBar`, `AppBarButton` | supported | Primary commands, labels, click simulation, tree export, layout, and `skia-v2` painters for public fixtures. |
| `ScrollViewer`, `ContentControl`, `ItemsControl` | supported | Single-slot or item collection containment, tree export, layout, and `skia-v2` support for public fixtures. |
| `Image`, `ListView` | partial | Logical model plus `skia-v2` placeholder/list painters for public scenarios. |
| `NavigationView`, `NavigationViewItem` | partial | Menu items, selection, pane footer, and `skia-v2` shell painter. |
| `FontIcon` | partial | Glyph and font size metadata with simple `skia-v2` glyph/dot rendering. |

## Visual Renderer Subset

`skia-v2` is intentionally narrower than WinUI 3. It currently paints the public
fixture subset: `Window`, `Page`, `Grid`, `StackPanel`, `Border`,
`ScrollViewer`, `ContentControl`, `ItemsControl`, `TextBlock`, `Button`,
`AppBarButton`, `ToggleButton`, `CheckBox`, `RadioButton`, `TextBox`,
`ComboBox`, `Frame`, `NavigationView`, `NavigationViewItem`, `ListView`,
`ProgressRing`, `ProgressBar`, `InfoBar`, `CommandBar`, `FontIcon`, `Image`,
and string content. A strict scenario records any unsupported control or missing
renderer feature in `unsupported-apis.json` and exits non-zero.

## Material And Composition Targets

| Type | Status | Notes |
| --- | --- | --- |
| `Microsoft.UI.Xaml.Media.MicaBackdrop` | planned | Placeholder records `unsupported-apis.json`; no Mica rendering yet. |
| `Microsoft.UI.Xaml.Media.AcrylicBrush` | planned | Placeholder records `unsupported-apis.json`; no Acrylic rendering yet. |
| `Microsoft.UI.Xaml.Media.SystemBackdrop` | planned | Source-level object-slot tracking only. |
| `Microsoft.UI.Composition.Compositor` and visual/effect APIs | planned | Catalog-only tracking for clean-room compositor-style modeling. |
| `ThemeShadow`, transforms, storyboards, and key-frame animations | planned | Catalog-only tracking for future Fluent depth and motion parity. |
| Fluent hover, pressed, focus, selected, and disabled states | partial/planned | Current metadata exists for selected/focused/disabled subsets; full interaction visuals are planned. |
