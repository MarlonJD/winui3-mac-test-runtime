# Final Production Readiness Gate Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `docs/release`, `docs/compatibility`,
`docs/architecture`, `docs/consumption`, `src`, `fixtures`, `tests`,
`.github/workflows`

## Goal

Verify that the current public `MarlonJD/winui3-mac-test-runtime` repository is
ready to present its documented Levels 0 through 7 compatibility claim as an
alpha production-readiness milestone.

This plan is a quality gate, not a broad feature expansion. The output should be
evidence: passing local checks, passing public Windows reference workflow,
inspected visual artifacts, clean documentation claims, clean private-name scan,
and a short release-readiness update if any evidence changed.

## Success Criteria

- The repository remains clean and synchronized with `origin/main` before final
  handoff.
- Build, test, runner smoke, strict visual smoke, Windows tooling build, native
  probe build, and package smoke checks pass.
- Public `windows-native-screenshot` workflow passes on GitHub Actions.
- Downloaded workflow artifacts contain reviewable Windows reference, macOS
  runtime, and pixel diff PNGs for every strict fixture category.
- At least one reference/runtime/diff PNG from each fixture category is
  inspected before handoff.
- `README.md`, `docs/compatibility/contracts.md`,
  `docs/compatibility/matrix.md`, `docs/architecture/artifacts.md`,
  `docs/consumption/quick-start.md`, and
  `docs/release/level-7-release-readiness.md` agree on the public claim and
  known limits.
- The operator-provided private-name denylist scan returns no matches.
- No private repositories, private screenshots, private product names, secrets,
  or proprietary fixture content are introduced.

## Assumptions

- The current compatibility target is an alpha public runtime, not full WinUI 3
  parity.
- Existing behavior for `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current
  Skia, and `skia-v2` must be preserved.
- The macOS-managed runtime remains Wine-free.
- `windows-latest` public GitHub Actions runs remain the Windows reference
  source of truth for visual artifacts.
- The operator will provide the actual private-name denylist regex when running
  the final gate.

## Open Questions

- Should the next published version remain `0.1.0-alpha.1`, or should verified
  Level 7 readiness move package docs to a new prerelease version?
- Should all workflow artifacts be retained as release evidence only in GitHub
  Actions, or should selected metadata summaries be copied into docs without
  storing PNG baselines in the repository?

## Scope

- Local and CI verification.
- Artifact inspection.
- Compatibility claim audit.
- Release-readiness documentation updates when evidence or known gaps change.
- Targeted fixes only for failures found during the gate.

## Non-Goals

- Adding new WinUI controls or XAML constructs unless required to fix a failing
  documented Level 0 through Level 7 contract.
- Broad renderer refactors or visual redesign.
- Storing private or Windows reference screenshots in the repository.
- Introducing Wine, Windows binary execution, `.msix` support, or arbitrary
  `.exe` compatibility.
- Claiming complete WinUI 3, Windows App SDK, Fluent compositor, Mica, Acrylic,
  or arbitrary pixel parity.

## Steps

### Phase 1: Repository And Claim Audit

- Confirm the worktree is clean and on the intended public branch.
- Read the current README, compatibility contracts, compatibility matrix,
  artifact docs, consumer quick start, and release readiness document.
- Check that the public claim is consistent: Levels 0 through 7 are supported
  only for documented public fixture-backed surfaces.
- Check that known limits are visible wherever a consumer might otherwise infer
  full WinUI 3 or Windows binary compatibility.

Verification:

- No unrelated staged or unstaged files exist.
- Documentation uses English for canonical docs.
- No docs claim broader compatibility than tests and fixtures support.

### Phase 2: Local Verification Matrix

- Run the full local build and test suite.
- Run source-checkout doctor and runner smoke commands.
- Run strict `skia-v2` visual scenarios for shell, interaction/binding, and
  control gallery.
- Build Windows capture and native probe projects.
- Run package smoke for each published package.

Verification:

- Every command exits successfully.
- If a command fails, inspect the failure, make the smallest relevant fix, and
  rerun only the affected checks before returning to the full gate.
- Do not loop on the same failure without new evidence.

### Phase 3: Public Windows Reference Gate

- Trigger the public `windows-native-screenshot` workflow.
- Wait for completion and require a passing result.
- Download the `windows-native-screenshot` artifact bundle.
- Inspect at least one `windows-reference.png`, `mac-runtime.png`, and
  `pixel-diff.png` from each changed or covered fixture category:
  shell, interaction/binding, and control gallery.
- Review `pixel-diff.json` and `visual-run.json` summaries for unexpected
  threshold drift or skipped comparisons.

Verification:

- Workflow succeeds.
- Artifact dimensions and scenario names match expectations.
- Visual differences are explainable and within scenario-local thresholds.

### Phase 4: Privacy, Publicness, And Artifact Hygiene

- Run the operator-provided private-name denylist scan.
- Inspect changed docs, fixtures, scenario JSON, workflow YAML, and release
  notes for accidental private content.
- Confirm generated artifacts and package outputs remain ignored or outside the
  committed set.

Verification:

- Private-name scan returns no matches.
- `git status --short` contains only intentional source/doc changes.
- No screenshots, secrets, or build outputs are staged.

### Phase 5: Release Evidence Update

- If workflow run IDs, package smoke evidence, known gaps, or verification
  results changed, update `docs/release/level-7-release-readiness.md`.
- If compatibility claims changed, update README and compatibility docs in the
  same commit.
- Keep changes factual and tied to evidence gathered in this gate.

Verification:

- Documentation references exact commands or workflow run IDs when useful.
- Docs do not overstate full WinUI 3 compatibility.
- `git diff --check` passes before commit.

### Phase 6: Commit And Handoff

- Stage only relevant files.
- Inspect staged diff.
- Commit with author `marlonjd <burak.karahan@mail.ru>` and a Conventional
  Commit message.
- Push immediately.
- Final handoff should list verification commands run, workflow run ID,
  artifact categories inspected, and any residual risks.

Verification:

- `git status --short --branch` is clean and aligned with `origin/main`.
- Commit author is correct.
- Push succeeds.

## Verification Gates

Run from the repository root:

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
dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release
dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release
dotnet pack src/WinUI3.MacTest.Sdk/WinUI3.MacTest.Sdk.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacCompat/WinUI3.MacCompat.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRuntime/WinUI3.MacRuntime.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacXaml/WinUI3.MacXaml.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRenderer.Skia/WinUI3.MacRenderer.Skia.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRunner/WinUI3.MacRunner.csproj --configuration Release --output ./artifacts/packages
rg -n "<private-name-denylist-regex>" .
```

Run for the public Windows reference check:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot-final-gate
```

Inspect PNG artifacts before final handoff:

- `shell-light/windows-reference.png`
- `shell-light/mac-runtime.png`
- `shell-light/pixel-diff.png`
- `interactions-light/windows-reference.png`
- `interactions-light/mac-runtime.png`
- `interactions-light/pixel-diff.png`
- `control-gallery-light/windows-reference.png`
- `control-gallery-light/mac-runtime.png`
- `control-gallery-light/pixel-diff.png`

## Risks And Mitigations

- Risk: A hosted runner image update changes Windows reference output.
  Mitigation: inspect downloaded PNGs and metadata before changing thresholds.
- Risk: The docs imply full WinUI 3 or binary compatibility.
  Mitigation: keep compatibility claims tied to documented levels and public
  fixture-backed subsets.
- Risk: Package smoke passes locally but consumer install docs are stale.
  Mitigation: verify the package quick start and update release readiness notes
  with exact evidence.
- Risk: Generated artifacts are accidentally committed.
  Mitigation: inspect staged files and commit only source/docs/workflow changes.
- Risk: Private names leak through fixtures or release notes.
  Mitigation: run the operator-provided denylist and use only generic public
  fixture content.

## Rollback And Recovery

- If verification exposes a real regression, fix only the failing contract and
  keep the production claim unchanged until evidence passes.
- If the workflow fails due infrastructure or runner drift, preserve artifacts,
  document the cause, and avoid threshold changes until visual inspection
  confirms the drift is acceptable.
- If package readiness cannot be proven, downgrade the Level 7 claim in docs
  rather than shipping an unsupported claim.
- If privacy scan finds a match, remove or replace the content with generic
  public fixture data before any commit.

## Affected Files And Docs

- `README.md`
- `docs/compatibility/contracts.md`
- `docs/compatibility/matrix.md`
- `docs/architecture/artifacts.md`
- `docs/consumption/quick-start.md`
- `docs/release/level-7-release-readiness.md`
- `docs/examples/consumer-github-actions.yml`
- `.github/workflows/windows-native-screenshot.yml`
- `fixtures/*`
- `tests/*`
- `src/*`

## Execution Prompt

Use `$google-eng-practices` and execute `docs/plans/2026-06-01-final-production-readiness-gate-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. The goal is to verify and, only if needed, minimally fix the alpha production-readiness gate for the documented Level 0 through Level 7 source-level WinUI compatibility claim. Keep the macOS-managed runtime Wine-free. Preserve existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current Skia, and `skia-v2` behavior. Do not use private repositories, private screenshots, private product names, secrets, or proprietary fixture content. Keep identifiers, comments, and canonical docs in English.

Start by auditing the current README, compatibility contracts, compatibility matrix, artifact docs, consumer quick start, release readiness notes, workflows, fixtures, and tests. Do not expand the support claim or add new WinUI controls unless a failing documented contract requires the smallest targeted fix. Run the local verification gate from the plan: `dotnet build`, `dotnet test`, `PATH="$PWD/tools:$PATH" winui3-mac-doctor`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia`, strict `skia-v2` runs for `SampleAdminShell`, `InteractionBindingApp`, and both `ControlGallery` light and high-contrast scenarios, `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release`, and all package smoke commands listed in the plan.

Run `rg -n "<private-name-denylist-regex>" .` with the operator-provided private-name denylist. Trigger the public `windows-native-screenshot.yml` workflow, wait for it to finish, download the `windows-native-screenshot` artifacts, and inspect at least one `windows-reference.png`, `mac-runtime.png`, and `pixel-diff.png` for `shell-light`, `interactions-light`, and `control-gallery-light` before final handoff. If evidence changes, update `docs/release/level-7-release-readiness.md`; if claims or artifact contracts are corrected, update the matching README and docs in the same change.

Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately. Final handoff must include the verification commands run, public workflow run ID, artifact categories inspected, private-name scan result, commit SHA, and any residual risks.
