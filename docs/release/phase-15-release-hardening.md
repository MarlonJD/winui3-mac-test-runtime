# Phase 15 Release Hardening

Phase 15 closes the portable-headless execution plan as a release-hardening
package for external developers. It does not expand the product claim.

## Scope

The release claim remains:

```text
source-level WinUI 3 compatibility harness for the documented public subset
```

It still does not mean:

```text
arbitrary WinUI 3 compatibility
Windows .exe/.msix execution
native WinUI visual fidelity
hosted macOS CI in the default PR path
```

## Hardening Checklist

| Area | Evidence | Gate |
| --- | --- | --- |
| CLI polish | `winui3-mac-release-ready-local`, `release-candidate`, `release-hardening-manifest` | local command and release-candidate |
| GitHub Action docs | `docs/release/sample-workflows.md` | manifest freshness |
| Sample projects | `fixtures/PublicAdminWorkbench.WinUI`, `fixtures/ComponentParityLab.WinUI`, `fixtures/ProductionSmoke.WinUI` | strict scenario and Windows reference lanes |
| Known gaps | this document, `docs/release/production-readiness.md`, Phase 14 broader control dashboard | release docs and compatibility dashboards |
| Baseline management | corpus baselines and visual dashboard artifacts | release-candidate freshness checks |
| Artifact retention | workflow docs and release gates | release docs plus workflow artifact uploads |
| Versioned compatibility matrix | compatibility matrix, catalog, level model, release manifest | release-candidate and manifest checks |

## No App Source Change Demo

The supported-subset demo is intentionally source-level. It inspects and renders
the public WinUI source fixture without mutating the fixture source, and it does
not run Windows binaries on macOS.

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner run \
  --project fixtures/PublicAdminWorkbench.WinUI/PublicAdminWorkbench.WinUI.csproj \
  --renderer skia-v2 \
  --scenario fixtures/PublicAdminWorkbench.WinUI/scenarios/public-admin-workbench-light.json \
  --output artifacts/portable-headless/public-admin-workbench-light
```

Reference truth still comes from Windows:

```sh
gh workflow run windows-native-screenshot.yml -f scenario=public-admin-workbench-light
```

The comparison dashboard may then join portable-headless and Windows reference
artifacts:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner portable-headless-dashboard \
  --portable artifacts/portable-headless/public-admin-workbench-light \
  --windows-reference artifacts/windows-reference/public-admin-workbench-light \
  --output artifacts/comparison/public-admin-workbench-light
```

## Local Release Gate

Use the single local gate from a clean checkout:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-release-ready-local
```

For a narrower documentation/artifact freshness check:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner release-hardening-manifest --check
PATH="$PWD/tools:$PATH" winui3-mac-runner release-candidate
```

`release-candidate` keeps `releaseAllowed` false until external evidence is
attached: full Windows native reference capture, full strict scenario sweep, and
package dry run with `release-check`.

## Baseline Policy

Tracked baselines are reviewed source artifacts, not auto-refreshed CI output.
Refresh them only after reviewing the behavior change:

```sh
PATH="$PWD/tools:$PATH" winui3-mac-runner ingest \
  --manifest fixtures/corpus.json \
  --write-baseline
PATH="$PWD/tools:$PATH" winui3-mac-runner component-quality-dashboard
PATH="$PWD/tools:$PATH" winui3-mac-runner state-coverage-matrix
PATH="$PWD/tools:$PATH" winui3-mac-runner native-quality-family-tranches
PATH="$PWD/tools:$PATH" winui3-mac-runner broader-control-state-coverage
PATH="$PWD/tools:$PATH" winui3-mac-runner release-hardening-manifest
```

## Known Gaps Stay Visible

Known gaps remain documented in:

- `docs/release/production-readiness.md`
- `docs/visual-parity/state-coverage-matrix.json`
- `docs/visual-parity/native-quality-family-tranches.json`
- `docs/visual-parity/broader-control-state-coverage.json`

Rows labeled `partial`, `planned`, `not-rendered`, `default-only`, or
`not-evaluated` must not be promoted to native-quality claims.
