# PII Compliance — Permission-Gated Masking

**Date**: 2026-02-23
**Scope**: Admin user endpoints now mask PII (email, username, phone) unless the caller has the `users.view_pii` permission

## Summary

Added a `users.view_pii` permission that controls visibility of personally identifiable information in admin user endpoints (`/api/admin/users`, `/api/admin/users/{id}`). Without the permission, email is masked to `j***@g***.com` format, username follows the same pattern, and phone numbers become `***`. SuperAdmin sees everything implicitly. Users always see their own data unmasked (self-view exemption).

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Application/.../AppPermissions.cs` | Added `Users.ViewPii = "users.view_pii"` | New permission constant; auto-discovered by reflection |
| `Application/.../IUserContext.cs` | Added `bool HasPermission(string permission)` | Controller needs to check permissions without `[RequirePermission]` attribute |
| `Infrastructure/.../UserContext.cs` | Implemented `HasPermission` with SuperAdmin bypass | Mirrors `PermissionAuthorizationHandler` logic |
| `WebApi/.../PiiMasker.cs` | New static class: `MaskEmail`, `MaskPhone` | Reusable masking utilities |
| `WebApi/.../AdminMapper.cs` | Added `WithMaskedPii` extension methods for single user and list | Applies masking while preserving non-PII fields and self-view exemption |
| `WebApi/.../AdminController.cs` | Applied masking in `ListUsers` and `GetUser` | Permission check at controller level, after mapping |
| `frontend/.../permissions.ts` | Added `ViewPii` to `Permissions.Users` | Frontend permission mirror |
| `Api.Tests/.../PiiMaskerTests.cs` | 11 unit tests for email/phone masking edge cases | Coverage for empty, whitespace, no-at, no-dot, multi-dot domains |
| `Api.Tests/.../AdminMapperPiiTests.cs` | 5 tests for masking extensions and self-view exemption | Verifies PII masked, non-PII preserved, caller exempt in lists |
| `Api.Tests/.../AdminControllerTests.cs` | Updated `GetUser` test to include `ViewPii` permission; refactored helpers to `TryAddWithoutValidation` | Existing test expected unmasked data; header validation fix for multi-permission values |
| `FILEMAP.md` | Added `PiiMasker.cs` impact row | Change traceability |

## Design Decisions

- **Permission-gated, not config-gated**: A dedicated permission is more flexible than a feature flag — operators can grant it per-role.
- **Masking at controller level**: Keeps Application/Infrastructure layers unaware of masking. The controller is the natural place since it owns the response shape.
- **Self-view exemption**: Users always see their own data unmasked, even in admin views. This avoids confusion when an admin views their own profile.
- **LastIndexOf for TLD**: `mail.co.uk` masks to `m***.uk` — this reveals less information than preserving the full TLD chain and is simpler to implement.
