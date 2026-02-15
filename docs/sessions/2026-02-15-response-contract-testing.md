# Response Contract Testing

**Date**: 2026-02-15
**Scope**: API response contract assertions — frozen test records that catch silent response DTO changes

## Summary

Added response contract testing to the API integration test suite. All success endpoints (200/201 with a body) now deserialize responses into independent contract records and assert key fields. If a production response DTO field is renamed, removed, or has its nullability changed, tests fail instead of silently passing while real consumers break.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `tests/MyProject.Api.Tests/Contracts/ResponseContracts.cs` | Created 13 contract records covering Auth, Users, Admin, and Jobs responses | Frozen copies of expected response shapes, independent of production DTOs |
| `tests/MyProject.Api.Tests/Controllers/AuthControllerTests.cs` | Added contract deserialization + assertions to 6 success tests | Validate `AuthTokensResponse` and `RegisterUserResponse` field shapes |
| `tests/MyProject.Api.Tests/Controllers/UsersControllerTests.cs` | Added contract assertions to 2 success tests | Validate `UserMeResponse` fields (Id, Username, FirstName, Roles) |
| `tests/MyProject.Api.Tests/Controllers/AdminControllerTests.cs` | Added contract assertions to 7 success tests | Validate `AdminUserListResponse`, `AdminUserResponse`, `AdminRoleResponse`, `RoleDetailResponse`, `CreateRoleResponse`, `PermissionGroupResponse` |
| `tests/MyProject.Api.Tests/Controllers/JobsControllerTests.cs` | Added contract assertions to 2 success tests | Validate `RecurringJobResponse` and `RecurringJobDetailResponse` |
| `SKILLS.md` | Added step 6 to "Add an API Integration Test" recipe | Document the contract testing pattern |
| `FILEMAP.md` | Added `Contracts/ResponseContracts.cs` to tree; added contract file to response DTO change impact | Track the new file and its change dependencies |
| `src/backend/AGENTS.md` | Added `Contracts/` to API Tests tree; added "Response Contract Testing" section; added step 6 to "Adding a New Test" | Document the pattern, its purpose, and when to use it |

## Decisions & Reasoning

### Independent Contract Records (not reusing production DTOs)

- **Choice**: Frozen `record` types in the test project that duplicate the expected response shape
- **Alternatives considered**: Deserializing into production `*Response` DTOs directly, using `JsonElement` assertions
- **Reasoning**: Reusing production DTOs defeats the purpose — a field rename would update both the DTO and the test type, hiding the breaking change. `JsonElement` assertions are verbose and fragile. Independent records are the sweet spot: concise, frozen, and they catch drift between what the API promises and what it delivers.

### Single File for All Contracts

- **Choice**: One `ResponseContracts.cs` file with all 13 records
- **Alternatives considered**: One file per controller, one file per feature
- **Reasoning**: The full API response surface fits in ~35 lines. A single file makes it easy to scan the entire contract at a glance. Splitting into multiple files adds navigation overhead with no benefit at this scale.

### `List<T>` Instead of `IReadOnlyList<T>` in Contract Records

- **Choice**: Concrete `List<T>` for collection properties
- **Alternatives considered**: `IReadOnlyList<T>`, `IEnumerable<T>`
- **Reasoning**: `System.Text.Json`'s `ReadFromJsonAsync<T>()` deserializes more reliably into concrete types. Since these are test-only records (not production API surface), mutability is irrelevant.

## Follow-Up Items

- [ ] Add contract assertions to any future success endpoints as they're created
