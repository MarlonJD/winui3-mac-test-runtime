# CI Strategy

Default pull request validation must not depend on hosted macOS runners. The
portable headless lane exists so source-level WinUI compatibility checks can run
cheaply on Linux, while real Windows remains the native reference source of
truth.

## Default Lanes

| Lane | Runner | Mode | Driver | Renderer | Purpose |
| --- | --- | --- | --- | --- | --- |
| Portable headless | `ubuntu-latest` | `portable-headless` | internal `AutomationCore` driver | Skia offscreen | Fast source-level compatibility, layout, text, automation, screenshot, and diagnostics signal |
| Windows reference | `windows-latest` | `windows-reference` | FlaUI.UIA3 | native WinUI | Real WinUI behavior, native UIA tree, native screenshot, and reference artifacts |

The current repository already keeps the primary build/test/release dry-run gate
on `ubuntu-latest`. Windows native screenshot/reference workflows stay on
`windows-latest`.

## macOS Lane Policy

macOS validation is still required for local developer experience and later
platform integration, but it is not part of the default PR path.

Use macOS runners only for:

- `workflow_dispatch`
- scheduled/nightly validation
- release validation
- self-hosted Mac validation
- explicit macOS windowed or AX adapter changes

The macOS lanes are:

| Lane | Runner | Mode | Driver | Renderer | Purpose |
| --- | --- | --- | --- | --- | --- |
| Local/windowed Mac | local Mac or self-hosted Mac | `macos-windowed` | internal | Skia windowed, later Skia-on-Metal | Manual visual/input debugging |
| macOS AX | local Mac, self-hosted Mac, scheduled, or release | `macos-windowed-ax` | AX adapter | macOS windowed renderer | NSAccessibility mapping validation |

## Artifact Lane Names

Artifacts must name the lane so custom runtime output is never mistaken for
native reference output:

- `portable-headless/`
- `windows-reference/`
- `macos-windowed/`
- `macos-windowed-ax/`
- `windows-custom-runtime/`

## What Not To Do

- Do not make `macos-latest` the default runner for generic headless validation.
- Do not require AppKit, AX, Metal, UIA, FlaUI, or a desktop session in
  `portable-headless`.
- Do not treat Linux headless results as native Windows UI automation.
- Do not mix `windows-reference` artifacts with `windows-custom-runtime`
  artifacts.

## Release Gates

The existing release gates remain in force. Portable-headless architecture work
must preserve catalog honesty, support-policy wording, release-candidate checks,
public product evidence, native reference provenance, and compatibility matrix
freshness.
