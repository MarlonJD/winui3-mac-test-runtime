# Compatibility Matrix

Status values:

- `supported`: implemented and covered by a fixture or test.
- `partial`: implemented for a constrained headless test subset.
- `planned`: part of the public direction but not implemented yet.
- `not supported`: intentionally outside the current public compatibility
  contract.

## Compatibility Levels

| Level | Status | Current public contract |
| --- | --- | --- |
| Level 0: Harness Reliability | supported | Managed macOS runner, doctor, SVG, current Skia, `skia-v2`, versioned artifacts, strict visual failures, and public CI workflow wiring. |
| Level 1: Core App And XAML Compatibility | supported | `Application`, `Window`, `Page`, `Frame`, resource dictionaries, startup activation, navigation, strict unsupported XAML diagnostics, and the documented public XAML subset. |
| Level 2: Layout And Controls Foundation | supported | Public fixture subset for `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl`, `ItemsControl`, `TextBlock`, `TextBox`, `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, `Image`, `ListView`, `ProgressRing`, `ProgressBar`, `InfoBar`, `CommandBar`, `AppBarButton`, `NavigationView`, `NavigationViewItem`, `Frame`, and `FontIcon`. |
| Level 3: Styling, Resources, And Theme Fidelity | supported | Resource lookup, style setter application for supported properties, light/dark/high-contrast renderer themes, and strict resource diagnostics for the public subset. |
| Level 4: Data Binding, Commands, And State | supported | One-way and two-way binding for supported properties, `INotifyPropertyChanged`, observable item collections, command execution state, button command invocation, and navigation state for public fixtures. |
| Level 5: Input, Accessibility, And Automation | partial | Scripted click, focus, navigation selection, frame navigation, accelerator invocation, and deterministic accessibility export. |
| Level 6: Windows Reference Visual Compatibility | partial | Public `windows-latest` reference workflow and scenario-local pixel thresholds for current strict fixtures. |
| Level 7: Release And Consumption Readiness | planned | Packages can be smoked, but release/consumer contracts are not complete. |

## Runtime

| Area | Status | Notes |
| --- | --- | --- |
| Wine-free managed host | supported | Runs facade-backed .NET assemblies on macOS. |
| Windows executable loading | not supported | Binary `.exe` compatibility is out of scope. |
| `run.json` and `tree.json` | supported | Emitted for every runner invocation. |
| `accessibility.json` | supported | Role/name/label/help/focus approximation from the logical tree. |
| `binding-failures.json` | supported | Versioned envelope captures unresolved paths and non-writable targets. |
| `resource-failures.json` | supported | Versioned envelope captures unresolved static and theme resources. |
| `unsupported-apis.json` | supported | Versioned envelope captures clean-room placeholder facade APIs that were touched. |
| `diagnostics.sarif` | supported | Warning diagnostics derived from binding, resource, and unsupported API reports with stable `WINUI3MAC001`, `WINUI3MAC002`, and `WINUI3MAC003` rule IDs. |
| Scripted click/focus actions | supported | Name-based interaction script actions. |
| Versioned interaction scripts | supported | Script input accepts `schemaVersion: 0.1`; reports emit `schemaVersion: 0.1`. |
| Keyboard accelerators | partial | Headless accelerator model exists; broader routing is planned. |
| Snapshot output | partial | Deterministic SVG fallback and Skia-backed PNG output are available for the supported tree subset. |
| Scenario JSON visual runs | supported | `--scenario`, `--viewport`, `--scale`, `--theme`, `--strict-visual`, `--reference`, and `--diff-output` drive the strict path. |
| Deterministic layout export | partial | `skia-v2` scenarios add arranged rectangles, desired sizes, padding, alignment, and visibility to `tree.json`. |
| Pixel diff artifacts | supported | `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, `pixel-diff.json`, and `visual-run.json` are emitted for reference-backed runs. |
| Windows reference source of truth | supported | Public `windows-latest` workflow captures generic reference screenshots and macOS comparison artifacts. |

## XAML

| Construct | Status | Notes |
| --- | --- | --- |
| `x:Class` | supported | Generates a partial class. |
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
| Control templates and materials | not supported | Templates, Mica, Acrylic, and compositor-backed materials are reported or documented as unsupported. |

## Facade Controls

| Type | Status | Notes |
| --- | --- | --- |
| `Application`, `Window`, `Page` | supported | Basic lifecycle and page activation. |
| `Frame` | supported | Supports `Navigate(Type, object?)`. |
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

## Unsupported Facade Placeholders

| Type | Status | Notes |
| --- | --- | --- |
| `Microsoft.UI.Xaml.Media.MicaBackdrop` | reported | Placeholder records `unsupported-apis.json`; no visual material behavior. |
| `Microsoft.UI.Xaml.Media.AcrylicBrush` | reported | Placeholder records `unsupported-apis.json`; no visual material behavior. |
