# Component Basic Input Light Native Quality Closure Plan

Date: 2026-06-05

Owner subtree: `tools/winui3-mac-test-runtime`

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> `superpowers:executing-plans` or `superpowers:subagent-driven-development` to
> implement this plan task-by-task. Use `emsi-workflows:emsi-task-router` before
> editing and `emsi-workflows:emsi-verification-gate` before claiming
> completion.

## Objective

Close the `component-basic-input-light` first native-quality tranche by moving
the first-tranche controls from source-level readiness to evidence-backed
native-quality review readiness, then promote only rows whose Windows native
crop, macOS runtime crop, pixel diff, provenance, interaction/accessibility
evidence, and manual inspection notes support the grade.

The north-star outcome is a bounded, honest first tranche:

- all first-tranche rows have matched native/runtime crop dimensions;
- first-order renderer blockers are reduced or explicitly documented;
- target rows that pass thresholds and manual inspection may be promoted;
- uncertain rows remain `nativeQualityGrade: not-evaluated`;
- `releaseAllowed=false` and `publishAllowed=false` remain unchanged unless a
  separate release permission task explicitly changes them.

## Architecture

The work is split into renderer fidelity, evidence generation, and inspection
application. Renderer work stays in shared `skia-v2` Fluent primitives where
possible. Evidence work uses strict visual runs against checked-in native
Windows references. Promotion is not automatic: inspection metadata is the final
gate.

## Scope

In scope:

- Scenario:
  `fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json`.
- First-tranche rows:
  `Button`, `RepeatButton`, `ToggleButton`, `HyperlinkButton`, `CheckBox`,
  `RadioButton`, `ComboBox`, `DropDownButton`, `SplitButton`,
  `ToggleSplitButton`, `Slider`, `ToggleSwitch`, and `RatingControl`.
- Renderer files under `src/WinUI3.MacRenderer.Skia/`.
- Runtime/evidence plumbing only when needed to preserve diagnostics or crop
  accuracy.
- Tests under `tests/WinUI3.MacRuntime.Tests/`.
- Evidence notes under `docs/visual-parity/` and plan notes under `docs/plans/`.

Out of scope:

- Broad WinUI compatibility claims.
- Any release or publish permission change.
- Bundling proprietary Windows fonts.
- Reclassifying rows to hide renderer gaps.
- Loosening visual thresholds.
- Branch creation, branch switching, committing, or pushing unless the user asks
  for that action in the current task.

## Current Evidence Baseline

Use this as the starting point unless a newer native-backed run exists:

- Current native-backed evidence:
  `artifacts/native-quality-tranche/check-radio-chrome-native`.
- Previous native-backed evidence:
  `artifacts/native-quality-tranche/button-text-baseline-native`.
- `Slider` and `ToggleSwitch` pass component crop thresholds.
- `Button`, `RepeatButton`, and `ToggleButton` improved in the button text
  tranche and must not regress.
- `CheckBox` improved from `18.046875` to `15.312500` changed-pixel percentage,
  but still narrowly fails MAE (`12.013672` against threshold `12`).
- `RadioButton` improved from `27.734375` to `20.338542` changed-pixel
  percentage, but still fails changed-pixel threshold.
- Font diagnostics are emitted in `snapshot.json`, `visual/visual-run.json`, and
  `visual/component-evidence.json`; fonts remain secondary evidence for this
  tranche.

## Assumptions And Open Questions

- Native Windows crops remain the visual source of truth.
- Crop/layout mismatch is no longer the primary blocker for the first-tranche
  rows that have already been worked.
- Text fallback to Helvetica is a measured secondary gap, not the main blocker
  for CheckBox/RadioButton chrome.
- Promotion may be `good` before `production-ready` if default-state evidence is
  strong but interaction/state coverage is not broad enough.
- Open question: which row should be the first actual promotion candidate once
  thresholds and inspection notes support it. Likely candidates are `Slider`,
  `ToggleSwitch`, and possibly `CheckBox` after the MAE gap closes.

## Phase 0: Freeze And Compare The Latest Baseline

Goal: prevent new work from drifting away from the latest evidence.

Steps:

- [ ] Confirm current branch without creating or switching branches:
  `git branch --show-current`.
- [ ] Confirm dirty worktree and identify unrelated existing changes:
  `git status --short`.
- [ ] Run:
  `PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check`.
- [ ] Run:
  `PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check`.
- [ ] Read the latest target metrics from
  `artifacts/native-quality-tranche/check-radio-chrome-native/visual/component-evidence.json`.
- [ ] Confirm every first-tranche row still reports
  `nativeReferenceBoundsDelta.width == 0` and
  `nativeReferenceBoundsDelta.height == 0`.
- [ ] Confirm all rows remain `nativeQualityGrade: not-evaluated` before
  promotion work starts.

Exit criteria:

- Baseline is current and machine-readable.
- No branch action has occurred.
- No release or publish gate has changed.

## Phase 1: Close The CheckBox And RadioButton Threshold Gap

Goal: finish the nearest renderer closure target before moving to broader rows.

Primary rows:

- `CheckBox` / `EnabledCheckBox`
- `RadioButton` / `HighPriorityRadioButton`

Steps:

- [ ] Inspect native/macOS/diff crops for both rows in
  `artifacts/native-quality-tranche/check-radio-chrome-native/visual/components/`.
- [ ] Write failing tests before production edits for the next visible blocker.
  Expected likely tests:
  - CheckBox MAE/chrome compactness around the tick and checked box edge.
  - RadioButton selected ring/dot edge treatment and native center knockout
    geometry.
- [ ] Adjust only shared CheckBox/RadioButton drawing or their renderer offsets.
- [ ] Do not touch Button, RepeatButton, ToggleButton, Slider, or ToggleSwitch.
- [ ] Run focused tests until green.
- [ ] Run the native-backed strict visual command:

  ```sh
  PATH="$PWD/tools:$PATH" winui3-mac-runner run \
    --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj \
    --renderer skia-v2 \
    --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json \
    --strict-visual \
    --reference docs/visual-parity/examples/component-basic-input-light \
    --output artifacts/native-quality-tranche/check-radio-threshold-closure-native
  ```

Exit criteria:

- CheckBox MAE is at or below threshold, or the remaining miss is documented as
  antialias/template fidelity.
- RadioButton changed-pixel percentage is reduced without regressing
  non-target rows.
- Slider and ToggleSwitch still pass.
- Button-family metrics do not regress.

## Phase 2: Finish Dropdown, Split, Combo, And Rating Renderer Fidelity

Goal: reduce remaining first-tranche renderer blockers after CheckBox/RadioButton
are stable.

Target rows:

- `ComboBox` / `StatusComboBox`
- `DropDownButton` / `DiagnosticDropDownButton`
- `SplitButton` / `DiagnosticSplitButton`
- `ToggleSplitButton` / `DiagnosticToggleSplitButton`
- `RatingControl` / `DiagnosticRatingControl`

Steps:

- [ ] Inspect latest native/macOS/diff crops for these rows.
- [ ] Add focused tests for the first visible blocker per row family:
  - dropdown chevron placement and stroke,
  - split separator opacity/position,
  - selected/checked split fill behavior,
  - rating star size, spacing, fill/stroke, and edge treatment.
- [ ] Implement narrow shared primitive changes.
- [ ] Avoid changing already-improved Button, RepeatButton, ToggleButton,
  CheckBox, RadioButton, Slider, and ToggleSwitch behavior.
- [ ] Run native-backed strict visual evidence to a new artifact directory.

Exit criteria:

- Each target row improves or has a bounded documented blocker.
- No first-tranche crop dimension regressions.
- No pass-row regressions.

## Phase 3: Button-Family Residual Fidelity Sweep

Goal: decide whether Button, RepeatButton, ToggleButton, and HyperlinkButton are
reviewable or still blocked by text/font/template fidelity.

Target rows:

- `Button` / `PrimaryActionButton`
- `RepeatButton` / `DiagnosticRepeatButton`
- `ToggleButton` / `PinnedToggleButton`
- `HyperlinkButton` / `DiagnosticHyperlinkButton`

Steps:

- [ ] Compare latest metrics against `button-text-baseline-native`.
- [ ] Inspect the crop triptychs for text baseline, text rasterization, edge
  stroke, bottom edge, checked fill, and hyperlink typography.
- [ ] Add tests only for renderer-controlled geometry/chrome gaps.
- [ ] Do not chase proprietary font parity by committing fonts.
- [ ] Document whether remaining diff is font/text rasterization or renderer
  template fidelity.

Exit criteria:

- Existing button-family improvements do not regress.
- Rows are either reviewable for `good` or explicitly blocked.

## Phase 4: Manual Inspection And Candidate Promotion

Goal: promote only evidence-backed rows and leave uncertain rows untouched.

Candidate rows:

- Start with `Slider` and `ToggleSwitch` because they already pass thresholds.
- Add `CheckBox` only if Phase 1 closes the MAE gap or manual inspection accepts
  the remaining gap as bounded.
- Add other rows only if strict evidence and manual inspection support them.

Steps:

- [ ] Open and inspect the latest generated `visual-review.html`.
- [ ] For every candidate row, inspect native Windows crop, macOS crop, and diff
  crop side by side.
- [ ] Record reviewer, date, native reference run ID, artifact paths, accepted
  gaps, and grade rationale in `component-inspection.json`.
- [ ] Apply inspection metadata through runner tooling.
- [ ] Regenerate dashboard and review index.
- [ ] Keep uncertain rows at `nativeQualityGrade: not-evaluated`.

Exit criteria:

- Every promoted row has manual inspection notes tied to visible evidence.
- No row is promoted solely because a scenario-level strict run passed.
- Release and publish gates remain false.

## Phase 5: Final Evidence Sync And Readiness Narrative

Goal: make the repository tell one consistent story.

Steps:

- [ ] Update `docs/visual-parity/font-resolution-diagnostics.md` with bounded
  latest metrics and remaining blockers.
- [ ] Update this plan with completed phase notes.
- [ ] Update generated dashboard/review artifacts only through runner tooling.
- [ ] Confirm `releaseAllowed=false` and `publishAllowed=false`.
- [ ] Confirm no proprietary fonts are present in the diff.

Exit criteria:

- Docs, generated evidence, dashboard, and inspection metadata agree.
- Native-quality claims are scoped to rows that were actually promoted.

## Verification Gates

Run before claiming completion:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --output artifacts/native-quality-tranche/component-basic-input-light-closure
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/native-quality-tranche/component-basic-input-light-closure-native
```

Manual evidence to inspect:

- `artifacts/native-quality-tranche/component-basic-input-light-closure-native/visual/components/*/windows-reference.png`
- `artifacts/native-quality-tranche/component-basic-input-light-closure-native/visual/components/*/mac-runtime.png`
- `artifacts/native-quality-tranche/component-basic-input-light-closure-native/visual/components/*/pixel-diff.png`

## Risks And Mitigations

- Risk: overfitting one crop causes non-target regressions.
  Mitigation: compare every first-tranche row after each visual run.
- Risk: text/font differences get mistaken for renderer bugs.
  Mitigation: preserve font diagnostics and document font gaps separately.
- Risk: a row is promoted from default-state evidence only.
  Mitigation: require manual inspection notes and keep claims at `good` unless
  state/interaction evidence supports more.
- Risk: thresholds are loosened to make progress look better.
  Mitigation: never change thresholds in this plan.
- Risk: branch, commit, or push actions happen implicitly.
  Mitigation: follow local `AGENTS.md`; do not perform branch, commit, or push
  operations unless the user explicitly asks in the current task.

## Dependencies And Ownership Boundaries

- Renderer owner subtree: `tools/winui3-mac-test-runtime`.
- Renderer implementation:
  `src/WinUI3.MacRenderer.Skia/`.
- Evidence/runtime plumbing:
  `src/WinUI3.MacRuntime/` and `src/WinUI3.MacRunner/`.
- Tests:
  `tests/WinUI3.MacRuntime.Tests/`.
- Visual references and docs:
  `docs/visual-parity/`.
- Native Windows reference crops:
  `docs/visual-parity/examples/component-basic-input-light/`.

No backend, API contract, mobile app, or production release ownership boundary is
changed by this plan.

## Affected Files Or Docs

Likely implementation files:

- `src/WinUI3.MacRenderer.Skia/FluentDrawingPrimitives.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`

Possible evidence/doc files:

- `docs/visual-parity/font-resolution-diagnostics.md`
- `docs/visual-parity/examples/component-basic-input-light/component-inspection.json`
- `docs/visual-parity/examples/component-basic-input-light/component-evidence.json`
- `docs/visual-parity/component-quality-dashboard.json`
- `docs/visual-parity/public-visual-review-index.json`
- `docs/visual-parity/public-visual-review-index.html`
- `docs/plans/2026-06-05-component-basic-input-light-native-quality-closure-plan.md`

## Rollback And Recovery

- If a renderer tweak regresses a non-target pass row, revert the narrow tweak
  or split the primitive behavior by control family.
- If a native-backed run worsens target rows, keep the generated artifact for
  diagnosis but do not update docs as a success.
- If manual inspection does not support promotion, leave the row at
  `nativeQualityGrade: not-evaluated` and document the blocker.
- If a generated dashboard or inspection artifact drifts unexpectedly, rerun the
  runner command that owns it rather than editing generated JSON by hand.

## Execution Prompt

Paste this into a fresh Codex task:

```text
Continue from:
docs/plans/2026-06-05-component-basic-input-light-native-quality-closure-plan.md
docs/plans/2026-06-04-renderer-fidelity-native-quality-promotion-plan.md
docs/visual-parity/font-resolution-diagnostics.md

Goal:
Execute the component-basic-input-light native-quality closure plan as one larger objective: make the first-tranche rows evidence-backed and reviewable for native-quality promotion, starting by closing the CheckBox/RadioButton threshold gap and then moving through dropdown/split/rating/button-family residual fidelity. Promote only rows whose native Windows crop, macOS runtime crop, diff, provenance, manual inspection, and state/interaction evidence support the grade.

Rules:
- Do not create, switch, rename, or delete branches.
- Do not commit or push unless I explicitly ask in the current task.
- Do not loosen thresholds.
- Do not bundle proprietary Windows fonts.
- Preserve font diagnostics in snapshot.json, visual-run.json, and component-evidence.json.
- Keep releaseAllowed=false and publishAllowed=false.
- Keep uncertain rows at nativeQualityGrade=not-evaluated.
- Treat fonts as measured secondary evidence unless an icon-font row is actually in scope.
- Avoid regressions to Button, RepeatButton, ToggleButton, Slider, and ToggleSwitch when working on CheckBox/RadioButton.

Required skills:
- emsi-workflows:emsi-task-router
- superpowers:systematic-debugging
- superpowers:test-driven-development
- emsi-workflows:emsi-verification-gate
- superpowers:verification-before-completion

Start with Phase 0 and Phase 1 of the plan:
1. Confirm dashboard/catalog baseline.
2. Inspect latest native-backed crops under artifacts/native-quality-tranche/check-radio-chrome-native/visual/components/.
3. Add focused failing tests for the next visible CheckBox/RadioButton blocker.
4. Implement only tightly scoped skia-v2 CheckBox/RadioButton primitive or renderer-offset changes.
5. Run focused tests, full runtime tests, renderer-only strict visual, and native-backed strict visual.
6. Update plan/docs only with bounded claims.

Verification:
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --output artifacts/native-quality-tranche/component-basic-input-light-closure
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario ./fixtures/ComponentParityLab.WinUI/scenarios/component-basic-input-light.json --strict-visual --reference docs/visual-parity/examples/component-basic-input-light --output artifacts/native-quality-tranche/component-basic-input-light-closure-native

Success criteria:
- First-tranche crop dimensions do not regress.
- Slider and ToggleSwitch remain passing.
- Button, RepeatButton, and ToggleButton metrics do not regress.
- CheckBox closes or clearly documents its remaining MAE gap.
- RadioButton changed-pixel percentage improves toward threshold without non-target regressions.
- Any promoted row has manual inspection notes and evidence-backed rationale.
- No release/publish gate changes.
```

## 2026-06-05 Execution Status

Completed Phase 0 through Phase 5 for the first tranche without branch, commit,
push, threshold-loosening, or proprietary-font changes.

Evidence generated:

- Phase 1 native-backed closure:
  `artifacts/native-quality-tranche/check-radio-threshold-closure-native`.
- Phase 2 dropdown/split/rating closure:
  `artifacts/native-quality-tranche/dropdown-split-rating-closure-native`.
- Phase 3 button-family closure:
  `artifacts/native-quality-tranche/button-family-closure-native`.
- External-font A/B evidence:
  `artifacts/native-quality-tranche/font-ab-external-native`.
- Canonical docs evidence:
  `docs/visual-parity/examples/component-basic-input-light/component-evidence.json`.
- Manual review:
  `docs/visual-parity/examples/component-basic-input-light/component-inspection.json`.

Promoted rows:

| Row | Latest status | Native-quality grade | Rationale |
| --- | ---: | --- | --- |
| `CheckBox` / `EnabledCheckBox` | passed (`14.765625`, MAE `10.186328`) | `good` | checked box/tick, label inset, crop dimensions, provenance, and default checked light-state triptych support bounded promotion |
| `Slider` / `DiagnosticSlider` | passed (`11.666667`, MAE `6.485851`) | `good` | default value light-state crop passes thresholds with bounded inactive-track/thumb-ring gaps |
| `ToggleSwitch` / `DiagnosticToggleSwitch` | passed (`11.296296`, MAE `8.258829`) | `good` | default on light-state crop passes thresholds with bounded text/font and track/thumb antialias gaps |
| `RatingControl` / `DiagnosticRatingControl` | passed (`13.385417`, MAE `6.450911`) | `good` | four-of-five light-state crop passes after star scale, spacing, and empty-stroke updates |

Rows explicitly left blocked at `nativeQualityGrade: not-evaluated`:

- `RadioButton` improved from `20.338542` to `19.375` changed-pixel
  percentage, but still fails the changed-pixel threshold.
- `HyperlinkButton` improved from `21.725` to `18.75` after centered text
  placement in the default-font native-backed run, but still fails the `18`
  changed-pixel threshold. Repo-external Segoe UI evidence now passes at
  `13.95`, so the row remains environment-dependent and
  `nativeQualityGrade: not-evaluated`.
- `Button`, `RepeatButton`, `ToggleButton`, `ComboBox`, `DropDownButton`,
  `SplitButton`, and `ToggleSplitButton` retain matching crop dimensions but
  still fail on text/font rasterization, border/template antialiasing, or
  native template fidelity.

Licensed-font A/B follow-up:

- Repo-external fonts in `/Users/marlonjd/winui-font-ab` resolved correctly:
  text uses `Segoe UI Variable`, symbol uses `Segoe Fluent Icons`.
- External fonts materially reduced text-heavy drift but did not create any new
  default-font promotion-ready row. After centered text placement,
  `HyperlinkButton` passes only with repo-external Segoe UI evidence at
  `13.95`; default-font evidence still fails at `18.75`.

Dashboard state after regeneration:

- 58 public component rows.
- 4 rows at `visualGrade: good` and `nativeQualityGrade: good`.
- 54 rows remain `nativeQualityGrade: not-evaluated`.
- 0 missing inspections and 0 dashboard blockers.
- `releaseAllowed=false` and `publishAllowed=false` remain unchanged.
