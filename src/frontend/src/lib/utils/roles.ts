/**
 * Client-side role hierarchy utilities.
 * Ranks come from the admin roles API (`role.rank`) - the server is authoritative.
 */

import type { AdminRole } from '$lib/types';

/** Map of role name to hierarchy rank, built from the admin roles API. */
export type RoleRankMap = ReadonlyMap<string, number>;

/** Builds a role name to rank map from the admin roles list. */
export function buildRoleRankMap(roles: AdminRole[]): RoleRankMap {
	const rankMap = new Map<string, number>();
	for (const role of roles) {
		if (role.name) {
			rankMap.set(role.name, role.rank ?? 0);
		}
	}
	return rankMap;
}

/** Returns the numeric rank for a role name. Unknown roles return 0. */
export function getRoleRank(role: string, rankMap: RoleRankMap): number {
	return rankMap.get(role) ?? 0;
}

/** Returns the highest rank from a list of role names. */
export function getHighestRank(roles: string[], rankMap: RoleRankMap): number {
	return Math.max(0, ...roles.map((role) => getRoleRank(role, rankMap)));
}

/** Returns true if the caller's roles outrank the target's roles (strictly greater). */
export function canManageUser(
	callerRoles: string[],
	targetRoles: string[],
	rankMap: RoleRankMap
): boolean {
	return getHighestRank(callerRoles, rankMap) > getHighestRank(targetRoles, rankMap);
}
