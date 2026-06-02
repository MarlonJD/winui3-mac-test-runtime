# Public Component Native-Quality Execution Plan

Date: 2026-06-02

Owner subtree: `tools/winui3-mac-test-runtime`

Parent plan:
`docs/plans/2026-06-02-winui-native-quality-all-components-plan.md`

## Goal

Convert the broad WinUI native-quality all-components plan into small,
verifiable implementation phases for the current public component-quality
dashboard without weakening the original goal.

The target remains strict:

- zero public dashboard blocker rows;
- zero final `not-rendered`, `poor`, `weak`, or `usable` visual rows;
- every public component row has native WinUI reference crop evidence, macOS
  crop evidence, component diff metrics, native reference provenance, and
  manual inspection metadata;
- every promoted interactive row has UI automation evidence that can drive the
  row by stable automation identity and capture screenshots for quality review;
- FlaUI 5.0 + FlaUI.UIA3 API-driven automation is treated as a production
  target, not an optional convenience, for the supported public subset;
- every public component row ends with `visualGrade` and `nativeQualityGrade`
  equal to `good` or `production-ready`;
- no threshold is loosened, no difficult row is deleted, and no claim is
  promoted before side-by-side native Windows, macOS, and diff crops are
  manually inspected.

## Starting Public Baseline

The checked-in public dashboard at the start of this execution plan was
`docs/visual-parity/component-quality-dashboard.json`.

Totals:

| Metric | Current value |
| --- | ---: |
| Scenarios | 4 |
| Component rows | 58 |
| `usable` rows | 32 |
| `not-rendered` rows | 26 |
| `nativeQualityGrade: not-evaluated` rows | 58 |
| Missing macOS runtime crops | 0 |
| Missing native reference crops | 0 |
| Missing native reference provenance | 0 |
| Missing component diffs | 0 |
| Missing inspection notes | 58 |
| Blocking rows | 58 |

Current progress after the first basic-input evidence pass:

| Metric | Current value |
| --- | ---: |
| Scenarios | 4 |
| Component rows | 58 |
| `usable` rows | 40 |
| `not-rendered` rows | 18 |
| `nativeQualityGrade: not-evaluated` rows | 58 |
| Missing macOS runtime crops | 0 |
| Missing native reference crops | 0 |
| Missing native reference provenance | 0 |
| Missing component diffs | 0 |
| Missing inspection notes | 58 |
| Blocking rows | 58 |

`component-basic-input-light` now has 13 rendered `usable` harness rows and
zero `not-rendered` rows. Those rows remain blockers because `usable` is not a
native-quality final grade, `nativeQualityGrade` is still `not-evaluated`, and
manual screenshot inspection metadata has not been applied.

Scenario breakdown:

| Scenario | Rows | Current blocker rows |
| --- | ---: | ---: |
| `component-basic-input-light` | 13 | 13 |
| `component-commands-menus-light` | 8 | 8 |
| `component-layout-media-light` | 28 | 28 |
| `public-admin-workbench-light` | 9 | 9 |

The evidence infrastructure is ahead of the original parent-plan baseline: all
public rows currently have crop, diff, and provenance artifacts. The remaining
work is real rendering quality, manual inspection, and grade promotion.

## Assumptions And Open Questions

- Native Windows screenshots remain the visual source of truth.
- The macOS runtime must render WinUI visual language, not macOS-native visual
  language.
- A row cannot be promoted from `usable` to `good` or `production-ready` from
  metrics alone; manual inspection metadata is required.
- Existing strict visual thresholds remain unchanged unless they are tightened.
- `WebView2`, `MediaPlayerElement`, title bar customization, Mica/system
  backdrop, and ink are platform-backed or policy-backed work, not simple Skia
  painter work.
- If a row currently marked `not supported` cannot be implemented, the product
  goal must be formally changed before the plan can complete. The row must not
  be silently removed from public evidence.
- Exact Windows font and icon fidelity may still require a documented font
  strategy. This plan does not treat font mismatch as acceptable without
  explicit inspection notes and accepted-gap rationale.
- FlaUI 5.0 + FlaUI.UIA3 normally talks to Windows UI Automation. The macOS
  runtime does not currently expose a native UIA provider, so FlaUI support
  must be implemented and documented as either native Windows FlaUI validation,
  a repo-owned FlaUI 5.0 UIA3-compatible adapter over macOS runtime artifacts,
  or both. The project must not claim broad FlaUI support until API-level tests
  prove the supported mode.

## Scope

In scope:

- Public dashboard rows in `docs/visual-parity/examples/*/component-evidence.json`.
- Shared Fluent rendering primitives, layout foundations, resource/template
  foundations, and platform adapters required by those public rows.
- Scenario fixture updates required to expose states, placement, and component
  regions for inspection.
- Visual review, inspection template, quality dashboard, catalog audit, and
  release evidence synchronization.
- Manual inspection metadata for each promoted public row.
- A supported UI automation contract for the public subset, including stable
  automation IDs, roles/control types, bounding rectangles, state/value export,
  action dispatch, screenshot capture, and FlaUI 5.0 + FlaUI.UIA3 API-level
  smoke tests or a documented compatible adapter surface.

Out of scope:

- Broad third-party WinUI app compatibility claims beyond the public evidence.
- Deleting difficult rows, lowering thresholds, or reclassifying unsupported
  rows to pass gates.
- Generic "modern" styling that does not match native WinUI references.
- Promoting rows based only on whole-screenshot pass/fail status.
- Large unrelated API expansion not needed by current public dashboard rows.
- Claiming arbitrary FlaUI or UIA coverage beyond the tested supported subset.
- Treating JSON accessibility export alone as equivalent to FlaUI automation.

## Phase Gates

Every implementation phase must end with these checks:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

Each component-family phase must also run the targeted strict visual command
for the affected scenario:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/<scenario>.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/<scenario>
```

If a check fails because remaining public dashboard blockers still exist, keep
the failure recorded as expected for that phase. Do not weaken the dashboard
gate to make the release candidate pass early.

## Implementation Phases

### Phase 0: Freeze The Dashboard Contract

Goal: make the current public target explicit before changing renderers.

Steps:

1. Regenerate and verify the public dashboard and visual review index.
2. Confirm every public row has native crop, macOS crop, diff metrics, and
   provenance.
3. Keep inspection templates generated for all four public scenarios.
4. Record the current 58-row baseline in docs without changing thresholds.

Exit criteria:

- The dashboard still reports 58 public rows and 58 blockers.
- The only missing evidence class is manual inspection and final native grade.
- No row is removed or recategorized to reduce blocker count.

### Phase 1: Shared Fluent Foundations

Goal: prevent one-off painters from diverging while families are promoted.

Steps:

1. Add or consolidate shared Fluent token primitives for fill, stroke, text,
   accent, disabled, focus, selection, corner radius, and elevation.
2. Add shared typography and baseline helpers for WinUI-like text layout.
3. Add shared primitives for borders, focus rectangles, glyphs, icons, popup
   shadows, command icons, slider/toggle/rating parts, and list selection.
4. Add a visual-state resolver for default, hover, pressed, disabled, focused,
   selected, checked, expanded, and invalid states.
5. Add focused unit tests around token selection and primitive geometry where
   deterministic.

Exit criteria:

- Existing public visual output does not regress unexpectedly.
- New component-family work can call shared primitives instead of duplicating
  painter logic.

### Phase 2: Basic Input Rendering Completion

Goal: move `component-basic-input-light` from 5 usable and 8 not-rendered rows
to 13 rendered rows with WinUI-quality evidence.

Rows:

- Promote after inspection: `Button`, `ToggleButton`, `CheckBox`,
  `RadioButton`, `ComboBox`.
- Implement from `not-rendered`: `RepeatButton`, `HyperlinkButton`,
  `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `Slider`,
  `ToggleSwitch`, `RatingControl`.

Steps:

1. Add missing source facades, XAML parsing, runtime node export, and catalog
   status updates for the 8 not-rendered controls.
2. Render the controls using the shared Fluent primitives from Phase 1.
3. Implement layout regions and target metadata for all 13 rows.
4. Run strict visual comparison against native Windows reference crops.
5. Inspect the triptych review page row by row.
6. Apply inspection metadata only for rows whose native/macOS/diff crops
   justify `good` or `production-ready`.

Exit criteria:

- `component-basic-input-light` has zero `not-rendered` rows.
- Each promoted row has crop, diff, provenance, and inspection metadata.
- Any row left below `good` remains a blocker with a specific rendering issue.

### Phase 3: Commands And Menus Rendering Completion

Goal: complete command, icon, flyout, menubar, and context-menu quality without
papering over popup and placement differences.

Rows:

- Promote after inspection: `CommandBar`, `AppBarButton`,
  `AppBarButton.Icon`, `CommandBarFlyout`, `MenuFlyout`.
- Implement from `not-rendered`: `CommandBar.Content`, `MenuBar`,
  `Context menu pattern`.

Steps:

1. Refine command sizing, icon alignment, overflow affordances, and content
   layout.
2. Implement popup placement, menu border, menu shadow, item spacing, keyboard
   accelerator text, and disabled/hover visual states where represented.
3. Add or update runtime/XAML support for `MenuBar` and context menu patterns.
4. Run strict visual comparison for `component-commands-menus-light`.
5. Inspect the triptych review page and apply final grades only with notes.

Exit criteria:

- `component-commands-menus-light` has zero `not-rendered` rows.
- Popup and menu rows are promoted only when placement, shape, shadow, spacing,
  and text/icon alignment match the native references closely enough for the
  stated grade.

### Phase 4: Layout, Resources, Icons, And Brushes

Goal: promote layout and resource rows that are foundational for later public
workbench quality.

Rows:

- Promote after inspection: `ScrollViewer`, `Grid`, `StackPanel`, `Border`,
  `FontIcon`, `Image`, `ResourceDictionary.ThemeDictionaries`,
  `ThemeResource`, `StaticResource`, `Style`, `Setter`, `SolidColorBrush`,
  `CornerRadius`.
- Implement from `not-rendered`: `SymbolIcon`, `XamlControlsResources`,
  `Color`.

Steps:

1. Ensure layout measurement and arrangement match the native reference regions.
2. Complete resource resolution for theme dictionaries and shared XAML control
   resources used by public rows.
3. Add deterministic icon rendering for `FontIcon` and `SymbolIcon`.
4. Validate color and brush rendering against native references.
5. Inspect and promote only rows with reliable per-component diff evidence.

Exit criteria:

- Foundational layout/resource rows are no longer blockers.
- Resource behavior is covered by focused XAML/runtime tests.

### Phase 5: Layout Containers And Adaptive Surfaces

Goal: implement container controls whose visual quality depends on composition
and child layout, not just simple primitive painting.

Rows:

- Implement from `not-rendered`: `Expander`, `Annotated scrollbar`,
  `SemanticZoom`, `SplitView`, `TwoPaneView`.

Steps:

1. Add the minimal runtime and renderer support required for each public row.
2. Implement collapsed/expanded, pane, two-column, zoom, and annotated-scroll
   visuals using shared tokens and layout primitives.
3. Keep interaction behavior scoped to what the public evidence requires.
4. Run strict visual comparison and inspect each row.

Exit criteria:

- These rows are either promoted with inspection metadata or remain blockers
  with specific missing behavior called out.

### Phase 6: Shapes, Animated Icons, And Ink

Goal: complete drawing-heavy rows with deterministic WinUI-like visuals.

Rows:

- Implement from `not-rendered`: `AnimatedIcon`, `Shapes`,
  `InkCanvas / InkToolbar`.

Steps:

1. Add shape primitives needed by the public row: line, rectangle, ellipse,
   path, stroke, fill, and corner behavior as represented in the fixture.
2. Implement the public `AnimatedIcon` state as a deterministic captured frame
   or supported animation state, with documented inspection notes.
3. Implement ink canvas and toolbar visuals for the public fixture, including
   stroke preview and toolbar affordances.
4. Run strict visual comparison and inspect against native references.

Exit criteria:

- Drawing-heavy rows have deterministic crops and final grades.
- Accepted gaps, if any, are explicitly recorded in manual inspection metadata.

### Phase 7: Platform-Backed Visual Rows

Goal: handle rows that cannot honestly be completed as pure Skia approximations.

Rows:

- `MediaPlayerElement`
- `WebView2`
- `Title bar customization`
- `Window.SystemBackdrop / MicaBackdrop`

Steps:

1. For each row, decide whether the correct implementation is a macOS host
   adapter, a deterministic public-fixture stand-in, or a formal product-goal
   change.
2. Implement real platform-backed behavior where feasible:
   - media surface placeholder and controls for `MediaPlayerElement`;
   - web surface host or accepted deterministic capture for `WebView2`;
   - title bar metrics and non-client visual simulation for title bar rows;
   - backdrop/material approximation only if explicitly accepted by product.
3. Preserve native reference comparison and manual inspection requirements.
4. Do not reclassify these rows to pass the gate without evidence.

Exit criteria:

- Each platform-backed row is promoted with evidence or remains an explicit
  blocker tied to a product/architecture decision.

### Phase 8: Public Admin Workbench Promotion

Goal: promote the integrated public app-like scenario after its constituent
component primitives are stable.

Rows:

- `NavigationView`, `NavigationViewItem`, `Frame`, `TextBox`, `CommandBar`,
  `AppBarButton`, `InfoBar`, `ListView`, `Button`.

Steps:

1. Re-run strict visual comparison for `public-admin-workbench-light` after
   basic input, commands, and layout primitives are complete.
2. Fix integrated spacing, list selection, navigation item visuals, command bar
   alignment, and InfoBar presentation.
3. Inspect row crops and whole-scenario context.
4. Apply final grades only when the integrated view matches the native WinUI
   reference at component level.

Exit criteria:

- `public-admin-workbench-light` has zero blockers.
- Integrated rows do not mask component-level failures.

### Phase 9: FlaUI 5.0 UIA3 Automation And Screenshot Contract

Goal: make UI automation and screenshot capture a first-class production
quality gate instead of a side artifact.

Steps:

1. Define the supported FlaUI 5.0 + FlaUI.UIA3 automation mode for this runtime:
   - native Windows FlaUI validation for real WinUI reference runs;
   - macOS runner adapter with FlaUI 5.0 UIA3-compatible concepts over
     `tree.json`, `accessibility.json`, `interactions.json`, and screenshot
     artifacts;
   - or both, with separate support claims.
2. Add a stable automation element contract for the public subset:
   - automation ID;
   - name;
   - control type or role;
   - bounding rectangle;
   - enabled/focusable/focused state;
   - checked/selected/expanded state;
   - value/range value where applicable;
   - parent/child relationships.
3. Add action dispatch required by public E2E flows:
   - invoke/click;
   - focus;
   - text entry;
   - toggle;
   - selection;
   - expand/collapse;
   - scroll/range changes where applicable.
4. Add screenshot capture APIs for:
   - full window;
   - scenario viewport;
   - element crop by automation ID;
   - before/after action capture.
5. Add API-level automation smoke tests that exercise the intended consumer
   shape. If direct FlaUI 5.0 + FlaUI.UIA3 cannot run on macOS, add a
   repo-owned adapter with FlaUI 5.0 UIA3-compatible naming and semantics, and
   keep the docs explicit about that boundary.
6. Wire automation and screenshot evidence into release docs and final gates so
   visual quality can be inspected from automation-driven states, not just
   static initial screenshots.

Exit criteria:

- A consumer can write supported-subset UI automation against stable automation
  identities and capture screenshots for quality review.
- The support claim clearly states whether the tested surface is real
  FlaUI 5.0 + FlaUI.UIA3, a FlaUI 5.0 UIA3-compatible adapter, or Windows-only
  FlaUI validation.
- Interactive rows promoted to `production-ready` include automation evidence
  for their meaningful actions and states.
- Docs do not equate accessibility JSON export with full UIA/FlaUI support.

### Phase 10: State, Theme, And Accessibility Evidence

Goal: prevent default-light-only evidence from being mistaken for production
quality.

Steps:

1. Add state scenarios for hover, pressed, disabled, focused, checked,
   selected, expanded, invalid, and loading where applicable.
2. Add dark and high-contrast coverage for rows whose WinUI visual contract
   depends on theme resources.
3. Add accessibility export evidence for rows with meaningful control roles,
   names, states, selection, or invoke/toggle/range behavior.
4. Keep public dashboard promotion tied to the required states for each row.

Exit criteria:

- Rows promoted to `production-ready` have state/theme/accessibility evidence
  appropriate to their control contract.
- Rows with only default-state evidence may be `good` only when the inspection
  notes justify that narrower claim.

### Phase 11: Manual Inspection And Grade Application Sweep

Goal: convert technical evidence into final dashboard state without overstating
quality.

Steps:

1. Open `docs/visual-parity/public-visual-review-index.html`.
2. Inspect each row's native, macOS, and diff crop side by side.
3. Update the relevant `component-inspection-template.json` row with:
   - final `visualGrade`;
   - final `nativeQualityGrade`;
   - reviewer;
   - inspection date;
   - native reference run ID;
   - accepted gaps;
   - notes explaining why the grade is justified.
4. Apply inspections through the checked tooling rather than hand-editing
   evidence rows.
5. Regenerate dashboard and review index after every batch.

Exit criteria:

- No public row has missing inspection metadata.
- No public row remains `nativeQualityGrade: not-evaluated`.
- No row is promoted without a note that ties the grade to visible evidence.

### Phase 12: Documentation Synchronization

Goal: make public claims match generated evidence exactly.

Steps:

1. Regenerate `docs/visual-parity/component-quality-dashboard.json`.
2. Regenerate `docs/visual-parity/public-visual-review-index.json` and `.html`.
3. Update public visual parity docs, support policy, release evidence,
   readiness inventory, and final production gate docs to match generated
   counts and remaining blockers.
4. Keep English docs canonical.

Exit criteria:

- Docs report the same counts as the generated dashboard.
- No doc claims production visual readiness for a row still blocked by the
  dashboard.

### Phase 13: Final Release-Candidate Closure

Goal: close the public dashboard and release gate without local failures.

Steps:

1. Run the full phase-gate verification set.
2. Run targeted strict visuals for all four public scenarios.
3. Run `winui3-mac-runner release-candidate`.
4. Investigate any local failure as a real release blocker unless it is
   demonstrably external workflow/package evidence outside the local runtime.
5. Commit and push only relevant files after each completed phase.

Exit criteria:

- `docs/visual-parity/component-quality-dashboard.json` has zero blocker rows.
- Public component evidence has zero `not-rendered`, `weak`, `poor`, or
  `usable` final visual rows.
- Every final row has crop, diff, provenance, native-quality grade, and manual
  inspection notes.
- Every promoted interactive row has automation evidence and screenshot capture
  coverage for the supported automation contract.
- `release-candidate` passes locally, except truly external workflow/package
  checks when documented as external.

## Suggested Phase Order

Use this order unless current visual evidence shows a lower-risk swap:

1. Phase 0: Freeze the dashboard contract.
2. Phase 1: Shared Fluent foundations.
3. Phase 2: Basic input.
4. Phase 3: Commands and menus.
5. Phase 4: Layout, resources, icons, and brushes.
6. Phase 5: Layout containers and adaptive surfaces.
7. Phase 6: Shapes, animated icons, and ink.
8. Phase 7: Platform-backed visual rows.
9. Phase 8: Public admin workbench.
10. Phase 9: FlaUI 5.0 UIA3 automation and screenshot contract.
11. Phase 10: State, theme, and accessibility evidence.
12. Phase 11: Manual inspection and grade application sweep.
13. Phase 12: Documentation synchronization.
14. Phase 13: Final release-candidate closure.

## Verification Commands

Required after each phase:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

Targeted strict visual commands:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/component-basic-input-light
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-commands-menus-light.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/component-commands-menus-light
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/component-layout-media-light
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/public-admin-workbench-light.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/public-admin-workbench-light
```

Generation commands:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard
PATH="$PWD/tools:$PATH" winui3-mac-runner visual-review-index
PATH="$PWD/tools:$PATH" winui3-mac-runner component-inspection-template --evidence docs/visual-parity/examples/component-basic-input-light/component-evidence.json
PATH="$PWD/tools:$PATH" winui3-mac-runner component-inspection-template --evidence docs/visual-parity/examples/component-commands-menus-light/component-evidence.json
PATH="$PWD/tools:$PATH" winui3-mac-runner component-inspection-template --evidence docs/visual-parity/examples/component-layout-media-light/component-evidence.json
PATH="$PWD/tools:$PATH" winui3-mac-runner component-inspection-template --evidence docs/visual-parity/examples/public-admin-workbench-light/component-evidence.json
```

## Risks And Recovery

- Risk: `usable` rows may look acceptable at whole-screenshot level while
  failing component crop inspection.
  Recovery: keep them blocked until row-level native/macOS/diff triptychs
  justify the final grade.
- Risk: platform-backed rows cannot be made honest with pure Skia rendering.
  Recovery: implement host adapters, document a deterministic public-fixture
  stand-in only with product acceptance, or keep the row blocked.
- Risk: shared primitive changes regress already-rendered rows.
  Recovery: run targeted scenario comparisons after every primitive change and
  revert only the relevant new change if it caused the regression.
- Risk: manual inspection becomes detached from generated evidence.
  Recovery: always regenerate dashboard and visual review index after applying
  inspections, then run `--check`.
- Risk: docs drift from generated counts.
  Recovery: treat docs synchronization as a release gate, not cleanup.

## Rollback Notes

- Keep each phase in a small commit so renderer regressions can be reverted
  without losing unrelated evidence tooling.
- Do not revert user changes or unrelated worktree changes.
- If a renderer foundation change causes broad regressions, revert the
  foundation commit and reintroduce it behind narrower helper calls with tests.
- If an inspection batch over-promotes rows, revert only the affected evidence
  and inspection-template changes, then restore the blocker status.

## Affected Files And Areas

Expected implementation areas:

- `src/WinUI3.MacRuntime/`
- `src/WinUI3.MacRunner/`
- `src/WinUI3.MacCompat/`
- `tests/WinUI3.MacRuntime.Tests/`
- `tests/WinUI3.MacXaml.Tests/`
- future automation adapter or package tests for FlaUI 5.0 UIA3-compatible UI
  automation, if added by the implementation phase
- `fixtures/ComponentParityLab.WinUI/`
- `docs/visual-parity/`
- `docs/release/`
- `docs/plans/`

## Execution Prompt

Use `$google-eng-practices` and continue implementing
`docs/plans/2026-06-02-public-component-native-quality-execution-plan.md`
without loosening the parent goal in
`docs/plans/2026-06-02-winui-native-quality-all-components-plan.md`.

Goal: drive `docs/visual-parity/component-quality-dashboard.json` to zero
public blocker rows while keeping every promoted row backed by native Windows
reference crops, macOS crops, component diff metrics, native reference
provenance, manual inspection metadata, and UI automation/screenshot evidence
for interactive rows.

Rules:

- Do not loosen thresholds.
- Do not delete difficult rows.
- Do not promote a row before manually inspecting the native/macOS/diff crops.
- Do not mark `usable` as final quality.
- Promote only to `good` or `production-ready` when the evidence supports it.
- Build shared Fluent rendering primitives before one-off component painters.
- Treat FlaUI 5.0 + FlaUI.UIA3 API-driven UI automation and screenshot capture
  as a core production target for the supported public subset.
- Do not claim full FlaUI/UIA support until API-level tests prove the documented
  support mode.
- Keep docs synchronized with generated evidence.

Work in phase order:

1. Freeze the current public dashboard contract.
2. Build shared Fluent rendering foundations.
3. Complete `component-basic-input-light`.
4. Complete `component-commands-menus-light`.
5. Complete layout, resources, icons, and brushes in
   `component-layout-media-light`.
6. Complete layout containers and adaptive surfaces.
7. Complete shapes, animated icons, and ink.
8. Resolve platform-backed rows: `MediaPlayerElement`, `WebView2`, title bar,
   and Mica/system backdrop.
9. Promote `public-admin-workbench-light`.
10. Add the FlaUI 5.0 UIA3 automation and screenshot contract.
11. Add state, theme, and accessibility evidence.
12. Apply manual inspection metadata and final grades.
13. Synchronize docs.
14. Close `release-candidate`.

After each phase, run:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

Also run the relevant targeted strict visual command for the affected scenario:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/<scenario>.json --strict-visual --reference <native-reference.png> --diff-output docs/visual-parity/examples/<scenario>
```

When repository files change, commit only relevant files and push immediately.
Use a Conventional Commit message and author exactly
`marlonjd <burak.karahan@mail.ru>`.
