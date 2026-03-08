<#
.SYNOPSIS
    Publish Deployment Artifacts

.DESCRIPTION
    Generates a production-ready deployment package from the Aspire AppHost.
    Output contains docker-compose.yaml, .env template, and env file examples.

.PARAMETER OutputDir
    Output directory (default: deploy\compose\)

.EXAMPLE
    .\deploy\publish.ps1

.EXAMPLE
    .\deploy\publish.ps1 -o C:\deploy\output

.NOTES
    Prerequisites:
      - .NET SDK (dotnet)
      - Aspire CLI: dotnet tool install --global aspire.cli --prerelease
#>

param(
    [Alias("o")]
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$AppHost = Join-Path $ProjectRoot "src\backend\MyProject.AppHost"

if (-not $OutputDir) {
    $OutputDir = Join-Path $ScriptDir "compose"
}

# ─────────────────────────────────────────────────────────────────────────────
# Prerequisites
# ─────────────────────────────────────────────────────────────────────────────
if (-not (Get-Command aspire -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Aspire CLI not found." -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global aspire.cli --prerelease"
    exit 1
}

if (-not (Test-Path $AppHost)) {
    Write-Host "Error: AppHost project not found at $AppHost" -ForegroundColor Red
    exit 1
}

# ─────────────────────────────────────────────────────────────────────────────
# Generate
# ─────────────────────────────────────────────────────────────────────────────
# Clean previous output to avoid stale files
if (Test-Path $OutputDir) {
    Write-Host "Cleaning previous output..."
    Remove-Item -Recurse -Force $OutputDir
}

Write-Host "Publishing deployment artifacts..."
Write-Host "  AppHost: $AppHost"
Write-Host "  Output:  $OutputDir"
Write-Host ""

aspire publish --project $AppHost -o $OutputDir --non-interactive
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Copy env templates
$EnvsDir = Join-Path $OutputDir "envs"
New-Item -ItemType Directory -Path $EnvsDir -Force | Out-Null
Copy-Item (Join-Path $ScriptDir "envs\production-example\api.env") (Join-Path $EnvsDir "api.env")
Copy-Item (Join-Path $ScriptDir "envs\production-example\seed.env") (Join-Path $EnvsDir "seed.env")

Write-Host ""
Write-Host "Deployment package ready at: $OutputDir" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Copy $OutputDir\ to your server"
Write-Host "  2. Fill in .env (images, passwords, domain)"
Write-Host "  3. Fill in envs\api.env (CORS, email, captcha)"
Write-Host "  4. Run: .\deploy\up.ps1 up -d"
