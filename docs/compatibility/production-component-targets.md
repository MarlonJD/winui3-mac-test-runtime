# Production Component Targets

Date: 2026-06-01

This document is the sanitized public inventory for the first production
component target. It maps required WinUI component families to clean-room public
fixtures without naming any private application, source path, screenshot,
customer data, API, secret, or proprietary workflow.

The target is **not all WinUI 3**. The target is the component subset needed to
prove production smoke and E2E workflows through public fixture evidence,
native WinUI Windows reference provenance, macOS renderer evidence,
interaction artifacts, and accessibility export.

## Scope And Safety

- Runtime model: Wine-free, source-level, managed macOS execution against local
  `Microsoft.UI.Xaml` facade types.
- Windows source of truth: public native WinUI fixture runs from
  `windows-native-screenshot.yml`.
- Fixture content: clean-room, generic component pages and workbench scenarios.
- Current support claim: production target inventory only. A component is not
  production-supported until the target grade and evidence gates below are met.
- Private content rule: do not commit private names, private source paths,
  private screenshots, private product data, secrets, copied WinUI Gallery
  content, or proprietary fixture text.

## Production Priority Model

| Priority | Meaning | Support expectation |
| --- | --- | --- |
| Ring 0 | Production smoke foundation. | Required before any production smoke claim. |
| Ring 1 | Production E2E enabler. | Required before the matching E2E flow is claimed. |
| Ring 2 | Production polish or broader compatibility. | Deferred unless a public production target explicitly needs it. |
| Non-goal | Outside the current clean-room macOS runtime. | Must remain documented as unsupported or Windows-only. |

## Evidence Gate

Every production-ring component must have all of the following before its
production target is considered met:

| Evidence | Requirement |
| --- | --- |
| Public fixture | The component appears in a clean-room public fixture scenario. |
| Native provenance | `windows-reference.json` records `referenceSource: native-winui`. |
| macOS renderer evidence | `mac-runtime.png` shows the component with meaningful visual output. |
| Component evidence | `component-evidence.json` records catalog status, presence, interaction status, visual grade, target layout region, and known gaps. |
| Interaction evidence | `interactions.json` records deterministic pass/fail details for the required action path when the component is interactive. |
| Accessibility evidence | `accessibility.json` exposes the required role, name, enabled state, selected/checked/value state, and relationships. |
| Visual grade | Ring 0 and claimed Ring 1 components reach at least `usable` after artifact inspection. |

Current checked-in evidence keeps planned and unsupported controls at
`not-rendered`. Ring 0 supported and partial controls require `usable`
component evidence with target layout regions before they can satisfy the
production smoke foundation.

## Ring 0 Targets

| Component family | Components and patterns | Current catalog status | Public fixture scenario | Required interaction coverage | Required accessibility coverage | Target visual grade | Smoke or E2E target | Current blocker |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| App shell | `Application`, `Window`, `Page`, `Frame` | supported | `public-admin-workbench-light`, `component-navigation-workbench-light` | launch, activate, navigate, stable title | window/page names, frame content relationship | `usable` | production smoke launch and navigation | Native window chrome and full lifecycle remain out of scope. |
| Core layout | `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl` | supported or partial | `component-layout-media-light` | scroll where applicable | names for scrollable regions and contained content | `usable` | smoke layout and resize | skia-v2 renders visible layout regions; full WinUI layout behavior remains partial. |
| Text | `TextBlock`, labels/forms pattern | supported or partial | `component-text-forms-light` | none beyond tree export | text names, labels, help text where supplied | `usable` | smoke form readability | Text metrics, wrapping, trimming, and form layout are approximate. |
| Basic commands | `Button`, `AppBarButton`, `CommandBar` | supported | `component-basic-input-light`, `component-commands-menus-light`, `public-admin-workbench-light` | click, command invocation, enabled and disabled assertions | button role, name, enabled state, command result status | `usable` | smoke primary command | skia-v2 renders command chrome; overflow, pointer states, and exact native spacing remain gaps. |
| Forms | `TextBox`, `ComboBox`, `CheckBox`, `RadioButton` | supported | `component-basic-input-light`, `component-text-forms-light` | focus, text entry, item selection, checked state export | edit/combo/check/radio roles, names, values, checked state | `usable` | smoke form entry and validation | skia-v2 renders field/glyph chrome; popup, caret, selection, and validation visuals remain gaps. |
| Workbench | `NavigationView`, `NavigationViewItem`, `NavigationView.MenuItems`, `NavigationView.PaneFooter`, `ListView`, list/details pattern | partial | `public-admin-workbench-light`, `component-navigation-workbench-light`, `component-collections-light` | navigation selection, list selection, detail update | navigation/list roles, selected state, pane footer name | `usable` | smoke workbench flow | skia-v2 renders pane, selected row, list/detail, and footer regions; adaptive layout and keyboarding remain gaps. |
| Status | `InfoBar`, `ProgressBar`, `ProgressRing` | supported | `component-status-pickers-light`, `public-admin-workbench-light` | state assertion, progress value export, status update | status title/message/severity, progress value, active state | `usable` | smoke loading, warning, error, success | skia-v2 renders InfoBar and progress chrome; close/action areas and native animations remain gaps. |
| Resources and theme | `StaticResource`, `ThemeResource`, `Style`, `Setter`, simple typography and spacing | supported or partial | `component-layout-media-light` | none beyond strict diagnostics | resource-driven text remains visible in light, dark, and high contrast | `usable` | smoke theme/resource gate | Full theme dictionaries and dynamic Fluent resource behavior remain planned. |
| Artifacts | `tree.json`, `accessibility.json`, `visual-run.json`, `component-evidence.json`, pixel diff artifacts | supported | all production-ring scenarios | deterministic pass/fail details | complete deterministic export for supported controls | n/a | every smoke run | Component-region evidence records target layout boxes so whole-screen thresholds cannot hide missing controls. |

## Ring 1 Targets

| Component family | Components and patterns | Current catalog status | Public fixture scenario | Required interaction coverage | Required accessibility coverage | Target visual grade | Smoke or E2E target | Current blocker |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Menus and flyouts | `MenuBar`, `MenuFlyout`, `CommandBarFlyout`, context menu pattern, `DropDownButton`, `SplitButton`, `ToggleSplitButton` | planned | `component-commands-menus-light`, `component-basic-input-light` | open, invoke item, dismiss, disabled item assertion | menu/menuitem roles, expanded state, item names, accelerator text | `usable` when claimed | command/menu E2E flow | Facade, compiler, renderer, and scenario open-state support are incomplete. |
| Rich form input | `PasswordBox`, `NumberBox`, `AutoSuggestBox`, `AutoSuggestBox.QueryIcon`, `Slider`, `ToggleSwitch` | planned | `component-text-forms-light`, `component-basic-input-light` | focus, type/change value, suggestions or selection, checked/value assertion | edit/spin/slider/switch roles, values, checked state, query icon name | `usable` when claimed | data-bound form E2E flow | Controls are diagnostic rows only. |
| Dialog decisions | `ContentDialog`, `Flyout`, `TeachingTip`, `ToolTip`, `ToolTipService.SetToolTip` | planned | `component-dialogs-flyouts-light` | open, primary/secondary action, dismiss, tooltip target assertion | dialog/flyout roles, title, body, buttons, expanded or popup relationship | `usable` when claimed | dialog/flyout decision E2E flow | Dialog and popup host behavior is not implemented. |
| Templates and collections | `DataTemplate`, `ItemsControl.ItemTemplate`, `ListView.ItemTemplate`, `ItemsRepeater`, `GridView` | planned except partial collection hosts | `component-collections-light` | item generation, list/grid selection, detail update | item roles, selected state, template text names | `usable` when claimed | list/detail E2E flow | Template parsing, container visuals, virtualization, and item chrome are missing. |
| Theme dictionaries and resources | `XamlControlsResources`, `ResourceDictionary.ThemeDictionaries`, `Color`, `SolidColorBrush`, `CornerRadius` | planned or partial | `component-layout-media-light` | strict missing-resource diagnostics | visible themed text and non-color-only state in light, dark, high contrast | `usable` when claimed | theme/scale E2E flow | Full Fluent resource dictionaries and typed brush/color conversion are incomplete. |
| Keyboard and accessibility | focus traversal, accelerators, selected/checked/value states | partial | all production-ring scenarios | tab order, accelerator invocation, keyboard selection, focus assertion | Narrator-ready names, roles, state, value, relationships | n/a | every production E2E flow | Focus visuals and broad keyboard routing are incomplete. |

## Ring 2 And Explicit Non-Goals

| Area | Components or APIs | Production status |
| --- | --- | --- |
| Advanced controls | `TabView`, `TreeView`, `BreadcrumbBar`, `Expander`, `RatingControl`, `PersonPicture`, `ColorPicker`, date/time controls | Ring 2 unless a public production target moves a component into Ring 1. |
| Materials and depth | Mica, Acrylic, system backdrops, shadows, transforms, compositor effects, motion | Ring 2 compatibility target; no production visual claim yet. |
| Media and platform integration | `MediaPlayerElement`, `WebView2`, ink, launcher, arbitrary platform APIs | Non-goal for the first production gate unless explicitly scoped into a future tier. |
| Windows binary execution | `.exe`, `.msix`, packaged Windows App SDK execution, Wine-backed execution | Non-goal for this repository. |

## Required Public Smoke Suite

The first production smoke suite must cover these public flows:

| Flow | Required scenario coverage | Required artifacts |
| --- | --- | --- |
| Launch | public workbench launch to a stable route | `run.json`, `tree.json`, `accessibility.json`, screenshot |
| Navigation | select navigation item and update detail content | `interactions.json`, `tree.json`, screenshot |
| Primary command | invoke a command and assert status text/state | `interactions.json`, `component-evidence.json` |
| Form entry | focus input, type text, select combo item, assert result | `interactions.json`, `accessibility.json` |
| List selection | select a list row and assert selected item/detail | `interactions.json`, `tree.json` |
| Status/error display | show success, warning, error, loading, disabled states | `accessibility.json`, screenshot |
| Dialog or flyout | open, choose, dismiss, assert resulting state | `interactions.json`, screenshot |
| Artifact generation | preserve visual, accessibility, interaction, and component evidence | all versioned JSON artifacts plus PNGs |

## Required Public E2E Suite

The first production E2E suite must cover these clean-room workflows:

| Flow | Required public scenario | Components exercised |
| --- | --- | --- |
| Workbench navigation and selection | public workbench or production smoke workbench scenario | shell, navigation, list/detail, status, commands |
| Data-bound form edit | component text/forms or production smoke form scenario | text input, combo/check/radio state, validation, command |
| Command/menu invocation | commands/menus scenario with open menu states | command bar, app bar buttons, menu/flyout controls |
| List/detail update | collections or workbench scenario | `ItemsControl`, `ListView`, templates when implemented |
| State transition | status/pickers scenario | loading, warning, error, success, disabled |
| Dialog/flyout decision | dialogs/flyouts scenario | dialog, flyout, tooltip, teaching tip |
| Theme, scale, and resize | layout/media scenarios for light, dark, high contrast, and target viewports | resources, layout, text, state visibility |

## Private Content Safety Checklist

Run this checklist before every production-target commit:

- The diff uses generic component names and public fixture names only.
- No private product names, source paths, screenshots, customer data, secrets,
  tokens, endpoint names, or proprietary process text are present.
- Native reference artifacts come only from public clean-room fixture projects.
- `WindowsNativeProbe` artifacts remain labeled synthetic smoke evidence.
- Component visual grades are not promoted unless native provenance and reviewed
  macOS output support the promotion.
- Thresholds are not loosened to convert failed native comparisons into passing
  evidence.

## Current Status

This inventory completes the public target mapping for Milestone 0 of
`docs/plans/2026-06-01-production-windows-component-completion-plan.md`.
Milestone 1 state scenario coverage is tracked in
`docs/compatibility/winui-component-inventory.json` under
`productionStateCoverage`.
Production readiness remains blocked by renderer, interaction, accessibility,
fixture, native reference, smoke, E2E, operations, and release gates listed in
that plan.
