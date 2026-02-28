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
#    ./deploy/up.sh production up -d          # Start production stack
#    ./deploy/up.sh production logs -f api    # Follow API logs
#
#  Note: Local development uses Aspire (see MyProject.AppHost).
#
#══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

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
    echo "  $0 production up -d"
    echo "  $0 production logs -f api"
    echo ""
    echo "Note: Local development uses Aspire (see MyProject.AppHost)."
    exit 1
fi

ENV_NAME="$1"
shift

# ─────────────────────────────────────────────────────────────────────────────
# Resolve files
# ─────────────────────────────────────────────────────────────────────────────
OVERLAY="$SCRIPT_DIR/docker-compose.${ENV_NAME}.yml"
ENV_DIR="$SCRIPT_DIR/envs/${ENV_NAME}"
COMPOSE_ENV="$ENV_DIR/compose.env"

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

if [[ ! -d "$ENV_DIR" ]]; then
    echo "Error: Environment directory not found: $ENV_DIR"
    echo ""
    EXAMPLE="${SCRIPT_DIR}/envs/${ENV_NAME}-example"
    if [[ -d "$EXAMPLE" ]]; then
        echo "Create it from the example:"
        echo "  cp -r $EXAMPLE $ENV_DIR"
    else
        echo "Ensure the environment directory exists at: $ENV_DIR"
    fi
    exit 1
fi

if [[ ! -f "$COMPOSE_ENV" ]]; then
    echo "Error: compose.env not found in $ENV_DIR"
    exit 1
fi

# ─────────────────────────────────────────────────────────────────────────────
# Execute
# ─────────────────────────────────────────────────────────────────────────────
exec docker compose \
    --project-directory "$PROJECT_ROOT" \
    -f "$SCRIPT_DIR/docker-compose.yml" \
    -f "$OVERLAY" \
    --env-file "$COMPOSE_ENV" \
    "$@"
