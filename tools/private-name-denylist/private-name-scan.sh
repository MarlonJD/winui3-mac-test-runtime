#!/usr/bin/env bash
#
# Operator private-name denylist scan.
#
# Scans the tracked public/production surface of this repository for denylisted
# private product names and fails (exit 1) when any are present. The goal is to
# guarantee that no production claim or public compatibility evidence depends on
# private product names.
#
# Usage:
#   tools/private-name-denylist/private-name-scan.sh [denylist-file]
#
# Exit codes:
#   0  clean (no denylisted names found)
#   1  denylisted private names found
#   2  usage / environment error
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DENYLIST="${1:-$SCRIPT_DIR/denylist.txt}"

if [ ! -f "$DENYLIST" ]; then
  echo "private-name-scan: denylist not found: $DENYLIST" >&2
  exit 2
fi

cd "$ROOT"

PATTERNS="$(mktemp)"
trap 'rm -f "$PATTERNS"' EXIT
# Strip comments and blank lines from the denylist.
grep -vE '^[[:space:]]*(#|$)' "$DENYLIST" > "$PATTERNS" || true
if [ ! -s "$PATTERNS" ]; then
  echo "private-name-scan: denylist has no active terms; nothing to scan"
  exit 0
fi

# Tracked plus new (not gitignored) files, excluding internal planning notes,
# this denylist tool, vendored packages, and build output. Those are not public
# compatibility evidence. Including new files lets the scan run before a commit.
FILES="$(git ls-files --cached --others --exclude-standard \
  | grep -vE '^(docs/plans/|tools/private-name-denylist/|\.packages/)' \
  | grep -vE '(^|/)(bin|obj)/' || true)"

if [ -z "$FILES" ]; then
  echo "private-name-scan: no tracked files to scan"
  exit 0
fi

FILE_COUNT="$(printf '%s\n' "$FILES" | wc -l | tr -d ' ')"

# Case-insensitive, whole-word scan across the file set.
HITS="$(printf '%s\n' "$FILES" | tr '\n' '\0' \
  | xargs -0 grep -HniwI -f "$PATTERNS" -- 2>/dev/null || true)"

if [ -n "$HITS" ]; then
  echo "private-name-scan: denylisted private names found in the public surface:" >&2
  echo "$HITS" >&2
  echo "private-name-scan: remove the private names above or justify and adjust the denylist scope." >&2
  exit 1
fi

echo "private-name-scan: clean ($FILE_COUNT tracked files scanned, $(wc -l < "$PATTERNS" | tr -d ' ') denylist terms)"
exit 0
