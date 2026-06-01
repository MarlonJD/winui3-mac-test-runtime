# Public Compatibility Contracts

This project provides source-level WinUI-style compatibility for public,
documented test surfaces. It does not run Windows binaries, `.msix` packages, or
arbitrary `.exe` files on macOS.

The macOS runtime contract is Wine-free: application code is compiled as managed
.NET, loaded into a managed macOS process, and exercised through clean-room
`Microsoft.UI.Xaml` facade types.

## Published Compatibility Level

The current published level is **Level 0: Harness Reliability**, with public
fixture-backed slices of Levels 1, 2, 4, 5, and 6 documented in
`docs/compatibility/matrix.md`.

Level claims are cumulative only when the matrix marks the relevant runtime,
XAML, control, renderer, interaction, accessibility, and artifact behaviors as
`supported` or `partial` with public fixture or test coverage.

## Runtime Contract

- `winui3-mac-doctor` reports the managed host and Wine optionality.
- `winui3-mac-runner run` builds and launches a managed app assembly.
- Strict visual runs fail on binding failures, resource lookup failures,
  unsupported facade APIs, unsupported visual features, failed interactions, or
  pixel thresholds that exceed the scenario contract.
- Unsupported features are reported structurally in artifacts and SARIF; they
  are not silently treated as supported behavior.

## XAML Contract

The XAML compiler supports only constructs listed in the matrix. Unsupported
elements produce `XAML1001` diagnostics, unsupported properties produce
`XAML1002`, unsupported property elements produce `XAML1003`, unsupported XAML
directives produce `XAML1004`, unsupported attached properties produce
`XAML1005`, and unsupported events produce `XAML1006`. Diagnostics include file
and line information when available. Resource dictionaries, static resources,
theme resources, bindings, property elements, and attached properties are
supported only to the extent documented in the matrix and public fixtures.

## Control And Renderer Contract

Facade controls and `skia-v2` painters are production claims only for the
documented public subset. When strict visual mode sees a logical tree node
without a supported painter, it records an unsupported visual feature and fails
the run.

SVG and the current Skia renderer remain smoke renderers. `skia-v2` is the
scenario-driven renderer used for public visual compatibility checks.

## Interaction And Accessibility Contract

Interaction scripts are versioned JSON documents with deterministic action
results. Supported actions are listed in the matrix and exported as
`interactions.json` when a script or scenario interactions are supplied.

Accessibility export is a deterministic approximation derived from the logical
tree. It covers role, element name, label, help text, focus state, and child
relationships for the documented control subset.

## Artifact Contract

Every runner-owned JSON artifact has a `schemaVersion`. Diagnostic collections
use versioned envelopes:

- `binding-failures.json`: `{ "schemaVersion": "0.1", "failures": [...] }`
- `resource-failures.json`: `{ "schemaVersion": "0.1", "failures": [...] }`
- `unsupported-apis.json`: `{ "schemaVersion": "0.1", "apis": [...] }`

`diagnostics.sarif` uses stable rule IDs:

- `WINUI3MAC001`: binding failure
- `WINUI3MAC002`: resource lookup failure
- `WINUI3MAC003`: unsupported compatibility API
