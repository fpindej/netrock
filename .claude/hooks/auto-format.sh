#!/usr/bin/env bash
# PostToolUse hook: auto-formats files after Write|Edit operations
# Runs async so it doesn't block Claude's workflow

set -euo pipefail

# Parse the tool input from stdin
INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

if [ -z "$FILE_PATH" ]; then
    exit 0
fi

# Only format if the file exists
if [ ! -f "$FILE_PATH" ]; then
    exit 0
fi

case "$FILE_PATH" in
    *.cs)
        # Format C# files with dotnet format
        # Find the solution file relative to the project
        SLNX=$(find "$CLAUDE_PROJECT_DIR/src/backend" -name "*.slnx" -maxdepth 1 2>/dev/null | head -1)
        if [ -n "$SLNX" ]; then
            dotnet format "$SLNX" --include "$FILE_PATH" --no-restore 2>/dev/null || true
        fi
        ;;
    *.ts|*.svelte|*.js|*.json|*.css|*.html)
        # Format frontend files with prettier
        if [ -f "$CLAUDE_PROJECT_DIR/src/frontend/node_modules/.bin/prettier" ]; then
            cd "$CLAUDE_PROJECT_DIR/src/frontend"
            pnpm exec prettier --write "$FILE_PATH" 2>/dev/null || true
        fi
        ;;
esac

exit 0
