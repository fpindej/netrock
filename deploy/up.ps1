<#
.SYNOPSIS
    Environment Launcher

.DESCRIPTION
    Thin wrapper that constructs the correct `docker compose` command
    for a given environment profile.

.PARAMETER Environment
    The environment to launch (e.g., local, production)

.PARAMETER ComposeArgs
    Additional arguments passed to docker compose

.EXAMPLE
    .\deploy\up.ps1 local up -d --build

.EXAMPLE
    .\deploy\up.ps1 production up -d

.EXAMPLE
    .\deploy\up.ps1 local logs -f api
#>

param(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$Environment,

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$ComposeArgs
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# ─────────────────────────────────────────────────────────────────────────────
# Resolve files
# ─────────────────────────────────────────────────────────────────────────────
$Overlay = Join-Path $ScriptDir "docker-compose.$Environment.yml"
$EnvFile = Join-Path $ScriptDir "envs\$Environment.env"

if (-not (Test-Path $Overlay)) {
    Write-Host "Error: Unknown environment '$Environment'" -ForegroundColor Red
    Write-Host ""
    Write-Host "Available environments:"
    Get-ChildItem -Path $ScriptDir -Filter "docker-compose.*.yml" | ForEach-Object {
        $envName = $_.Name -replace '^docker-compose\.(.*)\.yml$', '$1'
        Write-Host "  $envName"
    }
    exit 1
}

if (-not (Test-Path $EnvFile)) {
    Write-Host "Error: Environment file not found: $EnvFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Create it from the example:"
    Write-Host "  Copy-Item `"$ScriptDir\envs\$Environment.env.example`" `"$EnvFile`""
    exit 1
}

# ─────────────────────────────────────────────────────────────────────────────
# Execute
# ─────────────────────────────────────────────────────────────────────────────
$baseCompose = Join-Path $ScriptDir "docker-compose.yml"

$dockerArgs = @(
    "compose",
    "--project-directory", $ProjectRoot,
    "-f", $baseCompose,
    "-f", $Overlay,
    "--env-file", $EnvFile
)

if ($ComposeArgs) {
    $dockerArgs += $ComposeArgs
}

& docker @dockerArgs
exit $LASTEXITCODE
