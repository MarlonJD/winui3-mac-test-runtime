# WinUI Component Parity Lab Plan

## Goal

Build a public, reference-backed WinUI component parity lab before making
stronger visual parity claims. The lab should show which WinUI controls are
supported, partially supported, planned, not supported, or Windows-only; render
those controls across a small set of realistic demo pages; and publish
component-level evidence that explains where the macOS runtime is close to real
Windows and where it is still visibly wrong.

This plan intentionally pauses broad renderer implementation work until the
inventory, scenarios, scoring model, and evidence artifacts are clear.

## Sources

- Microsoft Learn, "Controls for Windows apps":
  https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/
- Microsoft Learn, "What's supported when migrating from UWP to WinUI 3":
  https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/what-is-supported
- Fluent 2 Windows components:
  https://fluent2.microsoft.design/components/windows

Microsoft Learn currently describes Windows app controls as the UI elements that
make up a Windows app and notes that Windows provides more than 45 ready-to-use
controls built on Fluent Design. The plan below uses that public WinUI control
inventory as the external reference and the repository's compatibility catalog
as the local source of truth.

## Current-State Audit

### Blocking

- The current README evidence can be read as "visually successful" even when
  the screenshot pair clearly has major visual differences. Passing pixel
  thresholds currently proves only that a scenario stayed inside a broad alpha
  gate, not that it is visually close to Windows.
- Component evidence is not granular enough. A whole scenario can pass while
  `CommandBar`, `InfoBar`, `ListView`, or text metrics are visibly weak.
- The current `component-support.md` is a static narrative table. It does not
  connect each component to a demo page, interaction check, bounding box, crop
  diff, or artifact.

### Important

- The public admin/workbench scenario is useful as a source-ingestion smoke
  test, but it should be labeled as `weak visual parity` until the structured
  controls inside the workbench are rendered closer to Windows.
- The existing `ControlGallery.MacTest` is too small to answer "which WinUI
  components render correctly?" It covers a subset of current facade controls,
  but it does not cover the full Microsoft Learn inventory.
- Unsupported and uncataloged controls need deliberate diagnostics. They should
  not disappear silently from screenshots.
- A downstream Windows WinUI 3 source audit found additional real-app coverage
  needs beyond visible control names: `SymbolIcon`, `XamlControlsResources`,
  theme dictionaries, style setters, item templates, property-slot XAML,
  `ToolTipService`, and `Window.SystemBackdrop` / `MicaBackdrop`. Treat these
  as lab requirements because real apps depend on them even when the visible
  control is already cataloged.

### Non-Goals

- Do not claim complete WinUI 3 compatibility.
- Do not copy private app UI, private screenshots, proprietary product names,
  or private repository content.
- Do not implement every control in this pass. The first pass builds the lab,
  evidence model, and demo pages; renderer fixes should follow from measured
  failures.
- Do not use the WinUI 3 Gallery source or screenshots as fixture content.
  Use only public generic clean-room fixtures.

## WinUI Component Inventory

The initial inventory should be checked into
`docs/compatibility/winui-component-inventory.json` and summarized in
`docs/compatibility/component-support.md`. Each entry should include:

- `component`
- `kind`: `control`, `pattern`, `platform-api`, `material`, or `layout`
- `winuiAvailability`: `stable`, `experimental`, `community-only`,
  `windows-only`, or `unknown`
- `catalogStatus`: `supported`, `partial`, `planned`, `windows-only`,
  `not supported`, or `unknown`
- `demoPage`
- `fixtureCoverage`
- `interactionCoverage`
- `visualEvidence`
- `knownGaps`

### Basic Input

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `Button` | supported | Page 1 |
| `RepeatButton` | unknown/planned | Page 1 diagnostic row |
| `HyperlinkButton` / hyperlink pattern | unknown/planned | Page 1 diagnostic row |
| `DropDownButton` | unknown/planned | Page 1 diagnostic row |
| `SplitButton` | unknown/planned | Page 1 diagnostic row |
| `ToggleSplitButton` | unknown/planned | Page 1 diagnostic row |
| `ToggleButton` | supported | Page 1 |
| `CheckBox` | supported | Page 1 |
| `RadioButton` | supported | Page 1 |
| `ComboBox` | supported | Page 1 |
| `Slider` | unknown/planned | Page 1 diagnostic row |
| `ToggleSwitch` | unknown/planned | Page 1 diagnostic row |
| `RatingControl` | unknown/planned | Page 1 diagnostic row |

### Text And Forms

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `TextBlock` | supported | Page 2 |
| `RichTextBlock` | unknown/planned | Page 2 diagnostic row |
| `TextBox` | supported | Page 2 |
| `RichEditBox` | unknown/planned | Page 2 diagnostic row |
| `PasswordBox` | unknown/planned | Page 2 diagnostic row |
| `NumberBox` | unknown/planned | Page 2 diagnostic row |
| `AutoSuggestBox` | unknown/planned | Page 2 diagnostic row |
| `AutoSuggestBox.QueryIcon` | unknown/planned | Page 2 diagnostic row |
| Labels/forms pattern | partial/planned | Page 2 |

### Collections

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `ItemsControl` | supported | Page 3 |
| `ItemsView` | unknown/planned | Page 3 diagnostic row |
| `ListView` | partial | Page 3 |
| `DataTemplate` / item templates | partial/planned | Page 3 |
| `ListView.ItemTemplate` | partial/planned | Page 3 |
| `ItemsControl.ItemTemplate` | partial/planned | Page 3 |
| `GridView` | unknown/planned | Page 3 diagnostic row |
| `FlipView` | unknown/planned | Page 3 diagnostic row |
| `PipsPager` | unknown/planned | Page 3 diagnostic row |
| `TreeView` | unknown/planned | Page 3 diagnostic row |
| `ItemsRepeater` | unknown/planned | Page 3 diagnostic row |
| Swipe pattern | unknown/planned | Page 3 diagnostic row |
| Pull-to-refresh pattern | unknown/planned | Page 3 diagnostic row |

### Dialogs, Flyouts, And Guidance

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `ContentDialog` | unknown/planned | Page 4 diagnostic row |
| `Flyout` | unknown/planned | Page 4 diagnostic row |
| `TeachingTip` | unknown/planned | Page 4 diagnostic row |
| `ToolTip` | unknown/planned | Page 4 diagnostic row |
| `ToolTipService.SetToolTip` | unknown/planned | Page 4 diagnostic row |

### Menus And Commanding

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `CommandBar` | supported but visually weak | Page 5 |
| `AppBarButton` | supported but visually weak | Page 5 |
| `CommandBar.Content` | partial/planned | Page 5 |
| `AppBarButton.Icon` | partial/planned | Page 5 |
| `CommandBarFlyout` | unknown/planned | Page 5 diagnostic row |
| `MenuFlyout` | unknown/planned | Page 5 diagnostic row |
| `MenuBar` | unknown/planned | Page 5 diagnostic row |
| Context menu pattern | unknown/planned | Page 5 diagnostic row |

### Navigation

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `NavigationView` | partial | Page 6 |
| `NavigationViewItem` | partial | Page 6 |
| `NavigationView.MenuItems` | partial | Page 6 |
| `NavigationView.PaneFooter` | partial | Page 6 |
| `Frame` / `Page` | supported | Page 6 |
| `BreadcrumbBar` | unknown/planned | Page 6 diagnostic row |
| List/details pattern | partial | Page 6 |
| `Pivot` | unknown/planned | Page 6 diagnostic row |
| `SelectorBar` | unknown/planned | Page 6 diagnostic row |
| `TabView` | unknown/planned | Page 6 diagnostic row |

### Pickers, People, Status, Layout, And Media

| Component or family | Initial local status | Lab placement |
| --- | --- | --- |
| `PersonPicture` | unknown/planned | Page 7 diagnostic row |
| `ColorPicker` | unknown/planned | Page 7 diagnostic row |
| `CalendarDatePicker` | unknown/planned | Page 7 diagnostic row |
| `CalendarView` | unknown/planned | Page 7 diagnostic row |
| `DatePicker` | unknown/planned | Page 7 diagnostic row |
| `TimePicker` | unknown/planned | Page 7 diagnostic row |
| `ProgressBar` | supported | Page 7 |
| `ProgressRing` | supported | Page 7 |
| `InfoBar` | supported but visually weak | Page 7 |
| `InfoBadge` | unknown/planned | Page 7 diagnostic row |
| `Expander` | unknown/planned | Page 8 diagnostic row |
| `ScrollViewer` | supported | Page 8 |
| Annotated scrollbar | unknown/planned | Page 8 diagnostic row |
| `SemanticZoom` | unknown/planned | Page 8 diagnostic row |
| `SplitView` | unknown/planned | Page 8 diagnostic row |
| `TwoPaneView` | unknown/planned | Page 8 diagnostic row |
| Icons / `FontIcon` | partial | Page 8 |
| `SymbolIcon` | unknown/planned | Page 8 diagnostic row |
| `AnimatedIcon` | unknown/planned | Page 8 diagnostic row |
| `Image` / image brushes | partial | Page 8 |
| `MediaPlayerElement` | not supported | Page 8 diagnostic row |
| Shapes | unknown/planned | Page 8 diagnostic row |
| `InkCanvas` / `InkToolbar` | experimental/not stable | Page 8 diagnostic row |
| Title bar customization | planned | Page 8 diagnostic row |
| `Window.SystemBackdrop` / `MicaBackdrop` | planned | Page 8 diagnostic row |
| `WebView2` | not supported | Page 8 diagnostic row |

### Resources, Templates, Slots, And Materials

These are not all standalone controls, but they must be represented in the
inventory because real WinUI apps depend on them for source ingestion, theming,
and visual parity.

| Feature or family | Initial local status | Lab placement |
| --- | --- | --- |
| `XamlControlsResources` | unknown/planned | `App.xaml` bootstrap plus Page 8 diagnostic row |
| `ResourceDictionary.ThemeDictionaries` | partial/planned | Page 8 and high-contrast scenario |
| `ThemeResource` / `StaticResource` | partial | Every supported page; explicit Page 8 resource checks |
| `Style` / `Setter` | supported subset | Every supported page; explicit Page 8 style checks |
| `Color` / `SolidColorBrush` | partial/planned | Page 8 resource checks |
| `CornerRadius` resources | partial/planned | Page 8 layout/material checks |
| Property-slot XAML | partial/planned | Owning pages: `CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`, `NavigationView.MenuItems`, `NavigationView.PaneFooter` |
| Item templates | partial/planned | Page 3: `DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate` |
| Tooltip service pattern | unknown/planned | Page 4: `ToolTipService.SetToolTip` |
| System backdrop material | planned | Page 8: `Window.SystemBackdrop`, `MicaBackdrop`, and high-contrast fallback |

## Demo Fixture Plan

Create a new public fixture:

`fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj`

It should be a Windows-targeted WinUI source fixture, not a mac-only fixture:

- `TargetFramework=net10.0-windows10.0.19041.0`
- `UseWinUI=true`
- `WindowsPackageType=None`
- `Microsoft.WindowsAppSDK` package reference
- `App.xaml`, `MainWindow.xaml`, and page XAML files
- `XamlControlsResources` in `App.xaml`
- light, dark, and high-contrast theme dictionaries
- Generic public content only

The fixture should use `NavigationView` to switch between demo pages. Each page
should fit in a single deterministic viewport and avoid private or brand-specific
content.

### Page 1: Basic Input

Controls:

- `Button`
- `ToggleButton`
- `CheckBox`
- `RadioButton`
- `ComboBox`
- diagnostic placeholders for `RepeatButton`, `DropDownButton`, `SplitButton`,
  `ToggleSwitch`, `Slider`, and `RatingControl`

Checks:

- click button
- toggle checked state
- select combo item
- assert state text
- compare Windows/mac runtime screenshot

### Page 2: Text And Forms

Controls:

- `TextBlock`
- `TextBox`
- `AutoSuggestBox.QueryIcon`
- form labels
- diagnostic placeholders for `AutoSuggestBox`, `NumberBox`, `PasswordBox`,
  `RichTextBlock`, and `RichEditBox`

Checks:

- focus text box
- type text
- assert text update
- verify label/accessibility export

### Page 3: Collections

Controls:

- `ItemsControl`
- `ListView`
- `DataTemplate`
- `ListView.ItemTemplate`
- `ItemsControl.ItemTemplate`
- diagnostic placeholders for `GridView`, `ItemsView`, `ItemsRepeater`,
  `TreeView`, `FlipView`, `PipsPager`, swipe, and pull-to-refresh

Checks:

- select list item
- assert selected item state
- compare list row geometry and selection highlight

### Page 4: Dialogs And Flyouts

Controls:

- diagnostic placeholders for `ContentDialog`, `Flyout`, `TeachingTip`, and
  `ToolTip`
- diagnostic coverage for `ToolTipService.SetToolTip`

Checks:

- no silent pass for unsupported flyout/dialog controls
- catalog diagnostics should identify planned or unknown entries

### Page 5: Commands And Menus

Controls:

- `CommandBar`
- `AppBarButton`
- `CommandBar.Content`
- `AppBarButton.Icon`
- diagnostic placeholders for `CommandBarFlyout`, `MenuFlyout`, `MenuBar`, and
  context menus

Checks:

- click primary command
- assert status update
- component crop diff for command bar and app bar buttons

### Page 6: Navigation And Workbench

Controls:

- `NavigationView`
- `NavigationViewItem`
- `NavigationView.MenuItems`
- `NavigationView.PaneFooter`
- `Frame`
- `Page`
- list/details pattern
- diagnostic placeholders for `BreadcrumbBar`, `Pivot`, `SelectorBar`, and
  `TabView`

Checks:

- select navigation item
- assert selected item and page title
- compare pane, selected indicator, frame content, and list/detail regions

### Page 7: Status, Progress, Pickers, People

Controls:

- `InfoBar`
- `ProgressBar`
- `ProgressRing`
- diagnostic placeholders for `InfoBadge`, `PersonPicture`, `ColorPicker`,
  `CalendarDatePicker`, `CalendarView`, `DatePicker`, and `TimePicker`

Checks:

- assert `InfoBar` severity/state
- compare progress visuals
- verify unsupported picker/person controls are explicit diagnostics

### Page 8: Layout, Media, Visuals

Controls:

- `ScrollViewer`
- `Grid`
- `StackPanel`
- `Border`
- `FontIcon`
- `SymbolIcon`
- `Image`
- `XamlControlsResources`
- `ResourceDictionary.ThemeDictionaries`
- `ThemeResource`
- `StaticResource`
- `Style`
- `Setter`
- `Color`
- `SolidColorBrush`
- `CornerRadius`
- diagnostic placeholders for `Expander`, annotated scrollbar,
  `SemanticZoom`, `SplitView`, `TwoPaneView`, `AnimatedIcon`, shapes,
  `MediaPlayerElement`, `WebView2`, title bar customization, system backdrop
  material, and ink controls

Checks:

- compare nested layout geometry
- verify light, dark, and high-contrast resource resolution
- verify `Window.SystemBackdrop` and `MicaBackdrop` are explicit planned
  diagnostics, with high contrast falling back to a non-material surface
- assert no unsupported visual features are hidden
- ensure `MediaPlayerElement` and `WebView2` are explicit non-supported rows

## Scenario Plan

Add scenario JSON files under
`fixtures/ComponentParityLab.WinUI/scenarios/`:

- `component-basic-input-light.json`
- `component-text-forms-light.json`
- `component-collections-light.json`
- `component-dialogs-flyouts-light.json`
- `component-commands-menus-light.json`
- `component-navigation-workbench-light.json`
- `component-status-pickers-light.json`
- `component-layout-media-light.json`
- high contrast follow-up for the pages that contain supported controls

Each scenario should include:

- viewport
- theme
- strict visual thresholds
- interaction steps
- required components
- required source features, including resource dictionaries, property slots,
  item templates, and material diagnostics
- expected component statuses

Example requirement shape:

```json
{
  "component": "InfoBar",
  "target": "StatusInfo",
  "expectedStatus": "supported",
  "minimumVisualGrade": "usable",
  "requiredProperties": ["title", "message", "severity", "isOpen"]
}
```

## Evidence Artifact Plan

Add a new artifact:

`component-evidence.json`

Schema version: `0.1`

Suggested shape:

```json
{
  "schemaVersion": "0.1",
  "scenarioName": "component-basic-input-light",
  "components": [
    {
      "component": "Button",
      "kind": "control",
      "target": "PrimaryButton",
      "catalogStatus": "supported",
      "presence": "present",
      "interactionStatus": "passed",
      "visualGrade": "usable",
      "changedPixelPercentage": 7.2,
      "meanAbsoluteError": 3.1,
      "rootMeanSquaredError": 14.8,
      "knownGaps": ["Exact Fluent pointer and focus states are not rendered."]
    }
  ],
  "sourceFeatures": [
    {
      "feature": "NavigationView.PaneFooter",
      "kind": "xaml-property-slot",
      "catalogStatus": "partial",
      "presence": "present",
      "knownGaps": ["Exact native footer layout and focus visuals remain partial."]
    }
  ]
}
```

Visual grades:

- `good`: structure and visible state are close; minor text/edge differences
- `usable`: recognizable and functionally correct, but native chrome differs
- `weak`: structure exists but important visual details are missing
- `poor`: visibly wrong, collapsed, misplaced, or misleading
- `not-rendered`: diagnostic only or unsupported

The global scenario result should not hide weak component results. README and
docs should show both:

- whole-screenshot metrics
- per-component grades and known gaps

## Windows Reference Workflow Plan

Update `.github/workflows/windows-native-screenshot.yml` to:

1. Capture Windows references for each component lab page.
2. Render macOS runtime output for each component lab page.
3. Upload all `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`,
   `visual-run.json`, and `component-evidence.json` files.
4. Fail when a supported component falls below its minimum visual grade.
5. Do not fail when a planned/unsupported component is rendered as a diagnostic
   placeholder, as long as the placeholder and catalog status are explicit.
6. Include theme/resource and property-slot evidence so a page cannot pass while
   `XamlControlsResources`, theme dictionaries, item templates, or common slot
   elements were skipped.

## Documentation Plan

Update README and docs after evidence exists:

- Replace "passed strict comparison" wording with a more honest grade:
  `good`, `usable`, `weak`, or `poor`.
- Show the component lab page list.
- Link to `component-evidence.json` examples.
- Keep `docs/compatibility/winui-api-compatibility.catalog.json` as the source
  of truth.
- Keep `docs/compatibility/component-support.md` as the human-readable report.
- Keep `docs/visual-parity/README.md` as the evidence index.

The existing public admin/workbench screenshot should be reclassified as
`weak visual parity / source-ingestion smoke evidence` until renderer fixes make
its structured controls closer to Windows.

## Implementation Phases

### Phase 1: Inventory And Lab Skeleton

- Add `winui-component-inventory.json`.
- Add `ComponentParityLab.WinUI` with 8 pages.
- Add scenario files with requirement metadata.
- Add inventory rows for the downstream source-audit gaps: `SymbolIcon`,
  `XamlControlsResources`, `ResourceDictionary.ThemeDictionaries`,
  `DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
  property-slot XAML, `ToolTipService.SetToolTip`, and
  `Window.SystemBackdrop` / `MicaBackdrop`.
- Add tests that ensure inventory entries map to catalog statuses.
- Add tests that every source feature discovered in the public fixture maps to
  either a supported catalog entry or an explicit diagnostic entry.
- Do not promote any control status yet.

### Phase 2: Component Evidence Artifact

- Extend scenario parsing to read component requirements.
- Extend scenario parsing to read source-feature requirements.
- Generate `component-evidence.json`.
- Add unit tests for evidence grading and missing component detection.
- Add unit tests for missing source-feature evidence.
- Add CLI output paths for component evidence.

### Phase 3: Windows Reference Expansion

- Update `WindowsNativeProbe` to draw matching clean-room reference pages.
- Update `windows-native-screenshot.yml` to include all lab pages.
- Download artifacts and inspect representative pages.

### Phase 4: Targeted Renderer Fixes

Only after Phase 1-3 identify concrete failures:

- Fix structured `Frame` rendering inside `NavigationView`.
- Improve `Grid` column layout.
- Improve `ListView` selected item visuals.
- Improve `CommandBar` and `AppBarButton` chrome.
- Improve `InfoBar` layout and severity visuals.
- Add minimal `SymbolIcon`, template, property-slot, and resource-dictionary
  support only when the lab evidence proves the source feature is needed.
- Add only the smallest facade/compiler support needed for controls selected in
  the current page.

### Phase 5: Evidence Docs

- Update README with a sober evidence table.
- Include representative public screenshots only when they are public,
  synthetic, and free of private names.
- Add per-component grade summaries.
- Document residual risks and unsupported controls.

## Verification Gate

Run after implementation:

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
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
```

For each component lab scenario, run the strict local macOS command and then
trigger `windows-native-screenshot.yml`. Download and inspect the artifacts
before final handoff.

Run the operator-provided private-name denylist:

```sh
rg -n "<private-name-denylist-regex>" .
```

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| The lab becomes a fake gallery that hides unsupported controls. | Unsupported controls must appear as explicit diagnostic rows with catalog status. |
| Whole-screenshot pass hides bad components. | Add `component-evidence.json` and component-level grades. |
| The fixture grows too broad to debug. | Split into 8 deterministic pages and one scenario per page. |
| Docs overstate parity. | Use `good`, `usable`, `weak`, `poor`, and `not-rendered` labels instead of broad pass language. |
| Renderer fixes become speculative. | Fix only controls that fail a public component scenario. |

## Execution Prompt

Use `$google-eng-practices` and `$windows-winui3-design` and implement
`docs/plans/2026-06-01-winui-component-parity-lab-plan.md` in the public
`MarlonJD/winui3-mac-test-runtime` repository.

Start by building the component parity lab foundation, not by making broad
renderer changes. Add a public Windows-targeted `ComponentParityLab.WinUI`
fixture with 8 demo pages covering the Microsoft Learn WinUI controls
inventory, add component inventory metadata, add scenario requirements, and
generate `component-evidence.json` so each component has catalog status,
presence, interaction status, visual grade, known gaps, and optional crop/diff
metrics. Include the downstream source-audit gaps in the foundation:
`SymbolIcon`, `XamlControlsResources`, `ResourceDictionary.ThemeDictionaries`,
`ThemeResource`, `StaticResource`, `Style`, `Setter`, `Color`,
`SolidColorBrush`, `CornerRadius`, `DataTemplate`, `ListView.ItemTemplate`,
`ItemsControl.ItemTemplate`, `CommandBar.Content`, `AppBarButton.Icon`,
`AutoSuggestBox.QueryIcon`, `NavigationView.MenuItems`,
`NavigationView.PaneFooter`, `ToolTipService.SetToolTip`, and
`Window.SystemBackdrop` / `MicaBackdrop`. Update README and compatibility docs
so visual evidence is honest:
whole-screenshot pass is not enough, and visibly weak components must be labeled
as weak or poor. Preserve existing `winui3-mac-doctor`, `winui3-mac-runner`,
SVG, current Skia, `skia-v2`, existing fixtures, and public admin/workbench
source ingestion.

Do not use private repositories, private screenshots, private product names,
secrets, proprietary fixture content, or copied WinUI Gallery fixture content.
Keep identifiers, source comments, and canonical docs in English.

Run targeted tests while working and the final local verification gate from the
plan. If visual scenarios or renderer behavior change, trigger
`windows-native-screenshot.yml`, wait for completion, download artifacts, and
inspect the relevant `windows-reference.png`, `mac-runtime.png`,
`pixel-diff.png`, `visual-run.json`, and `component-evidence.json` files before
final handoff. Commit only relevant files with author
`marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push
immediately.
