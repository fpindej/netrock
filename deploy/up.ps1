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
$EnvDir = Join-Path $ScriptDir "envs\$Environment"
$ComposeEnv = Join-Path $EnvDir "compose.env"

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

if (-not (Test-Path $EnvDir)) {
    Write-Host "Error: Environment directory not found: $EnvDir" -ForegroundColor Red
    Write-Host ""
    $Example = Join-Path $ScriptDir "envs\$Environment-example"
    if (Test-Path $Example) {
        Write-Host "Create it from the example:"
        Write-Host "  Copy-Item -Recurse `"$Example`" `"$EnvDir`""
    } else {
        Write-Host "Ensure the environment directory exists at: $EnvDir"
    }
    exit 1
}

if (-not (Test-Path $ComposeEnv)) {
    Write-Host "Error: compose.env not found in $EnvDir" -ForegroundColor Red
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
    "--env-file", $ComposeEnv
)

if ($ComposeArgs) {
    $dockerArgs += $ComposeArgs
}

& docker @dockerArgs
exit $LASTEXITCODE
