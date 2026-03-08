#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Publish Deployment Artifacts
#══════════════════════════════════════════════════════════════════════════════
#
#  Generates a production-ready deployment package from the Aspire AppHost.
#  Output contains docker-compose.yaml, .env template, and env file examples.
#
#  Usage:
#    ./deploy/publish.sh                    # Output to deploy/compose/
#    ./deploy/publish.sh -o /path/to/dir    # Output to custom path
#
#  Prerequisites:
#    - .NET SDK (dotnet)
#    - Aspire CLI: dotnet tool install --global aspire.cli --prerelease
#
#══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APPHOST="$PROJECT_ROOT/src/backend/MyProject.AppHost"

# ─────────────────────────────────────────────────────────────────────────────
# Parse arguments
# ─────────────────────────────────────────────────────────────────────────────
OUTPUT_DIR="$SCRIPT_DIR/compose"

while [[ $# -gt 0 ]]; do
    case $1 in
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [-o output-dir]"
            echo ""
            echo "Generates production deployment artifacts from the Aspire AppHost."
            echo ""
            echo "Options:"
            echo "  -o, --output DIR    Output directory (default: deploy/compose/)"
            echo ""
            echo "Output:"
            echo "  docker-compose.yaml    Service definitions (generated)"
            echo "  .env                   Environment variables (fill in before deploy)"
            echo "  envs/api.env           API configuration (fill in before deploy)"
            echo "  envs/seed.env          Seed users (optional)"
            exit 0
            ;;
        *)
            echo "Error: Unknown option '$1'. Use --help for usage."
            exit 1
            ;;
    esac
done

# ─────────────────────────────────────────────────────────────────────────────
# Prerequisites
# ─────────────────────────────────────────────────────────────────────────────
if ! command -v aspire &> /dev/null; then
    echo "Error: Aspire CLI not found."
    echo "Install with: dotnet tool install --global aspire.cli --prerelease"
    exit 1
fi

if [[ ! -d "$APPHOST" ]]; then
    echo "Error: AppHost project not found at $APPHOST"
    exit 1
fi

# ─────────────────────────────────────────────────────────────────────────────
# Generate
# ─────────────────────────────────────────────────────────────────────────────
# Clean previous output to avoid stale files
if [[ -d "$OUTPUT_DIR" ]]; then
    echo "Cleaning previous output..."
    rm -rf "$OUTPUT_DIR"
fi

echo "Publishing deployment artifacts..."
echo "  AppHost: $APPHOST"
echo "  Output:  $OUTPUT_DIR"
echo ""

aspire publish \
    --project "$APPHOST" \
    -o "$OUTPUT_DIR" \
    --non-interactive

# Copy env templates
mkdir -p "$OUTPUT_DIR/envs"
cp "$SCRIPT_DIR/envs/production-example/api.env" "$OUTPUT_DIR/envs/api.env"
cp "$SCRIPT_DIR/envs/production-example/seed.env" "$OUTPUT_DIR/envs/seed.env"

echo ""
echo "Deployment package ready at: $OUTPUT_DIR"
echo ""
echo "Next steps:"
echo "  1. Copy $OUTPUT_DIR/ to your server"
echo "  2. Fill in .env (images, passwords, domain)"
echo "  3. Fill in envs/api.env (CORS, email, captcha)"
echo "  4. Run: ./deploy/up.sh up -d"
