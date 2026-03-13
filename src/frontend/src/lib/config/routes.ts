import { Permissions } from '$lib/utils/permissions';

/**
 * Centralized route path constants.
 * Use these instead of hardcoding path strings to keep navigation targets
 * in sync and make route changes a single-point edit.
 */
export const routes = {
	dashboard: '/dashboard',
	login: '/login',
	register: '/register',
	forgotPassword: '/forgot-password',
	profile: '/profile',
	settings: '/settings'
} as const;

/**
 * Admin route registry - single source of truth for admin paths and their
 * required RBAC permissions. Consumed by route guards, sidebar, command
 * palette, and breadcrumbs so that path-permission pairs are never duplicated.
 */
interface AdminRoute {
	path: string;
	permission: string;
}

export const adminRoutes = {
	users: { path: '/admin/users', permission: Permissions.Users.View },
	roles: { path: '/admin/roles', permission: Permissions.Roles.View },
	jobs: { path: '/admin/jobs', permission: Permissions.Jobs.View },
	oauthProviders: { path: '/admin/oauth-providers', permission: Permissions.OAuthProviders.View }
} as const satisfies Record<string, AdminRoute>;
