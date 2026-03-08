# Rename SuperAdmin to Superuser

**Date**: 2026-03-08
**Scope**: Rename the SuperAdmin role to Superuser across the entire codebase, narrow last-admin protection to Superuser only, add init script credential prompting, fix MimeKit CVE

## Summary

Renamed the `SuperAdmin` role to `Superuser` across backend, frontend, tests, docs, skills, and init scripts. Narrowed the last-admin protection so only the Superuser role is protected from removal/deletion - Admin role holders can now be freely managed. Added Superuser credential prompting to init scripts so users configure their admin account during setup instead of using hardcoded defaults. Also bumped MailKit to 4.15.1 to resolve a MimeKit CRLF injection vulnerability.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/.../AppRoles.cs` | Renamed `SuperAdmin` constant to `Superuser` | Role rename |
| `src/backend/.../AppPermissions.cs` | Updated `SuperAdminPermissionsFixed` to `SuperuserPermissionsFixed` | Consistency |
| `src/backend/.../AdminService.cs` | Changed last-admin checks to use `AppRoles.Superuser` | Narrow protection to Superuser only |
| `src/backend/.../RoleManagementService.cs` | Updated role references | Consistency |
| `src/backend/.../UserService.cs` | Updated self-deletion protection to Superuser | Narrow protection |
| `src/backend/.../PermissionAuthorizationHandler.cs` | Updated implicit-all bypass to `Superuser` | Consistency |
| `src/backend/.../ErrorMessages.cs` | Updated messages to say "Superuser", fixed em dash | Consistency, rule compliance |
| `src/backend/.../appsettings.Development.json` | Single Superuser seed with `{INIT_SUPERUSER_EMAIL/PASSWORD}` placeholders | Configurable credentials, simplified seed |
| `src/backend/Directory.Packages.props` | MailKit 4.15.0 -> 4.15.1 | Fix MimeKit CRLF injection CVE (GHSA-g7hc-96xr-gvvx) |
| `init.sh` / `init.ps1` | Added `--email`/`--password` flags and Superuser credential prompting | Users configure admin account during init |
| `src/frontend/.../roles.ts` | `SuperAdmin` -> `Superuser` in role constants | Frontend consistency |
| `src/frontend/.../permissions.ts` | `isSuperAdmin` -> `isSuperuser` | Frontend consistency |
| `src/frontend/.../RoleCardGrid.svelte` | Updated Superuser check | Frontend consistency |
| `src/frontend/.../v1.d.ts` | Regenerated types | API contract update |
| All test files | Updated role references and test names | Test consistency |
| Docs and skills | Updated all `SuperAdmin` references to `Superuser` | Documentation consistency |

## Decisions & Reasoning

### Role name: Superuser instead of SuperAdmin

- **Choice**: `Superuser` - a single word, Unix-inspired
- **Alternatives considered**: Keep `SuperAdmin`, use `Root`
- **Reasoning**: `SuperAdmin` is redundant (super + admin). `Superuser` is a well-understood concept from Unix systems, is more concise, and avoids the awkward PascalCase of two concatenated words. `Root` was too Unix-specific and doesn't convey the admin aspect.

### Narrow protection to Superuser only

- **Choice**: Only the last Superuser is protected from deletion/role removal. Admin users can be freely managed.
- **Alternatives considered**: Protect both Superuser and Admin (previous behavior)
- **Reasoning**: Protecting Admin was overly cautious - if a Superuser accidentally deletes all Admins, they can simply create new ones. The only truly dangerous scenario is losing the last Superuser, which would lock out all administrative access.

### Init script credential prompting

- **Choice**: Prompt for Superuser email/password during init with defaults (`superuser@test.com` / `Superuser123!`)
- **Alternatives considered**: Keep hardcoded credentials, generate random password
- **Reasoning**: Prompting personalizes the dev environment from the start. Sensible defaults keep the non-interactive (`--yes`) flow fast. Random passwords would require users to look up credentials every time.

## Follow-Up Items

- [ ] Consider bulk-renaming em dashes to regular dashes in XML doc comments (pre-existing, not introduced here)
