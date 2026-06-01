# Visual Parity Evidence

This directory keeps public, reviewable visual parity evidence from the
`windows-native-screenshot.yml` workflow. The workflow captures a real Windows
client-area reference PNG, renders the same public scenario through the macOS
runtime, writes a pixel diff, and fails strict runs when metrics exceed the
scenario thresholds.

The checked-in example artifacts come from public GitHub Actions run
[`26752174485`](https://github.com/MarlonJD/winui3-mac-test-runtime/actions/runs/26752174485)
on commit `b6604c1`.

## How To Read The Metrics

Lower values are better.

| Metric | Meaning |
| --- | --- |
| `changedPixelPercentage` | Percent of pixels that are not byte-identical between Windows and macOS output. Anti-aliasing and text rasterization can move this number even when the layout is close. |
| `meanAbsoluteError` | Average per-channel color error across the image. This is usually the best quick signal for broad visual similarity. |
| `rootMeanSquaredError` | Error metric that penalizes larger color differences more strongly. |
| `maxChannelDelta` | Largest single-channel color delta. A value of `255` can be caused by one high-contrast edge and does not imply the whole image is unrelated. |

Passing strict comparison means the scenario stayed inside its documented
thresholds. It is not a claim of arbitrary WinUI 3 pixel-perfect compatibility.

## Current Public Evidence

| Scenario | Status | Changed pixels | Exact unchanged pixels | MAE | RMS | What matches | Known differences |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| `shell-light` | passed | 4.00% | 96.00% | 1.57 | 14.20 | Navigation shell geometry, pane selection, text hierarchy, card surfaces. | Text rasterization and edge anti-aliasing differ from Windows. |
| `interactions-light` | passed | 31.69% | 68.31% | 4.06 | 17.77 | Interaction result state, list/text/image layout, binding-driven updates. | Many pixels change because text and small control edges render differently on the two platforms. |
| `control-gallery-light` | passed | 6.49% | 93.51% | 1.67 | 12.69 | Supported public controls, high-level spacing, checked/progress/info states. | Native control chrome, exact typography, and Fluent focus/hover details are approximated. |
| `public-admin-workbench-light` | passed | 16.01% | 83.99% | 8.50 | 41.09 | Windows-targeted source ingestion, navigation, selected state, workbench list/detail shape, command click assertion. | The macOS renderer still simplifies parts of the command bar, InfoBar, list/detail cards, exact text metrics, Fluent depth, and native control chrome. |

## Public Admin Workbench Example

These files are copied from the public workflow artifact for
`public-admin-workbench-light`.

| Windows reference | macOS runtime | Pixel diff |
| --- | --- | --- |
| ![Windows reference](examples/public-admin-workbench-light/windows-reference.png) | ![macOS runtime](examples/public-admin-workbench-light/mac-runtime.png) | ![Pixel diff](examples/public-admin-workbench-light/pixel-diff.png) |

The example is successful for the current alpha milestone because it passes the
scenario thresholds and keeps the core shell/workbench structure recognizable.
The diff also makes the remaining work visible: exact Fluent control rendering,
text metrics, command surfaces, InfoBar layout, shadows, materials, focus
visuals, pointer states, and richer list/detail painters still need cataloged
implementation work before stronger parity claims can be made.

## Updating Evidence

When a visual scenario or renderer behavior changes:

1. Run the local strict scenario.
2. Trigger `windows-native-screenshot.yml` on the public repository.
3. Download the `windows-native-screenshot` artifact.
4. Inspect `windows-reference.png`, `mac-runtime.png`, `pixel-diff.png`, and
   `visual-run.json`.
5. Update the relevant example folder only when the artifact is public and does
   not contain private names, private screenshots, secrets, or proprietary
   fixture content.
