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
		AssignRoles: 'users.assign_roles'
	},
	Roles: {
		View: 'roles.view',
		Manage: 'roles.manage'
	},
	Jobs: {
		View: 'jobs.view',
		Manage: 'jobs.manage'
	}
} as const;

/** Returns true if the user is a SuperAdmin (implicit all permissions). */
export function isSuperAdmin(user: User | null | undefined): boolean {
	return user?.roles?.includes('SuperAdmin') ?? false;
}

/** Returns true if the user has a specific permission. SuperAdmin implicitly has all. */
export function hasPermission(user: User | null | undefined, permission: string): boolean {
	return isSuperAdmin(user) || (user?.permissions?.includes(permission) ?? false);
}

/** Returns true if the user has at least one of the given permissions. */
export function hasAnyPermission(user: User | null | undefined, permissions: string[]): boolean {
	return permissions.some((p) => hasPermission(user, p));
}
