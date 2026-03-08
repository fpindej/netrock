/**
 * Client-side permission utilities.
 * Mirrors backend AppPermissions constants.
 */

import type { User } from '$lib/types';
import { SystemRoles } from './roles';

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

/** Returns true if the user is a Superuser (implicit all permissions). */
export function isSuperuser(user: User | null | undefined): boolean {
	return user?.roles?.includes(SystemRoles.Superuser) ?? false;
}

/** Returns true if the user has a specific permission. Superuser implicitly has all. */
export function hasPermission(user: User | null | undefined, permission: string): boolean {
	return isSuperuser(user) || (user?.permissions?.includes(permission) ?? false);
}

/** Returns true if the user has at least one of the given permissions. */
export function hasAnyPermission(user: User | null | undefined, permissions: string[]): boolean {
	return permissions.some((p) => hasPermission(user, p));
}
