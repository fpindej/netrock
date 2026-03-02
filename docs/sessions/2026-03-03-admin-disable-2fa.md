# Admin Disable Two-Factor Authentication

**Date**: 2026-03-03
**Scope**: Full-stack feature allowing admins to disable 2FA for locked-out user accounts

## Summary

Added an admin endpoint and UI to disable two-factor authentication for users who have lost access to their authenticator device or recovery codes. The feature includes permission-based access control (`users.manage_2fa`), role hierarchy enforcement, session revocation, notification emails, audit logging, and an AlertDialog-based confirmation flow. Also upgraded the delete user dialog to use AlertDialog for consistency.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `AppPermissions.cs` | Added `ManageTwoFactor` constant | Fine-grained permission for 2FA management |
| `AuditActions.cs` | Added `AdminDisableTwoFactor` action | Track admin 2FA disablement in audit trail |
| `ErrorMessages.cs` | Added 3 error constants | Self-action, not-enabled, and failed messages |
| `EmailTemplateNames.cs` | Added template name constant | Reference for Liquid email template |
| `EmailTemplateModels.cs` | Added `AdminDisableTwoFactorModel` | Typed model for email template rendering |
| `admin-disable-2fa.liquid` | New HTML email template | Notify user their 2FA was disabled by admin |
| `admin-disable-2fa.text.liquid` | New text email template | Plain-text fallback |
| `admin-disable-2fa.subject.liquid` | New subject template | Email subject line |
| `IAdminService.cs` | Added `DisableTwoFactorAsync` method | Service interface contract |
| `AdminService.cs` | Implemented `DisableTwoFactorAsync` | Core business logic with hierarchy, session revocation, cache invalidation |
| `AdminController.cs` | Added `POST disable-2fa` endpoint | API surface with permission + rate limiting |
| `DisableTwoFactorRequest.cs` | New request DTO | Optional reason field |
| `DisableTwoFactorRequestValidator.cs` | New FluentValidation validator | 500-char max on reason |
| `permissions.ts` | Mirrored `ManageTwoFactor` | Frontend permission check |
| `AccountInfoCard.svelte` | Added disable 2FA button + AlertDialog | Admin UI for disabling 2FA with reason |
| `UserDetailCards.svelte` | Derived and passed `canManageTwoFactor` | Permission prop propagation |
| `AccountActions.svelte` | Upgraded delete dialog to AlertDialog | Correct destructive action pattern |
| `alert-dialog/*.svelte` | New shadcn component (12 files) | Destructive action confirmation dialogs |
| `button/button.svelte` | Updated by shadcn (with fixes) | Newer version with `aria-disabled` support |
| `v1.d.ts` | Added endpoint + schema types | Type safety for the new API call |
| `en.json` / `cs.json` | Added 10 i18n keys each | Translations for disable 2FA UI + audit action |
| `AdminServiceDisableTwoFactorTests.cs` | 10 component tests | Service layer coverage |
| `AdminControllerDisableTwoFactorTests.cs` | 9 API integration tests | Endpoint behavior coverage |
| `DisableTwoFactorRequestValidatorTests.cs` | 5 validator tests | Validation rule coverage |

## Decisions & Reasoning

### Separate permission instead of reusing `users.manage`

- **Choice**: Created `users.manage_2fa` as a dedicated permission
- **Alternatives considered**: Reusing `users.manage` to also cover 2FA actions
- **Reasoning**: Disabling 2FA is a high-impact security action that deserves its own permission. This allows orgs to grant general user management without granting 2FA override capability.

### AlertDialog for destructive admin actions

- **Choice**: Used shadcn AlertDialog (not Dialog) for disable 2FA and delete user confirmations
- **Alternatives considered**: Regular Dialog with destructive-styled buttons
- **Reasoning**: AlertDialog provides correct semantic meaning for irreversible destructive actions - it blocks interaction with the rest of the page and requires explicit confirmation. Regular Dialog is reserved for non-destructive flows like password reset.

### Session revocation on 2FA disable

- **Choice**: Reset authenticator key + rotate security stamp + revoke refresh tokens + invalidate cache
- **Alternatives considered**: Only disabling 2FA without revoking sessions
- **Reasoning**: Security-first approach. When 2FA is disabled by an admin, all existing sessions should be invalidated to force re-authentication, ensuring the actual account owner re-establishes their session.

### Manual v1.d.ts update

- **Choice**: Manually added the endpoint type definition to `v1.d.ts`
- **Alternatives considered**: Running the backend to auto-generate types
- **Reasoning**: The `api:generate` script requires a running backend instance. The manual addition follows the exact same pattern as existing endpoints and will be overwritten by the next `pnpm run api:generate` run.

## Follow-Up Items

- [ ] Regenerate `v1.d.ts` from running backend to ensure types match exactly
- [ ] PR 2: shadcn Table component upgrade for user admin table (stacked follow-up)
- [ ] Re-sign commit once 1Password SSH agent is available
