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

`production-component-targets.md` is the sanitized harness target inventory.
It maps the first harness-ring component families to public clean-room
fixture scenarios, required interaction and accessibility coverage, target
visual grades, smoke or E2E targets, and private-content safety checks. It does
not promote any component grade by itself.

Component grades are guarded at the claim level by the
`ClaimedSupportedComponentsAreNeverNotRendered` test, which scans every component
parity lab and corpus scenario and fails if any requirement that claims a
`supported` or `partial` component declares a `not-rendered` grade or a grade
below its declared minimum. This keeps whole-image comparisons from hiding
component-level regressions. The corpus `resource-catalog` app additionally
carries light, dark, and high-contrast theme scenarios that emit graded
`component-evidence.json` for its `TextBlock`, `Border`, and `ThemeResource`
surfaces.

The latest checked-in visual evidence uses native WinUI Windows references from
public workflow runs and local `skia-v2` artifact inspection. The Windows
references prove the public fixture pages show the intended native controls on
Windows. Current Ring 0 macOS output renders recognizable harness scaffolding
and target layout regions for selected controls, but it is still visibly
simplified and not native Fluent visual fidelity. Planned, unsupported,
Windows-only, or diagnostic-only rows remain `not-rendered`.

Support claims are restricted to components listed in
`production-component-targets.md` with public smoke or E2E coverage. A component
listed below is not visually production-supported merely because it has a facade
type; it must also have component evidence, interaction/accessibility evidence
where applicable, native WinUI reference provenance, and inspected renderer
quality. `usable` means suitable for harness testing, not pixel-level parity.

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
| Page 1: Basic input | Native Windows fixture controls for `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, `RepeatButton`, `HyperlinkButton`, `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `Slider`, `ToggleSwitch`, and `RatingControl`; current macOS `skia-v2` light evidence is 13 `usable` harness rows, plus 3 usable checked-state rows, with simplified but visible control chrome. |
| Page 2: Text and forms | `TextBlock`, native Windows fixture controls for `TextBox` and form labels, and diagnostic rows for rich text, password, number, and autosuggest controls; current macOS `skia-v2` TextBox/form output is usable for smoke testing with approximate input chrome. |
| Page 3: Collections | Native Windows fixture controls for `ItemsControl`, `ListView`, item-template diagnostics, and collection control diagnostics; current macOS `skia-v2` collection output is usable for basic item rows and selected chrome, with templates and virtualization missing. |
| Page 4: Dialogs and flyouts | Dialog, flyout, tooltip, teaching tip, and tooltip service coverage; current macOS `skia-v2` output is usable for the partial `ContentDialog`, `Flyout`, and `ToolTip` open-state subset while `TeachingTip` and tooltip service remain planned. |
| Page 5: Commands and menus | Native Windows fixture controls for `CommandBar`, `AppBarButton`, icon slot coverage, content-slot, MenuBar, context target, and menu/flyout diagnostics; current macOS `skia-v2` command output is 8 `usable` harness rows and zero `not-rendered`, with simplified command/flyout/menu chrome. |
| Page 6: Navigation and workbench | Native Windows fixture controls for `NavigationView`, `NavigationViewItem`, `Frame`, `Page`, menu items, pane footer, and list/details structure; current macOS `skia-v2` output is usable for the Ring 0 navigation/list-detail scaffold. |
| Page 7: Status and pickers | Native Windows fixture controls for `InfoBar`, `ProgressBar`, `ProgressRing`, and picker/person/status diagnostics; current macOS `skia-v2` base, loading, and success scenarios are usable for status/progress smoke testing, while animation and close/action areas remain gaps. |
| Page 8: Layout, media, visuals | Native Windows fixture controls for `ScrollViewer`, `Grid`, `StackPanel`, `Border`, `FontIcon`, `Image`, resources, theme/source diagnostics, adaptive containers, media, web, ink, and backdrop diagnostics; current macOS `skia-v2` light evidence is 21 `usable` and 7 planned/non-goal `not-rendered` rows while advanced diagnostics remain `not-rendered`. |

The foundation also tracks downstream source-audit gaps explicitly:
`SymbolIcon`, `XamlControlsResources`,
`ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
`Style`, `Setter`, `Color`, `SolidColorBrush`, `CornerRadius`,
`DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
`CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`,
`NavigationView.MenuItems`, `NavigationView.PaneFooter`,
`ToolTipService.SetToolTip`, and `Window.SystemBackdrop / MicaBackdrop`.

Milestone 1 adds public scenario coverage for focused, disabled, checked,
invalid, selected, command-invoked, loading, error, success, and open-popup
states. These scenarios are listed in
`winui-component-inventory.json` under `productionStateCoverage`; they provide
native Windows fixture capture targets. Milestone 2 promotes the supported and
partial Ring 0 `skia-v2` component output to `usable` only where local artifact
inspection shows recognizable harness output and `component-evidence.json`
records target layout regions. Promotion to higher visual quality still
requires direct renderer work and native reference review.

Latest inspected native comparison counts:

| Scenario | Native comparison | Component evidence |
| --- | --- | --- |
| `component-basic-input-light` / `component-basic-input-checked-light` | Native-reference strict visual comparison for the base light scenario is checked in and currently fails the native-quality threshold; checked-state native comparison remains a CI artifact concern. | Base scenario has 13 `usable` rows and zero `not-rendered` rows; checked-state scenario has 3 `usable`. Component targets include layout regions; native chrome remains approximate and not manually promoted. |
| `component-status-pickers-light` / `component-status-pickers-loading-light` / `component-status-pickers-success-light` | Local strict runs passed without a local Windows reference path; native reference comparison remains a CI artifact concern. | Base scenario has 3 `usable`, 7 planned `not-rendered`; loading and success scenarios add 4 `usable` state rows. Success-state evidence was regenerated with nonblank crops for `InfoBar` and `ProgressBar`. |
| `component-dialogs-flyouts-light` | Local strict run passed without a local Windows reference path; native reference comparison remains a CI artifact concern. | `ContentDialog`, `Flyout`, and `ToolTip` are `usable` with passed open/dismiss interaction evidence; `TeachingTip` and tooltip service remain planned `not-rendered`. |
| `component-commands-menus-light` | Native-reference strict visual comparison is checked in and currently fails the native-quality threshold. | 8 `usable` rows and zero `not-rendered`; `CommandBarFlyout` and `MenuFlyout` have passed open/invoke interaction evidence, while `CommandBar.Content`, `MenuBar`, and context menu target export remain static partial evidence. |
| `component-layout-media-light` | Native-reference strict visual comparison is checked in and currently fails the native-quality threshold. | 21 `usable`, 7 planned/non-goal `not-rendered`, including usable `SymbolIcon`, `XamlControlsResources`, `Color`, `Expander`, annotated scrollbar, `SemanticZoom`, `SplitView`, `TwoPaneView`, theme dictionary, `ThemeResource`, `SolidColorBrush`, and `CornerRadius` evidence with layout regions. |
| `component-layout-media-dark` / `component-layout-media-high-contrast` | Local strict runs passed without local Windows reference paths; native reference comparison remains a CI artifact concern. | 6 `usable` resource/layout smoke targets in each theme; `ThemeResource` resolves to theme-specific foreground values. |
| `public-admin-workbench-light` | Local strict run passed without a local Windows reference path; native reference comparison remains a CI artifact concern. | 9 `usable`, no missing component regions; visually it is a simplified workbench scaffold, not native-quality parity. |

## Cataloged Controls

| Component | Status | Current compatibility | Known gaps or failure modes |
| --- | --- | --- | --- |
| `Application` | supported | App launch facade and `Application` XAML root compile for public fixtures. | Full Windows lifecycle and packaging behavior are not modeled. |
| `Window` | supported | Content, title, activation, and system-backdrop diagnostics. | Native window chrome, real WinUI compositor integration, and material behavior are not implemented. |
| `Page` | supported | Page root and activation through `Frame.Navigate`. | Full navigation stack behavior is limited. |
| `UserControl` | supported | Content root subset. | Templates and broader resource behavior remain limited. |
| `Frame` | supported | `Navigate(Type, object?)`, page activation, and XAML `Frame.Content`. | Back stack, transition animations, and complex navigation state are not implemented. |
| `NavigationView` | partial | Source ingestion, tree export, selection, pane/footer layout, and `skia-v2` pane rendering are present. | Adaptive modes, keyboarding, focus visuals, and exact Fluent spacing remain gaps. |
| `NavigationViewItem` | partial | Content, tag metadata, selected state, tree export, and `skia-v2` item chrome are present. | Full Fluent item states and keyboarding remain gaps. |
| `CommandBar` | supported | `PrimaryCommands`, command click simulation, public fixture ingestion, and `skia-v2` command surface rendering are present. | Overflow, pointer states, focus visuals, and exact native spacing remain gaps. |
| `AppBarButton` | supported | Label, icon slot metadata, click event hookup, command-bar fixture coverage, and `skia-v2` label/icon chrome are present. | Overflow placement, focus visuals, and exact native command sizing remain gaps. |
| `CommandBarFlyout` | partial | Open state, primary/secondary command export, `invokeMenuItem` command invocation, accessibility expanded state, and `skia-v2` popup chrome are present for public smoke scenarios. | Native placement, overflow, dismissal semantics, focus trapping, and full keyboarding remain gaps. |
| `MenuFlyout` | partial | Open state, menu item export, `invokeMenuItem` item invocation, accessibility popup/menuitem roles, and `skia-v2` menu chrome are present for public smoke scenarios. | Native placement, light-dismiss, accelerator text, disabled item behavior, and full keyboarding remain gaps. |
| `Button` | supported | Content, command, click simulation, accessibility export, and `skia-v2` button chrome are present. | Native pressed, hover, focus, and template visuals remain approximate. |
| `ToggleButton` | supported | Checked state, click behavior, and `skia-v2` checked/disabled chrome are present. | Native pressed, hover, focus, and template visuals remain approximate. |
| `CheckBox` | supported | Checked state, tree export, accessibility role, and `skia-v2` glyph chrome are present. | Indeterminate state and full template visuals remain gaps. |
| `RadioButton` | supported | Checked state, group metadata, tree export, and `skia-v2` radio glyph chrome are present. | Group keyboarding and full template visuals remain gaps. |
| `TextBlock` | supported | Text content, binding, accessibility, and visual painter coverage. | Text wrapping, trimming, typography, and font metrics are approximated. |
| `TextBox` | supported | Text, focus, typed input, automation metadata, and `skia-v2` field chrome are present. | Caret, selection, validation visuals, and native input behavior remain gaps. |
| `Border` | partial | Single-child facade, deterministic layout metadata, and `skia-v2` surface/stroke rendering are present. | Brush and thickness fidelity remain partial. |
| `Grid` | partial | Child containment, column metadata, column spacing, two-column layout, and `skia-v2` region evidence are present. | Full Grid sizing semantics, row definitions, spanning, and adaptive layout remain partial. |
| `StackPanel` | partial | Orientation, spacing metadata, child containment, and `skia-v2` region evidence are present. | Full native layout behavior remains partial. |
| `ScrollViewer` | supported | Single content slot, vertical scroll bar metadata, and `skia-v2` scroll-region affordance are present. | Clipping, inertia, and scrolling physics are not modeled. |
| `ContentControl` | supported | Single content-slot facade. | Templates and complex content transitions are planned. |
| `ContentDialog` | partial | Title/content/primary button metadata, open/dismiss state, accessibility dialog role and expanded state, and `skia-v2` dialog chrome are present for public smoke scenarios. | Modal focus trapping, native overlay/window semantics, secondary buttons, and default button handling remain gaps. |
| `Flyout` | partial | Content, open state, accessibility expanded state, and `skia-v2` popup chrome are present for public smoke scenarios. | Native placement, light-dismiss, target relationship, and keyboarding remain gaps. |
| `ToolTip` | partial | Content, open state, accessibility tooltip role, and `skia-v2` popup chrome are present for public smoke scenarios. | Hover timing, placement, and tooltip service attachment semantics remain gaps. |
| `ItemsControl` | supported | Item collection binding, export, and `skia-v2` item row rendering are present. | Templates and virtualization remain gaps. |
| `ListView` | partial | Item collection, `SelectedIndex`, selected item metadata, export, and `skia-v2` row/selection chrome are present. | Templates, multi-select, keyboarding, and virtualization remain gaps. |
| `ComboBox` | supported | Items, selected index subset, and `skia-v2` field/chevron chrome are present. | Popup, editable mode, item chrome, and full keyboarding remain gaps. |
| `ProgressBar` | supported | Minimum, maximum, value metadata, and `skia-v2` track/indicator rendering are present. | Indeterminate animation remains a gap. |
| `ProgressRing` | supported | Active-state metadata and `skia-v2` static ring rendering are present. | Native animation remains a gap. |
| `InfoBar` | supported | Title, message, severity, open-state metadata, and `skia-v2` severity chrome are present. | Close button and action area are not modeled. |
| `Image` | partial | Source metadata, tree export, and a visible `skia-v2` placeholder are present. | Real image decode and stretch modes remain gaps. |
| `FontIcon` | partial | Glyph metadata and `skia-v2` glyph output are present. | Exact Segoe MDL2 metrics and glyph availability remain approximate on macOS. |
| `MediaPlayerElement` | not supported | Explicitly outside the current macOS-managed runtime contract. | Media playback is not implemented. |
| `WebView2` | not supported | Explicitly outside the current clean-room runtime. | Embedded browser hosting is not implemented. |
| `Expander` | partial | Static expanded header/content metadata, accessibility expanded state, and `skia-v2` header/content chrome are present for public evidence. | Collapse interaction, focus visuals, and expanded automation behavior remain partial. |
| Annotated scrollbar pattern | partial | Static rail and marker metadata plus `skia-v2` rail/marker diagnostics are present for public evidence. | Scroll position binding, annotation labels, and pointer interaction remain planned. |
| `SemanticZoom` | partial | Static zoomed-in and zoomed-out preview panes render in `skia-v2`. | Zoom switching and collection semantics remain planned. |
| `SplitView` | partial | Static open pane/content metadata and `skia-v2` pane/content chrome are present. | Display modes, light dismiss, compact overlay behavior, and interaction remain partial. |
| `TwoPaneView` | partial | Static side-by-side pane metadata and `skia-v2` pane layout are present. | Adaptive spanning behavior and responsive breakpoints remain planned. |

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
| `AutomationProperties.Name`, `AutomationProperties.HelpText`, `AutomationProperties.AutomationId` | supported | Exported to tree/accessibility artifacts; automation IDs are accepted by interaction selectors. | Full UIA automation peers and FlaUI provider compatibility are not modeled yet; FlaUI 5.0 + FlaUI.UIA3 is the native Windows automation target and macOS requires a compatible adapter. |
| `StaticResource`, `ThemeResource`, `Style.Setter` | partial/supported subset | Simple resource lookup, active light/dark/high-contrast theme dictionary lookup, supported style setters, and resource-backed `Border.CornerRadius` work with strict missing-resource diagnostics. | Full Fluent theme dictionaries, template resources, typed brush objects, and dynamic resource invalidation are planned. |
| `Control.Template` | planned | Cataloged for diagnostics. | Real templates are not implemented. |
| `VisualStateManager.VisualStateGroups` | planned | Cataloged for diagnostics. | Full state groups, transitions, and reduced-motion behavior are not implemented. |
| `Button.Flyout` | partial | Buttons can host the supported popup subset for scripted open/invoke smoke scenarios. | Native placement, light-dismiss, target relationship, and full pointer/keyboard behavior remain partial. |

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
`CalendarView`, `ColorPicker`, `DatePicker`, `FlipView`, `GridView`,
`NumberBox`, `PasswordBox`, `PersonPicture`, `RichEditBox`, `TabView`,
`TeachingTip`, `TimePicker`, and `TreeView` are not part of the current support
claim. The component parity lab now gives these controls explicit diagnostic
rows and `not-rendered` evidence entries; strict mode should still treat
uncataloged runtime usage as a compatibility gap until the feature is added to
the API catalog and backed by public tests. `MenuBar`, `Expander`,
`SplitView`, and `TwoPaneView` have static partial public evidence but are not
native-quality support claims. `RepeatButton`, `HyperlinkButton`,
`DropDownButton`, `SplitButton`, `ToggleSplitButton`, `Slider`,
`ToggleSwitch`, and `RatingControl` have bounded `component-basic-input-light`
rendering evidence, but remain partial harness rows until native-quality
inspection, API-level automation evidence, and full interaction coverage are
complete.
