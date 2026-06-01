# Production-Grade WinUI Compatibility Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `src/WinUI3.MacCompat`, `src/WinUI3.MacRuntime`, `src/WinUI3.MacXaml`, `src/WinUI3.MacRenderer.Skia`, `src/WinUI3.MacRunner`, `fixtures`, `tests`, `.github/workflows`

## Goal

Move the project from a constrained feasibility harness toward a production-grade
source-level WinUI 3 compatibility runtime for documented, public, testable
application surfaces.

The production claim must be earned through explicit compatibility levels,
reference-backed visual tests, deterministic runtime artifacts, documented
unsupported behavior, and CI gates. The project must remain Wine-free on macOS
and must not claim binary `.exe` compatibility or arbitrary WinUI 3 parity until
those claims are backed by public tests.

## Product Definition

Production-grade compatibility means:

- common WinUI-style C# and XAML application code can run in the macOS-managed
  runtime without Windows binaries;
- supported controls, properties, styles, resources, bindings, navigation,
  input, accessibility metadata, and visual states behave consistently enough
  for automated tests;
- unsupported features fail loudly with actionable diagnostics;
- every supported behavior has fixture coverage, compatibility matrix entries,
  and CI verification;
- visual behavior is checked against real Windows screenshots from public
  `windows-latest` GitHub Actions runs where pixel fidelity matters.

## Compatibility Levels

### Level 0: Harness Reliability

The existing runner, doctor, artifacts, SVG, Skia, and `skia-v2` paths remain
stable. Build, test, package, and workflow commands are deterministic across
clean checkouts.

Exit criteria:

- smoke commands pass locally;
- public CI runs on pull requests;
- artifacts have versioned schemas;
- failures include actionable diagnostics and non-zero exits.

### Level 1: Core App And XAML Compatibility

Support the common source-level app shape used by small WinUI apps.

Scope:

- `Application`, `Window`, `Page`, `Frame`, resource dictionaries, startup
  activation, and navigation;
- XAML constructs: `x:Class`, `x:Name`, namespaces, property elements,
  attached properties, events, text content, simple collections, static
  resources, theme resources, and bindings;
- generated code that preserves useful diagnostics and line mappings.

Exit criteria:

- fixtures cover code-first and XAML-first apps;
- unsupported XAML produces strict diagnostics rather than partial silent output;
- compatibility matrix marks each construct as supported, partial, planned, or
  not supported.

### Level 2: Layout And Controls Foundation

Support the controls and layout panels needed by realistic public desktop test
fixtures.

Scope:

- layout panels: `Grid`, `StackPanel`, `Border`, `ScrollViewer`, `ItemsRepeater`
  or a documented list substitute, and deterministic measure/arrange metadata;
- controls: `TextBlock`, `TextBox`, `PasswordBox`, `Button`, `ToggleButton`,
  `CheckBox`, `RadioButton`, `ComboBox`, `ListView`, `NavigationView`,
  `ContentControl`, `ItemsControl`, `Image`, `ProgressRing`, `ProgressBar`,
  `InfoBar`, and `CommandBar` where public fixtures need them;
- control states: enabled, disabled, focused, hovered, pressed, selected,
  checked, invalid, and placeholder states.

Exit criteria:

- each supported control has facade behavior tests, tree export tests, layout
  tests, `skia-v2` painter tests, and at least one public fixture scenario;
- strict visual mode reports missing painters, properties, resources, and states;
- no supported control relies on private product fixtures.

### Level 3: Styling, Resources, And Theme Fidelity

Make supported controls usable in public fixtures without one-off styling hacks.

Scope:

- resource lookup precedence for app, page, element, static, and theme resources;
- style setters for supported properties;
- light, dark, and high contrast theme tokens for the supported subset;
- brushes, borders, corner radius, spacing, typography, opacity, and visibility;
- documented handling for unsupported templates and materials.

Exit criteria:

- theme changes are scenario-driven and visible in artifacts;
- resource and style misses are exported to diagnostics;
- documentation explains which WinUI styling concepts are faithfully supported,
  approximated, or not supported.

### Level 4: Data Binding, Commands, And State

Support enough MVVM behavior for production-like test fixtures.

Scope:

- one-way and two-way bindings for supported dependency-like properties;
- observable collection changes for list controls;
- `INotifyPropertyChanged`;
- command execution and enabled state;
- binding failure diagnostics with source path, target property, and element
  context.

Exit criteria:

- fixtures cover form editing, validation, list refresh, navigation state, and
  command state;
- binding failures are deterministic and fail strict scenarios when required;
- tests cover happy path and failure path behavior.

### Level 5: Input, Accessibility, And Automation

Make the runtime credible for automated interaction tests, not only static
rendering.

Scope:

- pointer click, keyboard focus, text entry, tab navigation, accelerator routing,
  selection, scrolling, and command invocation;
- accessibility export for role, name, help text, focusability, enabled state,
  selection, checked state, and value where applicable;
- scenario actions for click, focus, type text, key press, select item, navigate,
  wait for idle, and assert tree state.

Exit criteria:

- interaction scenario JSON is stable and documented;
- each action emits structured success or failure details;
- accessibility artifacts are covered by tests and fixture expectations.

### Level 6: Windows Reference Visual Compatibility

Expand the current pixel-fidelity path into a broader public compatibility
suite.

Scope:

- public Windows reference screenshots for each supported fixture state;
- macOS `skia-v2` rendering for the same viewport, scale, theme, and scenario;
- pixel diff thresholds tuned per scenario and justified in JSON;
- reviewable artifacts for reference, runtime, diff image, diff metrics, layout
  tree, unsupported APIs, and diagnostics.

Exit criteria:

- every Level 2 supported visual control appears in at least one Windows
  reference scenario;
- workflow artifacts can be downloaded and inspected without private access;
- failures distinguish visual drift, missing renderer support, unsupported
  features, and environmental mismatch.

### Level 7: Release And Consumption Readiness

Make the runtime consumable by external public projects that accept the
documented compatibility limits.

Scope:

- .NET tool packaging and SDK package documentation;
- versioned artifact schemas;
- migration guides for adding a public fixture;
- sample CI workflow for consumers;
- release notes that state compatibility level, supported APIs, known gaps, and
  verification evidence.

Exit criteria:

- `dotnet pack` succeeds for published packages;
- README has a clear quick start and compatibility claim;
- docs include troubleshooting for strict visual failures, XAML gaps, renderer
  gaps, and CI environment drift.

## Assumptions

- The project targets source-level WinUI-style compatibility, not Windows binary
  execution.
- The primary macOS runtime remains a managed .NET process and remains Wine-free.
- Public GitHub-hosted `windows-latest` runners are acceptable as the Windows
  reference source of truth for screenshots.
- Pixel-perfect parity across fonts and OS rendering stacks may require
  thresholds, but every threshold must be explicit and reviewable.
- Generic public fixtures can be realistic without using private product names,
  screenshots, secrets, repositories, or proprietary content.

## Open Questions

- Which compatibility level should be the first funded milestone: Level 2
  controls, Level 4 MVVM state, or Level 7 package consumption?
- Should the public support claim use "WinUI compatibility level" labels in
  README releases, NuGet package metadata, or only docs until the suite is
  broader?
- Which minimum .NET SDK and macOS versions should be treated as supported for
  consumers?

## Scope

- Compatibility contracts, facades, XAML compiler behavior, runtime artifacts,
  renderer behavior, fixtures, tests, workflows, and docs.
- Public fixture content only.
- Deterministic CI and local verification commands.
- Diagnostics that make unsupported APIs visible and actionable.

## Non-Goals

- Running arbitrary Windows `.exe`, `.msix`, or native WinUI binaries on macOS.
- Adding Wine to the managed runtime path.
- Claiming complete WinUI 3, Windows App SDK, Fluent material, or native Windows
  compositor parity before there is public test evidence.
- Using private repositories, private screenshots, private product names,
  secrets, or proprietary fixture content.
- Building a general UI framework unrelated to WinUI compatibility.

## Implementation Phases

### Phase 1: Define Compatibility Contracts

- Add versioned compatibility contract docs for runtime, XAML, controls, visual
  renderer, interactions, accessibility, and artifacts.
- Convert `docs/compatibility/matrix.md` into the source of truth for supported,
  partial, planned, and not-supported behavior.
- Add a compatibility-level section to `README.md`.
- Add schema version fields where artifacts are missing them.

Verification:

- Docs link cleanly from README.
- Unit tests assert artifact schema versions for generated JSON files.
- No existing smoke command output changes except documented schema additions.

### Phase 2: Expand XAML And Runtime Diagnostics

- Add parser/compiler coverage for property elements, attached properties,
  common collection syntax, namespace aliases, styles, and resource references
  needed by the public fixture suite.
- Preserve useful diagnostics for unsupported XAML with element, property, and
  source location where available.
- Extend runtime diagnostics so unsupported facade APIs, missing resources,
  binding failures, and renderer gaps share stable error codes.

Verification:

- XAML tests cover supported constructs and unsupported failure cases.
- Strict scenarios fail on unsupported constructs before rendering silently.
- `diagnostics.sarif` includes stable rule IDs.

### Phase 3: Build A Control Compatibility Suite

- Add generic public fixtures for app shell, settings form, data table/list,
  command surface, validation form, navigation flow, and theme gallery.
- Implement only the control properties needed by those fixtures.
- Add facade tests, layout tests, renderer tests, interaction tests, and
  artifact assertions for each newly supported control.

Verification:

- `dotnet test` covers every newly supported control path.
- Each fixture has at least one scenario JSON file.
- Compatibility matrix entries are updated in the same change as control support.

### Phase 4: Productionize Layout, State, And Interaction

- Improve deterministic layout for supported panels and controls.
- Add scenario actions for text input, keyboard navigation, selection, scrolling,
  command invocation, and state assertions.
- Add idle/state synchronization so interaction scripts produce stable artifacts.

Verification:

- Interaction fixture scenarios pass in strict mode.
- Repeated `skia-v2` renders produce deterministic PNG hashes for the same
  scenario when no reference is provided.
- Failure artifacts identify the failed action and current visual tree state.

### Phase 5: Expand Windows Reference Coverage

- Add public Windows reference screenshots for every supported fixture category.
- Keep the reference workflow manually dispatchable and pull-request friendly.
- Upload artifacts per scenario so reviewers can inspect Windows reference,
  macOS runtime, pixel diff, metrics, and visual run metadata.

Verification:

- `windows-native-screenshot` passes for all strict scenarios.
- At least one artifact from each fixture category is manually inspected before
  release handoff.
- Pixel diff thresholds are scenario-local and documented.

### Phase 6: Package And Release Hardening

- Stabilize .NET tool and package outputs.
- Add a consumer quick-start fixture and sample CI workflow.
- Document versioning, compatibility levels, known unsupported features, and
  upgrade notes.
- Add release checklist items for build, tests, visual workflow, package smoke,
  private-name scan, and artifact inspection.

Verification:

- `dotnet pack` succeeds for all package projects.
- Generated packages can be installed into a clean public sample project.
- Release docs include exact verification evidence and known risk notes.

## Verification Gates

Run during implementation, using targeted subsets while working and the full set
before final handoff:

```sh
dotnet build
dotnet test
PATH="$PWD/tools:$PATH" winui3-mac-doctor
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual
PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual
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

Run for Windows visual reference coverage when scenarios change:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

Inspect at least one `windows-reference.png`, `mac-runtime.png`, and
`pixel-diff.png` from each changed fixture category before final handoff.

## Risks And Mitigations

- Risk: "General WinUI compatibility" becomes too broad to ship.
  Mitigation: ship by compatibility level and require public fixture evidence
  before expanding claims.
- Risk: Renderer approximations hide unsupported behavior.
  Mitigation: strict mode fails on missing painters, missing required
  properties, unresolved resources, unsupported states, and unsupported
  interactions.
- Risk: XAML compatibility grows into a second framework without discipline.
  Mitigation: implement only constructs required by documented public fixtures
  and add compatibility matrix entries in the same change.
- Risk: Windows reference screenshots drift with hosted runner updates.
  Mitigation: record OS image metadata, keep thresholds scenario-local, and
  upload reviewable artifacts for every strict run.
- Risk: Public fixtures accidentally include private product content.
  Mitigation: use generic fixture data, run the operator-provided private-name
  denylist, and keep screenshots public and synthetic.

## Rollback And Recovery

- Keep existing runner defaults, SVG output, and current Skia behavior
  backward-compatible while adding stricter paths.
- Gate new compatibility claims behind scenario flags, fixture coverage, and
  compatibility matrix updates.
- If a broad phase destabilizes the project, revert only that phase's control
  or XAML expansion while preserving diagnostics and docs.
- If a Windows reference workflow fails due hosted runner drift, keep artifacts,
  compare metadata, and adjust thresholds only after visual inspection.

## Affected Files And Docs

- `README.md`
- `docs/compatibility/matrix.md`
- `docs/architecture/artifacts.md`
- `docs/plans/2026-06-01-windows-reference-pixel-fidelity-plan.md`
- `src/WinUI3.MacCompat/*`
- `src/WinUI3.MacRuntime/*`
- `src/WinUI3.MacXaml/*`
- `src/WinUI3.MacRenderer.Skia/*`
- `src/WinUI3.MacRunner/*`
- `fixtures/*`
- `tests/*`
- `.github/workflows/*`

## Execution Prompt

Use `$google-eng-practices` and implement `docs/plans/2026-06-01-production-grade-winui-compatibility-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. The product goal is production-grade source-level WinUI 3 compatibility for documented public compatibility levels, while keeping the macOS-managed runtime Wine-free and preserving existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current Skia, and `skia-v2` behavior. Do not use private repositories, private screenshots, private product names, secrets, or proprietary fixture content. Keep identifiers, comments, and canonical docs in English.

Start with the smallest end-to-end milestone that materially advances the plan: define or update compatibility contracts, expand only the XAML/runtime/control/renderer surface needed by public fixtures, add strict unsupported-feature diagnostics, update deterministic artifacts, add or update realistic generic public fixtures, and keep Windows reference visual testing wired through public `windows-latest` GitHub Actions where visual behavior changes. Update `README.md`, `docs/compatibility/matrix.md`, and `docs/architecture/artifacts.md` whenever behavior or artifact contracts change.

Run targeted tests while working and run the full relevant verification before handoff: `dotnet build`, `dotnet test`, `PATH="$PWD/tools:$PATH" winui3-mac-doctor`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/SampleAdminShell.MacTest/SampleAdminShell.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/SampleAdminShell.MacTest/scenarios/shell-light.json --strict-visual`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/InteractionBindingApp.MacTest/InteractionBindingApp.MacTest.csproj --renderer skia-v2 --scenario ./fixtures/InteractionBindingApp.MacTest/scenarios/interactions-light.json --strict-visual`, `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release`, package smoke commands for changed packages, and `rg -n "<private-name-denylist-regex>" .` with the operator-provided private-name denylist. If visual scenarios change, trigger `windows-native-screenshot.yml`, wait for completion, download artifacts, and inspect at least one `windows-reference.png`, `mac-runtime.png`, and `pixel-diff.png` from each changed fixture category before final handoff.

Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately.
