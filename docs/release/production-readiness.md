# Production Readiness Assessment

Date: 2026-06-01

Scope: `winui3-mac-test-runtime` as a public, Wine-free macOS runtime for
source-level WinUI 3 compatibility testing.

## Verdict

The library is **production-ready for the documented public source-level WinUI 3
subset**, but **not production-ready for arbitrary WinUI 3 application
development**.

The important shift is that the project now has public harness evidence instead
of only intent, and every remaining broad WinUI 3 gap is either excluded from
the production claim or documented as a roadmap item:

- Local build, test, corpus ingestion, benchmark/flake, package dry run,
  release-check, private-name scan, runner, and Windows capture gates pass.
- Public `windows-native-screenshot.yml` now produces native WinUI Windows
  references for the public admin workbench, component parity lab, production
  smoke, and corpus fixtures, with `referenceSource: native-winui` provenance.
- `ComponentParityLab.WinUI` produces `component-evidence.json`, so component
  quality is graded independently from whole-screenshot pass/fail status.
- The support policy limits production support to the public clean-room
  source-level subset; unsupported APIs, templates, materials, compositor
  behavior, media, WebView2, Windows binaries, and arbitrary app compatibility
  remain excluded.
- Public docs include final gate evidence, security posture, release gates,
  artifact provenance, known exclusions, and triage policy.

This closes the native WinUI reference source-of-truth blocker for the public
fixture set: `WindowsNativeProbe` remains only as labeled synthetic smoke
evidence. Broader visual fidelity and API coverage are roadmap items outside
the current production support boundary.

## What Is Done

### Runtime And Tooling

- Wine-free managed macOS host for facade-backed .NET assemblies.
- `winui3-mac-doctor` and `winui3-mac-runner` source wrappers.
- Packaged runner and compatibility package contracts for the alpha release set.
- Versioned runtime artifacts:
  - `run.json`
  - `tree.json`
  - `accessibility.json`
  - `binding-failures.json`
  - `resource-failures.json`
  - `unsupported-apis.json`
  - `project-ingestion.json`
  - `diagnostics.sarif`
  - `visual-run.json`
  - `component-evidence.json`

### Source-Level WinUI Fixture Support

- Public Windows-targeted WinUI source fixture ingestion through compat shadow
  builds without mutating the source project.
- `UseWinUI`, Windows-targeted TFM, Windows App SDK package references, WinUI
  XAML files, and common project item types are detected and reported.
- Unsupported project features are surfaced through structured diagnostics
  instead of silently passing.

### XAML, Runtime, And Interaction Subset

- Core app types and XAML roots: `Application`, `Window`, `Page`, `Frame`.
- Core XAML constructs: `x:Class`, `x:Name`, `x:Uid`, text content, event
  hookup, basic resource lookup, simple style setter application, and binding
  subset.
- Public control subset for fixture validation: layout containers, text/input
  controls, navigation, command surfaces, list/items controls, progress/status
  controls, icons, and images.
- Scripted interactions for click, focus, text entry, item selection, property
  assertions, navigation selection, and command invocation.
- Deterministic accessibility export for the supported logical tree subset.

### Visual Evidence

- `skia-v2` strict visual scenarios for shell, interactions, control gallery,
  public admin/workbench, and component parity lab categories.
- Public native WinUI Windows reference workflow:
  `windows-native-screenshot.yml`.
- Checked-in public visual examples under `docs/visual-parity/examples/`.
- Component parity evidence is separated from whole-image pixel metrics.

### Component Parity Lab

- Public `ComponentParityLab.WinUI` fixture with eight clean-room pages:
  - Basic input
  - Text and forms
  - Collections
  - Dialogs and flyouts
  - Commands and menus
  - Navigation and workbench
  - Status and pickers
  - Layout, media, and resources
- Component inventory metadata in
  `docs/compatibility/winui-component-inventory.json`.
- Sanitized production-ring target mapping in
  `docs/compatibility/production-component-targets.md`.
- Scenario requirements with expected catalog status, presence, interaction
  status, minimum visual grade, known gaps, and optional diff metrics.
- Downstream source-audit gaps are explicitly represented:
  `SymbolIcon`, `XamlControlsResources`,
  `ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
  `Style`, `Setter`, `Color`, `SolidColorBrush`, `CornerRadius`,
  `DataTemplate`, `ListView.ItemTemplate`, `ItemsControl.ItemTemplate`,
  `CommandBar.Content`, `AppBarButton.Icon`, `AutoSuggestBox.QueryIcon`,
  `NavigationView.MenuItems`, `NavigationView.PaneFooter`,
  `ToolTipService.SetToolTip`, and `Window.SystemBackdrop / MicaBackdrop`.

## Evidence Available

Latest inspected public native WinUI visual workflow:

- Workflow: `windows-native-screenshot.yml`
- Run: `26791576394`
- Code commit inspected by that workflow: `cd814a4`
- Reference source: `native-winui`
- Viewport: `1028x720`
- Capture mode: client area.
- Native reference provenance exists for `public-admin-workbench-light`, all
  light Ring 0 and Ring 1 `ComponentParityLab.WinUI` scenarios,
  `production-smoke-light`, `production-e2e-workbench-light`, and the
  inspected component examples checked into `docs/visual-parity/examples`, and
  the current corpus native reference scenarios.
- Synthetic probe smoke artifacts are still produced separately for
  `shell-light`, `interactions-light`, and `control-gallery-light`, but they are
  not parity examples.

Latest inspected native comparison status for checked-in historical examples:

| Scenario | Status | Changed pixels | MAE | RMS | Component evidence |
| --- | --- | ---: | ---: | ---: | --- |
| `public-admin-workbench-light` | failed | 100.00% / threshold 45% | 9.72 | 35.87 | n/a |
| `component-basic-input-light` | historical failed example | 42.07% / threshold 18% | 9.92 | 38.84 | Superseded by current component evidence for the production subset. |
| `component-commands-menus-light` | historical failed example | 40.68% / threshold 24% | 8.45 | 35.23 | Superseded by current component evidence for the production subset. |
| `component-layout-media-light` | historical failed example | 45.83% / threshold 24% | 10.48 | 39.27 | Superseded by current component evidence for the production subset. |

Current production component status is sourced from fresh
`component-evidence.json` artifacts and the
`ClaimedSupportedComponentsAreNeverNotRendered` test. Supported and partial
production-ring components require at least `usable` evidence; planned,
unsupported, Windows-only, diagnostic-only, weak, poor, or `not-rendered` rows
remain outside the production claim.

Local verification gates run for the component parity foundation:

- `dotnet build`
- `dotnet test`
- `PATH="$PWD/tools:$PATH" winui3-mac-doctor`
- `winui3-mac-runner` Tiny fixture smoke, default renderer.
- `winui3-mac-runner` Tiny fixture smoke, `--renderer skia`.
- `skia-v2` strict visual scenarios for existing fixture categories.
- All eight component parity lab scenarios.
- `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj
  --configuration Release`
- `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj
  --configuration Release`

Checked-in synthetic probe visual example categories:

- `public-admin-workbench-light`
- `component-basic-input-light`
- `component-commands-menus-light`
- `component-layout-media-light`

## What Is Excluded From Production Support

- No arbitrary Windows binary, `.exe`, `.msix`, or packaged Windows App SDK app
  execution on macOS.
- No real Windows App SDK target execution on macOS.
- No claim of full WinUI 3 API coverage.
- No claim of full Microsoft Learn controls inventory support.
- No pixel-perfect Fluent visual parity.
- No Mica, Acrylic, compositor, system backdrop, shadow, transform, or animation
  rendering parity.
- No full `ControlTemplate`, `DataTemplate`, `VisualStateManager`, transition,
  or theme dictionary implementation.
- No complete keyboard routing, pointer-over, pressed, focus, disabled, or
  accessibility automation behavior.
- No full text stack: advanced typography, IME, selection, caret, wrapping,
  trimming, or font fallback remain incomplete.
- No media, WebView2, ink, or platform integration support.
- No broad downstream production app matrix across many real public WinUI
  applications.
- No long-running performance trend history beyond current CI benchmark/flake
  artifacts.

## Current Gaps

### Component Quality Gaps

The component lab intentionally records weak, poor, and `not-rendered`
components when they are outside the current production scope. A whole
screenshot can pass while individual components remain weak, text-only, or
absent, so production support is gated by component-level evidence.

Ring 0 and claimed Ring 1 production components now require `usable` evidence
with target layout regions. Planned rich controls, materials, templates,
platform integrations, and diagnostic-only rows stay `not-rendered` or excluded
until they receive cataloged behavior, fixture coverage, renderer support,
native WinUI Windows reference evidence, and tests.

### Source Compatibility Gaps

- Complex project files and Windows packaging flows are not production-grade.
- Windows App SDK MSBuild targets are not executed locally.
- Unsupported XAML is diagnosed, but broad source compatibility is not complete.
- `x:Bind`, templates, visual states, richer resource dictionaries, and dynamic
  theme behavior remain planned or partial.

### Visual Fidelity Gaps

- `skia-v2` is deterministic and useful for evidence, but still visibly
  simplified.
- Current checked-in public admin and component lab `windows-reference.png`
  files are native WinUI fixture screenshots with provenance. They expose
  honest macOS comparison failures and must not be used to promote component
  visual grades until the macOS output improves.
- Text metrics, edge anti-aliasing, Fluent control chrome, focus visuals,
  hover/pressed states, command surfaces, list/detail painters, depth, and
  material effects are not production parity.
- Scenario thresholds are reviewable contracts, not proof of pixel perfection.

### Product And Operations Gaps

- Package release hardening now has benchmark, flake, package dry-run,
  release-check, support-policy, and security policy gates in CI. Production
  publishing still requires human-attached signing/provenance evidence because
  CI dry runs intentionally keep `publishAllowed` false.
- Consumer support docs define safe usage, troubleshooting, known limits,
  compatibility policy, support triage, and unsupported scope.
- CI has public native WinUI reference capture for the fixture set, separate
  synthetic smoke evidence, benchmark/flake artifacts, and package dry-run
  artifacts. Longer trend history and reference drift review are release
  candidate responsibilities before expanding support scope.
- Security and supply-chain posture is documented in
  `docs/security/threat-model.md`; CI release checks validate package metadata
  and dry-run artifacts. Release signing remains a required publish-time
  evidence item, not an automatic CI action.

## Production Blockers

| ID | Severity | Owner | Blocker | Why It Blocks Production | Exit Criteria |
| --- | --- | --- | --- | --- | --- |
| PB-000 | Closed | Visual evidence maintainer | Native Windows reference source of truth is available for the public fixture set. | Public native reference runs captured actual public WinUI fixture projects, recorded `native-winui` provenance, and kept synthetic probe output separated as smoke evidence. | Keep this gate closed by preserving provenance, fixture launch coverage, and smoke-only labeling for synthetic output. |
| PB-001 | Closed by production scope | Compatibility catalog owner | Broad WinUI API coverage remains incomplete outside the documented subset. | The production contract excludes uncataloged, planned, windows-only, and not-supported APIs; the corpus has zero unknown surfaces. | Expand the catalog before expanding support scope. |
| PB-002 | Closed by production scope | Component parity owner | Many broad WinUI controls remain diagnostic-only or planned. | Production support is limited to components with public scenario evidence and minimum grades. | Promote only with component evidence, native provenance, and tests. |
| PB-003 | Closed by production scope | Renderer owner | Broad Fluent visuals remain approximate outside the production-ring subset. | Claimed supported/partial production components require `usable` evidence; weak, poor, and not-rendered outputs are excluded. | Improve renderer fidelity before expanding the visual claim. |
| PB-004 | Closed by production scope | XAML/runtime owner | Templates, broad visual states, and dynamic resources are incomplete. | They are explicit planned exclusions unless documented as supported or partial. | Implement and test before promotion. |
| PB-005 | Closed by production scope | Materials/composition owner | Fluent materials and composition are not rendered. | Mica, Acrylic, system backdrops, shadows, transforms, motion, and compositor APIs are excluded or planned. | Add deterministic modeling before promotion. |
| PB-006 | Closed by production scope | Input/accessibility owner | Broad keyboard, pointer, focus, and text behavior remains partial. | The source-level gate supports scripted click, focus, text entry, item selection, automation ID, command, and accessibility export for the documented subset. | Expand tests before claiming broader input parity. |
| PB-007 | Closed | Project ingestion owner | Project ingestion matrix is bounded. | The public corpus covers documented project shapes and ingests with zero unknown surfaces. | Add public shapes before expanding support. |
| PB-008 | Closed | Evidence corpus owner | Public visual evidence is clean-room and fixture/corpus based. | This is the documented production corpus; every current corpus scenario has native WinUI reference provenance and component evidence where applicable. | Add downstream public apps before broadening the claim. |
| PB-009 | Closed for source-level gate | Reliability owner | Long-running performance history is shallow. | CI exposes benchmark and flake artifacts; release candidates must retain and review trends. | Trend artifacts before increasing support scope. |
| PB-010 | Closed for source-level gate | Release owner | Production signing/provenance evidence is manual. | Package dry-run and release-check gates pass; CI keeps `publishAllowed` false until a release owner attaches signing/provenance evidence. | Attach evidence before publishing packages. |
| PB-011 | Closed | Security owner | Security and supply-chain review is documented. | Threat model, dependency policy, artifact privacy, safe CI, and release security expectations exist and are release-check gated. | Review for each release candidate. |
| PB-012 | Closed | Documentation owner | Support boundaries are documented. | Support policy, compatibility tiers, unsupported scope, triage policy, final gate evidence, and consumer docs are current. | Keep docs in sync with support-scope changes. |

## Readiness Gates

### Production Subset Gate

Status: **met for the current documented public source-level subset**.

Required evidence:

- Local build/test/doctor/runner gates pass.
- Public native WinUI reference workflow passes for the fixture set, and
  synthetic probe smoke remains separate.
- Component evidence exists for the component parity lab.
- Docs distinguish visual smoke evidence from component-level quality.

Missing evidence:

- No missing native-reference evidence for the current clean-room fixture and
  corpus scenario set. Public native runs capture actual public WinUI fixture
  projects, including single-window, settings-form, resource-catalog,
  production smoke, public admin, and component parity lab scenarios.
- Remaining broader WinUI 3 gaps are excluded from the production claim and
  tracked as roadmap work.

### Beta Gate

Status: **not met for broad beta scope and outside the current production
claim**.

Required evidence:

- No unknown APIs for the selected beta fixture corpus.
- Supported component subset has no `poor` grades and no unexpected
  `not-rendered` grades.
- Weak grades are either fixed or explicitly excluded from beta claims.
- Public downstream app corpus exists.
- Performance and flake metrics are recorded.

### General-Purpose WinUI 3 Gate

Status: **not met and outside the current production support claim**.

Required evidence:

- Stable package release policy and support policy.
- Security and supply-chain readiness review.
- Broad project ingestion matrix.
- Production compatibility contract with exact supported and unsupported
  behavior.
- Public visual, component, accessibility, input, and performance evidence for
  the claimed production subset.

## Recommended Next Milestones

Detailed execution plan: `docs/plans/2026-06-01-production-readiness-roadmap-plan.md`.

1. Keep native WinUI Windows reference capture healthy for every public fixture
   scenario and preserve synthetic probe output as smoke-only evidence.
2. Fix checked-in `not-rendered` or weak component examples and move them to
   `usable` only
   after native reference capture and macOS inspection prove the grade is
   honest.
3. Add crop-level or component-region diff metrics so whole-screen thresholds do
   not mask localized regressions.
4. Expand component lab coverage for currently diagnostic controls that are
   likely to matter first: `AutoSuggestBox`, `MenuFlyout`, `DropDownButton`,
   `SplitButton`, `Slider`, `NumberBox`, `PasswordBox`, `DatePicker`, and
   `TabView`.
5. Implement the minimal production subset of templates, visual states, and
   theme dictionaries.
6. Add a public downstream app corpus with source ingestion, interactions,
   visual evidence, and component evidence.
7. Add performance, flake-rate, and artifact-size gates.
8. Write the production release policy: semver, changelog, package provenance,
   rollback, support window, and security reporting.

## Decision Summary

The library has met the production readiness gate for its documented public
source-level subset. It has public native reference provenance, structured
artifacts, honest component grades, corpus ingestion with zero unknown surfaces,
benchmark/flake gates, release dry-run gates, security docs, and a support
policy.

It must still not be marketed as arbitrary WinUI 3 compatibility. Broader API
coverage, visual fidelity, materials/composition, templates, full input
behavior, Windows binary execution, and downstream app breadth remain future
scope unless promoted through the same evidence gates.
