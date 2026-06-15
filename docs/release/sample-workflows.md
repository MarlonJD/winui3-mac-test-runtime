# Sample Workflows

This document shows the Phase 15 external developer workflow shape. Keep the
lanes separate: portable-headless is source-level compatibility evidence, while
windows-reference is the native WinUI source of truth.

## Default PR Shape

```yaml
name: winui3-compat

on:
  pull_request:
  workflow_dispatch:

jobs:
  portable-headless:
    runs-on: ubuntu-latest
    env:
      WINUI3_COMPAT_MODE: portable-headless
      WINUI3_COMPAT_DRIVER: internal
      WINUI3_COMPAT_RENDERER: skia-offscreen
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --no-restore --filter Phase
      - run: dotnet run --project src/WinUI3.MacRunner -- run --project fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj --renderer skia-v2 --scenario fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json --output artifacts/portable-headless/public-admin-workbench-light
      - uses: actions/upload-artifact@v4
        with:
          name: portable-headless-artifacts
          path: artifacts/portable-headless
          retention-days: 14

  windows-reference:
    runs-on: windows-latest
    env:
      WINUI3_COMPAT_MODE: windows-reference
      WINUI3_COMPAT_RUNTIME: native-winui
      WINUI3_COMPAT_DRIVER: flaui-uia3
      WINUI3_COMPAT_RENDERER: native-winui
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --no-restore --filter WindowsNativeReferenceWorkflow
      - uses: actions/upload-artifact@v4
        with:
          name: windows-reference-screenshots
          path: artifacts/windows-reference
          retention-days: 30
```

The repository-owned workflows are:

- `.github/workflows/ci.yml`: default portable-headless and broader local gates.
- `.github/workflows/windows-native-screenshot.yml`: native WinUI reference
  capture on `windows-latest`.

## macOS Policy

Do not add hosted `macos-latest` to the default PR path. macOS windowed and AX
work remains local, manual, scheduled, release-validation, or self-hosted only.

## Artifact Retention

Use short retention for portable-headless PR artifacts and longer retention for
Windows reference artifacts:

| Artifact | Suggested retention | Reason |
| --- | ---: | --- |
| `portable-headless-artifacts` | 14 days | fast PR debugging |
| `windows-reference-screenshots` | 30 days | native reference comparison |
| `product-evidence` | 30 days | release-candidate review |
| package dry run | 30 days | release-check and provenance review |

Checked-in JSON dashboards remain the durable baseline. Workflow artifacts are
evidence attachments, not replacements for reviewed baseline updates.

## Release Candidate Closeout

Before tagging or publishing, run:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-release-ready-local
PATH="$PWD/tools:$PATH" winui3-mac-runner release-hardening-manifest --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate --package-dir artifacts/packages
```

The release owner must attach external evidence for:

- full native Windows reference workflow
- full strict scenario sweep
- package dry run plus `release-check`

Publishing stays blocked when any of those are missing.
