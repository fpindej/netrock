#!/usr/bin/env bash
# PreToolUse hook: blocks dangerous bash commands
# Exit 2 to block, exit 0 to allow

set -euo pipefail

# Require jq for JSON parsing
command -v jq >/dev/null || exit 0

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if [ -z "$COMMAND" ]; then
    exit 0
fi

# Block npm install (must use pnpm)
if echo "$COMMAND" | grep -qE '(^|[;&|])\s*npm\s+install\b'; then
    echo "Use pnpm, not npm. Run: pnpm install" >&2
    exit 2
fi

# Block force push
if echo "$COMMAND" | grep -qE 'git\s+push\s+.*--force($|\s)'; then
    echo "Force push blocked. Use --force-with-lease if you must, or ask the user first." >&2
    exit 2
fi

# Block destructive rm -rf on root-like paths
if echo "$COMMAND" | grep -qE 'rm\s+-rf\s+(/|~|\$HOME|\.\.)'; then
    echo "Destructive rm -rf on sensitive path blocked." >&2
    exit 2
fi

# Block git reset --hard without explicit target
if echo "$COMMAND" | grep -qE 'git\s+reset\s+--hard\s*$'; then
    echo "Bare 'git reset --hard' blocked - specify a target commit." >&2
    exit 2
fi

# Block git clean -f (removes untracked files)
if echo "$COMMAND" | grep -qE 'git\s+clean\s+-[a-zA-Z]*f'; then
    echo "git clean blocked - this removes untracked files permanently." >&2
    exit 2
fi

# Block git checkout/restore that discards all changes
if echo "$COMMAND" | grep -qE 'git\s+(checkout|restore)\s+\.\s*$'; then
    echo "Discarding all changes blocked. Specify individual files." >&2
    exit 2
fi

# Block force deletion of branches
if echo "$COMMAND" | grep -qE 'git\s+branch\s+-D\b'; then
    echo "Force branch deletion blocked. Use -d (safe delete) or ask the user." >&2
    exit 2
fi

# Block piping remote scripts to shell
if echo "$COMMAND" | grep -qE '(curl|wget)\s+.*\|\s*(ba)?sh'; then
    echo "Piping remote scripts to shell blocked. Download and review first." >&2
    exit 2
fi

# Block remote branch deletion via push
if echo "$COMMAND" | grep -qE 'git\s+push\s+\S+\s+:[^-]'; then
    echo "Remote branch deletion blocked. Ask the user first." >&2
    exit 2
fi

exit 0
