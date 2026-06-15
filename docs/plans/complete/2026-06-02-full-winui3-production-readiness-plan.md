# Full WinUI 3 Production Readiness Plan

Date: 2026-06-02

Owner subtree: root `docs/plans`, `src/WinUI3.MacCompat`,
`src/WinUI3.MacRuntime`, `src/WinUI3.MacXaml`,
`src/WinUI3.MacRenderer.Skia`, `src/WinUI3.MacRunner`, `fixtures`,
`tests`, `.github/workflows`

## Goal

Move `winui3-mac-test-runtime` from a production-subset compatibility harness
to a production-ready, source-level WinUI 3 compatibility runtime with broad
public WinUI 3 app coverage, deterministic diagnostics, native Windows
reference evidence, and release gates strong enough for external consumers.

This plan intentionally targets the full public WinUI 3 source-level surface,
not an EMSI-specific subset.

## Production Definition

The project is production-ready when all of these are true:

- common public WinUI 3 source projects build through the compatibility shadow
  pipeline without private patches;
- supported APIs, XAML constructs, controls, resources, templates, layout,
  binding, input, accessibility, and visual states have tests and compatibility
  matrix entries;
- unsupported APIs are explicitly cataloged and fail with actionable
  diagnostics, not silent partial behavior;
- every claimed visual behavior has native WinUI Windows reference evidence and
  macOS comparison artifacts;
- CI gates track correctness, visual parity, accessibility, performance,
  flake rate, package provenance, and release readiness;
- public documentation states exact support, unsupported behavior, upgrade
  policy, and troubleshooting guidance.

## Assumptions

- The target is source-level WinUI 3 compatibility. The runtime does not execute
  arbitrary Windows `.exe`, `.msix`, native COM, WebView2, DirectX, or OS-only
  integration as real Windows components on macOS.
- The runtime remains Wine-free and managed-first on macOS.
- Native Windows reference screenshots come from public GitHub Actions
  `windows-latest` runs against public fixture apps.
- Compatibility can be tiered, but production claims must be backed by tests and
  evidence for each tier.

## Non-Goals

- Do not claim Windows binary compatibility.
- Do not claim pixel-perfect parity for operating-system-only rendering unless
  native reference evidence and tolerances prove it.
- Do not use private product names, private screenshots, private repositories,
  or secrets as production evidence.
- Do not mark unsupported WinUI 3 APIs as supported to improve headline
  coverage.

## Plan Shape

Execute this as seven large sprints. Each sprint closes one production blocker
class and must end with a commit, push, and verification summary. A sprint may
contain many small commits only if needed to keep CI stable, but the sprint is
not complete until its exit criteria pass.

## Sprint 1: Public WinUI 3 Compatibility Corpus

Goal: replace fixture-only confidence with a broad public source corpus.

Scope:

- Add a curated public WinUI 3 app corpus with varied project shapes:
  single-window, navigation shell, MVVM forms, data grids/lists, settings,
  resource-heavy, command surfaces, dialogs/flyouts, theme switching, and
  packaging-like layouts.
- Add clean-room fixtures for any important WinUI 3 pattern that lacks a public
  source app.
- Generate a full API, XAML construct, resource, asset, and project-ingestion
  inventory from the corpus.
- Update `docs/compatibility` with tiered support status for every discovered
  public surface.

Exit criteria:

- Corpus ingestion runs in CI.
- Unknown API report is deterministic and tracked.
- Every discovered surface is classified as supported, partial, unsupported, or
  planned with owner and exit criteria.
- No production claim depends on private app evidence.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- corpus ingestion command for every public app
- `git diff --check`
- private-name denylist scan

## Sprint 2: XAML, Resources, Templates, And Styling

Goal: make realistic WinUI 3 XAML load and behave without one-off fixture hacks.

Scope:

- Expand XAML support for namespaces, property elements, attached properties,
  collections, markup extensions, `x:Bind` diagnostics, `StaticResource`,
  `ThemeResource`, merged dictionaries, and theme dictionaries.
- Implement the production subset of `Style`, `Setter`, `ControlTemplate`,
  `DataTemplate`, `ItemTemplate`, visual states, and common template bindings.
- Export precise resource, template, style, and binding failures.
- Add light, dark, and high-contrast coverage.

Exit criteria:

- Corpus XAML either loads or fails with explicit diagnostics.
- Claimed template/resource behavior has unit tests and fixture scenarios.
- No supported control depends on undocumented style shortcuts.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- strict XAML/resource fixture runs
- theme scenario runs for light, dark, and high contrast
- `git diff --check`

## Sprint 3: Full Control And Layout Parity Pass

Goal: move the public WinUI 3 control inventory from diagnostic coverage to
usable runtime behavior.

Scope:

- Implement or explicitly exclude the Microsoft Learn WinUI 3 control families:
  basic input, text, collections, navigation, commands, menus, dialogs, flyouts,
  status, pickers, layout, media, icons, and workbench shells.
- Add deterministic layout measurement, arrangement, clipping, scrolling,
  virtualization approximations, disabled/hover/pressed/focused/selected
  states, and validation states.
- Add component-region evidence for every claimed supported control.
- Keep unsupported controls as first-class diagnostics with docs and tests.

Exit criteria:

- No claimed supported control is `not-rendered`.
- No claimed supported control is below `usable` component grade.
- Whole-image comparisons cannot hide component-level failures.
- Compatibility matrix and component support docs match runtime behavior.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- all component parity lab scenarios
- all corpus component scenarios
- component evidence audit with zero unexpected `not-rendered`
- `git diff --check`

## Sprint 4: Runtime Behavior, Binding, Input, And Accessibility

Goal: support production E2E automation, not just static screenshots.

Scope:

- Implement production-grade focus, keyboard, pointer, text entry, selection,
  scrolling, command invocation, menu/flyout/dialog interaction, navigation,
  and wait-for-idle behavior.
- Implement `INotifyPropertyChanged`, observable collection updates, command
  enabled state, validation state, and two-way binding for supported controls.
- Expand accessibility export for role, name, value, enabled, focusable,
  focused, checked, selected, expanded, help text, and automation IDs.
- Add stable scenario actions and assertions for E2E tests.

Exit criteria:

- E2E scenarios can find elements semantically rather than by screenshot only.
- Interaction failures include action, selector, target, and observed state.
- Accessibility artifacts are test-covered and documented.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- full interaction scenario suite
- accessibility artifact snapshot tests
- public corpus E2E smoke runs
- `git diff --check`

## Sprint 5: Visual, Fluent, Materials, And Native Reference Parity

Goal: make visual output production-reviewable against native WinUI evidence.

Scope:

- Expand native Windows reference capture to every public corpus and component
  scenario.
- Improve Skia renderer output for Fluent chrome, typography, spacing, borders,
  corner radius, iconography, focus visuals, shadows, transforms, opacity,
  theme tokens, Mica/Acrylic/system backdrop approximations, and animation end
  states where deterministic.
- Add reference drift review tools, component crops, diff heatmaps, and
  scenario-specific thresholds.
- Document every non-pixel-perfect approximation.

Exit criteria:

- Every claimed supported visual scenario has native Windows reference
  provenance.
- Visual failures are classified as renderer gap, unsupported API,
  environmental drift, threshold drift, or test bug.
- PB-000 remains closed and synthetic probe artifacts remain smoke-only.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- Windows native reference workflow
- macOS strict visual comparison workflow
- artifact provenance audit for all production scenarios
- `git diff --check`

## Sprint 6: Reliability, Performance, Security, And Release Hardening

Goal: make the runtime operationally safe for external production consumers.

Scope:

- Add performance benchmarks for project ingestion, XAML compile, runtime
  startup, render, interaction, artifact generation, and memory use.
- Add flake tracking, retry policy, artifact retention policy, timeout policy,
  and CI failure triage.
- Document threat model, safe CI usage, dependency policy, artifact privacy,
  license review, signing/provenance, and supply-chain expectations.
- Add release packaging gates for tools, SDK packages, changelog, semver,
  rollback, and migration notes.

Exit criteria:

- CI exposes performance and flake metrics.
- Release checklist blocks publishing when provenance, security, or package
  gates fail.
- Consumer docs explain safe usage and known risks.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- benchmark suite
- package build/signing/provenance dry run
- release checklist dry run
- `git diff --check`

## Sprint 7: Final Production Gate And Public Claim Update

Goal: close or explicitly scope every production blocker and update the public
support claim.

Scope:

- Re-run every gate from Sprints 1-6 on a clean checkout.
- Audit PB-001 through PB-012 and close only blockers whose exit criteria are
  objectively met.
- Freeze the production compatibility contract.
- Update README, compatibility matrix, component support docs, visual parity
  docs, release readiness docs, troubleshooting, and consumer quick start.
- Publish final artifact provenance summary with workflow IDs, commit SHAs,
  scenario counts, unsupported APIs, weak components, performance numbers, and
  residual risks.

Exit criteria:

- The repo can truthfully state production-ready source-level WinUI 3
  compatibility for the documented public surface.
- Any excluded WinUI 3 feature is listed as unsupported with diagnostics.
- No blocker remains open without a documented production-scope exclusion.
- Clean checkout verification passes.

Verification:

- `dotnet build --verbosity minimal`
- `dotnet test --verbosity minimal`
- full corpus ingestion
- full production E2E suite
- full native Windows reference workflow
- full strict visual comparison workflow
- benchmark and flake gates
- release checklist dry run
- private-name denylist scan
- `git diff --check`

## Risks

- Full WinUI 3 contains OS-bound APIs that cannot be faithfully implemented on
  macOS without explicit exclusions.
- Visual parity may require approximations for fonts, materials, shadows,
  composition, and animation.
- Public corpus apps can change upstream; fixtures should pin revisions or keep
  clean-room equivalents.
- Over-broad claims can damage trust. Prefer precise compatibility tiers over
  marketing language.

## Rollback And Recovery

- Keep each sprint independently revertible.
- Do not delete existing alpha/subset gates while adding production gates.
- If a corpus app becomes unstable, pin it, replace it with a clean-room
  fixture, and keep the failure documented.
- If a visual threshold is loosened, require an artifact review note explaining
  why the tolerance still protects users.

## Affected Files And Areas

- `src/WinUI3.MacCompat`
- `src/WinUI3.MacRuntime`
- `src/WinUI3.MacXaml`
- `src/WinUI3.MacRenderer.Skia`
- `src/WinUI3.MacRunner`
- `fixtures`
- `tests`
- `docs/compatibility`
- `docs/visual-parity`
- `docs/release`
- `.github/workflows`
- `README.md`

## Execution Prompt

Use `$google-eng-practices` and execute
`docs/plans/2026-06-02-full-winui3-production-readiness-plan.md` sprint by
sprint until the full source-level WinUI 3 production readiness gate is met.
Preserve the repository instructions: do not use private product evidence, keep
official docs in English, commit only relevant files at the end of each
completed sprint with author `marlonjd <burak.karahan@mail.ru>` using a
Conventional Commit message, and push immediately. Run the verification commands
listed in each sprint before committing, including `dotnet build --verbosity
minimal`, `dotnet test --verbosity minimal`, `git diff --check`, native Windows
reference workflow checks where required, strict visual comparisons, benchmark
or release gates where required, and the operator private-name denylist scan.
Final handoff must include closed and remaining production blockers, workflow
run IDs, artifact provenance, component grades, unsupported WinUI 3 exclusions,
performance and flake results, release/security gate status, final commit SHA,
and residual risks.
