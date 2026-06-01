# Material And Composition Compatibility Contract

Mica, Acrylic, system backdrops, compositor-style effects, shadows, transforms,
motion, focus visuals, theme resources, high contrast, reduced motion, and full
Fluent interaction states are in-scope compatibility targets for the product.
They are not implemented broadly in the current alpha milestone.

The macOS-managed runtime remains Wine-free. Material and composition support
must be clean-room, driven by public WinUI 3 and Windows App SDK behavior, and
validated against real Windows output from public GitHub Actions runs.

## Current Alpha Contract

| Area | Current catalog status | Current runtime behavior |
| --- | --- | --- |
| `MicaBackdrop` | `planned` | Placeholder facade records `unsupported-apis.json`; no Mica rendering. |
| `AcrylicBrush` and desktop Acrylic backdrop | `planned` | Placeholder or catalog-only tracking; no Acrylic rendering. |
| `SystemBackdrop` | `planned` | `Window.SystemBackdrop` exists as a source-level object slot; material behavior is unavailable. |
| Compositor visuals and effect brushes | `planned` | Catalog-only tracking. |
| Shadows and transforms | `planned` | Catalog-only tracking. |
| Storyboards and key-frame animations | `planned` | Catalog-only tracking. |
| Fluent focus, disabled, selected states | `partial` | State metadata exists for supported controls; full Fluent visuals are planned. |
| Hover and pressed states | `planned` | Catalog-only tracking. |
| Light, dark, and high contrast themes | `partial` | `skia-v2` strict scenarios cover the public subset. |
| Reduced motion | `planned` | Catalog and fixture-plan target; no animation clock yet. |

## Promotion Levels

Material and composition entries move through these levels independently:

- `planned`: cataloged, diagnosed when used, not implemented.
- `API-compatible`: facade types and members compile without claiming behavior.
- `semantic-compatible`: deterministic clean-room behavior exists and is unit
  tested.
- `visually-approximated`: `skia-v2` renders a documented approximation for
  public fixtures.
- `reference-matched`: public Windows reference screenshots and macOS runtime
  output pass scenario-local thresholds.
- `windows-only`: the behavior depends on Windows OS integration and is
  validated only on Windows.

No entry may be promoted by docs alone. Promotion requires catalog updates,
focused tests, strict diagnostics, and public fixture evidence.

## Public Fixture Plan

A future `MaterialComposition.MacTest` fixture should remain generic and
public. It should avoid private product names, screenshots, secrets, and
proprietary content.

Planned scenarios:

- `materials-light`: active window, Mica backdrop, Acrylic command surface,
  visible focus target, and static content.
- `materials-dark`: same composition with dark theme resources.
- `materials-high-contrast`: high-contrast theme with explicit focus,
  foreground, background, and disabled-state contrast checks.
- `materials-reduced-motion`: visual-state transition fixture with reduced
  motion enabled and deterministic animation timing.
- `composition-transforms`: opacity, translation, scale, shadow, and layered
  surface metadata with strict diagnostics for unsupported effects.

Each scenario should produce `windows-reference.png`, `mac-runtime.png`,
`pixel-diff.png`, `pixel-diff.json`, `visual-run.json`, `tree.json`,
`diagnostics.sarif`, and `unsupported-apis.json`. New material or composition
behavior must start with a failing or planned catalog entry, then move through
tests before a renderer approximation is accepted.
