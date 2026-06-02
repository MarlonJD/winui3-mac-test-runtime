# Native Reference Source Audit

Date: 2026-06-02

Owner subtree: `docs/visual-parity`

## Scope

This audit reviews the checked-in native Windows reference artifacts under
`docs/visual-parity/examples/*/windows-reference.png` and their current
component crops. The current public reference set was captured by workflow run
`26777029415` on 2026-06-01.

The audit does not promote any row. It records source-quality risks that must be
resolved before `good` or `production-ready` native quality grades can be
applied.

## Summary

The native Windows full-window screenshots remain useful as scenario context,
but several component-level crops are not yet reliable enough for final manual
inspection. The current cropper uses the macOS/runtime layout region to crop
both the macOS image and the native Windows reference image. When native WinUI
layout and macOS renderer layout differ, the native crop can point at the wrong
row or a neighboring diagnostic.

This means native crop presence is not sufficient evidence of native crop
correctness. Final promotion requires either native element bounds from the
Windows reference capture pipeline or reviewed native-reference crop bounds that
are stored separately from macOS bounds.

## Scenario Findings

### `component-basic-input-light`

The native full-window screenshot contains visible native controls for the
public basic input subset: `Button`, `ToggleButton`, `CheckBox`, `RadioButton`,
`ComboBox`, `RepeatButton`, `HyperlinkButton`, `DropDownButton`, `SplitButton`,
`ToggleSplitButton`, `Slider`, `ToggleSwitch`, and `RatingControl`.

Current risk: several crops include both the diagnostic label and the control,
or are wider than the atomic control surface. They are acceptable as rough
context, but not as final component-quality inspection crops.

### `component-commands-menus-light`

The native full-window screenshot contains visible command surfaces for
`CommandBar`, `AppBarButton`, `CommandBar.Content`, `MenuFlyout`, `MenuBar`, and
the context menu target.

Current risks:

- Closed flyout/menu states are represented as static diagnostics rather than
  full open-popup evidence in this scenario.
- Some current native crops are too narrow or offset because they reuse runtime
  layout bounds.
- These rows need state-specific native references before interaction quality
  claims can be promoted.

### `component-layout-media-light`

The native full-window screenshot is not a complete native-quality reference
for every row in the scenario.

Rows with useful native context but partial diagnostic intent:

- `ScrollViewer`
- `Grid`
- `StackPanel`
- `Border`
- `FontIcon`
- `SymbolIcon`
- `XamlControlsResources`
- `ResourceDictionary.ThemeDictionaries`
- `ThemeResource`
- `StaticResource`
- `Style`
- `Setter`
- `Color`
- `SolidColorBrush`
- `CornerRadius`
- `Expander`
- `Annotated scrollbar`
- `SemanticZoom`
- `SplitView`
- `TwoPaneView`

Rows where the native reference itself is incomplete, unavailable, or only a
placeholder for final quality review:

- `Image`: the current native reference does not prove real image decode and
  stretch behavior; it is diagnostic context only.
- `AnimatedIcon`: the current reference does not show a production AnimatedIcon
  source/frame.
- `MediaPlayerElement`: the current reference is a placeholder surface, not
  media playback chrome evidence.
- `WebView2`: the current reference is a placeholder surface, not WebView2
  rendered content evidence.
- `InkCanvas / InkToolbar`: the current reference shows unavailable text for
  toolbar/canvas paths, so it cannot support a native-quality claim.
- `Title bar customization`: the row is effectively outside the visible crop
  area and does not prove title bar drag-region or custom chrome behavior.
- `Window.SystemBackdrop / MicaBackdrop`: the row is effectively outside the
  visible crop area and cannot prove system backdrop or Mica behavior.

Current risks:

- Several native crops point at neighboring rows because runtime bounds are used
  for the native image.
- The screenshot includes useful native context, but it is not a sufficient
  native source of truth for final grades on advanced media, web, ink, title
  bar, or backdrop rows.

### `public-admin-workbench-light`

The native full-window screenshot contains a visible native workbench scaffold:
`NavigationView`, `NavigationViewItem`, `Frame`, `TextBox`, `CommandBar`,
`AppBarButton`, `InfoBar`, `ListView`, and `Button`.

Current risks:

- Current crops are broad and sometimes include neighboring workbench regions.
- Command and button rows need tighter element crops before final inspection.
- This scenario can support workbench layout review, but it is not final
  component-level production evidence until native element bounds are captured.

## Required Follow-Up

1. Add separate native reference crop bounds or native element crop capture.
   The desired source is the Windows capture pipeline, ideally through FlaUI
   UIA3 element bounds and screenshots.
2. Keep macOS runtime bounds and native reference bounds separate in component
   evidence.
3. Regenerate `component-evidence.json`, `visual-review.json`, and the public
   review index after source-crop alignment changes.
4. Recapture or replace native references for rows whose Windows screenshot is
   currently placeholder, unavailable, or offscreen.
5. Do not promote any affected row above `usable` until native crop, macOS crop,
   diff crop, provenance, and manual inspection notes all point at the intended
   component.

## Release Gate Impact

This audit reinforces the existing hard rule: native Windows screenshots are the
visual source of truth, but source truth must include correct component-level
crop targeting. Current native crop presence does not by itself prove final
native-quality readiness.
