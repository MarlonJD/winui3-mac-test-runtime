# Production Evidence View

Date: 2026-06-02

This page is the running visual readiness dashboard and the single current
evidence view for the documented public source-level WinUI 3 subset. It
summarizes catalog counts, all-catalog dispositions, Ring 0 and Ring 1 harness
status, latest recorded workflow evidence, strict scenario results, and visual
examples without expanding the support claim.

The support claim remains bounded: this runtime supports the documented public
source-level harness subset only. It does not claim arbitrary WinUI 3 app
execution, Windows binary or `.msix` execution, production visual fidelity,
full Fluent pixel parity, uncataloged APIs, broad templates, broad visual
states, Mica, Acrylic, composition, media, WebView2, or platform integration.

## Current Verdict

| Area | Current evidence |
| --- | --- |
| Production status | Release evidence gate is met for the documented harness subset; production visual fidelity is not met. |
| Runtime model | Wine-free managed macOS execution against local `Microsoft.UI.Xaml` facade types. |
| Windows source of truth | Public native WinUI fixture captures from `windows-native-screenshot.yml`. |
| Native source readiness | `docs/visual-parity/native-reference-readiness.json`; currently 58/58 public rows are `ready` with zero blocker rows. |
| State coverage matrix | `docs/visual-parity/state-coverage-matrix.json`; currently tracks explicit default/state/interaction/accessibility gaps and labels default-only components without expanding claims. |
| Native-quality family tranches | `docs/visual-parity/native-quality-family-tranches.json`; currently tracks six Milestone C families, publishes each family's missing state requirement queue, and keeps all six blocked until native-quality inspection and non-default state evidence exist. |
| macOS renderer evidence | Local `skia-v2` strict scenario artifacts show usable scaffolding plus many simplified or `not-rendered` controls. |
| Component grade source | `component-evidence.json`, not whole-screenshot pass/fail alone; `usable` is not a native-fidelity grade. |
| Compatibility level | `docs/compatibility/compatibility-levels.json`; current productization level is L2 for the public source-level harness subset. |
| One-command evidence | `PATH="$PWD/tools:$PATH" winui3-mac-release-ready-local` runs the clean-checkout local gate: build, compiled test assemblies, strict scenario evidence, public product evidence, package dry-run artifacts, `release-check`, and `release-candidate` without expanding the visual claim. |
| Support boundary | Public sanitized Ring 0 and claimed Ring 1 harness components with required evidence. |
| Visual readiness inventory | `docs/compatibility/visual-readiness-inventory.json`. |

Primary source documents:

- `docs/compatibility/compatibility-levels.md`
- `docs/compatibility/compatibility-levels.json`
- `docs/release/final-production-gate.md`
- `docs/compatibility/matrix.md`
- `docs/compatibility/component-support.md`
- `docs/compatibility/production-component-targets.md`
- `docs/compatibility/visual-readiness-inventory.json`
- `docs/compatibility/contracts.md`
- `docs/release/support-policy.md`

## Visual Readiness Dashboard

The current completed outcome is **126/126 catalog dispositions** for
source-level WinUI 3 harness readiness. Locally executable entries have
implemented, bounded, diagnostic, Windows-only, or non-goal treatment with
tests, fixtures, native WinUI reference evidence when visual, macOS artifacts,
interaction/accessibility evidence where applicable, and docs. That is a
release-evidence result, not a renderer-fidelity result.

The checked-in public component-quality dashboard contains 58 component rows:
51 `usable` and 7 `not-rendered`. Those rows now have native/macOS/diff crop
triptychs and manual inspection metadata for the bounded source-level harness
gate, but large areas of WinUI chrome, templates, states, and advanced controls
still need direct renderer work before any native-quality claim.

The checked-in state coverage matrix currently tracks 23 components and 38
state requirements from `productionStateCoverage`. It records 15 components as
`default-only` and 8 as missing default component evidence, so state, interaction,
and accessibility gaps are visible release evidence rather than hidden
promotion assumptions. Each requirement row also carries the strict-sweep
component evidence, accessibility, and visual-run artifact paths that the
`public-product` rollup must validate after the sweep.

The checked-in native-quality family tranche queue currently groups 26 public
component rows across six Milestone C families: selection controls, button/link,
dropdown/menu, text/forms, navigation/list, and status/progress. All six
families are `native-quality-blocked`, with 23 rows still
`nativeQualityGrade: not-evaluated`, 14 components still `default-only`, and 8
components missing default component evidence. This keeps family-level renderer
quality work explicit without promoting source-level `usable` rows. Rows whose
component crops still fail strict thresholds now carry the exact threshold
blocker in `remainingBlocker`, so renderer work is separated from state-matrix
work.

Native crop presence is not enough for promotion. The checked-in native
reference source audit (`docs/visual-parity/native-reference-source-audit.md`)
found that several Windows reference crops are runtime-bound, placeholder,
unavailable, or offscreen in historical context. The release candidate gate
reads `docs/visual-parity/native-reference-readiness.json`; the current manifest
has zero source-readiness blockers, while native-quality promotion still
requires stricter renderer evidence.

| Family | Target grade | Current grade | Latest run ID | Next blocker |
| --- | --- | --- | --- | --- |
| App shell | renderer-fidelity target | usable scaffold | `26962358057` | Native window chrome and full lifecycle remain outside the local macOS support claim. |
| Core layout | renderer-fidelity target | usable scaffold | `26962358057` | Broader WinUI layout behavior remains partial; exact sizing, clipping, and scroll behavior need renderer work. |
| Text and forms | renderer-fidelity target | usable scaffold | `26962358057` | Caret, selection, validation visuals, rich input, and native field states require direct implementation. |
| Commands and menus | renderer-fidelity target | usable scaffold for supported command, content-slot, MenuBar, context target, split/dropdown button chrome, and partial flyout subset | `26962358057` | Native menu popup behavior, disabled item behavior, placement, light-dismiss, keyboarding, and final inspection remain incomplete. |
| Navigation and workbench | renderer-fidelity target | usable scaffold | `26962358057` | Adaptive behavior, broad keyboard routing, richer collection templates, and native list/detail chrome remain partial or planned. |
| Status and progress | renderer-fidelity target | usable scaffold in base, loading, and success scenarios | `26962358057` | Animation, close/action areas, and full native severity chrome need follow-up. |
| Dialogs, flyouts, and tooltips | renderer-fidelity target | usable scaffold for supported popup subset; `not-rendered` for planned surfaces | `26962358057` | Modal focus trapping, placement, TeachingTip, tooltip service, and full action relationships remain planned or partial. |
| Resources, theme, and visual states | renderer-fidelity target | usable for simple resources and theme dictionaries; diagnostic or `not-rendered` for broad Fluent states | `26962358057` | Full Fluent dictionaries, dynamic invalidation, pointer states, and template visual states require renderer and resource work. |
| Materials, composition, media, and platform integration | production-ready exclusion | exclusion target defined; no local macOS visual support claim | `26962358057` | Mica, Acrylic, compositor effects, media, WebView2, launcher, packaged apps, and binaries remain explicit exclusions or roadmap diagnostics. |

Promotion is evidence-backed:

| Grade | Promotion rule |
| --- | --- |
| `not-rendered` | Entry is absent, text-only when chrome is required, catalog-only, or diagnostic-only. It cannot be claimed production-ready local macOS support. |
| `usable` | Entry renders recognizable, functionally testable output for the documented subset and has catalog status, public scenario, macOS artifact, component evidence, required interaction/accessibility evidence, docs, and native WinUI reference when visual. It does not imply native visual fidelity. |
| `good` | Entry meets the usable bar plus reviewed component-level visual fidelity against native WinUI references across required state/theme scenarios. |
| production-ready | Entry has a deliberate catalog disposition: implemented support at the required harness grade, bounded partial support with exact limits, Windows-only exclusion, diagnostic roadmap exclusion, or explicit non-goal exclusion. It is not a visual fidelity label. |

## Phase 2-9 Gate Status

| Phase | Current status | Evidence gate |
| --- | --- | --- |
| Phase 2: Component crop and reference tooling | Implemented | `component-evidence.json` carries crop metadata, native reference provenance, and effective per-component thresholds; `visual-run.json` points to the crop directory and generated visual review page; `visual-review.html` places native, macOS, and diff crops side by side with reference source/run/commit provenance; strict visual fails claimed supported/partial rows with missing, blank, `not-rendered`, or over-threshold crops. |
| Phase 3: Fluent token and theme foundation | Implemented | `skia-v2` painters use a centralized token layer for light, dark, high contrast, typography, fills, strokes, status colors, focus, selected chrome, disabled surfaces, radius, and popup elevation. |
| Phase 4: Ring 0 Windows chrome completion | Implemented for the documented source-level subset | Ring 0 strict scenarios cover shell, layout, text, commands, forms, workbench, status/progress, resources/theme, state scenarios, and artifacts; claimed supported/partial rows require at least `usable`. |
| Phase 4B: State coverage matrix | Tracking implemented; state evidence pending promotion | `docs/visual-parity/state-coverage-matrix.json` joins `productionStateCoverage` requirements with checked-in component evidence and labels default-only rows so state gaps cannot be mistaken for native-quality coverage. |
| Phase 5: Ring 1 E2E visual completion | Implemented for claimed subsets | Static command/menu scenarios cover claimed `MenuFlyout` and `CommandBarFlyout` host targets, `MenuBar` static chrome, and context-menu target export; open-popup diagnostics remain source-level smoke evidence unless a matching native popup reference is captured. Selected collection and layout/theme scenarios cover collection hosts, theme dictionaries, `SolidColorBrush`, and `CornerRadius`; rich input, templates, broader keyboarding, advanced collections, `TeachingTip`, and menu popup behavior remain planned diagnostics. |
| Phase 6: All-126 catalog closure | Implemented | `docs/compatibility/all-catalog-readiness-audit.json` accounts for all 126 entries with a per-entry production disposition, owner phase, primary blocker, evidence profile, and release gate; `winui3-mac-runner catalog-audit --check` fails on drift and the audit agrees with the inventory buckets. |
| Phase 7: Broader WinUI control inventory | Inventory and gate implemented; controls pending promotion | `docs/compatibility/winui-component-inventory.json` `broaderControlInventory` enumerates 20 prioritized public WinUI controls with target family, required states, priority, and promotion exit criteria (`docs/compatibility/broader-control-inventory.md`); the honesty gate keeps every control `not-rendered` until it carries matching catalog status, visual evidence, and interaction coverage. |
| Phase 8: Materials, motion, and high-fidelity polish | Registry, motion/contrast rules, and drift dashboard implemented; surfaces pending promotion | `docs/compatibility/material-motion-approximations.json` documents every Mica, Acrylic, backdrop, shadow, transform, compositor, and motion surface as a deterministic approximation target or explicit exclusion with reduced-motion, high-contrast, and provenance rules and no OS composition claim; `docs/visual-parity/visual-drift-dashboard.json` gates component-crop drift and keeps whole-screen drift informational with values read from the checked-in pixel-diff artifacts. |
| Phase 9: Release candidate gate | Implemented | `winui3-mac-runner release-candidate` aggregates the deterministic local release requirements, including native reference source readiness, and lists the external workflow requirements; see the Release Candidate Gate section below. |

## Catalog Snapshot

`docs/compatibility/winui-api-compatibility.catalog.json` is the deterministic
catalog source for docs, XAML diagnostics, project ingestion, corpus inventory,
and placeholder facade runtime diagnostics.

| Status | Count | Production meaning |
| --- | ---: | --- |
| `supported` | 55 | Implemented and covered by a fixture or test for the documented subset. |
| `partial` | 35 | Implemented only for a constrained source-level or headless test subset. |
| `planned` | 31 | Cataloged roadmap item; unavailable usage must report diagnostics. |
| `windows-only` | 3 | Valid public surface that requires real Windows validation or OS integration. |
| `not supported` | 2 | Explicit non-goal for the current clean-room macOS runtime. |

Total catalog entries: **126**.

Kind counts from the same catalog:

| Kind | Count |
| --- | ---: |
| `api` | 48 |
| `fluent-resource` | 4 |
| `project-item` | 3 |
| `project-property` | 4 |
| `visual-state` | 5 |
| `xaml-attached-property` | 4 |
| `xaml-directive` | 5 |
| `xaml-element` | 34 |
| `xaml-event` | 3 |
| `xaml-markup` | 1 |
| `xaml-property` | 5 |
| `xaml-property-element` | 3 |
| `xaml-resource` | 7 |

Unknown public API usage remains outside the support claim until it receives
a catalog entry, fixture coverage, macOS artifact evidence, native WinUI
provenance, and release documentation.

## All-Catalog Readiness Audit

The bucket-level audit is in
`docs/compatibility/visual-readiness-inventory.json` and the per-entry audit is
in `docs/compatibility/all-catalog-readiness-audit.json` (summarized in
`docs/compatibility/all-catalog-readiness-audit.md`). Both account for all
**126/126** entries and leave **0** entries without a production disposition. The
per-entry audit is generated deterministically from the catalog and verified
with `winui3-mac-runner catalog-audit --check`; every entry carries a
disposition, owner phase, primary blocker, evidence profile, and release gate.

| Production disposition | Count | Evidence requirement |
| --- | ---: | --- |
| Source-level production implementation | 55 | Tests, fixtures, macOS artifacts, native WinUI reference when visual, interaction/accessibility evidence where applicable, and docs. |
| Bounded source-level production implementation | 35 | Same evidence bar as supported entries, plus exact partial boundary wording and diagnostics for missing behavior. |
| Production-ready diagnostic exclusion until promoted | 31 | Deterministic diagnostics, docs, owner or roadmap treatment, and promotion exit criteria. |
| Production-ready Windows-only exclusion | 3 | Deterministic exclusion, Windows validation evidence where applicable, support-policy wording, and no local macOS support claim. |
| Production-ready non-goal exclusion | 2 | Deterministic non-goal diagnostics and support-policy wording. |

| Primary blocker | Catalog entries | Current treatment |
| --- | ---: | --- |
| PB-001 | 14 | Planned API entries remain cataloged roadmap diagnostics until promoted. |
| PB-002 | 28 | Source-level nonvisual or parser/project/resource entries need implementation or bounded support evidence. |
| PB-003 | 62 | Visual API, element, resource, and state entries need component evidence so `not-rendered`, weak, or poor rows are not hidden. |
| PB-004 | 8 | Theme resources, visual states, and template property elements need state/theme/template evidence before promotion. |
| PB-012 | 14 | Planned, Windows-only, and non-goal entries need precise support-policy and exclusion handling. |

## Production Blocker Visual Mapping

| Blocker | Visual readiness treatment |
| --- | --- |
| PB-000 | Require `referenceSource: native-winui` for every promoted visual scenario; synthetic probes remain smoke-only. |
| PB-001 | Keep catalog/docs count gates aligned with the JSON source of truth and keep unknowns diagnostic. |
| PB-002 | Promote only cataloged source-level surfaces with exact support boundaries and fixture evidence. |
| PB-003 | Prevent weak, poor, or `not-rendered` visual rows from being hidden by whole-screen passes. |
| PB-004 | Require state/theme/template coverage before promoting visual states or template-backed controls. |
| PB-005 | Treat materials and composition as deterministic approximations or exclusions until native-reference-backed. |
| PB-006 | Require interaction and accessibility artifacts for interactive or semantic visual promotions. |
| PB-007 | Use clean-room public fixtures and corpus entries only. |
| PB-008 | Track native provenance and component evidence for every promoted visual family. |
| PB-009 | Add visual gates without loosening thresholds or hiding flake. |
| PB-010 | Carry visual readiness artifacts into release evidence before publication. |
| PB-011 | Keep private-name and artifact privacy rules mandatory for every new fixture or visual artifact. |
| PB-012 | Keep support policy and dashboard wording synchronized with every promotion or exclusion. |

## Workflow Evidence

These are the latest workflow IDs recorded in the release evidence docs for the
current production gate:

| Evidence | Run | Commit | Result |
| --- | --- | --- | --- |
| PR #3 CI release-candidate evidence | `26969205766` | `3c19711` | Passed build, tests, full strict scenario sweep, corpus ingestion, benchmark/flake, package dry run, release-check, and artifact uploads including `strict-scenario-sweep`. |
| PR #3 full native WinUI reference workflow | `26962358057` | `1a2eb01` | Passed `windows-reference` and `synthetic-probe-smoke`; uploaded `windows-reference-screenshots` and `windows-synthetic-probe-smoke`. |
| PR #3 local full strict sweep | local | `530e4f5` | 36/36 public scenarios passed with `--renderer skia-v2 --strict-visual`; CI now runs the same sweep and uploads `strict-scenario-sweep`. |
| Final Sprint 7 CI | `26792033784` | `3c929f4` | Passed build, tests, corpus ingestion, benchmark/flake, package dry run, release-check, and artifact uploads. |
| Final full native WinUI reference workflow | `26792033793` | `3c929f4` | Captured public native fixture references and synthetic probe smoke artifacts. |
| Sprint 6 CI | `26791576401` | `cd814a4` | Passed build, tests, corpus ingestion, benchmark/flake, package dry run, release-check, and artifact uploads. |
| Sprint 6 native WinUI reference workflow | `26791576394` | `cd814a4` | Captured public native fixture references and synthetic probe smoke artifacts. |
| Prior corpus native reference set | `26790967052` | `6d2fc9c` | Captured native references for public admin, component lab, production smoke, single-window, settings-form, and resource-catalog scenarios. |
| Prior Sprint 5 CI | `26791199828` | `3e54a99` | Passed after native reference evidence docs were updated. |

The latest full native reference artifact set, run `26962358057` on commit
`1a2eb01`, includes public admin/workbench scenarios, all light Ring 0 and
Ring 1 component lab scenarios,
`production-smoke-light`, `production-e2e-workbench-light`, and public corpus
references for single-window, settings-form, and resource-catalog scenarios.
Every native reference JSON should record `referenceSource: native-winui`,
workflow run ID, commit SHA, runner image, viewport, theme, title match, capture
mode, and image dimensions.

## Strict Scenario Results

The PR #3 local strict sweep on commit `530e4f5` passed **36 public scenarios**
with `--renderer skia-v2 --strict-visual`. PR #3 CI run `26969205766` on commit
`3c19711` ran the same full sweep successfully and uploaded the
`strict-scenario-sweep` artifact for the release-candidate evidence trail.
The same scenario list is now available locally through
`winui3-mac-runner product-evidence --profile strict-scenario-sweep --output
artifacts/product-evidence/strict-scenario-sweep`, which writes one
product-evidence step and one `visual-run.json` artifact per scenario. The
`public-product` rollup also checks the `productionStateCoverage` scenarios
against the strict-sweep artifact paths declared by
`state-coverage-matrix.json`. The attached accessibility tree must include the
component-evidence target and must export checked/disabled/focused/selected
state where the matrix requires it, so state coverage cannot be satisfied by a
passed whole-scenario screenshot alone or by stale release path metadata.

| Scenario group | Scenarios | Current result |
| --- | ---: | --- |
| Component parity lab | 23 | Local strict scenarios passed for light, state, popup, dark, and high-contrast component coverage. |
| Production smoke and E2E | 2 | `production-smoke-light` and `production-e2e-workbench-light` passed. |
| Public admin workbench | 2 | `public-admin-workbench-light` and `public-admin-workbench-deferred-light` passed. |
| Resource catalog | 3 | Light, dark, and high-contrast resource catalog scenarios passed. |
| Public corpus app scenarios | 2 | Single-window and settings-form scenarios are covered by the native workflow set. |
| Legacy mac test fixtures | 4 | Shell, interaction binding, and control-gallery light/high-contrast strict scenarios remain smoke evidence for the harness subset. |

Current inspected renderer component summaries:

| Scenario family | Renderer evidence summary |
| --- | --- |
| Basic input and forms | Base scenario has 13 `usable` rows and zero `not-rendered`; checked-state scenario adds 3 `usable` rows. Controls are recognizable but not native Fluent chrome. |
| Commands and menus | 8 `usable`, zero `not-rendered`; command surfaces, content-slot, MenuBar, context target, and popups are simplified. |
| Navigation and workbench | Workbench/list-detail scaffold is `usable`; adaptive behavior and broader keyboarding remain partial. |
| Status and progress | Base status picker scenario has 3 `usable`, 7 planned `not-rendered`; loading and success state scenarios add 4 `usable` rows. Animation and close/action areas remain gaps. |
| Layout, media, and resources | Light layout/media scenario has 21 `usable`, 7 planned/non-goal `not-rendered`; media, web, ink, animation, shapes, materials, and advanced visuals remain excluded or planned. |

The `ClaimedSupportedComponentsAreNeverNotRendered` test protects the claim by
failing if a scenario requirement marked `supported` or `partial` regresses to
`not-rendered` or below its declared minimum grade.

## Ring 0 Harness Status

Ring 0 is the smoke foundation. Every Ring 0 family requires public fixture
coverage, native WinUI provenance, macOS renderer evidence, component evidence,
interaction/accessibility evidence where applicable, and at least a `usable`
harness grade. This does not mean the component visually matches native WinUI.

| Family | Components or patterns | Current status | Evidence summary |
| --- | --- | --- | --- |
| App shell | `Application`, `Window`, `Page`, `Frame` | Met for subset | Launch, activation, navigation, and stable title evidence exist; native window chrome and full lifecycle remain out of scope. |
| Core layout | `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl` | Met for harness subset | `skia-v2` renders visible layout regions; full WinUI layout behavior remains partial. |
| Text | `TextBlock`, labels/forms pattern | Met for subset | Text is readable with approximate metrics, wrapping, trimming, and form layout. |
| Basic commands | `Button`, `AppBarButton`, `CommandBar` | Met for harness subset | Click and command invocation evidence exists; overflow, pointer states, icons, and exact native spacing remain gaps. |
| Forms | `TextBox`, `ComboBox`, `CheckBox`, `RadioButton` | Met for harness subset | Focus, text entry, item selection, and checked-state evidence exists; popup, caret, selection, and validation visuals remain gaps. |
| Workbench | `NavigationView`, `NavigationViewItem`, menu/footer, `ListView`, list/details | Met for subset | Navigation and list/detail evidence exists; adaptive layout and keyboarding remain gaps. |
| Status | `InfoBar`, `ProgressBar`, `ProgressRing` | Met for harness subset | Status and progress chrome are visible in base, loading, and success scenarios; close/action areas and native animations remain gaps. |
| Resources and theme | `StaticResource`, `ThemeResource`, `Style`, `Setter`, simple typography/spacing | Met for subset | Light, dark, and high-contrast strict scenarios pass for the clean-room subset; dynamic Fluent resource behavior remains planned. |
| Artifacts | `tree.json`, `accessibility.json`, `visual-run.json`, `component-evidence.json`, pixel diff artifacts | Met for subset | Versioned artifacts preserve deterministic evidence for supported controls. |

## Ring 1 Status

Ring 1 enables production E2E flows only where the claimed subset reaches the
same evidence bar. Unclaimed controls remain planned, diagnostic, or outside
the support claim.

| Family | Current status | Claim boundary |
| --- | --- | --- |
| Menus and flyouts | Partial | `MenuFlyout` and `CommandBarFlyout` static host targets, static `MenuBar` chrome, context-menu target export, and split/dropdown button chrome are usable when claimed; open/invoke popup behavior remains source-level diagnostic until matching native popup evidence exists. Disabled item behavior, menu popup behavior, native placement, light-dismiss, and broad keyboarding remain incomplete. |
| Rich form input | Planned | `PasswordBox`, `NumberBox`, `AutoSuggestBox`, `Slider`, and `ToggleSwitch` are diagnostic rows only. |
| Dialog decisions | Partial | `ContentDialog`, `Flyout`, and `ToolTip` open-state subset is usable when claimed; `TeachingTip`, tooltip service, modal focus trapping, placement, and full action relationships remain incomplete. |
| Templates and collections | Planned except partial hosts | Collection hosts have subset evidence; `DataTemplate`, item templates, `ItemsRepeater`, `GridView`, virtualization, and template visuals remain missing. |
| Theme dictionaries and resources | Partial | Theme dictionaries, `SolidColorBrush`, and `CornerRadius` have subset evidence; full Fluent dictionaries, dynamic invalidation, and typed resource fidelity remain incomplete. |
| Keyboard, accessibility, and UI automation | Partial | Deterministic accessibility export and runner interaction scripts exist for the subset. FlaUI 5.0 + FlaUI.UIA3 is the native Windows automation target, and a repo-owned FlaUI.UIA3-compatible macOS adapter is now a production goal. Broad focus traversal, accelerator routing, keyboard selection, focus visuals, and full UIA/FlaUI provider compatibility remain incomplete. |

## Checked-In Visual Examples

Checked-in PNG examples live under `docs/visual-parity/examples/` and are
summarized in `docs/visual-parity/comparisons.md`. They are historical
visual-review fixtures, not the current production grade source.

| Example | Checked-in comparison status | Current interpretation |
| --- | --- | --- |
| `public-admin-workbench-light` | Whole-image comparison fails with 99.988381% changed pixels over a 45% threshold. | Current public artifacts show 9 `usable` workbench scaffold rows, not native-quality parity. |
| `component-basic-input-light` | Whole-image comparison fails with 30.145914% changed pixels over an 18% threshold. | Current public artifacts show 13 `usable` rows and zero `not-rendered` rows; native control chrome remains approximate and not manually promoted. |
| `component-commands-menus-light` | Whole-image comparison fails with 28.416018% changed pixels over a 24% threshold. | Current public artifacts show 8 `usable` rows and zero `not-rendered` rows; command/static flyout host/MenuBar surfaces remain simplified and not manually promoted. |
| `component-layout-media-light` | Whole-image comparison fails with 44.828821% changed pixels over a 24% threshold. | Current public artifacts show 21 `usable` and 7 planned/non-goal `not-rendered` rows; media, web, ink, animation, shapes, and materials remain outside the claim. |

Fresh support status is determined by catalog status, strict scenario
results, `component-evidence.json`, interaction evidence, accessibility export,
and native-reference provenance. Historical whole-image comparison failures
must not be used to promote or demote current component grades by themselves;
manual screenshot inspection remains required before claiming visual quality.

The generated `docs/visual-parity/component-quality-dashboard.json` is the
current public component-quality gate. It now has zero source-level harness
blocker rows across 58 checked-in public component rows. The public rows have
macOS component crops, native WinUI reference crops, component diffs, native
reference provenance, and manual inspection metadata. They remain at 51
`usable`, 7 `not-rendered`, and 58 `nativeQualityGrade: not-evaluated`; this is
not a native-quality promotion. The generated
`docs/visual-parity/public-visual-review-index.html` is the row-by-row
inspection queue for those crops.

## Release Candidate Gate

`winui3-mac-runner release-candidate` turns "ready" into a release decision
instead of a subjective screenshot review. It writes
`artifacts/production-gates/release-candidate.json` and aggregates the
deterministic, locally verifiable requirements:

- 126/126 catalog entries have a production disposition;
- catalog and docs counts are consistent;
- zero unknown public surfaces;
- no broader control claims a rendered grade without evidence;
- no material/motion surface claims real Windows OS composition;
- component-crop drift is gated and whole-screen drift is informational;
- the generated component-quality dashboard has zero blocker rows;
- the generated state coverage matrix is current and labels default-only rows;
- the generated native-quality family tranche queue is current;
- attached strict-sweep accessibility targets and state exports are valid for
  `productionStateCoverage` rows;
- every checked-in visual reference declares native WinUI provenance;
- release and support-policy documents are present;
- the private-name denylist scan is clean.

It also lists the requirements that can only be satisfied with external workflow
evidence and keeps `releaseAllowed` false until they are confirmed:

- full native WinUI reference capture for every claimed scenario;
- the full `--renderer skia-v2 --strict-visual` scenario sweep, produced by
  `product-evidence --profile strict-scenario-sweep`;
- the package dry run plus `release-check --package-dir`.

The exact support boundary is unchanged: source-level WinUI 3 harness readiness
for the documented public subset. This is not Windows binary or `.msix`
execution, arbitrary WinUI 3 compatibility, full Fluent pixel parity, production
visual fidelity, or OS composition.

## Release And Risk Notes

- `release-check` validates release/security docs, package metadata, package
  output, support policy, release readiness, and package dry-run artifacts.
- `release-readiness.json` keeps `publishAllowed` false in CI; real publishing
  still requires human signing/provenance evidence.
- Planned, Windows-only, not-supported, uncataloged, diagnostic-only, weak,
  poor, or `not-rendered` rows remain outside the support claim.
- Native Windows hosted-runner image changes can cause visual reference drift;
  keep workflow run IDs and provenance with release notes.
- This page is a summary. When it conflicts with machine-readable artifacts,
  the catalog JSON and generated evidence artifacts are the source of truth.
