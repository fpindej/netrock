/**
 * Centralized route path constants.
 * Use these instead of hardcoding path strings to keep navigation targets
 * in sync and make route changes a single-point edit.
 */
export const routes = {
	dashboard: '/dashboard',
	login: '/login',
	profile: '/profile',
	settings: '/settings',
	admin: {
		users: '/admin/users',
		roles: '/admin/roles',
		jobs: '/admin/jobs',
		oauthProviders: '/admin/oauth-providers'
	}
} as const;
