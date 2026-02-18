import * as m from '$lib/paraglide/messages';

type TimelineVariant = 'default' | 'success' | 'destructive' | 'warning';

export function getAuditActionLabel(action: string | undefined): string {
	switch (action) {
		case 'LoginSuccess':
			return m.audit_action_loginSuccess();
		case 'LoginFailure':
			return m.audit_action_loginFailure();
		case 'Logout':
			return m.audit_action_logout();
		case 'Register':
			return m.audit_action_register();
		case 'PasswordChange':
			return m.audit_action_passwordChange();
		case 'PasswordResetRequest':
			return m.audit_action_passwordResetRequest();
		case 'PasswordReset':
			return m.audit_action_passwordReset();
		case 'EmailVerification':
			return m.audit_action_emailVerification();
		case 'ResendVerificationEmail':
			return m.audit_action_resendVerificationEmail();
		case 'ProfileUpdate':
			return m.audit_action_profileUpdate();
		case 'AccountDeletion':
			return m.audit_action_accountDeletion();
		case 'AdminCreateUser':
			return m.audit_action_adminCreateUser();
		case 'AdminLockUser':
			return m.audit_action_adminLockUser();
		case 'AdminUnlockUser':
			return m.audit_action_adminUnlockUser();
		case 'AdminDeleteUser':
			return m.audit_action_adminDeleteUser();
		case 'AdminVerifyEmail':
			return m.audit_action_adminVerifyEmail();
		case 'AdminSendPasswordReset':
			return m.audit_action_adminSendPasswordReset();
		case 'AdminAssignRole':
			return m.audit_action_adminAssignRole();
		case 'AdminRemoveRole':
			return m.audit_action_adminRemoveRole();
		case 'AdminCreateRole':
			return m.audit_action_adminCreateRole();
		case 'AdminUpdateRole':
			return m.audit_action_adminUpdateRole();
		case 'AdminDeleteRole':
			return m.audit_action_adminDeleteRole();
		case 'AdminSetRolePermissions':
			return m.audit_action_adminSetRolePermissions();
		default:
			return action ?? '-';
	}
}

export function getAuditActionVariant(action: string | undefined): TimelineVariant {
	if (!action) return 'default';

	switch (action) {
		case 'LoginSuccess':
		case 'Register':
		case 'EmailVerification':
		case 'AdminUnlockUser':
			return 'success';
		case 'LoginFailure':
		case 'AccountDeletion':
		case 'AdminDeleteUser':
		case 'AdminDeleteRole':
		case 'AdminLockUser':
			return 'destructive';
		case 'AdminCreateUser':
		case 'AdminVerifyEmail':
		case 'AdminSendPasswordReset':
		case 'AdminAssignRole':
		case 'AdminRemoveRole':
		case 'AdminCreateRole':
		case 'AdminUpdateRole':
		case 'AdminSetRolePermissions':
			return 'warning';
		default:
			return 'default';
	}
}

export function formatAuditDate(date: string | null | undefined): string {
	if (!date) return '-';
	return new Date(date).toLocaleString(undefined, {
		month: 'short',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit'
	});
}

export function formatAuditDateFull(date: string | null | undefined): string {
	if (!date) return '-';
	return new Date(date).toLocaleString();
}

export function getAuditDescription(
	action: string | undefined,
	metadata: string | null | undefined
): string | undefined {
	if (!metadata) return undefined;

	try {
		const parsed = JSON.parse(metadata) as Record<string, unknown>;

		if (parsed['Role'] || parsed['role']) {
			return m.audit_desc_role({ role: String(parsed['Role'] ?? parsed['role']) });
		}

		const keys = Object.keys(parsed);
		if (keys.length > 0) {
			return keys.map((k) => `${k}: ${String(parsed[k])}`).join(', ');
		}
	} catch {
		return metadata;
	}

	return undefined;
}
