# Runtime Artifacts

The macOS runner writes stable JSON artifacts under the requested output
directory. The files are intended for automation first and human inspection
second.

## Files

- `run.json`: run metadata, host details, Wine optionality, and artifact paths.
- `tree.json`: logical UI tree with stable type names, element names, selected
  state, visibility, focus state, and important content properties.
- `accessibility.json`: role/name/label tree derived from the logical tree.
- `binding-failures.json`: binding paths that could not be resolved or applied.
- `interactions.json`: emitted when `--script` is provided; records every
  scripted action and its result.
- `snapshot.json`: renderer metadata for the deterministic snapshot.
- `screenshots/snapshot.svg`: nonblank deterministic visual representation of
  the logical tree.

## Compatibility Position

Artifacts describe the compatibility runtime's supported subset. They are not a
claim of full WinUI 3 compatibility or Windows binary compatibility.
