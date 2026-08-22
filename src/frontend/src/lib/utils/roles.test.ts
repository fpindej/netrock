import { describe, expect, it } from 'vitest';
import type { AdminRole } from '$lib/types';
import { buildRoleRankMap, canManageUser, getHighestRank, getRoleRank } from './roles';

/** Creates a minimal AdminRole object for testing. */
function makeRole(name: string, rank: number): AdminRole {
	return {
		id: '00000000-0000-0000-0000-000000000001',
		name,
		rank,
		isSystem: rank > 0,
		userCount: 0,
		permissions: []
	};
}

const API_ROLES: AdminRole[] = [
	makeRole('Superuser', 3),
	makeRole('Admin', 2),
	makeRole('User', 1),
	makeRole('Moderator', 0)
];

const RANK_MAP = buildRoleRankMap(API_ROLES);

describe('buildRoleRankMap', () => {
	it('maps each role name to its rank', () => {
		expect(RANK_MAP.get('Superuser')).toBe(3);
		expect(RANK_MAP.get('Admin')).toBe(2);
		expect(RANK_MAP.get('User')).toBe(1);
		expect(RANK_MAP.get('Moderator')).toBe(0);
	});

	it('returns an empty map for an empty roles list', () => {
		expect(buildRoleRankMap([]).size).toBe(0);
	});

	it('defaults a missing rank to 0', () => {
		const role = makeRole('Custom', 0);
		delete role.rank;
		const map = buildRoleRankMap([role]);
		expect(map.get('Custom')).toBe(0);
	});

	it('skips roles without a name', () => {
		const role = makeRole('Nameless', 2);
		delete role.name;
		const map = buildRoleRankMap([role]);
		expect(map.size).toBe(0);
	});
});

describe('getRoleRank', () => {
	it('returns the rank from the map', () => {
		expect(getRoleRank('Superuser', RANK_MAP)).toBe(3);
		expect(getRoleRank('Admin', RANK_MAP)).toBe(2);
		expect(getRoleRank('User', RANK_MAP)).toBe(1);
	});

	it('returns 0 for unknown roles', () => {
		expect(getRoleRank('Unknown', RANK_MAP)).toBe(0);
	});

	it('returns 0 for an empty role name', () => {
		expect(getRoleRank('', RANK_MAP)).toBe(0);
	});

	it('is case-sensitive (unknown casing ranks 0)', () => {
		expect(getRoleRank('superuser', RANK_MAP)).toBe(0);
		expect(getRoleRank('admin', RANK_MAP)).toBe(0);
	});

	it('returns 0 for any role with an empty map', () => {
		expect(getRoleRank('Superuser', buildRoleRankMap([]))).toBe(0);
	});
});

describe('getHighestRank', () => {
	it('returns the highest rank among roles', () => {
		expect(getHighestRank(['User', 'Admin'], RANK_MAP)).toBe(2);
		expect(getHighestRank(['User', 'Superuser', 'Admin'], RANK_MAP)).toBe(3);
	});

	it('returns 0 for an empty roles list', () => {
		expect(getHighestRank([], RANK_MAP)).toBe(0);
	});

	it('returns 0 when all roles are unknown', () => {
		expect(getHighestRank(['Unknown', 'Viewer'], RANK_MAP)).toBe(0);
	});

	it('returns the rank of a single role', () => {
		expect(getHighestRank(['Admin'], RANK_MAP)).toBe(2);
	});

	it('ignores unknown roles when a known role is present', () => {
		expect(getHighestRank(['Unknown', 'User', 'AnotherUnknown'], RANK_MAP)).toBe(1);
	});

	it('returns 0 for any roles with an empty map', () => {
		expect(getHighestRank(['Superuser', 'Admin'], buildRoleRankMap([]))).toBe(0);
	});
});

describe('canManageUser', () => {
	it('higher rank can manage lower rank', () => {
		expect(canManageUser(['Superuser'], ['Admin'], RANK_MAP)).toBe(true);
		expect(canManageUser(['Superuser'], ['User'], RANK_MAP)).toBe(true);
		expect(canManageUser(['Admin'], ['User'], RANK_MAP)).toBe(true);
	});

	it('lower rank cannot manage higher rank', () => {
		expect(canManageUser(['Admin'], ['Superuser'], RANK_MAP)).toBe(false);
		expect(canManageUser(['User'], ['Admin'], RANK_MAP)).toBe(false);
	});

	it('equal rank cannot manage (strictly greater required)', () => {
		expect(canManageUser(['User'], ['User'], RANK_MAP)).toBe(false);
		expect(canManageUser(['Admin'], ['Admin'], RANK_MAP)).toBe(false);
		expect(canManageUser(['Superuser'], ['Superuser'], RANK_MAP)).toBe(false);
	});

	it('caller without roles cannot manage anyone', () => {
		expect(canManageUser([], ['User'], RANK_MAP)).toBe(false);
	});

	it('caller with rank can manage a target without roles', () => {
		expect(canManageUser(['User'], [], RANK_MAP)).toBe(true);
	});

	it('neither side with roles cannot manage', () => {
		expect(canManageUser([], [], RANK_MAP)).toBe(false);
	});

	it('uses the highest rank on each side', () => {
		expect(canManageUser(['User', 'Admin'], ['User'], RANK_MAP)).toBe(true);
		expect(canManageUser(['Admin'], ['User', 'Superuser'], RANK_MAP)).toBe(false);
	});

	it('unknown caller role cannot manage a ranked target', () => {
		expect(canManageUser(['Moderator'], ['User'], RANK_MAP)).toBe(false);
	});

	it('ranked caller can manage an unknown-role target', () => {
		expect(canManageUser(['User'], ['Unknown'], RANK_MAP)).toBe(true);
	});

	it('denies everything with an empty map (roles load failed)', () => {
		const emptyMap = buildRoleRankMap([]);
		expect(canManageUser(['Superuser'], ['User'], emptyMap)).toBe(false);
	});
});
