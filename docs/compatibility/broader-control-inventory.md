# Broader WinUI Control Inventory

Date: 2026-06-02

This is the Phase 7 ("Broader WinUI control inventory") promotion plan. It moves
beyond the documented production subset toward full public WinUI 3 visual
readiness by enumerating the prioritized broader controls, their target visual
family, required states, and the exact evidence each must produce before it can
be promoted out of diagnostic status.

This page is a roadmap, not a local macOS support claim. Every control below is
currently a **planned, not-rendered** diagnostic row in
`docs/compatibility/winui-component-inventory.json` (`entries`). The machine-
readable promotion plan is the `broaderControlInventory` section of the same
file.

## Promotion Gate

A control is promoted one family at a time through the same evidence gate used
for Ring 0 and Ring 1:

> facade behavior, XAML ingestion, layout, painter, component evidence, native
> WinUI reference, interaction evidence where applicable, accessibility export,
> docs, and tests, with no claimed `not-rendered`, `poor`, or `weak` rows.

The `BroaderControlInventoryTracksPrioritizedControlsHonestly` test enforces
honesty: a control may claim a rendered grade only when its tracking row carries
the matching catalog status, visual evidence, and interaction coverage.
Otherwise it must stay `not-rendered`.

## Prioritized Controls

| Priority | Control | Target family | Interaction | Required states |
| ---: | --- | --- | :---: | --- |
| 1 | `ToggleSwitch` | text-forms | yes | default, on, off, disabled, focused |
| 2 | `Slider` | basic-input | yes | default, pressed, focused, disabled |
| 3 | `PasswordBox` | text-forms | yes | default, focused, reveal, disabled |
| 4 | `NumberBox` | text-forms | yes | default, focused, spin, invalid, disabled |
| 5 | `AutoSuggestBox` | text-forms | yes | default, focused, suggestions-open, disabled |
| 6 | `DropDownButton` | commands-menus | yes | default, open-popup, disabled, focused |
| 7 | `SplitButton` | commands-menus | yes | default, invoked, open-popup, disabled |
| 8 | `ToggleSplitButton` | commands-menus | yes | default, checked, open-popup, disabled |
| 9 | `MenuBar` | commands-menus | yes | default, open-popup, invoked, disabled |
| 10 | `Expander` | layout-media | yes | collapsed, expanded, focused, disabled |
| 11 | `TabView` | navigation-workbench | yes | default, selected, hover, add-close |
| 12 | `TreeView` | navigation-workbench | yes | default, selected, expanded, focused |
| 13 | `GridView` | navigation-workbench | yes | default, selected, focused, empty |
| 14 | `TeachingTip` | dialogs-flyouts | yes | default, open-popup, dismissed |
| 15 | `RatingControl` | status-pickers | yes | default, hover, set, disabled |
| 16 | `PersonPicture` | status-pickers | no | initials, image, group |
| 17 | `CalendarView` | status-pickers | yes | default, selected, focused, disabled |
| 18 | `DatePicker` | status-pickers | yes | default, open-popup, selected, disabled |
| 19 | `TimePicker` | status-pickers | yes | default, open-popup, selected, disabled |
| 20 | `ColorPicker` | status-pickers | yes | default, focused, disabled |

## Relationship To The Catalog

These controls are intentionally tracked outside the 126-entry production
catalog. The catalog records the production scope; this inventory records the
broader roadmap. When a control passes the promotion gate it gains a catalog
entry (raising the catalog total) plus fixtures, native references, and
component evidence, and its row here moves from `not-rendered` to `usable` or
better. Per-control promotion requires CI-captured native WinUI references, so
it is delivered incrementally rather than as a single change.
