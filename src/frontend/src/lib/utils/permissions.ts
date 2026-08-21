/**
 * Client-side permission utilities.
 * Mirrors backend AppPermissions constants.
 */

import type { User } from '$lib/types';

export const Permissions = {
	Users: {
		View: 'users.view',
		ViewPii: 'users.view_pii',
		Manage: 'users.manage',
		AssignRoles: 'users.assign_roles',
		ManageTwoFactor: 'users.manage_2fa'
	},
	Roles: {
		View: 'roles.view',
		Manage: 'roles.manage'
	},
	Jobs: {
		View: 'jobs.view',
		Manage: 'jobs.manage'
	},
	OAuthProviders: {
		View: 'oauth_providers.view',
		Manage: 'oauth_providers.manage'
	}
} as const;

/** Returns true if the user has a specific permission. The wildcard `*` grants all. */
export function hasPermission(user: User | null | undefined, permission: string): boolean {
	const permissions = user?.permissions ?? [];
	return permissions.includes('*') || permissions.includes(permission);
}

/** Returns true if the user has at least one of the given permissions. */
export function hasAnyPermission(user: User | null | undefined, permissions: string[]): boolean {
	return permissions.some((p) => hasPermission(user, p));
}
