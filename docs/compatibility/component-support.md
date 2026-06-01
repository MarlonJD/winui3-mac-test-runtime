# WinUI Component Support

This page is the readable component view of
`winui-api-compatibility.catalog.json`. The JSON catalog remains the source of
truth for diagnostics and strict mode. A WinUI control or feature not listed
here is unsupported in this alpha until it receives a catalog entry, fixture
coverage, renderer behavior, and public Windows reference evidence.

`winui-component-inventory.json` is the component parity lab inventory. It maps
the Microsoft Learn controls inventory and local source-audit gaps to a public
fixture page, expected catalog status, interaction coverage, visual evidence
grade, and known gaps.

## Status Model

| Status | Meaning |
| --- | --- |
| `supported` | The documented subset is available and covered by tests or public fixtures. |
| `partial` | A limited subset is available; unsupported properties, templates, states, or visual behavior remain gaps. |
| `planned` | Cataloged roadmap item. Strict diagnostics should prevent silent parity claims. |
| `windows-only` | Validated on Windows or excluded from the macOS shadow build, not executed locally. |
| `not supported` | Explicit non-goal for the current clean-room runtime. |

## Visual Evidence Grades

Whole-screenshot pixel metrics are scenario smoke evidence only. Component
grades are recorded in `component-evidence.json` and must stay honest:

| Grade | Meaning |
| --- | --- |
| `good` | Structure and visible state are close to Windows; only minor text or edge differences remain. |
| `usable` | Recognizable and functionally correct, but native chrome differs. |
| `weak` | Structure exists, but important visual details are missing or visibly simplified. |
| `poor` | Visibly wrong, collapsed, misplaced, or misleading. |
| `not-rendered` | Diagnostic-only, planned, Windows-only, or unsupported. |

## Component Parity Lab

`ComponentParityLab.WinUI` is a public Windows-targeted fixture with eight
clean-room pages:

| Page | Coverage |
| --- | --- |
| Page 1: Basic input | `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, and diagnostic rows for remaining basic input controls. |
| Page 2: Text and forms | `TextBlock`, `TextBox`, form labels, and diagnostic rows for rich text, password, number, and autosuggest controls. |
| Page 3: Collections | `ItemsControl`, `ListView`, item-template diagnostics, and collection control diagnostics. |
| Page 4: Dialogs and flyouts | Diagnostic rows for dialog, flyout, teaching tip, tooltip, and tooltip service coverage. |
| Page 5: Commands and menus | `CommandBar`, `AppBarButton`, icon slot coverage, and menu/flyout diagnostics. |
| Page 6: Navigation and workbench | `NavigationView`, `NavigationViewItem`, `Frame`, `Page`, menu items, pane footer, and list/details structure. |
| Page 7: Status and pickers | `InfoBar`, `ProgressBar`, `ProgressRing`, and picker/person/status diagnostics. |
| Page 8: Layout, media, visuals | `ScrollViewer`, `Grid`, `StackPanel`, `Border`, `FontIcon`, `Image`, resources, theme/source diagnostics, media, web, ink, and backdrop diagnostics. |

The foundation also tracks downstream source-audit gaps explicitly:
`SymbolIcon`, `XamlControlsResources`,
`ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
`Style`, `Setter`, `Color`, `SolidColorBrush`, `CornerRadius`,
`DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
`CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`,
`NavigationView.MenuItems`, `NavigationView.PaneFooter`,
`ToolTipService.SetToolTip`, and `Window.SystemBackdrop / MicaBackdrop`.

## Cataloged Controls

| Component | Status | Current compatibility | Known gaps or failure modes |
| --- | --- | --- | --- |
| `Application` | supported | App launch facade and `Application` XAML root compile for public fixtures. | Full Windows lifecycle and packaging behavior are not modeled. |
| `Window` | supported | Content, title, activation, and system-backdrop diagnostics. | Native window chrome, real WinUI compositor integration, and material behavior are not implemented. |
| `Page` | supported | Page root and activation through `Frame.Navigate`. | Full navigation stack behavior is limited. |
| `UserControl` | supported | Content root subset. | Templates and broader resource behavior remain limited. |
| `Frame` | supported | `Navigate(Type, object?)`, page activation, and XAML `Frame.Content`. | Back stack, transition animations, and complex navigation state are not implemented. |
| `NavigationView` | partial | Menu items, selected item, pane footer, shell layout, and `SelectionChanged`. | Adaptive pane modes, settings item behavior, full keyboarding, focus visuals, and exact Fluent styling remain partial. |
| `NavigationViewItem` | partial | Content, tag metadata, selected state, and shell renderer coverage. | Full icon/text state styling and nested menu behavior remain partial. |
| `CommandBar` | supported | `PrimaryCommands` and public `skia-v2` command rendering. | Overflow, secondary commands, keyboard accelerators, and exact command surface chrome are not complete. |
| `AppBarButton` | supported | Label, icon slot, click event hookup, and command-bar fixture coverage. | Overflow behavior, full pressed/hover/focus states, and exact native visuals are planned. |
| `Button` | supported | Content, command, click simulation, accessibility export, and visual painter coverage. | Flyouts, templates, advanced states, and exact native chrome are not complete. |
| `ToggleButton` | supported | Checked state and click behavior. | Full Fluent state styling and templating are planned. |
| `CheckBox` | supported | Checked state, tree export, accessibility role, layout, and painter coverage. | Indeterminate styling and full Fluent state visuals remain limited. |
| `RadioButton` | supported | Checked state, group metadata, tree export, and painter coverage. | Full group behavior and exact styling remain limited. |
| `TextBlock` | supported | Text content, binding, accessibility, and visual painter coverage. | Text wrapping, trimming, typography, and font metrics are approximated. |
| `TextBox` | supported | Text, focus, two-way binding, typed input, and renderer coverage. | Selection, caret rendering, IME, validation states, and exact text metrics are not complete. |
| `Border` | partial | Single-child facade and deterministic layout subset. | Corner radius, border thickness variants, brushes, shadows, and complex backgrounds are limited. |
| `Grid` | partial | Child containment, column metadata, column spacing, and deterministic layout subset. | Full row/column sizing, spanning, alignment, and layout invalidation remain partial. |
| `StackPanel` | partial | Orientation, spacing, child containment, and deterministic layout subset. | Full measure/arrange parity and edge-case alignment are partial. |
| `ScrollViewer` | supported | Single content slot and vertical scroll bar metadata. | Real scrolling, scrollbars, inertia, and viewport clipping are limited. |
| `ContentControl` | supported | Single content-slot facade. | Templates and complex content transitions are planned. |
| `ItemsControl` | supported | Item collection binding and export. | Virtualization, item containers, templates, and selection are not general-purpose. |
| `ListView` | partial | Item collection, `SelectedIndex`, export, and placeholder/list painter coverage. | Virtualization, item templates, multi-select, keyboarding, and exact selected styling are partial. |
| `ComboBox` | supported | Items and selected index subset. | Popup behavior, editable mode, item templates, and full keyboarding are not complete. |
| `ProgressBar` | supported | Determinate and indeterminate metadata plus public renderer coverage. | Native animation and exact Fluent visuals are approximated. |
| `ProgressRing` | supported | Active state and public renderer coverage. | Native animation and reduced-motion behavior are planned. |
| `InfoBar` | supported | Title, message, severity, open state, and public fixture renderer coverage. | Close button behavior, action buttons, layout variants, and exact Fluent styling are partial. |
| `Image` | partial | Source metadata, tree export, and placeholder/list renderer coverage. | Real image decoding, scaling modes, nine-grid, and async loading are not complete. |
| `FontIcon` | partial | Glyph metadata and simple renderer support. | Font fallback, sizing, line metrics, and exact Segoe Fluent icon rendering are approximated. |
| `MediaPlayerElement` | not supported | Explicitly outside the current macOS-managed runtime contract. | Media playback is not implemented. |
| `WebView2` | not supported | Explicitly outside the current clean-room runtime. | Embedded browser hosting is not implemented. |

## Project And XAML Source Features

| Feature | Status | Current compatibility | Known gaps or failure modes |
| --- | --- | --- | --- |
| Windows-targeted `TargetFramework` | supported | `net*-windows*` WinUI projects are detected and redirected to a `net10.0` compat shadow project. | Multi-targeting selection is minimal. |
| `UseWinUI` | supported | Marks eligible WinUI projects for compat shadow build discovery. | Projects with unsupported packaging features fail before build. |
| `Microsoft.WindowsAppSDK` package | windows-only | Detected and excluded from macOS shadow builds. | Windows App SDK MSBuild targets are not executed on macOS. |
| `WindowsPackageType=None` | partial | Unpackaged projects are accepted. | Packaged outputs remain Windows-only. |
| `WindowsAppSDKSelfContained` | planned | Diagnosed before shadow build. | Self-contained Windows deployment is not supported on macOS. |
| `ApplicationDefinition` | supported | `App.xaml` compiles through the local XAML compiler. | Complex application resources remain limited. |
| `Page` item | supported | Window/Page XAML files compile through the local XAML compiler. | Unsupported elements fail with catalog diagnostics. |
| `x:Class`, `x:Name`, `x:Uid` | supported | Generates partial classes, named fields, and localization metadata. | `x:Bind` remains planned. |
| `AutomationProperties.Name`, `AutomationProperties.HelpText` | supported | Exported to tree/accessibility artifacts. | Broader automation peers are not modeled. |
| `StaticResource`, `ThemeResource`, `Style.Setter` | partial/supported subset | Simple resource lookup and supported style setters work with strict missing-resource diagnostics. | Full Fluent theme dictionaries, template resources, and dynamic theme behavior are planned. |
| `Control.Template` | planned | Cataloged for diagnostics. | Real templates are not implemented. |
| `VisualStateManager.VisualStateGroups` | planned | Cataloged for diagnostics. | Full state groups, transitions, and reduced-motion behavior are not implemented. |
| `Button.Flyout` | planned | Cataloged for diagnostics. | Flyout source compatibility and rendering are not implemented. |

## Materials, Composition, And Platform APIs

| Feature | Status | Current compatibility | Known gaps or failure modes |
| --- | --- | --- | --- |
| `MicaBackdrop`, `DesktopAcrylicBackdrop`, `AcrylicBrush`, `SystemBackdrop` | planned | Source compatibility targets with catalog diagnostics. | No Mica, Acrylic, or system backdrop rendering yet. |
| `ThemeShadow`, `DropShadow` | planned | Cataloged depth targets. | No real Fluent shadow parity yet. |
| `Compositor`, `SpriteVisual`, `CompositionEffectBrush` | planned | Cataloged compositor/effect targets. | No compositor visual tree or effect graph implementation yet. |
| Key-frame animations and `Storyboard` | planned | Cataloged motion targets. | No deterministic animation clock or transition system yet. |
| `Transform` | planned | Cataloged transform target. | Translate, scale, rotate, and composite transforms are not implemented. |
| Fluent disabled/focused/selected states | partial | State metadata is exported for supported controls. | Exact visual styling is incomplete. |
| Fluent pointer-over/pressed states | planned | Cataloged for future interaction parity. | Pointer and pressed visual states are not implemented broadly. |
| `Microsoft.Windows.AppLifecycle.AppInstance` | windows-only | Windows activation behavior is not executed locally. | Validate through real Windows workflows. |
| `Windows.System.Launcher` | windows-only | Host OS launching remains a Windows validation concern. | Not executed by the macOS runtime. |

## Not Yet Cataloged

Common WinUI controls such as `AutoSuggestBox`, `CalendarDatePicker`,
`CalendarView`, `ColorPicker`, `DatePicker`, `DropDownButton`, `Expander`,
`FlipView`, `GridView`, `HyperlinkButton`, `MenuBar`, `MenuFlyout`,
`NumberBox`, `PasswordBox`, `PersonPicture`, `RatingControl`, `RichEditBox`,
`Slider`, `SplitButton`, `SplitView`, `TabView`, `TeachingTip`, `TimePicker`,
`TreeView`, and `TwoPaneView` are not part of the current alpha support claim.
The component parity lab now gives these controls explicit diagnostic rows and
`not-rendered` evidence entries; strict mode should still treat uncataloged
runtime usage as a compatibility gap until the feature is added to the API
catalog and backed by public tests.
