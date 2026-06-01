# Full WinUI 3 macOS Development Plan

Date: 2026-06-01

Owner subtree: root `docs/plans`, `docs/compatibility`, `docs/architecture`,
`docs/consumption`, `docs/release`, `src`, `fixtures`, `tests`,
`.github/workflows`, `tools`

## Goal

Make the project a full source-level WinUI 3 development runtime for macOS: a
developer should be able to build, run, test, inspect, and visually validate a
real WinUI 3 application from macOS without using Wine in the managed runtime.
This includes the long-term goal of supporting WinUI 3 Fluent visual behavior:
Mica, Acrylic, system backdrops, composition-driven effects, motion, focus
visuals, theme resources, and full Fluent interaction states.

The current documented subset is an early milestone, not the final product
claim. The long-term product goal is broad WinUI 3 source compatibility for C#
and XAML application development, with real Windows validation used as the
source of truth for behavior and visual output.

## Product Definition

Full macOS WinUI development means:

- WinUI 3 / Windows App SDK C# and XAML source code can compile against
  compatible packages on macOS.
- The macOS runtime can execute supported application code through clean-room
  WinUI facade types, deterministic layout, input, resources, binding,
  accessibility, and rendering.
- Fluent materials and compositor-backed concepts are compatibility targets,
  not permanent exclusions. They should be implemented through clean-room
  material, effect, animation, and visual-state abstractions and validated
  against real Windows output.
- The runtime exposes every compatibility gap through analyzers, structured
  artifacts, and strict diagnostics.
- Real Windows builds and screenshots from public `windows-latest` GitHub
  Actions runs remain the reference source of truth.
- The developer workflow supports local macOS iteration and CI verification for
  Windows correctness.

The product should not settle for a small fixture subset. The subset exists to
bootstrap the compatibility engine, coverage measurement, and verification
pipeline on the way to full WinUI 3 source-level development.

## What "Full Compatibility" Means

The target is source-level WinUI 3 app development compatibility, not native
Windows binary execution on macOS.

Required:

- API coverage for the public WinUI 3 / Windows App SDK surface needed by
  desktop apps, tracked by generated compatibility catalogs.
- XAML coverage for real app markup, resource dictionaries, styles, templates,
  visual states, bindings, commands, and localization metadata.
- Runtime semantics for dependency properties, routed events, dispatcher
  behavior, measure/arrange, focus, input, navigation, resources, state, and
  accessibility.
- Renderer coverage for common WinUI controls and states, with Windows
  reference screenshots and scenario-local thresholds.
- Fluent visual parity coverage for Mica, Acrylic, system backdrops, shadows,
  opacity, transforms, transitions, focus visuals, reveal-like states,
  light/dark/high-contrast theme behavior, reduced motion, and accessibility
  contrast requirements.
- Developer tools for macOS build/run/test loops, package consumption, scenario
  generation, diagnostics, and Windows CI handoff.

Not required for the macOS-managed runtime:

- Running arbitrary Windows `.exe` or `.msix` binaries directly on macOS.
- Reimplementing private Windows internals.
- Using Wine as the primary runtime path.
- Treating OS integration APIs as locally complete before the clean-room
  implementation and Windows reference validation prove the compatibility level.

Windows-only APIs should be classified. They can be supported through clean-room
facades, deterministic test doubles, explicit diagnostics, or remote/public
Windows validation depending on the API category.

## Success Criteria

- The public documentation states the north star as full source-level WinUI 3
  development on macOS and treats the current subset as a milestone.
- A generated compatibility catalog classifies WinUI 3 APIs as supported,
  partial, planned, Windows-only, or not supported.
- Unknown public WinUI API usage is a product gap, not silent success.
- Compatibility claims are based on coverage numbers, tests, fixtures, and
  Windows reference artifacts.
- Mica, Acrylic, compositor, animation, and Fluent state support are tracked in
  the compatibility catalog and move through explicit states: planned,
  API-compatible, semantic-compatible, visually approximated,
  reference-matched, or Windows-only.
- The supported surface expands through conformance fixtures, not private app
  content.
- macOS local runs stay Wine-free.
- Real Windows CI validates build, behavior, and visual output for covered
  scenarios.

## Assumptions

- Public Microsoft NuGet packages, SDK metadata, documentation, and public
  sample patterns can be used as compatibility references, subject to their
  licenses.
- Full source compatibility will require generated code and analyzers; manual
  facade expansion alone will not scale.
- Some WinUI-adjacent Windows OS APIs cannot be meaningfully executed on macOS
  without Windows. Those APIs still need explicit classification and developer
  diagnostics.
- Visual parity must be evidence-driven because fonts, text rendering, and
  compositor behavior differ across platforms.
- Mica, Acrylic, composition, shadows, transforms, and Fluent motion can be
  supported only by modeling public behavior and validating against Windows
  reference output; private Windows compositor internals are not copied.
- Private screenshots, private repositories, private product names, secrets, and
  proprietary fixture content remain prohibited.

## Non-Goals

- Running arbitrary Windows binaries, `.msix` packages, or native Windows UI
  processes directly on macOS.
- Adding Wine to the macOS-managed runtime.
- Claiming complete parity before API catalog coverage, conformance tests, and
  Windows reference validation prove it.
- Forking or copying proprietary WinUI implementation code.
- Using private application content as compatibility evidence.

## Phases

### Phase 1: Reframe The Product Contract

- Update docs so the product north star is full source-level WinUI 3
  development on macOS.
- Mark the current Level 0 through Level 7 subset as the first alpha milestone,
  not the endpoint.
- Add explicit wording for local macOS development plus public Windows
  validation.
- Replace any language that could imply the current subset is the final
  production scope.

Verification:

- README, compatibility contracts, matrix, release notes, and consumption docs
  agree on the product goal and current limits.
- Docs still avoid any binary compatibility or Wine claim.

### Phase 2: Generate A WinUI API Compatibility Catalog

- Add tooling that inventories the public WinUI 3 / Windows App SDK API surface
  from public package metadata or reference assemblies.
- Generate a machine-readable compatibility catalog with stable IDs for types,
  members, XAML elements, properties, events, attached properties, resources,
  and controls.
- Connect the catalog to docs so compatibility claims are measurable.
- Add analyzer-style diagnostics for app code that touches unknown or
  unsupported public APIs.

Verification:

- The catalog generation is deterministic.
- Tests verify known supported, partial, planned, Windows-only, and unsupported
  APIs are classified correctly.
- Docs include coverage counts and explain how to interpret them.

### Phase 3: Build The Core WinUI Runtime Semantics

- Implement scalable foundations before adding many controls:
  `DependencyObject`, dependency-property metadata, attached properties,
  routed events, dispatcher queue behavior, resource lookup, theme lookup,
  focus, input routing, visual state, measure/arrange, and accessibility state.
- Keep strict diagnostics for missing semantics.
- Avoid control-specific hacks when a WinUI platform primitive is the real
  missing layer.

Verification:

- Unit tests cover each primitive directly.
- Existing fixtures keep passing.
- New fixture behavior is implemented through shared runtime primitives where
  practical.

### Phase 4: Build Fluent Materials And Composition Parity

- Add compatibility catalog entries for Mica, Acrylic, system backdrops,
  compositor concepts, shadows, opacity, transforms, transitions, animations,
  visual states, focus visuals, reveal-like interaction states, reduced motion,
  and high-contrast behavior.
- Build clean-room abstractions for material resolution, effect graphs,
  animation clocks, visual layers, theme-aware brushes, and fallback behavior.
- Implement initial public fixtures for material and composition surfaces:
  active/inactive windows, light/dark/high-contrast themes, Mica backdrop,
  Acrylic surfaces, layered command surfaces, focus states, animated state
  transitions, and reduced-motion mode.
- Define parity levels for these features:
  API-compatible, semantic-compatible, visually approximated,
  reference-matched, and Windows-only.
- Keep unsupported or partially supported material/composition behavior visible
  in strict diagnostics and compatibility reports.

Verification:

- Unit tests cover material fallback, theme switching, high contrast, reduced
  motion, visual-state transitions, opacity, transforms, and animation timing.
- Strict visual scenarios capture Windows references for material/composition
  fixtures and compare macOS output with scenario-local thresholds.
- Accessibility tests verify contrast and focus visibility across Mica,
  Acrylic, light, dark, and high-contrast cases.

### Phase 5: Expand XAML Compatibility Toward Real Apps

- Support broader XAML grammar, property elements, collection syntax,
  namespaces, attached properties, markup extensions, styles, templates,
  resource dictionaries, theme dictionaries, visual states, localization
  metadata, and binding forms.
- Preserve source-location diagnostics for unsupported constructs.
- Add strict mode that fails on unknown elements or properties unless explicitly
  classified.

Verification:

- XAML compiler tests cover supported and rejected constructs.
- Public conformance fixtures use realistic XAML without private content.
- Unsupported XAML emits stable diagnostic IDs and actionable messages.

### Phase 6: Expand Control And Layout Coverage

- Build a public WinUI compatibility gallery fixture that exercises the common
  desktop app controls and states.
- Expand facades, layout, input, accessibility, and `skia-v2` painters by
  control family:
  text, buttons, selection, forms, items, navigation, dialogs, command surfaces,
  data presentation, progress/status, scrolling, flyouts, menus, and settings.
- Track each control through the compatibility catalog, tests, scenario JSON,
  visual artifacts, and docs.
- Include Fluent interaction states for supported controls: hover, pressed,
  focused, selected, disabled, error, loading, animated transitions, theme
  resources, and material-backed surfaces where Windows guidance uses them.

Verification:

- Every newly supported control has facade tests, layout tests, interaction
  tests, accessibility export tests, renderer tests, and at least one public
  scenario.
- Strict mode fails on missing control states, unsupported properties, or
  missing painters.

### Phase 7: Make Visual Fidelity A Conformance Suite

- Expand the public Windows reference workflow from a few scenarios into a
  broad conformance suite.
- Capture Windows reference screenshots for control gallery, app shell, forms,
  navigation, data/list, dialogs/flyouts, theme, accessibility state, and
  interaction states.
- Add material/composition scenarios for Mica, Acrylic, system backdrop
  fallback, shadows, transforms, animated state transitions, reduced motion,
  focus visuals, and high-contrast Fluent resources.
- Keep scenario-local thresholds, reviewable artifacts, and deterministic
  layout export.

Verification:

- Public `windows-latest` workflow passes for every strict conformance category.
- Artifacts include Windows reference, macOS runtime, pixel diff, metrics,
  visual run metadata, tree, diagnostics, and unsupported API reports.
- Threshold changes require visual inspection and explanation.

### Phase 8: Build The macOS Developer Workflow

- Provide package and template support for creating and importing WinUI
  compatibility test projects on macOS.
- Add commands for doctor, build, run, scenario generation, visual compare,
  artifact inspection, compatibility report generation, and Windows CI handoff.
- Document how a developer iterates locally on macOS and proves correctness on
  Windows CI.

Verification:

- A clean public sample app can be created, run, visually checked, packed, and
  validated through public CI.
- Consumer docs and sample workflows work from a clean checkout.

### Phase 9: Define Full-Compatibility Release Gates

- Add release gates based on API catalog coverage, fixture coverage, test
  coverage, Windows reference coverage, and package/consumer readiness.
- Publish compatibility scores instead of broad unqualified claims.
- Promote from alpha to beta only when the catalog, conformance suite, and
  Windows reference workflow prove a meaningful app-development surface.

Verification:

- Release docs include exact coverage numbers, workflow run IDs, package smoke
  evidence, known unsupported API categories, and residual risks.
- The public claim is precise: full source-level WinUI development is the goal,
  with current coverage stated by evidence.
- Material and composition support is reported separately so consumers can see
  whether Mica, Acrylic, compositor effects, animations, and Fluent visual
  states are planned, approximated, or reference-matched.

## Verification Gates

Keep the existing baseline green while expanding the product:

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
rg -n "<private-name-denylist-regex>" .
```

Run package smoke when package or SDK behavior changes:

```sh
dotnet pack src/WinUI3.MacTest.Sdk/WinUI3.MacTest.Sdk.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacCompat/WinUI3.MacCompat.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRuntime/WinUI3.MacRuntime.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacXaml/WinUI3.MacXaml.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRenderer.Skia/WinUI3.MacRenderer.Skia.csproj --configuration Release --output ./artifacts/packages
dotnet pack src/WinUI3.MacRunner/WinUI3.MacRunner.csproj --configuration Release --output ./artifacts/packages
```

Run public Windows reference validation when visual behavior, scenarios, or
support claims change:

```sh
gh workflow run windows-native-screenshot.yml --repo MarlonJD/winui3-mac-test-runtime
gh run watch --repo MarlonJD/winui3-mac-test-runtime --exit-status
gh run download --repo MarlonJD/winui3-mac-test-runtime --name windows-native-screenshot --dir ./artifacts/github-windows-native-screenshot
```

## Risks And Mitigations

- Risk: "Full WinUI 3 compatibility" becomes an unbounded promise.
  Mitigation: use generated API catalogs, coverage reports, conformance suites,
  and explicit release gates.
- Risk: Mac runtime diverges from Windows behavior.
  Mitigation: treat public Windows builds and screenshots as the behavioral and
  visual source of truth.
- Risk: Manual facade expansion becomes unmaintainable.
  Mitigation: generate catalogs and scaffolding from public metadata, then
  implement semantics intentionally.
- Risk: Windows-only APIs block local macOS execution.
  Mitigation: classify them and provide clean-room facades, deterministic test
  doubles, diagnostics, or Windows CI validation paths.
- Risk: Mica, Acrylic, and compositor behavior cannot be copied from Windows
  internals.
  Mitigation: implement clean-room public-behavior approximations first, then
  promote individual features only when Windows reference scenarios prove
  visual and semantic parity.
- Risk: Fluent visual parity reduces accessibility or contrast on macOS.
  Mitigation: verify high contrast, reduced motion, focus visibility, and text
  contrast as first-class material/composition gates.
- Risk: Current alpha docs imply the subset is enough.
  Mitigation: reframe the subset as an initial milestone under the full macOS
  WinUI development roadmap.
- Risk: Private content leaks into broad conformance fixtures.
  Mitigation: keep fixtures generic and run the operator-provided private-name
  denylist.

## Rollback And Recovery

- Keep existing subset behavior green while expanding compatibility.
- If a generated catalog or analyzer blocks current fixtures incorrectly, roll
  back the classification change and add a focused test before retrying.
- If a new control family destabilizes shared runtime primitives, revert that
  family without removing the API catalog or diagnostics foundation.
- If a claim cannot be proven, downgrade the claim in docs rather than shipping
  an unsupported promise.

## Affected Files And Docs

- `README.md`
- `docs/compatibility/contracts.md`
- `docs/compatibility/matrix.md`
- `docs/architecture/artifacts.md`
- `docs/consumption/quick-start.md`
- `docs/release/level-7-release-readiness.md`
- `docs/plans/*`
- `src/WinUI3.MacCompat/*`
- `src/WinUI3.MacRuntime/*`
- `src/WinUI3.MacXaml/*`
- `src/WinUI3.MacRenderer.Skia/*`
- `src/WinUI3.MacRunner/*`
- `src/WinUI3.MacTest.Sdk/*`
- `tools/*`
- `fixtures/*`
- `tests/*`
- `.github/workflows/*`

## Execution Prompt

Use `$google-eng-practices` and `$windows-winui3-design` and implement `docs/plans/2026-06-01-full-winui3-macos-development-plan.md` in the public `MarlonJD/winui3-mac-test-runtime` repository. The product goal is full source-level WinUI 3 application development on macOS: developers should be able to build, run, test, inspect, and visually validate real WinUI 3 C# and XAML app code from macOS, while real Windows public GitHub Actions runs remain the behavioral and visual source of truth. Mica, Acrylic, system backdrops, compositor-style effects, shadows, transforms, motion, focus visuals, theme resources, high contrast, reduced motion, and full Fluent interaction states are in-scope compatibility targets and must be tracked explicitly. Keep the macOS-managed runtime Wine-free. Preserve existing `winui3-mac-doctor`, `winui3-mac-runner`, SVG, current Skia, and `skia-v2` behavior. Do not use private repositories, private screenshots, private product names, secrets, or proprietary fixture content. Keep identifiers, comments, and canonical docs in English.

Start by reframing the public docs so the current Level 0 through Level 7 subset is clearly an alpha milestone, not the final product scope. Then implement the smallest end-to-end foundation for the full-compatibility roadmap: a deterministic WinUI API compatibility catalog or equivalent source-of-truth mechanism that classifies public WinUI 3 / Windows App SDK APIs, XAML constructs, Fluent resources, Mica/Acrylic/system backdrop APIs, compositor/effect concepts, visual states, and animation-related APIs as supported, partial, planned, Windows-only, or not supported. Connect that classification to docs and strict diagnostics for unknown or unsupported API usage. Add a first material/composition compatibility contract and public fixture plan, but do not opportunistically implement broad controls or visual effects without tests. Add only the tests, fixtures, analyzers, docs, and runtime hooks needed to prove this first foundation while keeping existing smoke and visual scenarios green.

Run targeted tests while working and the relevant final verification before handoff: `dotnet build`, `dotnet test`, `PATH="$PWD/tools:$PATH" winui3-mac-doctor`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj`, `PATH="$PWD/tools:$PATH" winui3-mac-runner run --project ./fixtures/TinyWinUIApp.MacTest.csproj --renderer skia`, strict `skia-v2` runs for `SampleAdminShell`, `InteractionBindingApp`, and both `ControlGallery` light and high-contrast scenarios, `dotnet build tools/WindowsWindowCapture/WindowsWindowCapture.csproj --configuration Release`, `dotnet build fixtures/WindowsNativeProbe/WindowsNativeProbe.csproj --configuration Release`, package smoke commands for changed packages, and `rg -n "<private-name-denylist-regex>" .` with the operator-provided private-name denylist. If visual behavior or scenarios change, trigger `windows-native-screenshot.yml`, wait for it to finish, download artifacts, and inspect the relevant `windows-reference.png`, `mac-runtime.png`, and `pixel-diff.png` files before final handoff.

Commit only relevant files with author `marlonjd <burak.karahan@mail.ru>` using a Conventional Commit message and push immediately. Final handoff must state how the docs now express full WinUI 3 macOS development and Fluent/Mica/Acrylic/compositor parity as product goals, what compatibility catalog or foundation was added, which verification commands passed, whether Windows reference workflow was needed, the private-name scan result, commit SHA, and residual risks.
