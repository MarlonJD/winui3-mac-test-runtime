# WinUI Component Support

This page is the readable component view of
`winui-api-compatibility.catalog.json`. The JSON catalog remains the source of
truth for diagnostics and strict mode. A WinUI control or feature not listed
here is unsupported in this alpha until it receives a catalog entry, fixture
coverage, renderer behavior, and native WinUI public Windows reference
evidence.

`winui-component-inventory.json` is the component parity lab inventory. It maps
the Microsoft Learn controls inventory and local source-audit gaps to a public
fixture page, expected catalog status, interaction coverage, visual evidence
grade, and known gaps.

The latest checked-in visual evidence uses native WinUI Windows references from
public workflow run
[`26777029415`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26777029415)
on commit `95e8d7d`. The Windows references prove the public fixture pages show
the intended native controls on Windows. The macOS comparisons currently fail
for the inspected component scenarios, so text-only or absent macOS output stays
`not-rendered`.

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
| `not-rendered` | No meaningful macOS component visual is present yet; this includes diagnostic-only, planned, Windows-only, unsupported, empty, or text-only output. |

## Component Parity Lab

`ComponentParityLab.WinUI` is a public Windows-targeted fixture with eight
clean-room pages:

| Page | Coverage |
| --- | --- |
| Page 1: Basic input | Native Windows fixture controls for `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, and diagnostic rows for remaining basic input controls; current macOS screenshot evidence is text-only and graded `not-rendered`. |
| Page 2: Text and forms | `TextBlock`, native Windows fixture controls for `TextBox` and form labels, and diagnostic rows for rich text, password, number, and autosuggest controls; current macOS TextBox/form output is text-only and graded `not-rendered`. |
| Page 3: Collections | Native Windows fixture controls for `ItemsControl`, `ListView`, item-template diagnostics, and collection control diagnostics; current macOS collection output is text-only and graded `not-rendered`. |
| Page 4: Dialogs and flyouts | Diagnostic rows for dialog, flyout, teaching tip, tooltip, and tooltip service coverage. |
| Page 5: Commands and menus | Native Windows fixture controls for `CommandBar`, `AppBarButton`, icon slot coverage, and menu/flyout diagnostics; current macOS command output is text-only and graded `not-rendered`. |
| Page 6: Navigation and workbench | Native Windows fixture controls for `NavigationView`, `NavigationViewItem`, `Frame`, `Page`, menu items, pane footer, and list/details structure; current macOS inner navigation/list-detail visuals are text-only and graded `not-rendered` except `Frame` and `Page` host coverage. |
| Page 7: Status and pickers | Native Windows fixture controls for `InfoBar`, `ProgressBar`, `ProgressRing`, and picker/person/status diagnostics; current macOS status controls are absent from the screenshot and graded `not-rendered`. |
| Page 8: Layout, media, visuals | Native Windows fixture controls for `ScrollViewer`, `Grid`, `StackPanel`, `Border`, `FontIcon`, `Image`, resources, theme/source diagnostics, media, web, ink, and backdrop diagnostics; current macOS layout/media component visuals are text-only or absent and graded `not-rendered`, while resource smoke rows remain `usable`. |

The foundation also tracks downstream source-audit gaps explicitly:
`SymbolIcon`, `XamlControlsResources`,
`ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
`Style`, `Setter`, `Color`, `SolidColorBrush`, `CornerRadius`,
`DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
`CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`,
`NavigationView.MenuItems`, `NavigationView.PaneFooter`,
`ToolTipService.SetToolTip`, and `Window.SystemBackdrop / MicaBackdrop`.

Latest inspected native comparison counts:

| Scenario | Native comparison | Component evidence |
| --- | --- | --- |
| `component-basic-input-light` | Failed: `42.07%` changed pixels over the `18%` threshold. | 13 `not-rendered`. |
| `component-commands-menus-light` | Failed: `40.68%` changed pixels over the `24%` threshold. | 8 `not-rendered`. |
| `component-layout-media-light` | Failed: `45.83%` changed pixels over the `24%` threshold. | 4 `usable`, 24 `not-rendered`. |

## Cataloged Controls

| Component | Status | Current compatibility | Known gaps or failure modes |
| --- | --- | --- | --- |
| `Application` | supported | App launch facade and `Application` XAML root compile for public fixtures. | Full Windows lifecycle and packaging behavior are not modeled. |
| `Window` | supported | Content, title, activation, and system-backdrop diagnostics. | Native window chrome, real WinUI compositor integration, and material behavior are not implemented. |
| `Page` | supported | Page root and activation through `Frame.Navigate`. | Full navigation stack behavior is limited. |
| `UserControl` | supported | Content root subset. | Templates and broader resource behavior remain limited. |
| `Frame` | supported | `Navigate(Type, object?)`, page activation, and XAML `Frame.Content`. | Back stack, transition animations, and complex navigation state are not implemented. |
| `NavigationView` | partial | Source ingestion, tree export, and navigation selection are present. | Current macOS evidence for the inner fixture NavigationView is text-only, so component visual evidence is `not-rendered` until native pane and selection chrome render. |
| `NavigationViewItem` | partial | Content, tag metadata, selected state, and tree export are present. | Current macOS evidence emits item content as text only; native item chrome and selected state visuals are not rendered. |
| `CommandBar` | supported | `PrimaryCommands`, command click simulation, and public fixture ingestion are present. | Current macOS evidence emits only command result text; command surface chrome, overflow, focus, and pointer visuals are not rendered. |
| `AppBarButton` | supported | Label, icon slot metadata, click event hookup, and command-bar fixture coverage are present. | Current macOS evidence emits only command result text; AppBarButton label/icon chrome is not rendered. |
| `Button` | supported | Content, command, click simulation, and accessibility export are present. | Current macOS evidence emits only button text; native button chrome, focus, pressed, hover, and template visuals are not rendered. |
| `ToggleButton` | supported | Checked state and click behavior are present in the logical model. | Current macOS evidence emits only toggle text; checked native WinUI chrome is not rendered. |
| `CheckBox` | supported | Checked state, tree export, and accessibility role are present. | Current macOS evidence emits only checkbox text; checkbox glyph and state chrome are not rendered. |
| `RadioButton` | supported | Checked state, group metadata, and tree export are present. | Current macOS evidence emits only radio button text; radio glyph and state chrome are not rendered. |
| `TextBlock` | supported | Text content, binding, accessibility, and visual painter coverage. | Text wrapping, trimming, typography, and font metrics are approximated. |
| `TextBox` | supported | Text, focus, typed input, and automation metadata are present. | Current macOS evidence emits only the text value; TextBox border, caret, selection, focus, validation, and native input chrome are not rendered. |
| `Border` | partial | Single-child facade and deterministic layout metadata are present. | Current macOS evidence does not render the border surface, corner radius, thickness, brush, or background. |
| `Grid` | partial | Child containment, column metadata, and column spacing are present. | Current macOS evidence emits only child text; visible grid structure and native layout shape are not rendered. |
| `StackPanel` | partial | Orientation, spacing metadata, and child containment are present. | Current macOS evidence emits only child text; spacing/layout behavior is not enough for component visual parity. |
| `ScrollViewer` | supported | Single content slot and vertical scroll bar metadata are present. | Current macOS evidence emits only child text; scrollbars, clipping, inertia, and scrolling chrome are not rendered. |
| `ContentControl` | supported | Single content-slot facade. | Templates and complex content transitions are planned. |
| `ItemsControl` | supported | Item collection binding and export are present. | Current macOS evidence emits only item text; item containers, templates, virtualization, and native list spacing are not rendered. |
| `ListView` | partial | Item collection, `SelectedIndex`, selected item metadata, and export are present. | Current macOS evidence emits only list item text; rows, selection chrome, templates, multi-select, and keyboarding visuals are not rendered. |
| `ComboBox` | supported | Items and selected index subset are present. | Current macOS evidence emits combo box items as text; field, chevron, popup, item chrome, editable mode, and full keyboarding are not rendered. |
| `ProgressBar` | supported | Minimum, maximum, and value metadata are present. | Current macOS evidence does not render the ProgressBar track or indicator. |
| `ProgressRing` | supported | Active-state metadata is present. | Current macOS evidence does not render the ProgressRing. |
| `InfoBar` | supported | Title, message, severity, and open-state metadata are present. | Current macOS evidence does not render the InfoBar body, close button, action area, or severity chrome. |
| `Image` | partial | Source metadata and tree export are present. | Current macOS evidence does not render the image source or a visible placeholder in the inspected component fixture. |
| `FontIcon` | partial | Glyph metadata is present. | Current macOS evidence does not render the FontIcon glyph in the inspected component fixture. |
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
