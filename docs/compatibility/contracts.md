# Public Compatibility Contracts

This project provides Wine-free source-level WinUI 3 compatibility on macOS.
The long-term product goal is full WinUI 3 C# and XAML application development:
developers should be able to build, run, test, inspect, and visually validate
real app source from macOS while public Windows GitHub Actions runs remain the
behavioral and visual source of truth.

The current Level 0 through Level 7 support is an alpha milestone toward that
goal. It does not run Windows binaries, `.msix` packages, or arbitrary `.exe`
files on macOS, and it does not claim complete WinUI 3 behavior.

The macOS runtime contract is Wine-free: application code is compiled as managed
.NET, loaded into a managed macOS process, and exercised through clean-room
`Microsoft.UI.Xaml` facade types.

## Published Alpha Milestone

The current published alpha claim covers **Levels 0 through 7** as documented
in `docs/compatibility/matrix.md`. Level 6 is limited to the public strict
fixture categories captured by the `windows-latest` reference workflow: shell,
interaction/binding, and control gallery. Level 7 covers package and consumer
readiness contracts; it does not imply broader WinUI API support than the
matrix and catalog document.

Level claims are cumulative only when the matrix marks the relevant runtime,
XAML, control, renderer, interaction, accessibility, and artifact behaviors as
`supported` or `partial` with public fixture or test coverage.

## Compatibility Catalog Contract

`docs/compatibility/winui-api-compatibility.catalog.json` is the public
catalog seed for the broader full-compatibility roadmap. The same catalog is
compiled into the facade and XAML packages so diagnostics use the published
classification.

Catalog status values are `supported`, `partial`, `planned`, `windows-only`,
and `not supported`. Runtime or compiler diagnostics may report `unknown` when
app code touches a public API or XAML construct that is not in the catalog yet.
Unknown usage is a product gap and must not silently pass strict checks.

## Runtime Contract

- `winui3-mac-doctor` reports the managed host and Wine optionality.
- `winui3-mac-runner run` builds and launches a managed app assembly.
- Strict visual runs fail on binding failures, resource lookup failures,
  unavailable facade APIs, unsupported visual features, failed interactions, or
  pixel thresholds that exceed the scenario contract.
- Unavailable or unsupported features are reported structurally in artifacts
  and SARIF; they are not silently treated as supported behavior.

## XAML Contract

The XAML compiler supports only constructs listed in the matrix. Unsupported
elements produce `XAML1001` diagnostics, unsupported properties produce
`XAML1002`, unsupported property elements produce `XAML1003`, unsupported XAML
directives produce `XAML1004`, unsupported attached properties produce
`XAML1005`, and unsupported events produce `XAML1006`. Diagnostics include file
and line information when available. Resource dictionaries, static resources,
theme resources, bindings, property elements, and attached properties are
supported only to the extent documented in the matrix and public fixtures.
When a rejected construct exists in the compatibility catalog, the diagnostic
message includes its catalog status. When it does not, the message identifies
the construct as an uncataloged compatibility gap.

## Control And Renderer Contract

Facade controls and `skia-v2` painters are production claims only for the
documented public subset. The Level 2 public subset is fixture-backed by
`ControlGallery.MacTest`, `SampleAdminShell.MacTest`, and
`InteractionBindingApp.MacTest`. When strict visual mode sees a logical tree
node without a supported painter, it records an unsupported visual feature and
fails the run.

SVG and the current Skia renderer remain smoke renderers. `skia-v2` is the
scenario-driven renderer used for public visual compatibility checks.

## Windows Reference Contract

Windows reference screenshots are captured only from generic public fixture
content in the public `windows-native-screenshot` GitHub Actions workflow. The
workflow covers the shell, interaction/binding, and control-gallery strict
fixture categories, then compares the macOS `skia-v2` render against each
reference using scenario-local thresholds. Passing the workflow means those
documented public scenarios stayed within threshold; it is not a claim of full
WinUI 3 pixel parity.

## Styling And Theme Contract

Resource dictionaries support simple string resources and `Style` resources
with `Setter` values for supported public properties. Static and theme resource
lookups report deterministic failures when a key cannot be resolved. The
scenario renderer supports `light`, `dark`, and `high-contrast` themes for the
documented public subset. Control templates, Mica, Acrylic, system backdrops,
compositor effects, transforms, shadows, motion, reduced motion, and complete
Fluent interaction states are in-scope compatibility targets, but most are
cataloged as `planned` in the current alpha rather than rendered.

## Material And Composition Contract

Material and composition compatibility is tracked separately in
`docs/compatibility/material-composition.md`. Mica, Acrylic, system backdrops,
composition visuals, effect brushes, shadows, transforms, storyboards,
animations, focus visuals, high contrast, reduced motion, and Fluent states may
only be promoted when catalog entries, clean-room semantics, tests, strict
fixtures, and public Windows reference artifacts prove the claim.

## Interaction And Accessibility Contract

Interaction scripts are versioned JSON documents with deterministic action
results. Supported actions are listed in the matrix and exported as
`interactions.json` when a script or scenario interactions are supplied. The
public action subset includes click, focus, text entry, item selection, property
assertions, navigation selection, frame navigation, and keyboard accelerator
invocation.

Accessibility export is a deterministic approximation derived from the logical
tree. It covers role, element name, label, help text, focus state, enabled
state, checked state, selection state, value, and child relationships for the
documented control subset.

## Binding And State Contract

The supported MVVM subset includes one-way and two-way bindings for public
facade properties, `INotifyPropertyChanged` refresh, observable collection
refresh for item controls, and `ICommand` execution through supported button
controls. Binding failures include source path, target property, element name,
and element type in `binding-failures.json`.

## Artifact Contract

Every runner-owned JSON artifact has a `schemaVersion`. Diagnostic collections
use versioned envelopes:

- `binding-failures.json`: `{ "schemaVersion": "0.1", "failures": [...] }`
- `resource-failures.json`: `{ "schemaVersion": "0.1", "failures": [...] }`
- `unsupported-apis.json`: `{ "schemaVersion": "0.1", "apis": [...] }`

`diagnostics.sarif` uses stable rule IDs:

- `WINUI3MAC001`: binding failure
- `WINUI3MAC002`: resource lookup failure
- `WINUI3MAC003`: unavailable compatibility API

## Release And Consumption Contract

Package metadata, consumer quick-start documentation, sample public CI, release
checklists, package smoke commands, known-gap notes, and visual workflow
evidence are part of the Level 7 contract. Consumers should treat the
compatibility matrix and API catalog as the API boundary and the artifact
schema documentation as the automation boundary.
