# WinUI API Compatibility Catalog

`docs/compatibility/winui-api-compatibility.catalog.json` is the public
source-of-truth seed for the full WinUI 3 macOS compatibility roadmap. It is
compiled into the facade and XAML packages so runtime and compiler diagnostics
use the same classification that the docs publish.

The catalog is intentionally deterministic:

- Entries have stable `id` values and are sorted lexically.
- `schemaVersion` is `0.1`.
- Status values are `supported`, `partial`, `planned`, `windows-only`, and
  `not supported`.
- A runtime or compiler diagnostic may report `unknown` when public app code
  touches a WinUI API or XAML construct that is not yet cataloged.

## Seed Coverage

The `0.1` catalog has 113 entries:

| Status | Count |
| --- | ---: |
| `supported` | 52 |
| `partial` | 28 |
| `planned` | 28 |
| `windows-only` | 3 |
| `not supported` | 2 |

| Kind | Count |
| --- | ---: |
| `api` | 48 |
| `fluent-resource` | 4 |
| `project-item` | 3 |
| `project-property` | 4 |
| `visual-state` | 5 |
| `xaml-attached-property` | 3 |
| `xaml-directive` | 4 |
| `xaml-element` | 30 |
| `xaml-event` | 3 |
| `xaml-property` | 4 |
| `xaml-property-element` | 2 |
| `xaml-resource` | 3 |

The seed is not an exhaustive WinUI inventory yet. Its job is to make the
current alpha claims measurable and to make important future targets explicit:
Mica, Acrylic, system backdrops, compositor concepts, shadows, transforms,
motion, focus visuals, theme resources, high contrast, reduced motion, and
Fluent interaction states are cataloged rather than treated as permanent
exclusions.

## Diagnostic Contract

The XAML compiler fails unsupported or unknown constructs with the existing
strict diagnostic IDs:

- `XAML1001`: unsupported or unknown XAML element.
- `XAML1002`: unsupported or unknown XAML property.
- `XAML1003`: unsupported or unknown property element.
- `XAML1004`: unsupported or unknown XAML directive.
- `XAML1005`: unsupported or unknown attached property.
- `XAML1006`: unsupported or unknown event.

The message includes the catalog status when a construct is known. If the
construct is not cataloged, the message says it is not present in the WinUI
compatibility catalog.

Runtime placeholder facades report touched APIs through `unsupported-apis.json`
and `WINUI3MAC003` SARIF diagnostics. The entry status comes from the catalog
when known, or `unknown` when the API is not cataloged. Strict visual runs still
fail when any unavailable API entry is present.
