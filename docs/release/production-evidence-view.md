# Production Evidence View

Date: 2026-06-02

This page is the single current production evidence view for the documented
public source-level WinUI 3 subset. It summarizes the catalog counts, production
Ring 0 and Ring 1 status, latest recorded workflow evidence, strict scenario
results, and checked-in visual examples without expanding the support claim.

The production claim remains bounded: this runtime supports the documented
public source-level subset only. It does not claim arbitrary WinUI 3 app
execution, Windows binary or `.msix` execution, full Fluent pixel parity,
uncataloged APIs, broad templates, broad visual states, Mica, Acrylic,
composition, media, WebView2, or platform integration.

## Current Verdict

| Area | Current evidence |
| --- | --- |
| Production status | Met for the documented public source-level subset. |
| Runtime model | Wine-free managed macOS execution against local `Microsoft.UI.Xaml` facade types. |
| Windows source of truth | Public native WinUI fixture captures from `windows-native-screenshot.yml`. |
| macOS renderer evidence | Local `skia-v2` strict scenario artifacts and component evidence. |
| Component grade source | `component-evidence.json`, not whole-screenshot pass/fail alone. |
| Support boundary | Public sanitized Ring 0 and claimed Ring 1 components with required evidence. |

Primary source documents:

- `docs/release/final-production-gate.md`
- `docs/compatibility/matrix.md`
- `docs/compatibility/component-support.md`
- `docs/compatibility/production-component-targets.md`
- `docs/compatibility/contracts.md`
- `docs/release/support-policy.md`

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

Unknown public API usage remains outside the production claim until it receives
a catalog entry, fixture coverage, macOS artifact evidence, native WinUI
provenance, and release documentation.

## Workflow Evidence

These are the latest workflow IDs recorded in the release evidence docs for the
current production gate:

| Evidence | Run | Commit | Result |
| --- | --- | --- | --- |
| Final Sprint 7 CI | `26792033784` | `3c929f4` | Passed build, tests, corpus ingestion, benchmark/flake, package dry run, release-check, and artifact uploads. |
| Final full native WinUI reference workflow | `26792033793` | `3c929f4` | Captured public native fixture references and synthetic probe smoke artifacts. |
| Sprint 6 CI | `26791576401` | `cd814a4` | Passed build, tests, corpus ingestion, benchmark/flake, package dry run, release-check, and artifact uploads. |
| Sprint 6 native WinUI reference workflow | `26791576394` | `cd814a4` | Captured public native fixture references and synthetic probe smoke artifacts. |
| Prior corpus native reference set | `26790967052` | `6d2fc9c` | Captured native references for public admin, component lab, production smoke, single-window, settings-form, and resource-catalog scenarios. |
| Prior Sprint 5 CI | `26791199828` | `3e54a99` | Passed after native reference evidence docs were updated. |

The latest full native reference artifact set includes public admin/workbench
scenarios, all light Ring 0 and Ring 1 component lab scenarios,
`production-smoke-light`, `production-e2e-workbench-light`, and public corpus
references for single-window, settings-form, and resource-catalog scenarios.
Every native reference JSON should record `referenceSource: native-winui`,
workflow run ID, commit SHA, runner image, viewport, theme, title match, capture
mode, and image dimensions.

## Strict Scenario Results

The Sprint 7 local strict sweep passed **36 public scenarios** with
`--renderer skia-v2 --strict-visual`.

| Scenario group | Scenarios | Current result |
| --- | ---: | --- |
| Component parity lab | 23 | Local strict scenarios passed for light, state, popup, dark, and high-contrast component coverage. |
| Production smoke and E2E | 2 | `production-smoke-light` and `production-e2e-workbench-light` passed. |
| Public admin workbench | 2 | `public-admin-workbench-light` and `public-admin-workbench-deferred-light` passed. |
| Resource catalog | 3 | Light, dark, and high-contrast resource catalog scenarios passed. |
| Public corpus app scenarios | 2 | Single-window and settings-form scenarios are covered by the native workflow set. |
| Legacy mac test fixtures | 4 | Shell, interaction binding, and control-gallery light/high-contrast strict scenarios remain smoke evidence for the harness subset. |

Current inspected production component summaries:

| Scenario family | Production evidence summary |
| --- | --- |
| Basic input and forms | Supported Ring 0 controls are `usable`; planned rich input rows remain `not-rendered`. |
| Commands and menus | Supported command surfaces and the partial flyout/menu subset are `usable`; menu bar, context menu pattern, split/dropdown buttons remain planned or diagnostic. |
| Navigation and workbench | Navigation/list-detail production subset is `usable`; adaptive behavior and broader keyboarding remain partial. |
| Status and progress | `InfoBar`, `ProgressBar`, and `ProgressRing` production subset is `usable`; animation and close/action areas remain gaps. |
| Layout, media, and resources | Ring 0 layout/resource regions are `usable`; media, web, ink, materials, and advanced visuals remain excluded or planned. |

The `ClaimedSupportedComponentsAreNeverNotRendered` test protects the claim by
failing if a scenario requirement marked `supported` or `partial` regresses to
`not-rendered` or below its declared minimum grade.

## Ring 0 Status

Ring 0 is the production smoke foundation. Every Ring 0 family requires public
fixture coverage, native WinUI provenance, macOS renderer evidence,
component evidence, interaction/accessibility evidence where applicable, and at
least a `usable` visual grade.

| Family | Components or patterns | Current status | Evidence summary |
| --- | --- | --- | --- |
| App shell | `Application`, `Window`, `Page`, `Frame` | Met for subset | Launch, activation, navigation, and stable title evidence exist; native window chrome and full lifecycle remain out of scope. |
| Core layout | `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ContentControl` | Met for subset | `skia-v2` renders visible layout regions; full WinUI layout behavior remains partial. |
| Text | `TextBlock`, labels/forms pattern | Met for subset | Text is readable with approximate metrics, wrapping, trimming, and form layout. |
| Basic commands | `Button`, `AppBarButton`, `CommandBar` | Met for subset | Click and command invocation evidence exists; overflow, pointer states, and exact native spacing remain gaps. |
| Forms | `TextBox`, `ComboBox`, `CheckBox`, `RadioButton` | Met for subset | Focus, text entry, item selection, and checked-state evidence exists; popup, caret, selection, and validation visuals remain gaps. |
| Workbench | `NavigationView`, `NavigationViewItem`, menu/footer, `ListView`, list/details | Met for subset | Navigation and list/detail evidence exists; adaptive layout and keyboarding remain gaps. |
| Status | `InfoBar`, `ProgressBar`, `ProgressRing` | Met for subset | Status and progress chrome are visible; close/action areas and native animations remain gaps. |
| Resources and theme | `StaticResource`, `ThemeResource`, `Style`, `Setter`, simple typography/spacing | Met for subset | Light, dark, and high-contrast strict scenarios pass for the clean-room subset; dynamic Fluent resource behavior remains planned. |
| Artifacts | `tree.json`, `accessibility.json`, `visual-run.json`, `component-evidence.json`, pixel diff artifacts | Met for subset | Versioned artifacts preserve deterministic evidence for supported controls. |

## Ring 1 Status

Ring 1 enables production E2E flows only where the claimed subset reaches the
same evidence bar. Unclaimed controls remain planned, diagnostic, or outside
the production claim.

| Family | Current status | Claim boundary |
| --- | --- | --- |
| Menus and flyouts | Partial | `MenuFlyout` and `CommandBarFlyout` open/invoke subset is usable when claimed; `MenuBar`, context menu pattern, disabled item behavior, split/dropdown buttons, native placement, and broad keyboarding remain incomplete. |
| Rich form input | Planned | `PasswordBox`, `NumberBox`, `AutoSuggestBox`, `Slider`, and `ToggleSwitch` are diagnostic rows only. |
| Dialog decisions | Partial | `ContentDialog`, `Flyout`, and `ToolTip` open-state subset is usable when claimed; `TeachingTip`, tooltip service, modal focus trapping, placement, and full action relationships remain incomplete. |
| Templates and collections | Planned except partial hosts | Collection hosts have subset evidence; `DataTemplate`, item templates, `ItemsRepeater`, `GridView`, virtualization, and template visuals remain missing. |
| Theme dictionaries and resources | Partial | Theme dictionaries, `SolidColorBrush`, and `CornerRadius` have subset evidence; full Fluent dictionaries, dynamic invalidation, and typed resource fidelity remain incomplete. |
| Keyboard and accessibility | Partial | Deterministic accessibility export exists for the subset; broad focus traversal, accelerator routing, keyboard selection, and focus visuals remain incomplete. |

## Checked-In Visual Examples

Checked-in PNG examples live under `docs/visual-parity/examples/` and are
summarized in `docs/visual-parity/comparisons.md`. They are historical
visual-review fixtures, not the current production grade source.

| Example | Checked-in comparison status | Current interpretation |
| --- | --- | --- |
| `public-admin-workbench-light` | Historical whole-image comparison failed with 100.00% changed pixels. | Superseded for production claims by fresh strict scenario results and component evidence for the documented workbench subset. |
| `component-basic-input-light` | Historical whole-image comparison failed with 42.07% changed pixels over an 18% threshold. | Current Ring 0 basic input evidence is judged by fresh `component-evidence.json`; planned controls remain `not-rendered`. |
| `component-commands-menus-light` | Historical whole-image comparison failed with 40.68% changed pixels over a 24% threshold. | Current command/flyout claims are limited to supported command surfaces and the partial usable menu/flyout subset. |
| `component-layout-media-light` | Historical whole-image comparison failed with 45.83% changed pixels over a 24% threshold. | Current layout/resource claims are limited to Ring 0 usable regions; advanced layout, media, web, ink, materials, and diagnostics remain outside the claim. |

Fresh production support is determined by catalog status, strict scenario
results, `component-evidence.json`, interaction evidence, accessibility export,
and native-reference provenance. Historical whole-image comparison failures
must not be used to promote or demote current component grades by themselves.

## Release And Risk Notes

- `release-check` validates release/security docs, package metadata, package
  output, support policy, release readiness, and package dry-run artifacts.
- `release-readiness.json` keeps `publishAllowed` false in CI; real publishing
  still requires human signing/provenance evidence.
- Planned, Windows-only, not-supported, uncataloged, diagnostic-only, weak,
  poor, or `not-rendered` rows remain outside the production claim.
- Native Windows hosted-runner image changes can cause visual reference drift;
  keep workflow run IDs and provenance with release notes.
- This page is a summary. When it conflicts with machine-readable artifacts,
  the catalog JSON and generated evidence artifacts are the source of truth.
