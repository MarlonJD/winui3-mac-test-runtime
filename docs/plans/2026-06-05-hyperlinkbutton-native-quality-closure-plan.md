# HyperlinkButton Native-Quality Closure Plan

Date: 2026-06-05

Owner subtree: `tools/winui3-mac-test-runtime`

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> `superpowers:executing-plans` or `superpowers:subagent-driven-development` to
> implement this plan task-by-task. Use `emsi-workflows:emsi-task-router` before
> editing and `emsi-workflows:emsi-verification-gate` before claiming
> completion.

## Objective

Close the residual `HyperlinkButton` fidelity gap in
`component-basic-input-light` using native-backed evidence, default-font and
repo-external-font comparisons, and manual triptych inspection. Promote the row
only if native Windows crop, macOS runtime crop, diff, provenance, interaction
evidence, and review notes support the bounded grade.

## Architecture

This is a narrow renderer-fidelity task. The likely implementation surface is
`RenderHyperlinkButton` in `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
plus focused renderer tests in `tests/WinUI3.MacRuntime.Tests/`. Font behavior is
already instrumented through `FontResolver` and `SnapshotFontDiagnostics`; this
plan uses that provenance rather than adding font files to the repository.

## Scope

In scope:

- Scenario:
  `fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json`.
- Target row:
  `HyperlinkButton` / `DiagnosticHyperlinkButton`.
- Non-regression checks for first-tranche rows already promoted or improved:
  `CheckBox`, `Slider`, `ToggleSwitch`, `RatingControl`, `Button`,
  `RepeatButton`, `ToggleButton`, and `RadioButton`.
- External-font diagnostic evidence using repo-external fonts at
  `/Users/marlonjd/winui-font-ab`, when present.
- Documentation updates in `docs/visual-parity/font-resolution-diagnostics.md`
  and this plan status section.

Out of scope:

- Bundling, copying, committing, packaging, or redistributing Windows font files.
- Loosening visual thresholds.
- Promoting rows other than `HyperlinkButton`.
- Claiming `production-ready` from default-state evidence.
- Branch creation, branch switching, committing, or pushing unless explicitly
  requested in the current task.
- Broad hyperlink states not represented by the current scenario, such as
  visited, pressed, focus-visible, disabled, high contrast, or keyboard state.

## Current Baseline

Default-font native-backed evidence:

- `artifacts/native-quality-tranche/font-ab-default-native`
- Font diagnostics: text resolves to `Helvetica` with
  `resolvedSource: platform-default`; symbol resolves to `Helvetica` with
  `resolvedSource: text-font-fallback`.
- `HyperlinkButton` remains failed:
  - changed-pixel percentage: `21.725`
  - MAE: `12.877375`

External-font native-backed evidence:

- `artifacts/native-quality-tranche/font-ab-external-native`
- Font diagnostics: text resolves to `Segoe UI Variable` from
  `external-font-directory`; symbol resolves to `Segoe Fluent Icons` from
  `external-font-directory`.
- `HyperlinkButton` remains failed but is very close:
  - changed-pixel percentage: `18.025`
  - MAE: `10.621188`
  - threshold changed-pixel percentage: `18`

The external-font result proves text font fidelity is material, but it does not
by itself justify native-quality promotion. The remaining gap is likely
HyperlinkButton template fidelity: text origin, vertical baseline, color,
antialiasing, padding, or default underline/focus assumptions.

## Assumptions And Open Questions

- Native Windows crop remains the visual source of truth.
- The target should pass in both default-font and external-font runs if the row
  is to be promoted without an environment caveat.
- If only the external-font run passes, the row must stay `not-evaluated` unless
  reviewers explicitly accept an environment-dependent `good` grade with clear
  provenance notes.
- The current scenario captures the default enabled light state only; it is not
  sufficient for `production-ready`.
- Open question: whether native default HyperlinkButton text is offset by one
  subpixel/one pixel compared with the current renderer, or whether the last
  `0.025` changed-pixel gap is mostly antialias/color.

## Phase 0: Baseline Freeze

Goal: make sure the worker starts from known evidence and does not accidentally
promote stale data.

Steps:

- [ ] Confirm current branch without creating or switching branches:
  `git branch --show-current`.
- [ ] Confirm dirty worktree and preserve unrelated user changes:
  `git status --short`.
- [ ] Run:
  `PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check`.
- [ ] Run:
  `PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check`.
- [ ] Read the default-font target row from:
  `artifacts/native-quality-tranche/font-ab-default-native/visual/component-evidence.json`.
- [ ] Read the external-font target row from:
  `artifacts/native-quality-tranche/font-ab-external-native/visual/component-evidence.json`.
- [ ] Confirm both evidence files report `nativeReferenceBoundsDelta.width == 0`
  and `nativeReferenceBoundsDelta.height == 0` for `HyperlinkButton`.
- [ ] Confirm `releaseAllowed=false` and `publishAllowed=false` in production
  gate artifacts.

Exit criteria:

- Baseline metrics and provenance are known.
- No branch or release-gate operation occurred.

## Phase 1: Manual Pixel Evidence Review

Goal: identify the remaining HyperlinkButton difference before changing code.

Steps:

- [ ] Inspect the default-font triptych:
  `artifacts/native-quality-tranche/font-ab-default-native/visual/components/hyperlinkbutton-diagnostichyperlinkbutton/triptych.png`
  if present, or the three crop files in that directory.
- [ ] Inspect the external-font triptych:
  `artifacts/native-quality-tranche/font-ab-external-native/visual/components/hyperlinkbutton-diagnostichyperlinkbutton/triptych.png`
  if present, or the three crop files in that directory.
- [ ] Compare native and macOS text bounding pixels:
  - leftmost blue text pixel,
  - topmost blue text pixel,
  - bottommost blue text pixel,
  - visible underline or near-baseline blue pixels,
  - color delta against native link blue.
- [ ] Record the observed hypothesis in this plan under an execution status
  section before editing renderer code.

Exit criteria:

- A single testable hypothesis exists, such as "runtime link text is one pixel
  too low" or "runtime link color is too saturated".
- No renderer change has been made yet.

## Phase 2: TDD Renderer Adjustment

Goal: make one minimal HyperlinkButton renderer correction with an automated
guard.

Steps:

- [ ] Add a focused failing test in
  `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`. Use existing
  `SkiaV2SnapshotRenderer...HyperlinkButton...` helpers and patterns near the
  current HyperlinkButton underline test.
- [ ] The test must assert one concrete visual property derived from Phase 1,
  for example:
  - no default underline band,
  - expected native-like link text origin,
  - expected native-like text color,
  - expected absence/presence of pixels outside the native text band.
- [ ] Run the focused test and confirm it fails for the intended reason:
  `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter HyperlinkButton`.
- [ ] Implement the smallest renderer change in
  `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`.
- [ ] Re-run the focused test and confirm it passes.
- [ ] If the first hypothesis does not improve native-backed metrics, revert the
  experimental renderer change and document the rejected hypothesis. Do not
  stack unrelated adjustments.

Exit criteria:

- Focused test passes.
- The code change is limited to the proven HyperlinkButton behavior.

## Phase 3: Native-Backed Evidence Runs

Goal: prove whether the adjustment closes the gap and whether it depends on
external fonts.

Steps:

- [ ] Run default-font strict visual evidence:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference docs/visual-parity/examples/component-basic-input-light \
  --output artifacts/native-quality-tranche/hyperlinkbutton-closure-native
```

- [ ] Run external-font strict visual evidence:

```sh
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference docs/visual-parity/examples/component-basic-input-light \
  --output artifacts/native-quality-tranche/hyperlinkbutton-closure-external-native
```

- [ ] Compare `HyperlinkButton` against the two baselines:
  - default baseline `21.725`, MAE `12.877375`;
  - external baseline `18.025`, MAE `10.621188`.
- [ ] Confirm no crop-size regression for all first-tranche rows.
- [ ] Confirm no changed-pixel or MAE regression for already promoted rows:
  `CheckBox`, `Slider`, `ToggleSwitch`, `RatingControl`.
- [ ] Confirm font diagnostics remain present in:
  - `snapshot.json`,
  - `visual/visual-run.json`,
  - `visual/component-evidence.json`.

Exit criteria:

- Evidence exists for default-font and external-font runs.
- The target either passes thresholds or remains explicitly blocked with fresh
  metrics.

## Phase 4: Promotion Decision

Goal: update review metadata only if the evidence is strong enough.

Promotion rules:

- Promote `HyperlinkButton` only if:
  - native/runtime crop dimensions match,
  - strict crop thresholds pass,
  - native reference provenance is ready,
  - macOS runtime crop and pixel diff are present,
  - manual triptych inspection supports the bounded grade,
  - state/interaction evidence for this scenario is present,
  - font provenance is explicitly documented.
- If default-font evidence fails but external-font evidence passes, prefer
  leaving `nativeQualityGrade: not-evaluated` and documenting the row as
  environment-dependent unless the user explicitly accepts an
  environment-dependent bounded grade.
- Do not promote to `production-ready`.

Steps:

- [ ] If promotion is justified, update
  `docs/visual-parity/examples/component-basic-input-light/component-inspection.json`
  for only `HyperlinkButton`.
- [ ] Apply inspection to regenerate component evidence using the repo's
  existing inspection tooling.
- [ ] Regenerate visual review, component dashboard, and public review index.
- [ ] If promotion is not justified, leave `nativeQualityGrade: not-evaluated`
  and add/update blocker notes in docs.

Exit criteria:

- `HyperlinkButton` is either promoted with complete evidence or explicitly
  documented as still blocked.

## Phase 5: Verification Gate

Run:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --output artifacts/native-quality-tranche/hyperlinkbutton-closure
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference docs/visual-parity/examples/component-basic-input-light \
  --output artifacts/native-quality-tranche/hyperlinkbutton-closure-native
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
  --renderer skia-v2 \
  --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
  --strict-visual \
  --reference docs/visual-parity/examples/component-basic-input-light \
  --output artifacts/native-quality-tranche/hyperlinkbutton-closure-external-native
```

Expected outcomes:

- Dashboard and catalog checks pass.
- Tests pass.
- Non-native strict visual run passes.
- Native-backed runs may still return `visual-status: failed` if non-target
  first-tranche rows remain over threshold; this is acceptable only if
  `HyperlinkButton` status and blocker notes are correctly documented.

## Risks And Mitigations

- Risk: optimizing to external fonts hides a renderer issue.
  Mitigation: always run and report both default-font and external-font
  evidence.
- Risk: the final `0.025` changed-pixel miss is antialias noise rather than a
  template bug.
  Mitigation: require manual triptych inspection and avoid stacking speculative
  one-pixel nudges.
- Risk: changing HyperlinkButton text drawing regresses normal Button text.
  Mitigation: keep the implementation scoped to `RenderHyperlinkButton` and
  compare first-tranche metrics.
- Risk: proprietary fonts are accidentally added.
  Mitigation: keep fonts in `/Users/marlonjd/winui-font-ab` only and verify no
  `.ttf`, `.otf`, `.ttc`, `.woff`, or `.woff2` files appear under the repository.
- Risk: over-promotion from one default state.
  Mitigation: cap any accepted grade at `good`; leave `production-ready` out of
  scope.

## Dependencies And Ownership Boundaries

- Renderer: `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`.
- Font diagnostics: `src/WinUI3.MacRenderer.Skia/FontResolver.cs` and
  `src/WinUI3.MacRuntime/SnapshotFontDiagnostics.cs`.
- Evidence models and tooling: `src/WinUI3.MacRuntime/` and
  `src/WinUI3.MacRunner/`.
- Tests: `tests/WinUI3.MacRuntime.Tests/`.
- Scenario and native references:
  `fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json`
  and `docs/visual-parity/examples/component-basic-input-light/`.
- Documentation: `docs/visual-parity/font-resolution-diagnostics.md`,
  `docs/visual-parity/component-quality-dashboard.json`, and this plan.

## Affected Files Or Docs

Likely modified:

- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- `docs/visual-parity/font-resolution-diagnostics.md`
- `docs/visual-parity/examples/component-basic-input-light/component-inspection.json`
- `docs/visual-parity/examples/component-basic-input-light/component-evidence.json`
- `docs/visual-parity/examples/component-basic-input-light/visual-review.json`
- `docs/visual-parity/examples/component-basic-input-light/visual-review.html`
- `docs/visual-parity/component-quality-dashboard.json`
- `docs/visual-parity/public-visual-review-index.json`
- `docs/visual-parity/public-visual-review-index.html`

Possibly modified if evidence is regenerated:

- `docs/visual-parity/examples/component-basic-input-light/components/hyperlinkbutton-diagnostichyperlinkbutton/mac-runtime.png`
- `docs/visual-parity/examples/component-basic-input-light/components/hyperlinkbutton-diagnostichyperlinkbutton/pixel-diff.png`
- other first-tranche crop images if the whole scenario evidence is refreshed.

## Rollback And Recovery

- If the renderer adjustment worsens default-font or external-font metrics,
  revert that adjustment and keep only diagnostic/doc updates.
- If external-font evidence passes but default-font evidence regresses, do not
  promote. Document the environment dependency and continue with template/color
  analysis.
- If dashboard or catalog checks fail after evidence regeneration, inspect the
  generated diff and fix the artifact mismatch rather than weakening checks.
- Do not revert unrelated user changes in the dirty worktree.

## Execution Prompt

Continue from:

```text
docs/plans/2026-06-05-hyperlinkbutton-native-quality-closure-plan.md
docs/plans/2026-06-05-component-basic-input-light-native-quality-closure-plan.md
docs/visual-parity/font-resolution-diagnostics.md
```

Required skills:

```text
superpowers:using-superpowers
superpowers:executing-plans
superpowers:systematic-debugging
superpowers:test-driven-development
superpowers:verification-before-completion
emsi-workflows:emsi-task-router
emsi-workflows:emsi-verification-gate
```

Goal:

Execute the HyperlinkButton native-quality closure plan. Use the existing
default-font and repo-external-font evidence to close the residual
`HyperlinkButton` gap in `component-basic-input-light`. Start by manually
inspecting the default and external font triptychs, form one testable hypothesis,
then use TDD for any renderer adjustment. Promote `HyperlinkButton` only if the
native Windows crop, macOS runtime crop, diff, provenance, manual inspection, and
state/interaction evidence support the grade. If evidence remains uncertain,
keep `nativeQualityGrade=not-evaluated` and document the blocker.

Rules:

- Do not create, switch, rename, or delete branches.
- Do not commit or push unless explicitly asked in the current task.
- Do not loosen thresholds.
- Do not bundle, copy, commit, package, or redistribute proprietary Windows
  fonts.
- Use repo-external fonts only from `/Users/marlonjd/winui-font-ab` via
  `WINUI3_MAC_TEST_FONT_DIRS`.
- Preserve font diagnostics in `snapshot.json`, `visual-run.json`, and
  `component-evidence.json`.
- Keep `releaseAllowed=false` and `publishAllowed=false`.
- Keep uncertain rows at `nativeQualityGrade=not-evaluated`.
- Do not promote any row other than `HyperlinkButton`.

Verification:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --output artifacts/native-quality-tranche/hyperlinkbutton-closure
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/native-quality-tranche/hyperlinkbutton-closure-native
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/native-quality-tranche/hyperlinkbutton-closure-external-native
```

## 2026-06-05 Execution Status

Phase 0 baseline freeze completed on the current
`codex/source-level-rc-gate` branch without branch operations. The worktree was
already dirty with prior renderer, evidence, and docs changes; those changes
were preserved. `component-quality-dashboard --check` and `catalog-audit
--check` passed. Production gate artifacts still report
`releaseAllowed=false` and `publishAllowed=false`.

Phase 1 manual crop inspection reviewed the default-font and repo-external-font
HyperlinkButton native/runtime/diff crops:

- default-font native text pixels: bbox `x=12..112`, `y=10..23`, ink average
  approximately RGB `(32, 86, 160)`;
- default-font runtime text pixels: bbox `x=4..103`, `y=10..23`, ink average
  approximately RGB `(20, 115, 197)`;
- external-font native text pixels: bbox `x=12..112`, `y=10..23`, ink average
  approximately RGB `(32, 86, 160)`;
- external-font runtime text pixels: bbox `x=4..104`, `y=10..24`, ink average
  approximately RGB `(19, 114, 197)`.

Testable hypothesis: the residual HyperlinkButton gap is primarily caused by
renderer-controlled horizontal text placement. `RenderHyperlinkButton` draws
the label at `rect.Left + 4`, while the native crop centers the default link
text inside the 125 px control bounds. The vertical text band is already close
enough that the first renderer test should isolate horizontal centering rather
than stacking color or baseline changes.

Phase 2 TDD completed:

- Added `SkiaV2SnapshotRendererCentersDefaultHyperlinkButtonText` beside the
  existing no-underline HyperlinkButton guard.
- Verified RED with
  `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter HyperlinkButton`;
  the new test failed because the rendered text center was left of the control
  center.
- Implemented the minimal renderer change in `RenderHyperlinkButton`: measure
  the label bounds and center the X origin while preserving the existing
  baseline and enabled/disabled color behavior.
- Re-ran the same focused test filter and verified both HyperlinkButton tests
  passed.

Phase 3 native-backed evidence completed:

| Evidence | HyperlinkButton status | Changed pixels | MAE | RMS | Font provenance |
| --- | --- | ---: | ---: | ---: | --- |
| `artifacts/native-quality-tranche/hyperlinkbutton-closure-native` | failed | `18.75` | `9.976125` | `35.467300` | text `Helvetica` from `platform-default`; symbol `Helvetica` from `text-font-fallback` |
| `artifacts/native-quality-tranche/hyperlinkbutton-closure-external-native` | passed | `13.95` | `2.980438` | `12.268157` | text `Segoe UI Variable` and symbol `Segoe Fluent Icons` from `/Users/marlonjd/winui-font-ab` |

The centered text fix improved the default-font run from `21.725` to `18.75`
changed-pixel percentage and improved the repo-external-font run from `18.025`
to `13.95`. All checked first-tranche rows retained
`nativeReferenceBoundsDelta.width == 0` and
`nativeReferenceBoundsDelta.height == 0`; existing pass rows stayed passing.

Phase 4 decision: `HyperlinkButton` remains
`nativeQualityGrade: not-evaluated`. The external-font crop now passes strict
thresholds, but default-font evidence still fails the changed-pixel threshold
and the row is environment-dependent. Manual inspection and state/interaction
coverage therefore do not justify promotion in this task.

Phase 5 verification completed:

- `component-quality-dashboard --check` passed with 58 rows and 0 blocker rows.
- `catalog-audit --check` passed with 126 accounted entries and 0 unassigned
  entries.
- `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj`
  passed: 121/121 tests.
- Non-native strict visual evidence passed at
  `artifacts/native-quality-tranche/hyperlinkbutton-closure`.
- Default-font native-backed evidence refreshed at
  `artifacts/native-quality-tranche/hyperlinkbutton-closure-native`; the
  scenario visual status remains failed and `HyperlinkButton` remains failed at
  `18.75` changed pixels, MAE `9.976125`, RMS `35.467300`.
- Repo-external-font native-backed evidence refreshed at
  `artifacts/native-quality-tranche/hyperlinkbutton-closure-external-native`;
  the scenario visual status remains failed because other rows are still over
  threshold, but `HyperlinkButton` itself passes at `13.95` changed pixels, MAE
  `2.980438`, RMS `12.268157`.
- Font diagnostics are present in `snapshot.json`, `visual/visual-run.json`,
  and `visual/component-evidence.json` for both native-backed runs.
- `releaseAllowed=false` and `publishAllowed=false` remain unchanged, and no
  `.ttf`, `.otf`, `.ttc`, `.woff`, or `.woff2` files are present under the
  repository.
