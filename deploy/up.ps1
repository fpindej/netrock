<#
.SYNOPSIS
    Compose Launcher

.DESCRIPTION
    Runs docker compose against the Aspire-generated deployment package.

.PARAMETER ComposeArgs
    Arguments passed to docker compose

.EXAMPLE
    .\deploy\up.ps1 up -d

.EXAMPLE
    .\deploy\up.ps1 logs -f api

.NOTES
    First-time setup:
      1. .\deploy\publish.ps1
      2. Fill in deploy\compose\.env and deploy\compose\envs\*.env
      3. .\deploy\up.ps1 up -d

    Local development uses Aspire (see MyProject.AppHost):
      dotnet run --project src\backend\MyProject.AppHost
#>

param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$ComposeArgs
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ComposeDir = Join-Path $ScriptDir "compose"
$ComposeFile = Join-Path $ComposeDir "docker-compose.yaml"

if (-not $ComposeArgs -or $ComposeArgs.Count -eq 0) {
    Write-Host "Usage: .\deploy\up.ps1 [docker compose args...]"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\deploy\up.ps1 up -d          Start the stack"
    Write-Host "  .\deploy\up.ps1 logs -f api    Follow API logs"
    Write-Host "  .\deploy\up.ps1 down           Stop the stack"
    Write-Host "  .\deploy\up.ps1 ps             List running services"
    Write-Host ""
    Write-Host "Setup:"
    Write-Host "  1. .\deploy\publish.ps1          Generate compose files"
    Write-Host "  2. Edit deploy\compose\.env       Fill in secrets"
    Write-Host "  3. .\deploy\up.ps1 up -d         Start"
    exit 1
}

if (-not (Test-Path $ComposeFile)) {
    Write-Host "Error: No deployment package found at $ComposeDir" -ForegroundColor Red
    Write-Host ""
    Write-Host "Generate it first:"
    Write-Host "  .\deploy\publish.ps1"
    exit 1
}

$dockerArgs = @(
    "compose",
    "--project-directory", $ComposeDir,
    "-f", $ComposeFile
)
$dockerArgs += $ComposeArgs

& docker @dockerArgs
exit $LASTEXITCODE
