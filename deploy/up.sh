#!/bin/bash

#══════════════════════════════════════════════════════════════════════════════
#  Compose Launcher
#══════════════════════════════════════════════════════════════════════════════
#
#  Runs docker compose against the Aspire-generated deployment package.
#
#  Usage:
#    ./deploy/up.sh up -d              # Start the stack
#    ./deploy/up.sh logs -f api        # Follow API logs
#    ./deploy/up.sh down               # Stop the stack
#    ./deploy/up.sh ps                 # List running services
#
#  First-time setup:
#    1. Generate: ./deploy/publish.sh
#    2. Fill in:  deploy/compose/.env and deploy/compose/envs/*.env
#    3. Start:    ./deploy/up.sh up -d
#
#  Local development uses Aspire (see MyProject.AppHost):
#    dotnet run --project src/backend/MyProject.AppHost
#
#══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_DIR="$SCRIPT_DIR/compose"

if [[ $# -lt 1 ]]; then
    echo "Usage: $0 [docker compose args...]"
    echo ""
    echo "Examples:"
    echo "  $0 up -d          Start the stack"
    echo "  $0 logs -f api    Follow API logs"
    echo "  $0 down           Stop the stack"
    echo "  $0 ps             List running services"
    echo ""
    echo "Setup:"
    echo "  1. ./deploy/publish.sh          Generate compose files"
    echo "  2. Edit deploy/compose/.env     Fill in secrets"
    echo "  3. $0 up -d                     Start"
    exit 1
fi

if [[ ! -f "$COMPOSE_DIR/docker-compose.yaml" ]]; then
    echo "Error: No deployment package found at $COMPOSE_DIR"
    echo ""
    echo "Generate it first:"
    echo "  ./deploy/publish.sh"
    exit 1
fi

exec docker compose \
    --project-directory "$COMPOSE_DIR" \
    -f "$COMPOSE_DIR/docker-compose.yaml" \
    "$@"
