# Init & Deploy Script Fixes

**Date**: 2026-02-16
**Scope**: Bug fixes and UX improvements across init.sh, init.ps1, deploy.sh, deploy.ps1

## Summary

Fixed critical bugs in the PowerShell init script (RandomNumberGenerator crash on Windows PS 5.1, Write-Warning cmdlet shadowing, Windows-only self-deletion) and improved UX across all four scripts with single-keypress checklist toggles, root directory validation, elapsed time display, and removal of stale naming. Also corrected the migration output directory from the old `Features/Postgres/Migrations` to `Persistence/Migrations` in both init scripts.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `init.sh` | Fix migration dir `Features/Postgres/Migrations` -> `Persistence/Migrations` (4 places) | Path didn't match actual project structure |
| `init.ps1` | Fix migration dir (3 places) | Same |
| `init.ps1` | `RandomNumberGenerator::Fill()` -> `Create()` + `GetBytes()` + `Dispose()` | `Fill()` is .NET Core 2.1+ only; Windows PS 5.1 uses .NET Framework |
| `init.ps1` | Rename `Write-Warning` function -> `Write-WarnMsg` | Shadowed the built-in PowerShell `Write-Warning` cmdlet |
| `init.ps1` | Self-deletion: detect current PS exe via `(Get-Process -Id $PID).Path`, conditional `-WindowStyle` | `powershell` command and `-WindowStyle Hidden` are Windows-only |
| `init.sh`, `init.ps1` | Checklist: `read -rsn1` / `[Console]::ReadKey($true)` | Single keypress to toggle, no Enter needed per toggle |
| `init.sh`, `init.ps1` | Replace `clear`/`Clear-Host` with blank line | Don't wipe user's terminal scrollback |
| `init.sh`, `init.ps1` | Add root dir validation (`src/backend` + `src/frontend` must exist) | Prevent running from wrong directory |
| `init.sh`, `init.ps1` | Add `node` to prerequisite checks | Fullstack template needs Node.js |
| `init.sh`, `init.ps1` | Add elapsed time at completion | UX feedback |
| `init.sh`, `init.ps1` | "Web API Template" -> "Project Initialization" | Stale naming from before template rename |
| `init.sh`, `init.ps1` | "Docker Desktop" -> "Docker" | Users may use OrbStack, Rancher Desktop, Colima, etc. |
| `deploy.sh`, `deploy.ps1` | Rename `Write-Warning` -> `Write-WarnMsg` in PS1 | Same cmdlet shadowing issue |
| `deploy.sh`, `deploy.ps1` | Remove "Web API Template" naming | Stale naming |
| `deploy.ps1` | `$env:USERPROFILE` -> `$HOME` for Docker config path | `$env:USERPROFILE` doesn't exist on macOS/Linux PS Core |
| `deploy.sh`, `deploy.ps1` | Add root dir validation and elapsed time | Consistency with init scripts |

## Decisions & Reasoning

### Single-keypress checklist vs Read-Host/read

- **Choice**: `read -rsn1` (bash) and `[Console]::ReadKey($true)` (PowerShell)
- **Alternatives considered**: Keep `read`/`Read-Host` requiring Enter after each toggle
- **Reasoning**: Instant-toggle feels natural for a checklist. Enter is only needed once to confirm the final selection. Cursor redraw math simplified (no prompt line to account for).

### RandomNumberGenerator API choice

- **Choice**: Instance-based `Create()` + `GetBytes()` + `Dispose()`
- **Alternatives considered**: Keep `Fill()` (static), require PS 7+
- **Reasoning**: `Create()` + `GetBytes()` works on both .NET Framework (PS 5.1) and .NET Core (PS 7+). PS 5.1 is still the default shell on Windows, so cross-compat matters.

### Write-WarnMsg naming

- **Choice**: Rename custom function to `Write-WarnMsg`
- **Alternatives considered**: Use the built-in `Write-Warning` directly, prefix with script scope
- **Reasoning**: The custom function outputs `[WARN]` with specific formatting that differs from PowerShell's built-in warning stream. Renaming avoids silent behavioral surprises.

## Follow-Up Items

- [ ] Test init.ps1 on Windows PowerShell 5.1 to confirm RandomNumberGenerator fix
- [ ] Test deploy.ps1 on macOS with PowerShell Core to confirm `$HOME` path works
