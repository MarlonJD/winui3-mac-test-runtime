# WinUI Native-Quality All Components Plan

Date: 2026-06-02

Owner subtree: `tools/winui3-mac-test-runtime`

## Goal

Move every cataloged visual component row from the current harness-grade state
to reference-backed WinUI-native-quality rendering on macOS.

The final outcome is not "usable enough for smoke tests." The final outcome is:

- zero `not-rendered`, `poor`, or `weak` component rows in the public component
  evidence set;
- every visual component row has native WinUI reference provenance, macOS
  runtime crop evidence, component-level diff evidence, and manual screenshot
  inspection notes;
- every promoted visual row is graded `good` or `production-ready`;
- docs, support policy, release evidence, and readiness inventory make no
  broader claim than the inspected evidence supports;
- `winui3-mac-runner release-candidate` has no local failures and no visual
  component quality blockers.

This plan intentionally includes the currently difficult rows such as
`WebView2`, `MediaPlayerElement`, Mica/system backdrop, title bar
customization, ink, templates, menus, popups, pickers, and collection controls.
If a row cannot be made visually WinUI-quality on macOS with the project
runtime, the plan is not complete; that row must either receive a real
platform-backed implementation or the product goal must be explicitly changed.

## Historical Baseline

This was the broad all-components baseline when the plan was written. The
current public-dashboard execution baseline is maintained in
`docs/plans/2026-06-02-public-component-native-quality-execution-plan.md` and
`docs/visual-parity/component-quality-dashboard.json`.

Original inspected local macOS evidence:

- 143 component evidence rows.
- 86 `usable` rows.
- 57 `not-rendered` rows.
- `usable` remains a harness grade, not a WinUI-native-quality grade.

Current `not-rendered` rows by family:

| Family | Rows | Components or patterns |
| --- | ---: | --- |
| Basic input | 8 | `RepeatButton`, `HyperlinkButton`, `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `Slider`, `ToggleSwitch`, `RatingControl` |
| Text and forms | 6 | `RichTextBlock`, `RichEditBox`, `PasswordBox`, `NumberBox`, `AutoSuggestBox`, `AutoSuggestBox.QueryIcon` |
| Collections and templates | 11 | `DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`, `ItemsView`, `GridView`, `FlipView`, `PipsPager`, `TreeView`, `ItemsRepeater`, `Swipe pattern`, `Pull-to-refresh pattern` |
| Commands and menus | 3 | `CommandBar.Content`, `MenuBar`, `Context menu pattern` |
| Dialogs and flyouts | 3 evidence rows | `TeachingTip`, `ToolTipService.SetToolTip`, `TeachingTip` open-state row |
| Navigation | 4 | `BreadcrumbBar`, `Pivot`, `SelectorBar`, `TabView` |
| Status, pickers, and people | 7 | `InfoBadge`, `PersonPicture`, `ColorPicker`, `CalendarDatePicker`, `CalendarView`, `DatePicker`, `TimePicker` |
| Layout, media, visuals, and platform | 15 | `SymbolIcon`, `XamlControlsResources`, `Color`, `Expander`, `Annotated scrollbar`, `SemanticZoom`, `SplitView`, `TwoPaneView`, `AnimatedIcon`, `Shapes`, `MediaPlayerElement`, `WebView2`, `InkCanvas / InkToolbar`, `Title bar customization`, `Window.SystemBackdrop / MicaBackdrop` |

Existing `usable` rows also need upgrade work. A `usable` row may pass harness
requirements while still having approximate typography, spacing, state visuals,
icons, popup placement, shadows, animation, template behavior, or layout
semantics. This plan treats those 86 rows as incomplete until component-level
native reference comparisons justify `good` or `production-ready`.

## Assumptions And Open Questions

- Native Windows reference screenshots remain the source of visual truth.
- macOS rendering should look like WinUI, not like native macOS AppKit or
  SwiftUI.
- Existing thresholds must not be loosened to make screenshots pass.
- API catalog expansion is allowed only when required to implement currently
  cataloged visual rows and their documented dependencies; broad unrelated API
  expansion is out of scope.
- The project may need a distinct `visualQuality` or `nativeQualityGrade`
  field so harness usability and native visual quality are not conflated.
- Exact Windows font fidelity may require a legal/product decision. If Segoe UI
  and Segoe Fluent Icons cannot be bundled or installed in CI, text crop
  thresholds and docs must clearly identify the supported font strategy.
- `WebView2`, media playback, Mica/Acrylic/system backdrop, title bar
  customization, and ink are not pure Skia painter tasks. They require either
  platform-backed host adapters, deterministic visual stand-ins explicitly
  accepted by product, or a changed product goal.

## Scope

In scope:

- Upgrade all current `usable` rows to WinUI-quality `good` or
  `production-ready` evidence.
- Implement all 57 current `not-rendered` rows to WinUI-quality evidence.
- Add source facades, XAML compiler support, runtime tree export, layout
  support, interaction support, accessibility export, renderer support, native
  reference capture, component crop comparison, docs, and tests needed for
  those rows.
- Build shared Fluent rendering infrastructure before expanding one-off
  painters.
- Preserve strict visual thresholds unless evidence justifies tightening them.
- Require native-reference screenshot inspection before updating claims.

Out of scope:

- Claiming arbitrary third-party WinUI app compatibility before public fixture
  evidence proves it.
- Marking a component `good` because a whole screenshot passes.
- Reclassifying `not supported` rows as complete without implementing their
  visual behavior.
- Loosening thresholds, deleting difficult rows, or moving hard controls out of
  scope to make counts look complete.
- Styling controls to look "modern" in a generic way instead of matching WinUI.

## Success Criteria

The project is done only when all of these are true:

1. `jq` over every public `component-evidence.json` returns zero rows with
   `visualGrade` equal to `not-rendered`, `poor`, or `weak`.
2. Every component row has:
   - catalog status that matches the implemented behavior;
   - target layout region;
   - macOS component crop;
   - native WinUI reference crop;
   - component diff metrics;
   - manual screenshot inspection notes;
   - accessibility evidence when applicable;
   - interaction evidence when applicable.
3. Every claimed visual row has `visualGrade` `good` or `production-ready`.
4. No planned, windows-only, or not-supported row remains in public visual
   evidence unless the product goal has been formally changed.
5. All state/theme variants required by the component inventory pass:
   default, hover, pressed, disabled, focused, checked, selected, expanded,
   loading, success, warning, error, invalid, light, dark, and high-contrast as
   applicable.
6. The full strict scenario sweep passes with `--renderer skia-v2`.
7. `winui3-mac-runner catalog-audit --check` passes.
8. `winui3-mac-runner release-candidate` passes with no visual quality blocker.
9. README, support policy, visual parity docs, component support docs,
   readiness inventory, and production evidence docs match the evidence.

## Architecture Workstreams

### Workstream A: Evidence And Quality Gates

Goal: make it impossible to claim WinUI-quality without reference-backed
component evidence.

Steps:

1. Add a component-quality dashboard generated from all public
   `component-evidence.json` files.
2. Split harness grade from native visual grade:
   - keep existing `visualGrade` semantics for compatibility if needed;
   - add `nativeQualityGrade` or equivalent if the current schema would confuse
     `usable` with WinUI-quality.
3. Add per-component inspection metadata:
   - inspected by;
   - inspected date;
   - native reference run ID;
   - comparison artifact paths;
   - accepted gaps;
   - reason for any non-pixel-perfect tolerance.
4. Add gates that fail when:
   - a claimed row has no reference crop;
   - a claimed row has no macOS crop;
   - a claimed row has no manual inspection metadata;
   - a claimed row has `not-rendered`, `poor`, or `weak`;
   - a planned/not-supported row appears in final target evidence;
   - docs report counts that differ from generated evidence.
5. Add a checked-in summary artifact under `docs/visual-parity/` that lists
   every row, current grade, target grade, owner family, required states,
   required scenarios, and remaining blocker.

Verification:

- Runtime tests for grade gating.
- Schema tests for inspection metadata.
- `catalog-audit --check`.
- Full strict scenario sweep.

### Workstream B: Native Reference Capture And Crop Comparison

Goal: make Windows native references complete and directly comparable for every
component row and every required state.

Steps:

1. Extend `windows-native-screenshot.yml` to capture every component parity
   scenario and every state scenario needed by the inventory.
2. Store reference provenance for:
   - fixture project path;
   - scenario path;
   - commit SHA;
   - GitHub Actions run ID;
   - runner image;
   - Windows App SDK version;
   - viewport;
   - scale;
   - theme;
   - capture mode.
3. Add component crop extraction for Windows references using the same target
   layout regions as the macOS runtime.
4. Add deterministic reference download/import tooling for local review.
5. Add per-family thresholds that are stricter than whole-image smoke
   thresholds and never looser than current component thresholds.
6. Add a review command:
   `winui3-mac-runner visual-review --scenario <path> --reference <dir>`.
7. Add generated HTML or Markdown visual review pages that place native,
   macOS, and diff crops side by side by component.

Verification:

- Native reference workflow completes for all scenarios.
- Every final row has native crop metadata.
- Pixel diff artifacts exist for every final claimed row.

### Workstream C: Fluent Rendering Foundation

Goal: replace one-off approximate painters with a shared Fluent visual system.

Steps:

1. Create `FluentThemeTokens` with light, dark, and high-contrast values:
   - fill colors;
   - text colors;
   - stroke colors;
   - accent colors;
   - control opacity states;
   - corner radii;
   - focus stroke values;
   - elevation and shadow tokens;
   - disabled values;
   - validation colors;
   - selection colors.
2. Create a typography layer:
   - body, caption, title, subtitle, icon, monospaced if needed;
   - text baseline helpers;
   - truncation and wrapping helpers;
   - font fallback strategy documented and tested.
3. Create reusable drawing primitives:
   - control border;
   - focus rectangle;
   - selection pill;
   - checkbox glyph;
   - radio glyph;
   - toggle switch track/thumb;
   - slider track/thumb/ticks;
   - progress track/indicator;
   - popup shadow and border;
   - command icon+label;
   - info/status icon;
   - calendar cell;
   - rating star;
   - expander chevron.
4. Create a visual state resolver:
   - normal;
   - pointer-over;
   - pressed;
   - disabled;
   - focused;
   - selected;
   - checked;
   - expanded;
   - invalid.
5. Create a popup placement engine:
   - anchor target;
   - placement mode;
   - viewport collision;
   - light-dismiss overlay;
   - z-order and crop region support.
6. Create a deterministic animation clock:
   - static review frame for loading controls;
   - documented frame timestamp;
   - no nondeterministic screenshot drift.
7. Create renderer tests that sample reusable primitives and state resolution.

Verification:

- Existing strict scenarios still pass.
- New primitive tests pass.
- No control painter duplicates state/color logic when a shared primitive
  exists.

### Workstream D: Layout, Template, And Resource Engine

Goal: provide the visual infrastructure required for WinUI-quality templates
and richer controls.

Steps:

1. Implement enough `ResourceDictionary.ThemeDictionaries`,
   `XamlControlsResources`, `ThemeResource`, and `StaticResource` behavior to
   resolve Fluent control resources used by public fixtures.
2. Implement typed resource conversion for:
   - `Color`;
   - `SolidColorBrush`;
   - `CornerRadius`;
   - `Thickness`;
   - typography values;
   - control-specific tokens.
3. Implement a constrained template system:
   - parse supported `Control.Template`;
   - instantiate supported template visual trees;
   - bind `TemplateBinding` equivalents for supported properties;
   - emit diagnostics for unsupported template constructs.
4. Implement `VisualStateManager.VisualStateGroups` for supported states:
   - state matching;
   - setters;
   - state-specific resources;
   - deterministic transitions or static final state.
5. Improve layout engine:
   - Grid rows and columns;
   - star sizing;
   - spanning;
   - alignment;
   - margin/padding;
   - clipping;
   - scroll viewport;
   - popup coordinates;
   - template content measurement.
6. Add tree export metadata for all layout and visual states needed by
   component crops.

Verification:

- XAML compiler tests for resources, templates, visual states, and typed values.
- Runtime layout tests.
- Component crop tests for template-generated visual regions.

## Component Family Implementation Plan

Each family follows the same pipeline:

1. Add or update public WinUI fixture XAML with real native controls.
2. Add default, focused, disabled, selected, checked, opened, invalid, loading,
   and theme scenarios as applicable.
3. Capture native Windows references.
4. Implement facade/source/XAML/compiler/runtime/layout/accessibility support.
5. Implement renderer with shared Fluent primitives.
6. Run local macOS strict scenarios.
7. Compare component crops.
8. Manually inspect native vs macOS screenshots.
9. Promote only rows that meet `good` or `production-ready`.
10. Update docs and inventory from generated evidence.

### Phase 1: Existing Harness Rows To WinUI Quality

Upgrade current `usable` rows before using them as foundations for planned
controls.

Families:

- Window, Page, Frame, NavigationView, NavigationViewItem.
- Grid, StackPanel, Border, ScrollViewer, ContentControl.
- TextBlock, TextBox, Button, ToggleButton, CheckBox, RadioButton, ComboBox.
- CommandBar, AppBarButton, CommandBarFlyout, MenuFlyout.
- ContentDialog, Flyout, ToolTip.
- ItemsControl, ListView.
- ProgressBar, ProgressRing, InfoBar.
- Image and FontIcon.
- Resource/theme rows already used by layout/media scenarios.

Required improvements:

- Match native spacing and sizing against references.
- Add focus, disabled, selected, checked, pressed, hover, and invalid visuals.
- Replace placeholder icons with Fluent glyph rendering or vector primitives.
- Improve popup/menu/dialog shadows, borders, placement, and overlay.
- Improve text metrics, wrapping, truncation, and baseline alignment.
- Add dark and high-contrast state coverage where missing.
- Add component-level crop thresholds for every promoted row.

Exit criteria:

- All 86 current `usable` rows are upgraded to `good` or `production-ready`.
- No row is promoted without native crop comparison and inspection notes.

### Phase 2: Basic Input Not-Rendered Rows

Rows:

- `RepeatButton`
- `HyperlinkButton`
- `DropDownButton`
- `SplitButton`
- `ToggleSplitButton`
- `Slider`
- `ToggleSwitch`
- `RatingControl`

Implementation details:

- Add facade classes and properties:
  - click/command support;
  - flyout/dropdown relationships;
  - split command and secondary command;
  - checked state for toggle split;
  - slider minimum/maximum/value/tick metadata;
  - toggle switch on/off state and content;
  - rating value, placeholder, max rating.
- Add XAML compiler element/property/event support.
- Add interaction scripts:
  - click repeat;
  - open dropdown;
  - invoke split secondary;
  - drag/set slider;
  - toggle switch;
  - set rating.
- Add accessibility roles and values.
- Add renderer primitives:
  - hyperlink text state;
  - dropdown chevron;
  - split divider;
  - slider track/thumb/tick;
  - toggle switch track/thumb;
  - rating stars.
- Add scenarios:
  - default;
  - hover/pressed if supported by deterministic interaction;
  - focused;
  - disabled;
  - opened;
  - checked/on;
  - value changed.

Exit criteria:

- Every basic input row is `good` or `production-ready`.
- Existing Button, ToggleButton, CheckBox, RadioButton, and ComboBox rows remain
  `good` or `production-ready`.

### Phase 3: Text And Forms Not-Rendered Rows

Rows:

- `RichTextBlock`
- `RichEditBox`
- `PasswordBox`
- `NumberBox`
- `AutoSuggestBox`
- `AutoSuggestBox.QueryIcon`

Implementation details:

- Add facade/source support for text runs, password masking, numeric value,
  increment/decrement buttons, suggestion lists, query icon, placeholder,
  validation, and disabled/focused states.
- Add XAML compiler support for the constrained public fixture subset.
- Add text layout primitives:
  - wrapping;
  - inline emphasis;
  - masked glyphs;
  - caret frame;
  - selection frame;
  - validation border/message.
- Add AutoSuggestBox popup placement using the shared popup engine.
- Add interaction scripts:
  - type text;
  - reveal password if fixture includes it;
  - increment/decrement number;
  - open suggestions;
  - choose suggestion.
- Add accessibility value/name/help metadata.

Exit criteria:

- All six text/form rows are `good` or `production-ready`.
- Existing TextBlock and TextBox rows are upgraded from approximate to
  WinUI-quality for required states.

### Phase 4: Collections, Templates, And Virtualized Controls

Rows:

- `DataTemplate`
- `ListView.ItemTemplate`
- `ItemsControl.ItemTemplate`
- `ItemsView`
- `GridView`
- `FlipView`
- `PipsPager`
- `TreeView`
- `ItemsRepeater`
- `Swipe pattern`
- `Pull-to-refresh pattern`

Implementation details:

- Build constrained DataTemplate parsing and instantiation.
- Add template-generated layout regions to `component-evidence.json`.
- Implement collection item containers:
  - default;
  - selected;
  - focused;
  - disabled;
  - group/header if needed by fixtures.
- Implement GridView tile layout.
- Implement FlipView viewport and navigation buttons.
- Implement PipsPager indicators.
- Implement TreeView indentation, expand/collapse glyph, selection, and nested
  row layout.
- Implement ItemsRepeater deterministic layout for public fixture data.
- Implement swipe action visual state and pull-to-refresh indicator as
  deterministic static review states.
- Add accessibility roles for list, grid, tree, pager, and item states.

Exit criteria:

- Template rows and all collection control rows are `good` or
  `production-ready`.
- Existing ItemsControl/ListView rows are upgraded to WinUI-quality and do not
  rely on text-only output.

### Phase 5: Commands And Menus

Rows:

- `CommandBar.Content`
- `MenuBar`
- `Context menu pattern`

Implementation details:

- Add CommandBar content slot source/runtime support.
- Implement MenuBar facade, XAML compiler support, menu hierarchy, open state,
  disabled state, checked item state, accelerator text, and separators.
- Implement context flyout semantics for supported controls.
- Use shared popup placement and shadow primitives.
- Add keyboard/accessibility metadata for menu, menu item, submenu, and
  context menu roles.
- Add scenarios:
  - menu closed;
  - menu open;
  - submenu open;
  - disabled item;
  - checked item;
  - context menu open;
  - command invoked.

Exit criteria:

- All command/menu rows are `good` or `production-ready`.
- Existing CommandBar, AppBarButton, CommandBarFlyout, and MenuFlyout rows are
  upgraded to WinUI-quality.

### Phase 6: Dialogs, Flyouts, Tooltips, And Teaching UI

Rows:

- `TeachingTip`
- `ToolTipService.SetToolTip`
- `TeachingTip` open-state evidence row

Implementation details:

- Implement TeachingTip facade/source support:
  - title;
  - subtitle;
  - content;
  - icon;
  - action button;
  - close button;
  - placement;
  - light-dismiss state.
- Implement ToolTipService attached property support and target relationship.
- Add popup anchor metadata to the runtime tree.
- Add overlay, z-order, focus trap, and dismissal evidence for dialogs/flyouts.
- Add deterministic scenarios for open, anchored, dismissed, focused, and
  disabled target states.

Exit criteria:

- TeachingTip and ToolTipService rows are `good` or `production-ready`.
- Existing ContentDialog, Flyout, and ToolTip rows are upgraded to
  WinUI-quality.

### Phase 7: Navigation Controls

Rows:

- `BreadcrumbBar`
- `Pivot`
- `SelectorBar`
- `TabView`

Implementation details:

- Add facade/source/compiler support for items, selected item/index, icons,
  close buttons, overflow, and navigation events as needed by fixtures.
- Implement renderer primitives:
  - breadcrumb separator;
  - selected pivot underline;
  - selector item pill;
  - tab item shape;
  - close glyph;
  - overflow button.
- Add scenarios:
  - default;
  - selected;
  - focused;
  - overflow;
  - disabled item;
  - close tab;
  - navigation invoked.
- Add accessibility roles and selected/focused metadata.

Exit criteria:

- All navigation rows are `good` or `production-ready`.
- Existing NavigationView and NavigationViewItem rows are upgraded to
  WinUI-quality across pane states and selected/focused states.

### Phase 8: Status, Pickers, And People

Rows:

- `InfoBadge`
- `PersonPicture`
- `ColorPicker`
- `CalendarDatePicker`
- `CalendarView`
- `DatePicker`
- `TimePicker`

Implementation details:

- Add facade/source/compiler support for:
  - badge value/status;
  - initials/avatar image;
  - color channels and swatches;
  - date/time values;
  - calendar grid;
  - picker flyouts;
  - selected/today/blackout dates if used by fixtures.
- Implement renderer primitives:
  - badge shape;
  - avatar circle/image/initials;
  - color spectrum or constrained swatch panel;
  - calendar header;
  - calendar day cell;
  - picker flyout list/spinner;
  - selected date/time state.
- Add interaction scripts:
  - open picker;
  - select date;
  - select time;
  - select color;
  - clear selection if supported.
- Add accessibility values for dates, times, colors, and person picture names.

Exit criteria:

- All status/picker/person rows are `good` or `production-ready`.
- Existing InfoBar, ProgressBar, and ProgressRing rows are upgraded to
  WinUI-quality for required static states.

### Phase 9: Layout, Media, Visuals, And Platform Rows

Rows:

- `SymbolIcon`
- `XamlControlsResources`
- `Color`
- `Expander`
- `Annotated scrollbar`
- `SemanticZoom`
- `SplitView`
- `TwoPaneView`
- `AnimatedIcon`
- `Shapes`
- `MediaPlayerElement`
- `WebView2`
- `InkCanvas / InkToolbar`
- `Title bar customization`
- `Window.SystemBackdrop / MicaBackdrop`

Implementation details:

- Implement SymbolIcon using the same Fluent icon strategy as FontIcon.
- Implement enough `XamlControlsResources` to make supported controls resolve
  native-like tokens rather than local approximations.
- Implement typed `Color` conversion through resources and XAML.
- Implement Expander header, chevron, content, expanded/collapsed states.
- Implement annotated scrollbar visual markers and scroll thumb states.
- Implement SemanticZoom zoomed-in/zoomed-out static states.
- Implement SplitView pane modes, overlay, compact width, and open/closed
  states.
- Implement TwoPaneView layout modes and hinge/fold simulation metadata.
- Implement AnimatedIcon deterministic review frames.
- Implement Shapes for the fixture subset:
  - Rectangle;
  - Ellipse;
  - Line;
  - Path if needed;
  - fill/stroke/thickness.
- Implement platform-backed rows:
  - MediaPlayerElement: deterministic media surface poster frame, controls,
    play/pause state, and reference evidence; real playback if product
    requires it.
  - WebView2: platform-backed web surface or deterministic captured web
    content surface with browser chrome/state evidence; document any engine
    difference.
  - InkCanvas/InkToolbar: stroke collection, deterministic ink rendering,
    toolbar selected tool, eraser/highlighter states.
  - Title bar customization: runtime/title bar metadata, custom title bar
    region, drag region visual evidence, and native reference comparison.
  - Window.SystemBackdrop/MicaBackdrop: material approximation or platform
    material bridge with explicit visual reference comparison. Do not claim
    real Windows compositor behavior unless implemented.

Exit criteria:

- All layout/media/platform rows are `good` or `production-ready`.
- Product signs off on any platform-backed approximation where exact Windows
  OS behavior is impossible on macOS.

## Documentation And Claim Updates

After each family reaches exit criteria:

1. Regenerate local artifacts.
2. Import native references.
3. Inspect screenshots.
4. Update generated quality dashboard.
5. Update:
   - `README.md`;
   - `docs/compatibility/component-support.md`;
   - `docs/compatibility/production-component-targets.md`;
   - `docs/compatibility/visual-readiness-inventory.json`;
   - `docs/release/production-evidence-view.md`;
   - `docs/release/final-production-gate.md`;
   - `docs/visual-parity/README.md`;
   - `docs/visual-parity/comparisons.md`.
6. Promote grade only after evidence and docs agree.

## Verification Gates

Run continuously during the project:

- `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj`
- `dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj`
- `dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj`
- targeted `winui3-mac-runner run --renderer skia-v2 --strict-visual` for each
  changed scenario
- full component parity strict scenario sweep
- full public app corpus strict scenario sweep
- `winui3-mac-runner catalog-audit --check`
- `winui3-mac-runner release-candidate`
- native Windows reference workflow for every promoted scenario
- component crop diff review for every promoted row
- manual screenshot inspection for every promoted row

Final-only gates:

- zero `not-rendered` component rows;
- zero `weak` or `poor` component rows;
- all final visual rows are `good` or `production-ready`;
- no missing native reference crop;
- no missing macOS crop;
- no missing inspection notes;
- no docs/evidence count drift;
- no threshold loosening introduced by the project.

## Risks

- Exact WinUI visual fidelity may require licensed fonts or icon assets.
- Platform rows may require substantial native host work outside `skia-v2`.
- Template and VisualState support can grow quickly; keep the fixture subset
  explicit and test-backed.
- Pixel thresholds can be too coarse for component quality. Prefer tightening
  component thresholds after visual fidelity improves.
- Existing `usable` rows can regress while planned rows are implemented.
  Keep family-level regression sweeps mandatory.
- Native Windows references may drift with Windows runner image, Windows App
  SDK version, or font rendering changes. Record provenance for every capture.

## Rollback Or Recovery

- Revert a family implementation if it worsens existing `good` rows.
- Keep evidence/schema/gating improvements even if a painter family needs
  rollback.
- If a platform-backed control cannot match WinUI closely enough, keep it out
  of promotion and document the blocker; do not mark it complete.
- If native references are unavailable, keep rows unpromoted.
- If thresholds fail, fix renderer/layout/state behavior; do not loosen
  thresholds to pass.

## Affected Files Or Areas

- `src/WinUI3.MacCompat/**`
- `src/WinUI3.MacRuntime/**`
- `src/WinUI3.MacXaml/**`
- `src/WinUI3.MacRenderer.Skia/**`
- `src/WinUI3.MacRunner/**`
- `src/Shared/**`
- `tests/WinUI3.MacRuntime.Tests/**`
- `tests/WinUI3.MacXaml.Tests/**`
- `fixtures/ComponentParityLab.WinUI/**`
- `fixtures/PublicAdminWorkbench.WinUI/**`
- `fixtures/ProductionSmoke.WinUI/**`
- `fixtures/ResourceCatalogApp.WinUI/**`
- `docs/compatibility/**`
- `docs/visual-parity/**`
- `docs/release/**`
- `.github/workflows/windows-native-screenshot.yml` if present in the owning
  repository

## Recommended Execution Order

1. Evidence/schema/dashboard gates.
2. Native reference crop import tooling.
3. Fluent rendering foundation.
4. Layout/resource/template foundation.
5. Existing 86 `usable` rows to `good`.
6. Basic input 8 rows.
7. Text/forms 6 rows.
8. Commands/menus 3 rows.
9. Dialogs/flyouts 3 rows.
10. Navigation 4 rows.
11. Status/pickers/people 7 rows.
12. Collections/templates 11 rows.
13. Layout/media/platform 15 rows.
14. Full strict scenario sweep.
15. Final docs and release evidence update.

This order gets visible control quality high early while leaving the hardest
platform-backed rows until the renderer, template, popup, and evidence systems
are strong enough to support them.

## Execution Prompt

Use `$google-eng-practices` and implement
`docs/plans/2026-06-02-winui-native-quality-all-components-plan.md`.
The goal is not to make rows merely `usable`; every cataloged visual component
row must reach WinUI-native-quality `good` or `production-ready` evidence.
Work family by family, starting with evidence/schema/dashboard gates, native
reference crop tooling, Fluent rendering primitives, and layout/resource/template
foundation before promoting component rows. Do not loosen thresholds, do not
delete difficult rows from scope, and do not update claims until macOS
screenshots have been inspected against native WinUI references. Verify each
phase with the relevant targeted
`winui3-mac-runner run --renderer skia-v2 --strict-visual` commands,
`dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj`,
`dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj`,
`winui3-mac-runner catalog-audit --check`, and
`winui3-mac-runner release-candidate`. The final completion criteria are zero
`not-rendered`, `weak`, or `poor` visual component rows, all final rows graded
`good` or `production-ready`, native reference crop evidence for every row,
manual inspection notes for every promoted row, and docs that exactly match the
generated evidence. Commit only relevant files with author
`marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message, then
push immediately.
