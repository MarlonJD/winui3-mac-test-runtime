# Full Windows UI Visual Readiness Plan

Date: 2026-06-02

Owner subtree: root `docs/plans`, `docs/compatibility`, `docs/release`,
`docs/visual-parity`, `src/WinUI3.MacCompat`, `src/WinUI3.MacRuntime`,
`src/WinUI3.MacXaml`, `src/WinUI3.MacRenderer.Skia`,
`src/WinUI3.MacRunner`, `fixtures`, `tests`, `.github/workflows`

## Goal

Make `winui3-mac-test-runtime` visually ready for source-level WinUI 3
application development by making the macOS runtime output look and behave like
native Windows WinUI for every claimed support tier.

The target user-facing result is: when a supported WinUI 3 source app or public
fixture is run through the macOS runtime, the visible shell, controls,
typography, spacing, command surfaces, dialogs, menus, navigation, status
states, themes, and accessibility states are recognizable as Windows 11 WinUI
and are backed by native Windows reference artifacts.

This is not a plan to execute Windows binaries, `.msix` packages, DirectX,
WebView2, or OS-only Windows integrations on macOS. It is a source-level visual
readiness plan with strict evidence gates.

## Current-State Audit

### Blocking

- Full Windows UI appearance is not ready for arbitrary WinUI 3 apps. The
  repository currently claims production readiness only for the documented
  public source-level subset.
- Level 6 Windows reference visual compatibility is still `partial`; native
  reference artifacts exist, but whole-screenshot parity does not yet cover the
  broad WinUI surface.
- Planned, Windows-only, not-supported, uncataloged, diagnostic-only, weak,
  poor, and `not-rendered` rows remain outside the production claim.

### Suggestion

- The newly added `docs/release/production-evidence-view.md` should become the
  running executive dashboard for visual readiness progress.
- Visual readiness should be advanced by tiered, reference-backed promotions
  instead of one large renderer rewrite.

### FYI

- Existing plans already cover production readiness, component completion,
  native reference capture, and pixel fidelity. This plan narrows the headline
  goal to "make it look like Windows UI" and sequences the work around Fluent
  visual fidelity and verification.

## Production Blocker Mapping

The final source-level production gate marks PB-000 through PB-012 closed for
the documented subset. Full Windows UI visual readiness reopens several of
those as broader-scope visual readiness work, while preserving the closed
production-subset claim.

| Blocker | Current gate status | Full Windows UI visual readiness treatment |
| --- | --- | --- |
| PB-000: native WinUI reference source of truth | Closed | Keep closed by requiring `referenceSource: native-winui` for every promoted visual scenario and keeping synthetic probe artifacts smoke-only. |
| PB-001: API/catalog coverage | Closed by production scope | Expand from "zero unknown production subset surfaces" to a docs/test gate for all newly claimed visual surfaces and keep unknowns diagnostic. |
| PB-002: component support boundary | Closed by production scope | Promote only components with catalog status, public fixture coverage, native reference, macOS evidence, interaction/accessibility evidence, and target grade. |
| PB-003: weak or absent visuals | Closed by production scope | Primary blocker for this plan: use component crops and grade thresholds so `weak`, `poor`, and `not-rendered` cannot be hidden by whole-screen passes. |
| PB-004: templates, visual states, theme dictionaries | Closed by production scope | Treat as phased work: state matrix and theme/token foundation first, then templates and broader visual states only when evidence-backed. |
| PB-005: materials and composition | Closed by production scope | Keep outside early readiness; later implement deterministic approximations for Mica/Acrylic/shadows only with explicit non-OS-composition wording. |
| PB-006: input/accessibility breadth | Closed by production scope | Require interaction and accessibility artifacts to match every promoted visual state, especially focus, selected, checked, disabled, expanded, and value states. |
| PB-007: public corpus coverage | Closed | Expand corpus only with clean-room public fixtures for newly claimed Windows UI families. |
| PB-008: native provenance and component evidence | Closed | Extend provenance and component evidence to component crops, state scenarios, and every promoted Ring 1/broader control. |
| PB-009: performance and flake | Closed for source-level gate | Add visual drift and crop comparison gates without making strict scenarios flaky; trend before release candidate. |
| PB-010: release packaging/provenance | Closed for source-level gate | Preserve package dry run and release-check; add visual readiness artifacts to release candidate evidence before publish. |
| PB-011: security and supply chain | Closed | Keep private-name scan and artifact privacy rules mandatory for every new fixture or checked-in visual artifact. |
| PB-012: support policy and triage | Closed | Update support policy and production evidence view whenever a status is promoted or excluded. |

## Catalog Status Plan

The current catalog snapshot is **126 entries**:

| Status | Count | Plan treatment |
| --- | ---: | --- |
| `supported` | 55 | Must never regress to `not-rendered`; for visual readiness, each claimed visual component needs component crop evidence, native provenance, interaction/accessibility evidence where applicable, and target grade. |
| `partial` | 35 | Must keep the exact supported subset documented; promote only the implemented subset and keep missing states/properties visible as known gaps or diagnostics. |
| `planned` | 31 | Do not silently render as supported. Each item needs either a later phase implementation path or an explicit diagnostic/non-production entry in the readiness dashboard. |
| `windows-only` | 3 | Keep excluded from macOS execution. Use Windows workflow evidence where relevant, but do not claim local macOS runtime support. |
| `not supported` | 2 | Keep explicit non-goals unless a future product decision changes the contract and adds full catalog, fixture, runtime, docs, and release evidence. |

Phase 1 must add an automated docs/test gate so README, compatibility matrix,
API catalog docs, production evidence view, and the JSON catalog cannot drift on
these counts.

## Assumptions And Open Questions

Assumptions:

- The runtime remains Wine-free, managed-first, and source-level.
- Native Windows screenshots continue to come from public
  `windows-native-screenshot.yml` runs on public clean-room fixture apps.
- Fluent visual parity can be tiered: production subset first, then broader
  public WinUI 3 controls, then material/motion approximations where feasible.
- Exact byte-for-byte screenshots are not a reasonable cross-platform target
  because font rasterization, GPU paths, and OS composition differ. The target is
  component-level visual correctness plus bounded whole-image diff metrics.
- WinUI 3 and Fluent 2 Windows guidance are the design source of truth for
  component shape, density, state, material, theme, keyboard, and accessibility
  behavior.

Open questions:

- Which downstream app surfaces are mandatory for the first "looks like
  Windows" milestone, and can they be represented by existing clean-room
  fixtures?
- Should Mica, Acrylic, shadows, transforms, and animation end states be
  considered strict requirements for the first full visual readiness claim, or
  documented as high-fidelity approximations?
- What release threshold should distinguish `usable`, `good`, and
  production-ready visual grades for component crops?

## Scope

- WinUI 3 visual shell and navigation: `Window`, title/content region,
  `NavigationView`, `Frame`, `Page`, list/detail workbench patterns.
- Fluent control chrome: buttons, toggles, checkboxes, radio buttons, text
  inputs, combo boxes, command bars, app bar buttons, menus, flyouts, dialogs,
  tooltips, status/progress controls, lists, scroll regions, icons, and image
  placeholders.
- Fluent states: default, hover, pressed, focused, disabled, selected, checked,
  expanded, loading, error, warning, success, empty, and validation states.
- Light, dark, and high contrast rendering through theme resources.
- Typography, spacing, corner radius, border/stroke, iconography, status color,
  and non-color-only state treatment.
- Component-region evidence, component crops, native Windows references,
  macOS runtime screenshots, pixel diffs, interaction artifacts, and
  accessibility artifacts.
- Compatibility docs, release evidence docs, support policy, and CI/release
  gates.

## Non-Goals

- Do not run Windows `.exe`, `.msix`, packaged Windows App SDK binaries, Wine,
  WebView2, DirectX, media playback, ink, or Windows OS-only integrations on
  macOS.
- Do not claim arbitrary WinUI 3 compatibility before the catalog, fixtures,
  renderer, interaction, accessibility, native reference, and docs are all
  updated.
- Do not promote components by loosening screenshot thresholds or hiding failed
  evidence.
- Do not copy private app content, private screenshots, secrets, proprietary
  workflow text, WinUI Gallery content, or private repository references into
  public fixtures.
- Do not implement speculative custom controls when built-in WinUI/Fluent
  semantics can be modeled through the existing facade and renderer structure.

## Target Desktop Information Architecture

The primary visual benchmark should be a Windows-style workbench app because it
exercises the most production-critical surface:

| Region | WinUI pattern | Visual requirement |
| --- | --- | --- |
| App shell | `Window`, `Page`, `Frame` | Native Windows density, page background, title/content separation, theme-aware surfaces. |
| Primary navigation | `NavigationView` | Pane sizing, selected item chrome, icons, text, footer, compact/wide behavior. |
| Command region | `CommandBar`, `AppBarButton`, `MenuFlyout`, `CommandBarFlyout` | Fluent command chrome, icon slots, labels, overflow/flyout open states, disabled and invoked states. |
| Queue/list | `ListView`, `ItemsControl` | Row height, selected state, hover/focus state, dividers or spacing, accessible list roles. |
| Detail panel | `Grid`, `StackPanel`, `Border`, text/forms/status controls | Windows spacing rhythm, readable hierarchy, status and form states. |
| Decision surface | `ContentDialog`, `Flyout`, `ToolTip` | Windows popup shape, elevation approximation, focus state, dismiss/invoke behavior. |

## Visual System Plan

- Typography: use Segoe UI Variable-compatible metrics where available and
  deterministic fallback metrics where unavailable. Document differences.
- Spacing: encode WinUI/Fluent spacing tokens used by component painters rather
  than ad hoc per-control constants.
- Surface and material: model layered surfaces, strokes, radius, and elevation
  before approximating Mica/Acrylic. Mica/Acrylic may start as documented
  deterministic approximations, not OS composition claims.
- Color and status: use theme resources and high-contrast-safe state indicators;
  no status may rely on color alone.
- Icons: use Segoe Fluent Icons/MDL2-compatible glyph mapping with explicit
  fallback diagnostics when a glyph cannot be rendered faithfully.
- Motion: deterministic visual evidence should capture stable end states first;
  animation timing and transitions should be separate evidence later.

## State Matrix

| State | Required evidence |
| --- | --- |
| Default | Component crop and whole-screen native reference comparison. |
| Hover | Pointer state scenario or explicit planned diagnostic if unsupported. |
| Pressed | Interaction scenario with rendered pressed/invoked state where applicable. |
| Focused | Visible focus rectangle or documented focus state approximation plus accessibility focus export. |
| Disabled | Non-color-only disabled affordance and enabled state in accessibility export. |
| Selected/checked | Visual selected/checked chrome plus semantic selected/checked state. |
| Loading | Progress visual, stable layout, and accessible progress/state text. |
| Empty | Stable empty state layout with accessible text. |
| Error/warning/success | `InfoBar` or validation surface with icon/text semantics, not color alone. |
| High contrast | Dedicated strict scenario proving visible text, borders, focus, and state markers. |

## Implementation Phases

### Phase 1: Evidence Contract And Visual Dashboard

Goal: make the target measurable before changing renderer behavior.

Steps:

- Promote `docs/release/production-evidence-view.md` into the visual readiness
  dashboard with per-family status, target grade, current grade, latest run ID,
  and next blocker.
- Add or update a machine-readable visual readiness inventory under
  `docs/compatibility` that maps every control family to required states,
  required scenarios, and target grades.
- Add a docs/test gate that verifies catalog counts in README, matrix,
  api-catalog, and production evidence view match
  `winui-api-compatibility.catalog.json`.
- Define promotion rules for `not-rendered` -> `usable` -> `good` ->
  production-ready.

Verification:

- `dotnet test --filter CompatibilityCatalog`
- `git diff --check`
- Manual review of `docs/release/production-evidence-view.md`

### Phase 2: Component Crop And Reference Tooling

Goal: stop relying on whole-screen screenshots to judge individual control
quality.

Steps:

- Extend visual artifacts to emit component crop metadata for each required
  `component-evidence.json` row.
- Emit cropped PNGs for native reference, macOS runtime, and pixel diff where
  target layout regions exist.
- Add per-component thresholds in addition to scenario thresholds.
- Fail strict visual scenarios when claimed supported/partial components have
  missing crop regions, blank crops, `not-rendered` grades, or crop diff metrics
  above target thresholds.

Verification:

- Unit tests for crop bounds and blank-crop detection.
- Strict runs for component basic input, commands/menus, layout/media,
  navigation/workbench, status/pickers, and dialogs/flyouts.
- Inspect at least one crop triplet per component family.

### Phase 3: Fluent Token And Theme Foundation

Goal: centralize Windows visual constants before expanding painters.

Steps:

- Add a renderer token layer for typography, spacing, corner radius, stroke,
  fill, status colors, focus indicators, disabled opacity, selected chrome,
  popup elevation, and theme resources.
- Route existing `skia-v2` painters through the token layer instead of
  hardcoded local values.
- Add light, dark, and high-contrast token tests.
- Keep unsupported Fluent resources diagnostic until mapped.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- Strict `component-layout-media-light`, `component-layout-media-dark`, and
  `component-layout-media-high-contrast` runs.
- Visual inspection of theme/resource artifacts.

### Phase 4: Ring 0 Windows Chrome Completion

Goal: make the current production smoke foundation look like Windows UI.

Steps:

- Upgrade painters and layout for app shell, core layout, text, basic commands,
  forms, workbench, status/progress, resources/theme, and artifacts.
- Add state scenarios for hover, pressed, focus, disabled, selected, checked,
  loading, error, success, empty, and high contrast where Ring 0 components use
  those states.
- Ensure interaction/accessibility artifacts agree with visual state.
- Promote only inspected component rows that meet the target grade and crop
  threshold.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- All Ring 0 component parity lab scenarios.
- `production-smoke-light`
- `production-e2e-workbench-light`
- `public-admin-workbench-light`
- `public-admin-workbench-deferred-light`
- Component crop audit with zero claimed Ring 0 `not-rendered` rows.

### Phase 5: Ring 1 E2E Visual Completion

Goal: make claimed E2E enabler surfaces Windows-like, while leaving unclaimed
controls honest.

Steps:

- Complete usable Windows-like visuals for claimed subsets of
  `MenuFlyout`, `CommandBarFlyout`, `ContentDialog`, `Flyout`, `ToolTip`,
  theme dictionaries, `SolidColorBrush`, `CornerRadius`, and collection hosts.
- Keep rich form input, templates, broader keyboarding, and advanced collection
  controls planned until real support is implemented.
- Add native reference scenarios for menu open, flyout open, dialog open,
  tooltip open, selected list/detail, and high-contrast resource states.

Verification:

- Strict commands/menus open-popup scenarios.
- Strict dialogs/flyouts open-popup scenarios.
- Strict collections selected scenario.
- Strict layout/media light/dark/high-contrast scenarios.
- Interaction and accessibility artifact tests for claimed Ring 1 flows.

### Phase 6: Broader WinUI Control Inventory

Goal: move beyond production subset toward full public WinUI visual readiness.

Steps:

- Prioritize controls from Microsoft Learn and local inventory:
  `AutoSuggestBox`, `PasswordBox`, `NumberBox`, `Slider`, `ToggleSwitch`,
  `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `MenuBar`,
  `TeachingTip`, `Expander`, `TabView`, `TreeView`, `GridView`,
  `CalendarView`, `DatePicker`, `TimePicker`, `ColorPicker`, `RatingControl`,
  and `PersonPicture`.
- For each control: add facade behavior, XAML ingestion, layout, painter,
  component evidence, native reference, interaction evidence where applicable,
  accessibility export, docs, and tests.
- Promote one family at a time through the same evidence gate.

Verification:

- All component parity lab scenarios.
- Corpus ingestion with zero unknown surfaces for newly claimed controls.
- Component evidence audit with no claimed `not-rendered`, `poor`, or `weak`
  rows.

### Phase 7: Materials, Motion, And High-Fidelity Polish

Goal: close the highest-visibility Windows UI differences.

Steps:

- Implement deterministic approximations for Mica, Acrylic, shadows,
  transforms, opacity, focus reveal, selected/hover/pressed chrome, and
  animation end states only where evidence and tests can prove behavior.
- Document every approximation that cannot be real Windows OS composition on
  macOS.
- Add reduced-motion and high-contrast verification.
- Add visual drift dashboards for component crops and whole-screen scenarios.

Verification:

- Native reference workflow for all claimed visual scenarios.
- macOS strict visual comparison workflow or documented local equivalent.
- Artifact provenance audit for all promoted material/motion claims.
- Release-check and production evidence view update.

### Phase 8: Release Candidate Gate

Goal: make "ready" a release decision, not a subjective screenshot review.

Steps:

- Add a release candidate visual gate that requires:
  - catalog/docs count consistency;
  - zero unknown production surfaces;
  - zero claimed `not-rendered`, `poor`, or `weak` rows;
  - native provenance for every claimed visual scenario;
  - crop and whole-screen thresholds inside target;
  - interaction/accessibility evidence for every interactive claim;
  - private-name scan;
  - package dry run and release-check.
- Update README, compatibility docs, support policy, final production gate, and
  production evidence view with the exact support boundary.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- full strict scenario sweep
- Windows native reference workflow
- visual artifact provenance audit
- package dry run
- `PATH="$PWD/tools:$PATH" winui3-mac-runner release-check --package-dir artifacts/packages`
- private-name denylist scan
- `git diff --check`

## Verification Gates

Every implementation phase must end with:

- targeted unit tests for changed runtime/compiler/renderer behavior;
- strict visual scenario runs for changed fixture families;
- component evidence review for every promoted visual grade;
- accessibility and interaction evidence review for interactive controls;
- docs updates in the same change as support claim changes;
- no private names, private screenshots, secrets, copied WinUI Gallery content,
  or proprietary fixture text;
- commit and push of only relevant files.

## Risks

- Full WinUI visual readiness is large; batching too much into one change will
  make regressions hard to review.
- Pixel thresholds can hide wrong UI if component crops are missing or too
  broad.
- Fonts, anti-aliasing, and hosted-runner image changes can create legitimate
  visual drift.
- Mica/Acrylic and compositor behavior cannot be truly native Windows OS
  composition on macOS; claims must stay precise.
- Broad keyboarding and screen reader behavior can lag visual polish unless
  enforced as promotion criteria.

## Rollback And Recovery

- Revert a component promotion by restoring its catalog/status/docs entry to
  `partial`, `planned`, or `not-rendered` and preserving the failed evidence.
- Keep older native reference workflow run IDs in release notes when runner
  image drift changes baselines.
- If renderer token changes cause broad regressions, roll back the token layer
  change before touching individual component painters.
- If a new strict gate is flaky, mark the promoted component blocked rather than
  loosening thresholds silently.

## Affected Files Or Docs

Expected areas:

- `README.md`
- `docs/release/production-evidence-view.md`
- `docs/release/final-production-gate.md`
- `docs/release/support-policy.md`
- `docs/compatibility/matrix.md`
- `docs/compatibility/component-support.md`
- `docs/compatibility/production-component-targets.md`
- `docs/compatibility/winui-component-inventory.json`
- `docs/compatibility/winui-api-compatibility.catalog.json`
- `docs/visual-parity/README.md`
- `docs/visual-parity/comparisons.md`
- `src/WinUI3.MacRenderer.Skia`
- `src/WinUI3.MacRuntime`
- `src/WinUI3.MacXaml`
- `src/WinUI3.MacRunner`
- `fixtures/ComponentParityLab.WinUI`
- `fixtures/ProductionSmoke.WinUI`
- `fixtures/PublicAdminWorkbench.WinUI`
- `fixtures/ResourceCatalogApp.WinUI`
- `tests`
- `.github/workflows`

## Execution Prompt

Use `$google-eng-practices` and `$windows-winui3-design`. Implement the saved
plan at `docs/plans/2026-06-02-full-windows-ui-visual-readiness-plan.md`.

Start with Phase 1 only unless I explicitly ask for a later phase: promote
`docs/release/production-evidence-view.md` into a running visual readiness
dashboard, add or update a machine-readable visual readiness inventory under
`docs/compatibility`, add a docs/test gate that verifies catalog counts in
README, matrix, api-catalog, and production evidence view match
`docs/compatibility/winui-api-compatibility.catalog.json`, include explicit
PB-000 through PB-012 visual readiness mapping, keep the catalog snapshot at
`126 = 55 supported / 35 partial / 31 planned / 3 windows-only / 2 not
supported`, and define promotion rules for `not-rendered` -> `usable` -> `good`
-> production-ready.

Keep the support claim precise: source-level WinUI 3 visual readiness, not
Windows binary or arbitrary full WinUI compatibility. Follow the repository
instructions: small self-contained changes, English comments/identifiers/docs,
no private names or screenshots, no threshold loosening to hide failures, and
commit only relevant files. Use author `marlonjd <burak.karahan@mail.ru>`.

Verification for Phase 1:

- `dotnet test --filter CompatibilityCatalog`
- `git diff --check`
- manual review of `docs/release/production-evidence-view.md`
- confirm the visual readiness inventory and docs count gate describe the same
  catalog totals as `winui-api-compatibility.catalog.json`

When complete, commit with a Conventional Commit message and push immediately.
