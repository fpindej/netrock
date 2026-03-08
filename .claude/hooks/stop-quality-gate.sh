#!/usr/bin/env bash
# Stop hook: warns if there are uncommitted changes when Claude finishes
# Does not block - just provides feedback

set -euo pipefail

cd "$CLAUDE_PROJECT_DIR"

WARNINGS=""

# Check for uncommitted changes
if ! git diff --quiet 2>/dev/null || ! git diff --cached --quiet 2>/dev/null; then
    UNCOMMITTED=$(git diff --name-only 2>/dev/null; git diff --cached --name-only 2>/dev/null)
    FILE_COUNT=$(echo "$UNCOMMITTED" | sort -u | wc -l | tr -d ' ')
    WARNINGS="${WARNINGS}Uncommitted changes in ${FILE_COUNT} file(s). Consider committing before ending. "
fi

# Check for untracked files in src/
UNTRACKED=$(git ls-files --others --exclude-standard -- src/ 2>/dev/null)
if [ -n "$UNTRACKED" ]; then
    UNTRACKED_COUNT=$(echo "$UNTRACKED" | wc -l | tr -d ' ')
    WARNINGS="${WARNINGS}${UNTRACKED_COUNT} untracked file(s) in src/. May be new files that need committing."
fi

if [ -n "$WARNINGS" ]; then
    echo "{\"hookSpecificOutput\":{\"hookEventName\":\"Stop\"},\"systemMessage\":\"Quality gate: ${WARNINGS}\"}"
fi

exit 0
