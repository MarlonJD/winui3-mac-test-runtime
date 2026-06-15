# Renderer Fidelity Recovery Plan

Date: 2026-06-02

Owner subtree: `tools/winui3-mac-test-runtime`

## Goal

Move the project from release-gate-ready harness scaffolding toward credible
WinUI visual rendering by improving the current `skia-v2` renderer for the
most visible public component scenarios, regenerating stale artifacts, and
only promoting visual claims after screenshot inspection against native WinUI
references.

## Assumptions And Open Questions

- The current catalog, scenario, release-gate, and evidence infrastructure is
  useful and should remain in place.
- The current renderer is not production-fidelity WinUI. The latest inspected
  local artifact set has 138 component evidence rows: 79 `usable` and 59
  `not-rendered`.
- The first recovery pass should target user-visible controls already present
  in public fixtures instead of expanding the API catalog.
- Open question: whether the project wants `usable` to remain a harness grade
  or introduce a separate `visual-quality` field so functionally usable and
  visually close controls are not conflated.

## Scope

- Regenerate stale local artifacts, especially
  `component-status-pickers-success-light`, and update any checked-in evidence
  that contradicts scenario requirements.
- Improve `skia-v2` painters for the highest-value controls:
  `Button`, `ToggleButton`, `CheckBox`, `RadioButton`, `ComboBox`, `TextBox`,
  `CommandBar`, `AppBarButton`, `InfoBar`, `ProgressBar`, `ProgressRing`,
  `NavigationView`, `NavigationViewItem`, `ListView`, `ItemsControl`,
  `ContentDialog`, `Flyout`, `ToolTip`, `Border`, `Grid`, `StackPanel`,
  `ScrollViewer`, `FontIcon`, and `Image`.
- Preserve existing support boundaries for `planned`, `windows-only`, and
  `not supported` entries.
- Add or tighten component-crop assertions only after renderer output is
  visibly improved.

## Non-Goals

- Do not add new broad WinUI controls before the existing visible Ring 0 and
  claimed Ring 1 controls look credible.
- Do not claim Mica, Acrylic, compositor effects, media playback, WebView2,
  real templates, virtualization, or OS integration.
- Do not loosen pixel thresholds to make screenshots pass.
- Do not market this as arbitrary WinUI 3 application compatibility.

## Steps

1. Regenerate Current Evidence

   - Run the current public strict scenarios with `--renderer skia-v2`.
   - Regenerate `artifacts/winui3-mac/component-status-pickers-success-light`
     and any other stale scenario whose artifact rows contradict scenario JSON.
   - Verify that supported or partial scenario requirements do not produce
     `not-rendered` rows unless the scenario itself is corrected to stop
     claiming support.

2. Establish A Visual Baseline

   - Create a current artifact summary that lists every scenario, component,
     catalog status, visual grade, and known gap.
   - Compare current macOS screenshots against the latest native Windows
     reference artifacts for the same public scenarios.
   - Identify the top visual misses by user impact: missing chrome, wrong
     spacing, missing state glyphs, missing icons, incorrect popup surface,
     and missing selected/focused/disabled states.

3. Improve Core Control Painters

   - Update `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs` and
     related theme helpers to render credible Fluent-like control chrome for
     basic input, forms, command surfaces, status/progress, navigation, and
     collection rows.
   - Keep implementation direct and local to the renderer until duplication or
     complexity justifies helper extraction.
   - Add tests around deterministic component evidence and known key states.

4. Tighten Evidence After Visual Improvements

   - Promote rows only when inspected screenshots show meaningful component
     output and crop metadata exists.
   - Add stricter crop thresholds for improved components.
   - Keep planned and diagnostic rows `not-rendered`.

5. Refresh Documentation And Examples

   - Update `docs/visual-parity/examples` only from public, native-provenance
     artifacts.
   - Keep historical examples labeled historical or replace them with current
     artifacts after inspection.
   - Update README, support policy, component support, production evidence, and
     visual readiness inventory so claims match artifacts.

## Verification Gates

- `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj`
- `dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj`
- Targeted `winui3-mac-runner run --renderer skia-v2 --strict-visual` commands
  for each changed scenario.
- `winui3-mac-runner catalog-audit --check`
- `winui3-mac-runner release-candidate`
- Manual screenshot inspection for every promoted visual claim.

## Risks

- Improving painter fidelity may shift layout and break existing snapshot
  assumptions.
- Native Windows reference artifacts can drift when the hosted runner image
  changes.
- The current `usable` grade mixes functional smoke usability with visual
  quality; the project may need an additional quality dimension.
- Renderer work can become broad quickly; keep each pass limited to a small
  component family and its scenarios.

## Rollback Or Recovery

- If a painter change worsens screenshots, revert only that renderer change and
  keep the evidence/docs boundary corrections.
- If a scenario claim is too optimistic, lower the scenario requirement instead
  of weakening gates.
- If native reference comparison is unavailable, keep the artifact as local
  smoke evidence and do not promote visual fidelity.

## Affected Files Or Docs

- `src/WinUI3.MacRenderer.Skia/SkiaV2SnapshotRenderer.cs`
- `src/WinUI3.MacRenderer.Skia/SkiaV2Theme.cs`
- `src/WinUI3.MacRenderer.Skia/ComponentCropper.cs`
- `fixtures/ComponentParityLab.WinUI/scenarios/*.json`
- `artifacts/winui3-mac/**`
- `docs/visual-parity/**`
- `docs/compatibility/component-support.md`
- `docs/compatibility/production-component-targets.md`
- `docs/compatibility/visual-readiness-inventory.json`
- `docs/release/production-evidence-view.md`
- `docs/release/final-production-gate.md`
- `README.md`

## Execution Prompt

Use `$google-eng-practices` and implement
`docs/plans/2026-06-02-renderer-fidelity-recovery-plan.md`. Keep the work
renderer-first: regenerate stale artifacts, improve only the highest-impact
`skia-v2` control painters needed for the targeted scenarios, and update claims
only after inspecting macOS screenshots against native WinUI references. Do not
expand the API catalog or loosen thresholds. Verify with
`dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj`,
`dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj`,
targeted `winui3-mac-runner run --renderer skia-v2 --strict-visual` commands,
`winui3-mac-runner catalog-audit --check`, and
`winui3-mac-runner release-candidate`. Commit only relevant files with author
`marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message, then
push immediately.
