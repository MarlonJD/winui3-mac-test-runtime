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
| `accessibility.json` | supported | Role/name/label approximation from the logical tree. |
| `binding-failures.json` | supported | Captures unresolved paths and non-writable targets. |
| Scripted click/focus actions | supported | Name-based interaction script actions. |
| Keyboard accelerators | partial | Headless accelerator model exists; broader routing is planned. |
| Snapshot output | partial | Deterministic SVG snapshot is available; Skia-backed raster output is planned. |

## XAML

| Construct | Status | Notes |
| --- | --- | --- |
| `x:Class` | supported | Generates a partial class. |
| `x:Name` / `Name` | supported | Generates strongly typed fields. |
| Text content | supported | Covered for `TextBlock` and `Button`. |
| Event hookup | supported | Covered for routed click and navigation selection events. |
| `{StaticResource ...}` | partial | Simple dictionary lookup. |
| `{ThemeResource ...}` | partial | Simple dictionary lookup with key fallback. |
| `{Binding Path}` | partial | One-way refresh through `BindingOperations.RefreshTree`. |
| Styles and templates | planned | Style values are stored but not applied visually. |

## Facade Controls

| Type | Status | Notes |
| --- | --- | --- |
| `Application`, `Window`, `Page` | supported | Basic lifecycle and page activation. |
| `Frame` | supported | Supports `Navigate(Type, object?)`. |
| `StackPanel`, `Grid`, `Border` | partial | Logical child containment only. |
| `TextBlock`, `TextBox`, `Button` | supported | Basic content/text and button click. |
| `Image`, `ListView` | partial | Logical model only. |
| `NavigationView`, `NavigationViewItem` | partial | Menu items, selection, and pane footer. |
| `FontIcon` | partial | Glyph and font size metadata only. |
