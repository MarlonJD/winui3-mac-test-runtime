# Production Readiness Roadmap Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `docs/release`, `docs/compatibility`,
`docs/visual-parity`, `docs/architecture`, `.github/workflows`, `fixtures`,
`src/WinUI3.MacCompat`, `src/WinUI3.MacRuntime`, `src/WinUI3.MacXaml`,
`src/WinUI3.MacRenderer.Skia`, `src/WinUI3.MacRunner`, `tests`

## Goal

Move `winui3-mac-test-runtime` from an evidence-backed alpha foundation to a
production-ready source-level WinUI 3 compatibility runtime for a clearly
documented public subset.

Production-ready does not mean arbitrary Windows binary execution or full WinUI
3 parity. It means the project has a stable, honest, externally consumable
production contract backed by public tests, native WinUI Windows reference
artifacts for the actual public WinUI fixtures, component-level evidence,
source-ingestion coverage, performance/reliability gates, security/release
policy, and clear unsupported behavior.

## Current Baseline

The current baseline is documented in
`docs/release/production-readiness.md`:

- Current verdict: alpha-ready for public compatibility experiments and fixture
  validation, not production-ready for general-purpose WinUI 3 development.
- Public harness evidence exists for local gates, Windows-hosted synthetic probe
  screenshots, macOS runtime screenshots, pixel diffs, `visual-run.json`, and
  `component-evidence.json`.
- Current `windows-native-screenshot.yml` references are captured from
  `WindowsNativeProbe`, a synthetic WinForms/GDI reference app. They are not
  native WinUI renders of the fixture projects and must not be used to promote
  component visual grades.
- `ComponentParityLab.WinUI` provides a clean-room component lab with eight
  pages and source-audit gap coverage.
- Known production blockers are tracked as `PB-000` through `PB-012`.

## Plan Reframe

This roadmap supersedes the previous production ordering. Because every current
checked-in visual reference starts from synthetic `WindowsNativeProbe` output,
the project does not yet have a trustworthy native WinUI visual baseline.
`PB-000` and Milestone 0 block all production visual claims, component grade
promotions, and broad renderer work. Later milestones may use current artifacts
as harness smoke evidence, but not as proof of native WinUI parity.

## Assumptions

- The product remains Wine-free on macOS.
- The product targets source-level WinUI-style compatibility, not `.exe`,
  `.msix`, or arbitrary Windows binary execution.
- Public GitHub Actions Windows runners remain the intended visual reference
  source of truth, but the workflow must launch and capture the actual public
  WinUI fixture projects rather than `WindowsNativeProbe` synthetic drawings.
- Public, clean-room fixtures are acceptable for production evidence when they
  are representative and documented.
- Every production claim must map to tests, docs, component evidence, or public
  visual artifacts.
- No private repositories, private screenshots, private product names, secrets,
  proprietary fixture content, or copied WinUI Gallery fixture content may be
  introduced.

## Non-Goals

- Do not add Windows binary compatibility.
- Do not claim full WinUI 3, Fluent, Mica, Acrylic, compositor, WebView2, media,
  or packaged app parity unless the exact supported subset is implemented and
  evidenced.
- Do not hide weak components behind whole-screenshot thresholds.
- Do not replace the existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG,
  current Skia, `skia-v2`, existing fixtures, or public admin/workbench source
  ingestion paths.
- Do not make broad renderer rewrites before defining component-level exit
  criteria and tests.

## Production Definition

The project is production-ready only when all of these are true:

1. A production compatibility contract defines exactly what is supported,
   partial, planned, Windows-only, not supported, and out of scope.
2. The production subset has no uncataloged API usage in the selected public
   fixture corpus.
3. Claimed supported components have component evidence with no unexpected
   `not-rendered` entries and no `poor` grades.
4. Any `weak` component grade is either fixed, explicitly excluded from the
   production claim, or documented as a production-tier limitation.
5. XAML, resource, template, visual-state, input, accessibility, and project
   ingestion behavior needed by the production subset has public tests.
6. Public Windows reference workflows pass and artifacts are inspected for every
   changed visual category; production visual claims require native WinUI
   fixture references, not synthetic probe screenshots.
7. Performance, flake-rate, artifact-size, and release gates are documented and
   enforced.
8. Security, privacy, supply-chain, package provenance, semver, rollback, and
   support policy are documented.

## Milestones

### Milestone 0: Replace Synthetic References With Native WinUI References

Blockers addressed: `PB-000`, `PB-003`, `PB-008`

Tasks:

- Redesign `windows-native-screenshot.yml` so the Windows reference job builds,
  launches, drives, and captures the actual public WinUI fixture projects for
  all reference-backed scenarios.
- Keep `WindowsNativeProbe` only as a synthetic harness smoke fallback, and label
  its artifacts as non-parity probe output.
- Add reference provenance metadata that records the captured executable type,
  fixture project path, scenario path, commit SHA, runner image, viewport, scale,
  theme, and whether the reference is native WinUI or synthetic probe output.
- Update artifact schemas and docs so `windows-reference.png` cannot be assumed
  to be native unless provenance says so.
- Regenerate or quarantine checked-in visual examples that currently come from
  synthetic probe output.
- Make component grade promotion impossible unless the scenario has native
  Windows reference provenance.

Exit criteria:

- Public workflow artifacts can distinguish `native-winui` references from
  `synthetic-probe` references.
- Component parity lab scenarios that claim visual quality use native WinUI
  fixture screenshots.
- Existing synthetic examples remain clearly labeled as harness smoke only or
  are replaced by native WinUI examples.

### Milestone 1: Freeze The Production Claim

Blockers addressed: `PB-001`, `PB-012`

Tasks:

- Add `docs/release/production-compatibility-contract.md`.
- Define the first production subset by component, XAML construct, renderer
  behavior, interaction action, artifact, and unsupported category.
- Update `docs/compatibility/matrix.md`,
  `docs/compatibility/component-support.md`, and
  `docs/compatibility/api-catalog.md` so the production subset is explicit.
- Add a status rule: production-supported APIs and components must have tests,
  docs, and evidence.

Exit criteria:

- No production claim is broader than public tests and public evidence.
- `docs/release/production-readiness.md` can link to a concrete production
  contract rather than an alpha-only readiness assessment.

### Milestone 2: Close Catalog And Diagnostic Unknowns

Blockers addressed: `PB-001`, `PB-007`, `PB-012`

Tasks:

- Expand `winui-api-compatibility.catalog.json` and
  `winui-component-inventory.json` for the chosen production subset.
- Add tests that fail when production fixture usage produces `unknown`
  diagnostics.
- Make uncataloged production-scope API usage fail strict mode with actionable
  diagnostics.
- Keep planned, Windows-only, not-supported, and excluded features explicit.

Exit criteria:

- Selected production fixtures produce no unknown API or component usage.
- All unsupported usage has stable diagnostics and documentation.

### Milestone 3: Move Weak Checked-In Components To Usable Or Excluded

Blockers addressed: `PB-002`, `PB-003`, `PB-008`

Tasks:

- Prioritize weak checked-in evidence items:
  `CommandBar`, `AppBarButton`, `AppBarButton.Icon`, `Grid`, `Border`,
  `FontIcon`, and `Image`.
- Add component-region evidence or crop metrics for these components so
  whole-screen thresholds cannot mask local regressions.
- Improve facade behavior, layout metadata, renderer output, and scenario
  assertions only where needed for the production subset.
- Update `component-evidence.json` expectations and docs only after native WinUI
  Windows reference artifacts justify stronger grades.

Exit criteria:

- Production-claimed components are at least `usable`.
- Any remaining `weak` grades are excluded from production or explicitly
  documented as production-tier limitations.

### Milestone 4: Expand High-Value Component Coverage

Blockers addressed: `PB-002`, `PB-008`

Tasks:

- Add fixture and catalog coverage for the next high-value controls:
  `AutoSuggestBox`, `MenuFlyout`, `DropDownButton`, `SplitButton`, `Slider`,
  `NumberBox`, `PasswordBox`, `DatePicker`, and `TabView`.
- Prefer narrow clean-room scenarios over broad galleries.
- Add interaction requirements and visual grades for each new component.
- Keep not-yet-implemented controls diagnostic-only until renderer and runtime
  behavior is real.

Exit criteria:

- The production component subset is represented by fixture pages, scenario
  requirements, docs, tests, and public visual evidence.

### Milestone 5: Implement Production XAML, Theme, Template, And Visual-State Subset

Blockers addressed: `PB-004`, `PB-005`

Tasks:

- Define the minimal production subset for `ControlTemplate`, `DataTemplate`,
  `ResourceDictionary.ThemeDictionaries`, `ThemeResource`, `StaticResource`,
  `Style`, `Setter`, and visual states.
- Implement only the subset required by production fixtures.
- Add strict diagnostics for unsupported template, visual-state, and theme
  behavior.
- Add deterministic material/composition placeholders or a documented
  production exclusion for Mica, Acrylic, system backdrops, shadows,
  transforms, and motion.

Exit criteria:

- Production fixtures do not depend on silently ignored template, theme, or
  visual-state behavior.
- Materials/composition are either implemented for the production subset or
  explicitly excluded from the production claim.

### Milestone 6: Harden Input, Accessibility, And Text Behavior

Blockers addressed: `PB-006`

Tasks:

- Expand scripted actions for keyboard routing, tab navigation, key presses,
  pointer state, scrolling, selection, and wait-for-idle behavior.
- Improve focus, enabled, selected, checked, value, and role/name/help text
  accessibility artifacts.
- Define the text editing production subset: caret, selection, typing,
  password masking, validation state, wrapping, and font fallback.
- Add tests that cover successful and failing interaction/accessibility paths.

Exit criteria:

- Production interaction scenarios are deterministic and failure messages are
  actionable.
- Accessibility output for the production subset is stable and documented.

### Milestone 7: Add A Public Downstream App Corpus

Blockers addressed: `PB-007`, `PB-008`

Tasks:

- Add a public clean-room downstream app corpus that represents real app shapes
  without private names or proprietary content.
- Include at least:
  - a shell/workbench app,
  - a data-entry app,
  - a list/detail app,
  - a command-heavy app,
  - a themed/resource-heavy app.
- Run each corpus app through source ingestion, interactions, visual evidence,
  component evidence, diagnostics, and accessibility checks.
- Add workflow coverage for corpus scenarios.

Exit criteria:

- Production claims are backed by more than isolated component fixtures.
- Corpus failures are structured and documented.

### Milestone 8: Add Performance, Reliability, And Artifact Gates

Blockers addressed: `PB-009`

Tasks:

- Add performance benchmarks for:
  - runner startup,
  - compat shadow build generation,
  - XAML compilation,
  - layout/render,
  - interaction execution,
  - artifact generation.
- Add flake tracking for strict visual and interaction scenarios.
- Add artifact-size limits and retention guidance.
- Document acceptable thresholds and update CI gates.

Exit criteria:

- Production readiness includes objective performance and reliability numbers,
  not only correctness tests.

### Milestone 9: Complete Release, Security, And Support Hardening

Blockers addressed: `PB-010`, `PB-011`, `PB-012`

Tasks:

- Add `docs/release/production-release-policy.md`.
- Add security and privacy guidance for building and executing user source
  projects locally and in CI.
- Document package provenance, signing or verification strategy, semver,
  changelog requirements, rollback path, support window, and issue triage.
- Add final release checklist and package smoke automation.

Exit criteria:

- A production release can be shipped with documented support boundaries,
  provenance, rollback, and security posture.

### Milestone 10: Final Production Readiness Gate

Blockers addressed: all blockers

Tasks:

- Run the full local gate.
- Trigger public Windows reference workflows for every changed visual scenario,
  and verify that production claims use `native-winui` reference provenance.
- Download and inspect all relevant `windows-reference.png`,
  `mac-runtime.png`, `pixel-diff.png`, `visual-run.json`, and
  `component-evidence.json` artifacts.
- Confirm every production visual claim has `native-winui` reference provenance.
- Update checked-in evidence only from public artifacts.
- Re-run private-name denylist scan.
- Update `docs/release/production-readiness.md` from "not production-ready" to
  the exact production-ready claim only if every blocker is closed or scoped out
  by the production contract.

Exit criteria:

- `docs/release/production-readiness.md` accurately states production readiness
  with no unresolved blocking items for the claimed production subset.

## Verification Gates

Run the relevant subset after each milestone and the full gate before any
production-ready claim.

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ControlGallery.MacTest/ControlGallery.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/ControlGallery.MacTest/scenarios/control-gallery-high-contrast.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --strict-visual
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
```

For component lab scenarios, run every
`fixtures/ComponentParityLab.WinUI/scenarios/*.json` strict visual scenario.

When visual scenarios or renderer behavior change:

1. Trigger `windows-native-screenshot.yml`.
2. Wait for completion.
3. Download artifacts.
4. Inspect the relevant `windows-reference.png`, `mac-runtime.png`,
   `pixel-diff.png`, `visual-run.json`, and `component-evidence.json` files.
5. Update checked-in examples only when they are public, generic, and free of
   private or proprietary content.

Before any production-ready claim, also run:

- package pack smoke for all published packages;
- public workflow verification;
- private-name denylist scan;
- performance and flake gates added by this plan;
- security/release checklist added by this plan.

## Risks

- Production scope can drift too broad. Mitigation: freeze the production
  contract before implementing broad renderer work.
- Visual thresholds can become a hiding place for component regressions.
  Mitigation: component-region evidence and `component-evidence.json` must gate
  component claims.
- Public Windows runner image changes can shift references. Mitigation: record
  workflow metadata, inspect artifacts, and avoid threshold changes without
  evidence.
- Template and theme support can become overly broad. Mitigation: implement only
  the subset needed by production fixtures and diagnose the rest.
- Performance gates can reveal architectural limitations late. Mitigation: add
  benchmarks before expanding the fixture corpus too far.

## Rollback Or Recovery

- Keep each milestone independently shippable.
- Revert only the milestone-specific commit if a change destabilizes existing
  alpha behavior.
- Preserve existing runner, doctor, current Skia, `skia-v2`, and public fixture
  paths while adding production gates.
- If a visual workflow fails, download artifacts first, classify the failure,
  and only then adjust scenarios, thresholds, or renderer behavior.
- If a production claim cannot be backed by evidence, update docs to scope the
  claim down rather than weakening gates.

## Affected Files And Areas

- `docs/release/production-readiness.md`
- `docs/release/production-compatibility-contract.md`
- `docs/release/production-release-policy.md`
- `docs/compatibility/matrix.md`
- `docs/compatibility/component-support.md`
- `docs/compatibility/api-catalog.md`
- `docs/compatibility/winui-api-compatibility.catalog.json`
- `docs/compatibility/winui-component-inventory.json`
- `docs/architecture/artifacts.md`
- `docs/visual-parity/README.md`
- `.github/workflows/windows-native-screenshot.yml`
- `fixtures/ComponentParityLab.WinUI`
- future public downstream corpus fixtures under `fixtures`
- `src/WinUI3.MacCompat`
- `src/WinUI3.MacRuntime`
- `src/WinUI3.MacXaml`
- `src/WinUI3.MacRenderer.Skia`
- `src/WinUI3.MacRunner`
- `tests`

## Execution Prompt

```text
/goal Use $google-eng-practices and implement docs/plans/2026-06-01-production-readiness-roadmap-plan.md in the public MarlonJD/winui3-mac-test-runtime repository.

The goal is to move the library from evidence-backed alpha to production-ready for a clearly documented public source-level WinUI 3 compatibility subset. Start by replacing the synthetic WindowsNativeProbe visual references with native WinUI Windows captures of the actual public WinUI fixture projects; do not freeze production claims, promote component grades, or begin broad renderer rewrites until the reference source of truth is fixed. Work milestone by milestone, keeping each change small, testable, and backed by public fixtures, docs, and evidence. At the end of every milestone, run the milestone's relevant verification gate, commit only that milestone's relevant files with author marlonjd <burak.karahan@mail.ru> using a Conventional Commit message, and push immediately before starting the next milestone.

Preserve the Wine-free macOS runtime, existing winui3-mac-doctor, winui3-mac-runner, SVG, current Skia, skia-v2, existing fixtures, public admin/workbench source ingestion, and the ComponentParityLab.WinUI foundation. Do not use private repositories, private screenshots, private product names, secrets, proprietary fixture content, or copied WinUI Gallery fixture content. Keep identifiers, source comments, and canonical docs in English.

For every supported production claim, require matching compatibility catalog entries, tests, docs, public fixture coverage, and component or visual evidence. Whole-screenshot pass is not enough: component-evidence.json must remain the component-level source of truth, and visibly weak or poor components must stay labeled weak or poor until public native WinUI Windows reference artifacts justify stronger grades. Synthetic WindowsNativeProbe artifacts may be kept only as harness smoke evidence and must be labeled as synthetic-probe, not native WinUI visual parity evidence.

Address the production blockers from docs/release/production-readiness.md in order, starting with PB-000 native WinUI Windows reference source of truth. Then address API/catalog coverage, not-rendered controls, weak visuals, templates/theme/visual states, material/composition policy, input/accessibility, project ingestion matrix, public downstream app corpus, performance/reliability gates, release hardening, security/supply-chain posture, and production support documentation.

Run targeted tests while working and the full verification gate from the plan before any production-ready claim. If visual scenarios, reference capture, or renderer behavior change, trigger windows-native-screenshot.yml, wait for completion, download artifacts, and inspect the relevant windows-reference.png, mac-runtime.png, pixel-diff.png, visual-run.json, component-evidence.json, and reference provenance metadata before handoff. Do not batch multiple completed milestones into one commit; each completed milestone must have its own focused commit and push. Final handoff must list every milestone completed, its commit SHA, verification run, reference provenance status, and any remaining blockers.
```
