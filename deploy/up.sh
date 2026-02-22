#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Environment Launcher
#══════════════════════════════════════════════════════════════════════════════
#
#  Thin wrapper that constructs the correct `docker compose` command
#  for a given environment profile.
#
#  Usage:
#    ./deploy/up.sh <environment> [docker compose args...]
#
#  Examples:
#    ./deploy/up.sh local up -d --build      # Start local dev
#    ./deploy/up.sh local down                # Stop local
#    ./deploy/up.sh local logs -f api         # Follow API logs
#    ./deploy/up.sh production up -d          # Start production stack
#
#══════════════════════════════════════════════════════════════════════════════

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# ─────────────────────────────────────────────────────────────────────────────
# Validate arguments
# ─────────────────────────────────────────────────────────────────────────────
if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <environment> [docker compose args...]"
    echo ""
    echo "Environments:"
    for f in "$SCRIPT_DIR"/docker-compose.*.yml; do
        env_name=$(basename "$f" | sed 's/docker-compose\.\(.*\)\.yml/\1/')
        echo "  $env_name"
    done
    echo ""
    echo "Examples:"
    echo "  $0 local up -d --build"
    echo "  $0 production up -d"
    echo "  $0 local logs -f api"
    exit 1
fi

ENV_NAME="$1"
shift

# ─────────────────────────────────────────────────────────────────────────────
# Resolve files
# ─────────────────────────────────────────────────────────────────────────────
OVERLAY="$SCRIPT_DIR/docker-compose.${ENV_NAME}.yml"
ENV_FILE="$SCRIPT_DIR/envs/${ENV_NAME}.env"

if [[ ! -f "$OVERLAY" ]]; then
    echo "Error: Unknown environment '$ENV_NAME'"
    echo ""
    echo "Available environments:"
    for f in "$SCRIPT_DIR"/docker-compose.*.yml; do
        env_name=$(basename "$f" | sed 's/docker-compose\.\(.*\)\.yml/\1/')
        echo "  $env_name"
    done
    exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Error: Environment file not found: $ENV_FILE"
    echo ""
    echo "Create it from the example:"
    echo "  cp ${SCRIPT_DIR}/envs/${ENV_NAME}.env.example ${ENV_FILE}"
    exit 1
fi

# ─────────────────────────────────────────────────────────────────────────────
# Execute
# ─────────────────────────────────────────────────────────────────────────────
exec docker compose \
    --project-directory "$PROJECT_ROOT" \
    -f "$SCRIPT_DIR/docker-compose.yml" \
    -f "$OVERLAY" \
    --env-file "$ENV_FILE" \
    "$@"
