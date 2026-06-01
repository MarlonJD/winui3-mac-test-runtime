# Private-Name Denylist Scan

`private-name-scan.sh` is the operator gate that keeps private product names out
of the public `winui3-mac-test-runtime` surface. It supports the production
Non-Goal that no private product names, repositories, or screenshots are used as
production evidence.

## What It Does

- Reads `denylist.txt` (one case-insensitive, whole-word term per line; `#`
  comments and blank lines ignored).
- Scans the tracked files of this repository, excluding internal planning notes
  (`docs/plans/`), this tool directory, vendored packages (`.packages/`), and
  build output (`bin/`, `obj/`).
- Exits non-zero when any denylisted term appears, printing `file:line` hits.

## Scope Rationale

The scan targets the public/production surface: source, fixtures, public
compatibility/visual/release docs, README, and workflows. Internal planning
notes under `docs/plans/` are intentionally excluded because they are working
documents (for example, they may state that this runtime is deliberately *not* a
private-product-specific subset) and are not published as compatibility
evidence.

## Usage

```bash
tools/private-name-denylist/private-name-scan.sh
```

CI runs this scan in `.github/workflows/ci.yml`. A non-zero exit fails the build.

## Maintaining The Denylist

Add private product, brand, or internal repository names as whole-word terms.
Avoid common English words that would cause false positives. When a legitimate
occurrence is unavoidable, narrow the scan scope explicitly and document why in
this README rather than weakening the term list.
