# Fix: Indirect Privilege Escalation via Custom Role Assignment

**Date**: 2026-02-23
**Scope**: Prevent admins from assigning custom roles that carry permissions they don't hold

## Summary

`AssignRoleAsync` allowed any admin with `users.assign_roles` to assign users to any custom role (rank 0), regardless of that role's permissions. An admin could escalate a user's privileges beyond their own by assigning a pre-existing custom role with permissions the admin doesn't hold. This fix adds a permission-subset check for custom role assignments, closing the indirect escalation path. Companion fix to #272 (direct escalation via `SetRolePermissions`).

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/MyProject.Shared/ErrorMessages.cs` | Added `Admin.RoleAssignEscalation` constant | Consistent error message for the new guard |
| `src/backend/MyProject.Infrastructure/Features/Admin/Services/AdminService.cs` | Added `EnforceRolePermissionEscalationAsync` private method; wired into `AssignRoleAsync` for rank-0 roles | Core fix — checks target role's permissions are a subset of caller's permissions |
| `src/backend/MyProject.Application/Features/Admin/IAdminService.cs` | Updated `AssignRoleAsync` XML doc | Document the new escalation guard in the interface contract |
| `src/backend/tests/MyProject.Component.Tests/Services/AdminServiceTests.cs` | Added 6 component tests | Cover all guard paths: unheld permissions, held permissions, multi-role aggregation, SuperAdmin bypass, system role skip, empty permissions |
| `src/backend/tests/MyProject.Api.Tests/Controllers/AdminControllerTests.cs` | Added 1 API test | Verify 403 + ProblemDetails response for the escalation path |

## Decisions & Reasoning

### Guard only custom roles (rank 0)

- **Choice**: The escalation check only fires when `AppRoles.GetRoleRank(input.Role) == 0`
- **Alternatives considered**: Checking all roles regardless of rank
- **Reasoning**: System roles (User=1, Admin=2, SuperAdmin=3) are already guarded by the rank hierarchy — an admin cannot assign roles at or above their own rank. Custom roles have rank 0, making them assignable by any admin, so they need the additional permission-subset check.

### Reuse callerRoles already loaded in AssignRoleAsync

- **Choice**: Pass `callerRoles` (fetched at line 117) into the guard method instead of re-fetching
- **Alternatives considered**: Passing `callerUserId` and resolving roles inside the guard (as `RoleManagementService.EnforcePermissionEscalationAsync` does)
- **Reasoning**: `callerRoles` is already available in scope. Passing it avoids a redundant DB round-trip while keeping the method signature explicit about its inputs.

### RemoveRoleAsync intentionally excluded

- **Choice**: No escalation guard on `RemoveRoleAsync`
- **Alternatives considered**: Adding a symmetric guard for removal
- **Reasoning**: Removing a role reduces permissions, not escalates. The existing rank check is sufficient. Same principle as #272.

## Follow-Up Items

- [ ] Consider adding a distributed lock or serializable transaction to close the theoretical TOCTOU gap between the permission check and role assignment (low priority — exploiting requires a separate admin to simultaneously modify the role, which is itself gated by the companion fix)
