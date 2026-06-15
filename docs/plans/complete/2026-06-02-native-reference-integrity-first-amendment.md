# Native Reference Integrity First Amendment

Date: 2026-06-02

Owner subtree: `tools/winui3-mac-test-runtime`

Parent plan: `docs/plans/2026-06-02-public-component-native-quality-execution-plan.md`

## Goal

Add a required Phase -1 native reference integrity gate before any renderer or
component-quality work continues. No visual row may be promoted until its native
reference crop integrity is proven from Windows native element bounds or a
reviewed native crop source that is explicitly marked ready.

## Assumptions

- Native Windows screenshots remain the visual source of truth.
- Crop presence is not proof of source validity.
- macOS runtime layout bounds must not be used to validate or produce native
  Windows reference crops.
- Existing invalid references may remain checked in as blocked evidence while
  the gate is introduced.

## Scope

- `ComponentParityLab.WinUI` and `PublicAdminWorkbench.WinUI` native reference
  target export.
- Native reference import and component crop generation.
- Public dashboard, public visual review index, and component evidence schemas.
- Workflow and tests that prevent component references without
  `native-reference-targets.json` from being treated as valid.

## Non-Goals

- Renderer or component-quality improvements.
- Row promotion, threshold loosening, row deletion, or treating `usable` as
  final quality.
- Hand-editing dashboard counts to make checks pass.

## Phase -1 Gate

Every public row must distinguish:

- native reference crop exists;
- native reference crop is valid;
- native reference crop has Windows native element bounds;
- native reference crop is diagnostic, placeholder, offscreen, or
  state-incomplete.

Rows with a crop but no trustworthy Windows native bounds remain promotion
blockers. The dashboard must expose these as native reference integrity blockers
separately from missing native crops.

## Steps

1. Emit `native-reference-targets.json` from Windows native fixture runs with
   scenario identity, component target identity, automation/name identity where
   available, and Windows client-area bounds.
2. Require `native-reference-targets.json` for component scenarios in
   `.github/workflows/windows-native-screenshot.yml`.
3. Teach native reference import and component crop generation to consume
   Windows native bounds and write `nativeReferenceBounds`, provenance, and
   readiness into component evidence.
4. Update dashboard and visual review index generation to surface native
   reference integrity blockers separately from missing crops.
5. Block final visual/native-quality grade application unless native reference
   readiness is `ready` or `verified`.
6. Regenerate public dashboard, public visual review index, and affected
   component evidence through tooling.
7. Keep old references blocked until recaptured with valid native target bounds.

## Verification Gates

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard --check
PATH="$PWD/tools:$PATH" winui3-mac-runner catalog-audit --check
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

If checks fail only because newly exposed native-reference-integrity blockers
remain, keep that failure documented as expected. Do not weaken the gate.

## Risks And Recovery

- Existing checked-in references may look less ready after this change. That is
  the intended safe state.
- Windows-only fixture code may need workflow validation after merge. If target
  export fails, keep the component row blocked and fix the exporter instead of
  accepting macOS/runtime bounds.
- Rollback is to revert this gate and all generated artifacts together; partial
  rollback would make the dashboard misleading again.

## Affected Files

- `.github/workflows/windows-native-screenshot.yml`
- `fixtures/ComponentParityLab.WinUI/**`
- `fixtures/PublicAdminWorkbench.WinUI/**`
- `src/WinUI3.MacRuntime/**`
- `src/WinUI3.MacRenderer.Skia/**`
- `src/WinUI3.MacRunner/**`
- `docs/visual-parity/**`
- `tests/WinUI3.MacRuntime.Tests/**`

## Execution Prompt

Use `$google-eng-practices`. Implement `docs/plans/2026-06-02-native-reference-integrity-first-amendment.md` in `tools/winui3-mac-test-runtime`. Do not start renderer/component-quality work, do not promote rows, do not loosen thresholds, do not delete difficult rows, and do not hand-edit dashboard counts. Add Windows native target-bound export for ComponentParityLab and PublicAdminWorkbench, require component `native-reference-targets.json` in `.github/workflows/windows-native-screenshot.yml`, update importer/cropper/evidence/dashboard/index generation so invalid native references are explicit blockers separate from missing crops, add tests for the gate/schema/cropper/workflow behavior, regenerate artifacts through tooling, then run the listed verification gates. Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using Conventional Commits and push immediately.
