# WinUI3 Mac Test Runtime Launch Demo

This is a compact HyperFrames-ready launch concept for introducing the runtime
without overstating the support claim.

## Positioning

Wine-free source-level WinUI 3 harness validation on macOS.

The demo should be explicit that the runtime does not run arbitrary Windows
binaries, `.msix` packages, or full native-quality Fluent rendering. The claim
is bounded to the documented public source-level harness subset.

## 60-Second Cut

| Time | Scene | On-screen message | Visual |
| ---: | --- | --- | --- |
| 0-8s | Constraint | WinUI 3 validation usually waits for Windows. | Dark stage, terminal line, Windows/macOS labels. |
| 8-18s | Runtime model | Run source-level WinUI fixtures locally on macOS. | `winui3-mac-runner` command card with check artifacts. |
| 18-32s | Visual evidence | Native Windows reference. macOS runtime render. Pixel diff. | Three real screenshots from `docs/visual-parity/examples/public-admin-workbench-light`. |
| 32-44s | Evidence contract | The output is inspectable: tree, accessibility, interactions, SARIF, visual-run, component evidence. | Artifact rail and component status counters. |
| 44-54s | Honest boundary | Useful for documented supported/partial surfaces, not arbitrary WinUI or binary execution. | Compatibility boundary callout. |
| 54-60s | Close | Build. Run. Inspect. Gate. On macOS. | Product mark, command, evidence badges. |

## Voiceover Draft

WinUI 3 teams need a fast way to exercise source fixtures before waiting on a
Windows machine. WinUI3 Mac Test Runtime gives them a Wine-free managed host for
macOS. It builds clean-room facade-backed projects, runs scripted scenarios,
and emits the evidence a release gate can inspect.

The important part is the contract. Every visual claim is tied to a native
Windows reference, a macOS render, and a pixel diff. Every compatibility gap is
reported instead of hidden.

This is not arbitrary Windows binary execution, and it is not full Fluent
pixel parity. It is a bounded source-level harness for the documented public
subset, with artifacts you can audit.

Build. Run. Inspect. Gate. On macOS.

## HyperFrames Port Notes

- Treat `index.html` as the first visual pass and split each `.scene` into a
  composition section.
- Use the existing real PNG assets as frame media. Keep them lossless.
- Add voiceover and captions after the timing is locked.
- For final production, animate scene transitions with GSAP or HyperFrames
  timeline helpers rather than changing the message or support boundary.

