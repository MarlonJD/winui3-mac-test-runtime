# Settings Policy Editor Fixture Refresh Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` or `superpowers:subagent-driven-development` before implementing this plan. Track task progress with the checkboxes below.

## Date

2026-06-03

## Owner Subtree

Cross-cutting fixture and native visual parity infrastructure:

- `fixtures/ComponentParityLab.WinUI`
- `fixtures/PublicAdminWorkbench.WinUI`
- `tests/WinUI3.MacRuntime.Tests`
- `tests/WinUI3.MacXaml.Tests`
- `docs/visual-parity`
- `.github/workflows/windows-native-screenshot.yml` only if the Windows capture workflow needs a narrow scenario coverage update

## Goal

Replace weak public native-reference evidence with credible, dense WinUI fixture evidence:

- `component-layout-media-light` must stop producing a blank or placeholder-like Layout / Media reference.
- `public-admin-workbench-light` must become a Settings / Policy Editor example with many form components, toggles, combos, validation states, and sections.
- Native-reference readiness must remain full GO for the current expected row count before any renderer work is considered.

The current source readiness result, 58/58 ready/verified rows, is necessary but not sufficient. This work improves the quality of the native reference targets while preserving the rule that anything below full readiness is NO-GO for renderer purposes.

## Assumptions And Open Questions

- The selected product direction is **Settings / Policy Editor**.
- The fixture content must be clean-room and public; no private app data, real user names, or customer-specific policy text.
- The implementation must stay inside fixture, native capture, validation, workflow, test, and documentation/reporting infrastructure.
- Renderer/component implementation files must not be modified.
- Renderer parity work must not begin as part of this plan.
- The branch remains `test/windows-native-recapture-pipeline`; do not merge to `main`, do not push to `main`, and do not open a PR.
- Commit author must be exactly `marlonjd <burak.karahan@mail.ru>`.

## Non-Goals

- Do not change Mac renderer or component runtime behavior.
- Do not broaden WinUI compatibility abstractions to support this fixture.
- Do not add speculative fixture controls that cannot be captured reliably.
- Do not claim renderer parity GO from native-reference readiness.
- Do not stage local shadow-build directories or unrelated generated artifacts.

## Current Problems

`component-layout-media-light`:

- The full-page Windows reference is mostly white.
- Media, web, ink, and diagnostic rows can look like empty boxes or tiny placeholder text.
- Several crops are technically ready but not useful for visual inspection.

`public-admin-workbench-light`:

- The page is too sparse for a public admin fixture.
- It does not exercise enough dense form controls or validation states.
- The current review-queue page is a weak example for future visual parity review.

## Target UX Direction

### Settings / Policy Editor

Keep the fixture as a desktop operational tool, but replace the sparse review queue with a dense policy editing surface:

- Left `NavigationView` shell with policy areas such as Overview, Access policy, Review workflow, Audit, and Notifications.
- Header row with page title, status, and `CommandBar` actions.
- Filter row with `TextBox`, `ComboBox`, validation `InfoBar`, and progress/completeness signal.
- Split body with `ListView` policy selection on the left and sectioned settings editor on the right.
- Sections for access policy, review policy, risk controls, notifications, and audit logging.
- Named visible targets for `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `ToggleSwitch`, `Slider`, `ProgressBar`, `InfoBar`, `ListView`, `CommandBar`, `AppBarButton`, and `Button`.
- Validation examples such as warning info bars, required-field helper text, disabled/pending apply state, and risk labels.

Keep the page dense but readable in the existing capture viewport. Avoid a marketing layout; this should look like a real Windows admin settings surface.

### Layout / Media

Keep the Layout / Media page as a component parity fixture, but make every media-like target visually meaningful:

- Add or reuse a deterministic public fixture image asset with visible content.
- Show a bounded image preview, media poster, playback/progress row, and descriptive status.
- Represent unavailable media/web/ink surfaces with visible diagnostic panels rather than blank controls.
- Use swatches, sample typography, icon samples, and bounded preview panels for resource/theme rows.
- Prefer stable fixture visuals over real network or playback dependencies in CI.

## Affected Files

Likely files:

- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml`
- `fixtures/PublicAdminWorkbench.WinUI/MainWindow.xaml.cs`
- `fixtures/PublicAdminWorkbench.WinUI/NativeReferenceTargetExporter.cs`
- `fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json`
- `fixtures/ComponentParityLab.WinUI/Pages/LayoutMediaPage.xaml`
- `fixtures/ComponentParityLab.WinUI/Pages/LayoutMediaPage.xaml.cs`
- `fixtures/ComponentParityLab.WinUI/NativeControlSamples.cs`
- `fixtures/ComponentParityLab.WinUI/NativeReferenceTargetExporter.cs`
- `fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json`
- `fixtures/ComponentParityLab.WinUI/Assets/*`
- `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`
- Generated reporting files under `docs/visual-parity/**`

Files that must not be modified:

- `src/WinUI3.MacRenderer.Skia/**`
- renderer/component implementation files
- unrelated runtime implementation files

## Implementation Tasks

### Task 1: Add Evidence-Quality Tests

- [ ] Add a runtime test that rejects blank or near-blank canonical public Windows references, including `component-layout-media-light` and `public-admin-workbench-light`.
- [ ] Add a small deterministic image sampling helper that calculates visual variation from sampled pixels.
- [ ] Make the assertion require meaningful variation, for example `Assert.IsGreaterThan(variationRatio, 0.01, ...)`.
- [ ] Add a test that requires `public-admin-workbench-light` scenario requirements to include the dense Settings / Policy Editor target set.
- [ ] Run the focused tests before fixture changes and confirm they fail for the current weak evidence.

Focused command:

```bash
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "PublicNativeReferenceScreenshotsAreNotBlank|PublicAdminWorkbench"
```

### Task 2: Replace Public Admin With Settings / Policy Editor

- [ ] Update `MainWindow.xaml` to keep the `NavigationView` shell and replace the sparse review queue content with the selected Settings / Policy Editor page.
- [ ] Add named targets for the form-heavy control set: `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `ToggleSwitch`, `Slider`, `ProgressBar`, `InfoBar`, `ListView`, `CommandBar`, `AppBarButton`, and `Button`.
- [ ] Populate list/combo items in code-behind when XAML item syntax is fragile in the shadow compiler.
- [ ] Use existing supported WinUI fixture patterns first. If a property is not supported by the facade/compiler, move fixture-only setup to code-behind rather than changing renderer/component implementation.
- [ ] Update `public-admin-workbench-light.json` requirements to point at meaningful named controls.
- [ ] Update `NativeReferenceTargetExporter.cs` only if new names or bounds need explicit native export support.

Smoke command:

```bash
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- run --project fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --output /private/tmp/public-admin-policy-editor-smoke --diff-output /private/tmp/public-admin-policy-editor-smoke
```

Expected: base run passes. Visual comparison can fail until Windows references are recaptured for the new fixture.

### Task 3: Make Layout / Media Visibly Contentful

- [ ] Add a simple public fixture asset, such as `fixtures/ComponentParityLab.WinUI/Assets/PublicPolicyPreview.svg`, with visible shapes, color, and media-like poster content.
- [ ] Replace the blank `PublicImagePlaceholder` usage with a bounded `PublicImagePreview` or equivalent visible target.
- [ ] Update `NativeControlSamples.PopulateLayoutAndMedia` so diagnostic surfaces are useful:
  - `MediaPlayerElement`: poster-style panel with title, play glyph, and progress/status.
  - `WebView2`: bounded web preview panel with fake address row and content preview.
  - `InkCanvas` / `InkToolbar`: visible unavailable or sample-stroke panel.
  - resource/theme rows: visible swatches and text samples.
- [ ] Keep layout within the capture viewport so targets do not become offscreen-only crops.
- [ ] Update `component-layout-media-light.json` and native target export mapping if target names change.

Smoke command:

```bash
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- run --project fixtures/ComponentParityLab.WinUI/ComponentParityLab.WinUI.csproj --renderer skia-v2 --scenario fixtures/ComponentParityLab.WinUI/scenarios/component-layout-media-light.json --output /private/tmp/layout-media-rich-smoke --diff-output /private/tmp/layout-media-rich-smoke
```

Expected: base run passes. Visual comparison can fail until Windows references are recaptured for the new fixture.

### Task 4: Regenerate Local Evidence

- [ ] Regenerate local evidence for `component-layout-media-light`.
- [ ] Regenerate local evidence for `public-admin-workbench-light`.
- [ ] Regenerate `native-reference-readiness`, `component-quality-dashboard`, and `visual-review-index` artifacts.
- [ ] Inspect the full screenshots and crop sheets manually before commit. The Layout / Media page must no longer be blank, and Public Admin must visibly read as a Settings / Policy Editor.
- [ ] Treat a partial quality/dashboard state as acceptable only for renderer/manual parity gates, not for native-reference readiness.

Representative commands:

```bash
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- native-reference-readiness
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- component-quality-dashboard
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- visual-review-index --output docs/visual-parity
```

### Task 5: Verify Before Commit

- [ ] Run focused runtime tests.
- [ ] Run full runtime tests.
- [ ] Run XAML tests.
- [ ] Run generated artifact checks.
- [ ] Run `git diff --check`.
- [ ] Confirm no renderer/component implementation files were changed.

Required commands:

```bash
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- native-reference-readiness --check
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- component-quality-dashboard --check
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- visual-review-index --output docs/visual-parity --check
git diff --check
```

### Task 6: Commit And Push To The Temporary Branch

- [ ] Confirm branch is `test/windows-native-recapture-pipeline`.
- [ ] Stage only relevant fixture, native capture, validation, workflow, test, and documentation/reporting infrastructure files.
- [ ] Before committing, show:

```bash
git diff --cached --name-only
```

- [ ] Confirm staged files are limited to the allowed scope.
- [ ] Confirm no renderer/component implementation files are staged.
- [ ] Commit with required author and a Conventional Commit message that names the blocker addressed, for example:

```bash
git commit --author="marlonjd <burak.karahan@mail.ru>" -m "fix(visual-parity): replace weak native fixture evidence"
```

- [ ] Push only to:

```bash
git push origin test/windows-native-recapture-pipeline
```

### Task 7: Windows Recapture And Evidence Bundle

- [ ] Trigger or verify `windows-native-screenshot.yml` for the pushed branch commit.
- [ ] Confirm the run uses the latest `test/windows-native-recapture-pipeline` commit.
- [ ] Confirm the `windows-reference-screenshots` artifact exists and is not expired.
- [ ] Download the artifact.
- [ ] Import native references.
- [ ] Recompute readiness.
- [ ] Produce a `/private/tmp` evidence ZIP and README with run id, branch, commit, artifact status, import status, readiness totals, and a note that renderer parity was not started.

Representative commands:

```bash
gh workflow run windows-native-screenshot.yml --ref test/windows-native-recapture-pipeline
gh run watch <run-id> --exit-status
gh run view <run-id> --json databaseId,headSha,headBranch,status,conclusion,event,url
gh run download <run-id> --name windows-reference-screenshots --dir /private/tmp/winui-run-<run-id>-windows
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- native-reference-import --source /private/tmp/winui-run-<run-id>-windows --output /private/tmp/winui-run-<run-id>-windows/native-reference-import
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- native-reference-readiness --check
```

## Verification Gates

Implementation is complete only when all gates pass:

- Layout / Media full-page Windows reference is not blank or near-blank.
- Layout / Media media/web/ink/resource crops are visually meaningful enough for inspection.
- Public Admin full-page Windows reference clearly shows a dense Settings / Policy Editor.
- Public Admin scenario requirements include the dense form/control target set.
- Latest Windows native artifact imports with 0 missing component scenarios and 0 problems.
- Native-reference readiness is full GO for the current expected row count. If the row count grows beyond 58, every row in the new total must be ready/verified.
- Runtime and XAML tests pass.
- Generated artifact checks pass.
- No renderer/component implementation files are changed or staged.
- Commit author and push branch follow policy.
- No PR is opened.

## Risks

- Adding too many scenario requirements can increase the readiness row count. This is acceptable only if the new total is fully ready.
- Some WinUI controls may not capture reliably in CI. Prefer bounded, visible fixture diagnostics over blank controls.
- The shadow compiler may reject unsupported XAML properties. Use fixture code-behind rather than renderer/runtime implementation changes.
- Dense UI can produce offscreen targets if the layout is too tall. Keep required targets visible in the capture viewport or export intentionally visible bounds.
- Local visual comparisons may fail until the Windows artifact is recaptured; that is expected, but readiness must return to full GO after artifact import.

## Rollback / Recovery

- If a new control prevents Windows capture, remove it from scenario requirements or represent it with a visible diagnostic fixture panel.
- If generated docs become noisy, revert generated artifacts and regenerate only after smoke tests pass.
- If readiness falls below full GO, stop before renderer work and fix fixture target export, scenario requirements, or Windows capture.
- Use a normal revert commit if a pushed fixture change must be backed out.

## Execution Prompt

Use `$google-eng-practices`, `$windows-winui3-design`, `superpowers:executing-plans`, `superpowers:test-driven-development`, and `superpowers:verification-before-completion`. Implement the plan at `docs/plans/2026-06-03-settings-policy-editor-fixture-plan.md` on branch `test/windows-native-recapture-pipeline`. Keep changes limited to fixture, native capture, validation, workflow, test, and documentation/reporting infrastructure. Do not modify renderer/component implementation files, do not begin renderer parity work, do not merge to `main`, do not push to `main`, and do not open a PR. Before each commit, show `git diff --cached --name-only`, confirm staged files are limited to the allowed scope, and confirm no renderer/component implementation files are staged. Commit with author `marlonjd <burak.karahan@mail.ru>` and a Conventional Commit message that explains the blocker addressed. Push only to `test/windows-native-recapture-pipeline`. After pushing, trigger or verify `windows-native-screenshot.yml`, confirm the run uses the latest branch commit, confirm `windows-reference-screenshots` exists, download artifacts, import native references, recompute readiness, produce a `/private/tmp` ZIP and README, and report GO only if the current expected readiness total is fully ready/verified.
