# Compatibility Matrix

Status values:

- `supported`: implemented and covered by a fixture or test.
- `partial`: implemented for a constrained headless test subset.
- `planned`: part of the public direction but not implemented yet.

## Runtime

| Area | Status | Notes |
| --- | --- | --- |
| Wine-free managed host | supported | Runs facade-backed .NET assemblies on macOS. |
| Windows executable loading | not supported | Binary `.exe` compatibility is out of scope. |
| `run.json` and `tree.json` | supported | Emitted for every runner invocation. |
| `accessibility.json` | supported | Role/name/label/help/focus approximation from the logical tree. |
| `binding-failures.json` | supported | Captures unresolved paths and non-writable targets. |
| `resource-failures.json` | supported | Captures unresolved static and theme resources. |
| `unsupported-apis.json` | supported | Captures clean-room placeholder facade APIs that were touched. |
| `diagnostics.sarif` | supported | Warning diagnostics derived from binding, resource, and unsupported API reports. |
| Scripted click/focus actions | supported | Name-based interaction script actions. |
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
| Text content | supported | Covered for `TextBlock` and `Button`. |
| Event hookup | supported | Covered for routed click and navigation selection events. |
| `{StaticResource ...}` | partial | Simple dictionary lookup with missing-resource reporting. |
| `{ThemeResource ...}` | partial | Simple dictionary lookup with missing-resource reporting. |
| `AutomationProperties.Name` | supported | Exported into `tree.json` and `accessibility.json`. |
| `AutomationProperties.HelpText` | supported | Exported into `tree.json` and `accessibility.json`. |
| `{Binding Path}` | partial | One-way refresh through `BindingOperations.RefreshTree`. |
| Styles and templates | planned | Style values are stored but not applied visually. |

## Facade Controls

| Type | Status | Notes |
| --- | --- | --- |
| `Application`, `Window`, `Page` | supported | Basic lifecycle and page activation. |
| `Frame` | supported | Supports `Navigate(Type, object?)`. |
| `StackPanel`, `Grid`, `Border` | partial | Logical child containment and deterministic `skia-v2` layout for public scenarios. |
| `TextBlock`, `TextBox`, `Button` | supported | Basic content/text and button click. |
| `Image`, `ListView` | partial | Logical model plus `skia-v2` placeholder/list painters for public scenarios. |
| `NavigationView`, `NavigationViewItem` | partial | Menu items, selection, pane footer, and `skia-v2` shell painter. |
| `FontIcon` | partial | Glyph and font size metadata with simple `skia-v2` glyph/dot rendering. |

## Visual Renderer Subset

`skia-v2` is intentionally narrower than WinUI 3. It currently paints the public
fixture subset: `Window`, `Page`, `Grid`, `StackPanel`, `Border`, `TextBlock`,
`Button`, `TextBox`, `Frame`, `NavigationView`, `NavigationViewItem`,
`ListView`, `FontIcon`, `Image`, and string content. A strict scenario records
any unsupported control or missing renderer feature in `unsupported-apis.json`
and exits non-zero.

## Unsupported Facade Placeholders

| Type | Status | Notes |
| --- | --- | --- |
| `Microsoft.UI.Xaml.Media.MicaBackdrop` | reported | Placeholder records `unsupported-apis.json`; no visual material behavior. |
| `Microsoft.UI.Xaml.Media.AcrylicBrush` | reported | Placeholder records `unsupported-apis.json`; no visual material behavior. |
