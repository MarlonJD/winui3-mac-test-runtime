# Public WinUI 3 Application Corpus

The corpus replaces fixture-only confidence with a broad, public, clean-room
WinUI 3 application surface. Every corpus app is a Windows-targeted WinUI 3
source project that is ingested through the macOS compat shadow build into a
deterministic, classified inventory of API, XAML construct, resource, asset, and
project-ingestion surface.

- Manifest: [`../../fixtures/corpus.json`](../../fixtures/corpus.json)
- Classified surface: [`corpus-inventory.json`](corpus-inventory.json)
- Tracked unknown report: [`corpus-unknown-apis.json`](corpus-unknown-apis.json)
- Classification source of truth: [`winui-api-compatibility.catalog.json`](winui-api-compatibility.catalog.json)
  (see [`api-catalog.md`](api-catalog.md))

## Provenance And Non-Goals

- Every app is **clean-room** public source. No private product names,
  repositories, screenshots, or secrets are used as evidence.
- The [`tools/private-name-denylist`](../../tools/private-name-denylist/README.md)
  scan fails CI when a denylisted private name appears in the public surface.
- Ingestion is discovery-only: corpus apps are parsed and classified, never
  executed, during inventory generation.

## Running Corpus Ingestion

```bash
# Write artifacts and verify against the tracked baseline (CI mode):
dotnet run --project src/WinUI3.MacRunner -- \
  ingest --manifest fixtures/corpus.json --output artifacts/corpus --check

# Refresh the tracked baseline after an intentional corpus surface change:
dotnet run --project src/WinUI3.MacRunner -- \
  ingest --manifest fixtures/corpus.json --write-baseline
```

`--check` fails when any app fails ingestion, when any discovered surface is
unclassified (`unknown`), or when the generated inventory drifts from the
committed baseline. CI runs the `--check` form in
[`.github/workflows/ci.yml`](../../.github/workflows/ci.yml), and
`CorpusInventoryMatchesTrackedBaseline` enforces the same baseline in the test
suite.

## Corpus Apps

| App | Project shape | Tier | Provenance | Owner | Exit criteria |
| --- | --- | --- | --- | --- | --- |
| `production-smoke` | navigation shell (forms, commands, lists, theme dictionaries) | production-subset | clean-room | Evidence corpus owner | Ingests with zero blocking diagnostics, zero unknown surface, native WinUI reference scenarios; advances PB-007/PB-008. |
| `public-admin-workbench` | navigation shell (command surfaces, review list, `x:Uid`) | production-subset | clean-room | Evidence corpus owner | Ingests with zero blocking diagnostics, zero unknown surface, native WinUI reference scenarios; advances PB-007/PB-008. |
| `single-window` | single window, no navigation shell | production-subset | clean-room | Project ingestion owner | Ingests with zero blocking diagnostics, zero unknown surface, and native WinUI reference scenario; advances PB-007/PB-008. |
| `settings-form` | MVVM settings form (two-way `{Binding}`, `INotifyPropertyChanged`) | production-subset | clean-room | Project ingestion owner | Ingests cleanly, two-way binding resolves against the view model, and native WinUI reference scenario is captured; advances PB-007/PB-008. |
| `resource-catalog` | resource-heavy, packaging-like (merged + theme dictionaries, styles, assets) | production-subset | clean-room | Compatibility catalog owner | Ingests cleanly with merged/theme dictionaries, static/theme resources, styles, assets classified, and native WinUI light/dark/high-contrast references; advances PB-001/PB-007/PB-008. |

## Project-Shape Coverage

The plan calls for varied public WinUI 3 project shapes. Coverage across the
corpus apps:

| Project shape | Covered by |
| --- | --- |
| single-window | `single-window` |
| navigation shell | `production-smoke`, `public-admin-workbench` |
| MVVM forms | `settings-form`, `production-smoke` |
| settings | `settings-form`, `production-smoke` |
| data grids / lists | `production-smoke`, `public-admin-workbench` (`ListView`) |
| command surfaces | `production-smoke`, `public-admin-workbench` (`CommandBar`/`AppBarButton`) |
| resource-heavy | `resource-catalog` |
| theme switching | `resource-catalog`, `production-smoke` (light/dark/high-contrast theme dictionaries) |
| packaging-like layouts | `resource-catalog` (`Assets/`, `WindowsPackageType=None`) |

Dialogs and flyouts are exercised by the component parity lab
(`fixtures/ComponentParityLab.WinUI`), which has its own inventory in
[`winui-component-inventory.json`](winui-component-inventory.json) and is the
component-grade surface rather than an application shape.

## Classified Surface

The current corpus discovers 44 distinct surfaces across 5 apps, all classified,
with an empty unknown report:

| Status | Count |
| --- | ---: |
| `supported` | 28 |
| `partial` | 15 |
| `windows-only` | 1 |
| `unknown` | 0 |

Surfaces are grouped by kind: `xaml-element`, `xaml-attached-property`,
`xaml-directive`, `xaml-markup`, `xaml-resource`, `xaml-event`, `project-item`,
`project-property`, and `api`. The full machine-readable detail, including the
apps that use each surface, is in `corpus-inventory.json`.

`windows-only` is the `Microsoft.WindowsAppSDK` package reference, which the
shadow build replaces with the macOS compatibility facade. It is an excluded
production surface, not an unknown.

## Unknown Surface Tracking

The unknown report (`corpus-unknown-apis.json`) lists every discovered surface
that the catalog does not classify. It is currently empty. The report is:

- **Deterministic**: ingestion sorts entries and `usedBy` lists with no
  timestamps; `CorpusInventoryGenerationIsDeterministic` asserts stable output.
- **Tracked**: the report is committed and compared on every run with
  `ingest --check` and `CorpusInventoryMatchesTrackedBaseline`.

When a new corpus app introduces an uncataloged construct, the unknown report
becomes non-empty and the gate fails. Resolution is to classify the surface in
the catalog (with status, owner, and exit criteria) or document it as an
explicit unsupported exclusion, then refresh the baseline.

## Relationship To Production Blockers

- **PB-001** (catalog coverage): the corpus drives the catalog toward
  no-unknown-API usage for common public apps; the unknown report is empty.
- **PB-007** (project ingestion shapes): the corpus validates a documented set
  of public WinUI 3 project shapes with structured ingestion output.
- **PB-008** (public evidence corpus): the corpus is the representative
  clean-room public surface that fixture-only evidence lacked. Public GitHub
  Actions run `26791576394` captured native WinUI references for every current
  corpus app scenario, including the single-window, settings-form, and
  resource-catalog scenarios added after the initial corpus ingestion work.
