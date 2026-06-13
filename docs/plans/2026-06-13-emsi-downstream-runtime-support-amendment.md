# EMSI Downstream Runtime Support Amendment Plan

Date: 2026-06-13

Owner subtree: `tools/winui3-mac-test-runtime`

Status: Re-baselined; renderer/layout support update verified

Related plan: `docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`

## Objective

Correct the execution target for the EMSI Windows visual/runtime work.

The goal is not to compile and run the full `apps/windows/src/MeetingChallenge.Windows`
application as a native macOS app. That project is a Windows App SDK app and
contains Windows-only bootstrap, OS APIs, packaging, storage, security, and
runtime behavior that are expected to remain Windows-only.

The goal is to improve this WinUI 3 macOS test runtime so EMSI Windows UI can be
tested through source-level XAML/runtime fixtures and downstream probes that
exercise the same production XAML shapes, control families, layout patterns,
icons, typography, and renderer states we need screenshots for.

## Correct Boundary

- Runtime repo responsibility:
  - XAML compiler support for EMSI production-style XAML surfaces.
  - `Microsoft.UI.Xaml` facade/control semantics needed by supported tests.
  - Layout/tree/accessibility export for those surfaces.
  - Skia renderer fidelity for screenshots.
  - Public clean-room fixtures that protect the runtime contract.
  - Comparison tooling against native Windows reference evidence.
- EMSI Windows app responsibility:
  - Real Windows build, packaging, OS integration, and native runner evidence.
  - App-owned probe/scenario content when needed.
  - Private screenshots and pixel-diff evidence in QA storage, not this repo.
- Not a goal:
  - Make `MeetingChallenge.Windows.csproj` fully compile on macOS.
  - Emulate Windows App SDK deployment, PasswordVault, WinRT storage, packaged
    app activation, UI Automation, or Windows shell behavior.

## Why The Direct Real App Command Fails

Running `WinUI3.MacRunner` directly against
`apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj` is
the wrong gate today. The project includes Windows-only code and app bootstrap
that the source-level test runtime is not meant to own.

That failure is useful only as a signal that the runtime cannot ingest arbitrary
Windows app projects. It should not become the implementation target. The
implementation target is the existing source-level coverage path:

```bash
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

and downstream probe/scenario runs that render supported EMSI UI surfaces through
this runtime.

## Current Runtime Work Already In Place

The existing downstream coverage plan already established the correct contract:

- production XAML shape support is a runtime compatibility target;
- downstream app content and private evidence stay outside the public runtime;
- source-level support and native Windows visual parity are separate claims;
- public clean-room fixtures guard every promoted runtime feature.

This amendment keeps that direction and rejects the newer, incorrect idea that
we must make the full Windows app project macOS-compilable.

## Phase 0: Re-Baseline The Real Problem

- [x] Run the production XAML compile command against
  `apps/windows/src/MeetingChallenge.Windows/**/*.xaml`.
- [x] Run the downstream probe screenshot command that currently best represents
  the problematic screen, using external Segoe/Fluent icon fonts when available.
- [x] Record only sanitized diagnostics and aggregate visual findings in this
  repo. Keep PNGs, pixel diffs, and private downstream evidence in
  `/private/tmp/emsi_qa` or the approved QA repo location.
- [x] Confirm the blocker list is expressed as runtime support gaps: unsupported
  XAML, facade semantics, layout, renderer, font/icon handling, or comparison
  tooling.

Acceptance:

- The current failure is framed as runtime support gaps, not as "EMSI Windows
  app cannot run on macOS."
- No phase depends on compiling the full `MeetingChallenge.Windows.csproj`.

2026-06-13 re-baseline:

- Production XAML compile now exits 0 and writes an empty diagnostics array to
  `/private/tmp/emsi-windows-prod-xaml-generated.diagnostics.json`.
- Downstream probe `shell-staff-light` passes through the source-level macOS
  runtime at `/private/tmp/emsi_qa/windows/mac-runtime-probes/shell-staff-light`.
- Probe `unsupported-apis.json` is empty and `visual-run.json` reports
  `visual-status: passed`; native Windows reference comparison remains skipped
  because no Windows reference image was supplied to this gate.
- This is source-level runtime/probe evidence only, not evidence that the full
  native Windows app compiled or ran on macOS.

## Phase 1: Close XAML And Facade Gaps In The Runtime

- [ ] For each remaining unsupported EMSI XAML surface, add a public clean-room
  compiler test first.
- [ ] Verify RED with the expected diagnostic.
- [ ] Add the minimum compiler/catalog/facade support.
- [ ] Verify GREEN and keep unsupported surfaces explicit.

Typical target areas:

- `PasswordBox`, text input, multiline/wrapping behavior.
- `InfoBar`, status/progress properties, close affordance.
- `CommandBar` content/search patterns.
- `ListView`/`ItemsControl` item templates and selection.
- `Grid` row/column sizing, row spans, padding, bounds.
- resource dictionaries and theme token lookup.

Acceptance:

- Production-style EMSI XAML surfaces compile through runtime-supported fixtures.
- Unsupported surfaces fail with precise diagnostics.

## Phase 2: Fix Renderer And Layout Fidelity For Visible EMSI Issues

- [x] Write fail-first renderer/layout tests for each visible issue before
  changing drawing code.
- [x] Prefer runtime fixture tests over downstream private screenshots for the
  public gate.
- [x] Keep native Windows reference comparison as evidence, not as a source of
  copied private assets.

Current visible target areas:

- Fluent icon glyph resolution and fallback behavior.
- InfoBar severity icons, close affordance, and content alignment.
- Navigation selected indicator length and placement.
- Text metrics, baselines, weight, and password mask density.
- Border/card stroke, padding, corner radius, and surface fill.
- Row heights, separator length, list/detail bounds, and command/search chrome.

Acceptance:

- Public runtime tests protect the renderer behavior.
- Downstream probe screenshots improve without threshold relaxation.

2026-06-13 support update:

- Added public runtime coverage for InfoBar severity icon visibility and close
  affordance density, NavigationView selected indicator height, and navigation
  FontIcon fallback when the symbol font resolves to a text fallback.
- Updated `skia-v2` rendering so InfoBar uses Fluent severity glyphs only when a
  resolved symbol typeface is available and otherwise falls back to legible text
  glyphs; NavigationView keeps a stable selected indicator height and falls back
  to deterministic primitives for known shell icons without a symbol font.
- Restored StackPanel layout semantics for naturally sized basic input/icon
  controls while preserving stretch behavior for ProgressBar and explicit-width
  ProgressRing centering.

## Phase 3: Keep Downstream Probes As Consumers, Not The Runtime Core

- [ ] Update or add downstream probe scenarios only when the runtime surface is
  supported by public tests.
- [ ] The probe may mirror EMSI production XAML shape, but it must stay a test
  harness and must not be described as the native Windows app running on macOS.
- [ ] If a downstream probe needs app-specific data, keep that data in the
  downstream app/QA boundary.

Acceptance:

- The user can request a macOS runtime screenshot of an EMSI-like screen and get
  one from the supported probe/runtime path.
- The artifact label says source-level macOS runtime/probe, not real native app.

## Phase 4: Evidence And Claim Hygiene

- [x] Run targeted runtime tests.
- [x] Run the downstream probe screenshot command.
- [x] Run native Windows reference comparison only with external/private evidence
  paths.
- [x] Report source-level support, renderer fidelity, and native Windows visual
  parity as separate outcomes.

Acceptance:

- No private PNG/screenshot/pixel-diff files are committed to this repo.
- No final wording claims that macOS ran the real Windows app.
- Any remaining differences are listed as runtime support/rendering gaps.

## Verification Commands

Runtime targeted tests:

```bash
dotnet test tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "InfoBar|Icon|Navigation|Renderer|Layout"
dotnet test tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
```

Production XAML diagnostic gate:

```bash
tools/winui3-mac-runner xaml compile \
  --output /private/tmp/emsi-windows-prod-xaml-generated \
  /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/src/MeetingChallenge.Windows/**/*.xaml
```

Downstream probe screenshot gate:

```bash
cd /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows
WINUI3_MAC_TEST_FONT_DIRS="$HOME/winui-font-ab" \
dotnet ../../tools/winui3-mac-test-runtime/src/WinUI3.MacRunner/bin/Debug/net10.0/WinUI3.MacRunner.dll run \
  --project tests/MeetingChallenge.WinUI.MacRuntimeProbe/MeetingChallenge.WinUI.MacRuntimeProbe.csproj \
  --renderer skia-v2 \
  --scenario tests/MeetingChallenge.WinUI.MacRuntimeProbe/scenarios/shell-staff-light.json \
  --output /private/tmp/emsi_qa/windows/mac-runtime-probes/shell-staff-light
```

## Execution Prompt

Implement the amendment in `tools/winui3-mac-test-runtime/docs/plans/2026-06-13-emsi-downstream-runtime-support-amendment.md` together with the existing `tools/winui3-mac-test-runtime/docs/plans/2026-06-06-downstream-windows-full-xaml-coverage-plan.md`. Work only in the current branch. Do not push, do not run GitHub workflows, and do not copy private PNG/screenshot/pixel-diff evidence into the repo. Treat the full `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj` macOS compile failure as out of scope unless it reveals a reusable runtime support gap. The implementation target is source-level runtime support for EMSI production XAML/görünüm shapes through public clean-room runtime fixtures and downstream probe scenarios. For every XAML/facade/layout/renderer behavior change, write a failing test first, verify RED, implement the minimum runtime support, then verify GREEN. Keep source-level support, probe screenshot output, and native Windows visual parity claims separate. Run the verification commands in the amendment, and stop if the same blocker repeats three times.
