import { describe, expect, it } from 'vitest';
import { canManageUser, getAssignableRoles, getHighestRank, getRoleRank } from './roles';

describe('getRoleRank', () => {
	it('returns 3 for SuperAdmin', () => {
		expect(getRoleRank('SuperAdmin')).toBe(3);
	});

	it('returns 2 for Admin', () => {
		expect(getRoleRank('Admin')).toBe(2);
	});

	it('returns 1 for User', () => {
		expect(getRoleRank('User')).toBe(1);
	});

	it('returns 0 for an unknown role name', () => {
		expect(getRoleRank('Moderator')).toBe(0);
	});

	it('returns 0 for an empty string', () => {
		expect(getRoleRank('')).toBe(0);
	});

	it('is case-sensitive — lowercase variants return 0', () => {
		expect(getRoleRank('superadmin')).toBe(0);
		expect(getRoleRank('admin')).toBe(0);
		expect(getRoleRank('user')).toBe(0);
	});
});

describe('getHighestRank', () => {
	it('returns the highest rank from mixed roles', () => {
		expect(getHighestRank(['User', 'Admin'])).toBe(2);
	});

	it('returns 3 when SuperAdmin is present', () => {
		expect(getHighestRank(['User', 'SuperAdmin', 'Admin'])).toBe(3);
	});

	it('returns 0 for an empty array', () => {
		expect(getHighestRank([])).toBe(0);
	});

	it('returns 0 when all roles are unknown', () => {
		expect(getHighestRank(['Moderator', 'Viewer'])).toBe(0);
	});

	it('returns correct rank for a single known role', () => {
		expect(getHighestRank(['Admin'])).toBe(2);
	});

	it('ignores unknown roles and returns highest known rank', () => {
		expect(getHighestRank(['Unknown', 'User', 'AnotherUnknown'])).toBe(1);
	});
});

describe('canManageUser', () => {
	it('SuperAdmin can manage Admin', () => {
		expect(canManageUser(['SuperAdmin'], ['Admin'])).toBe(true);
	});

	it('SuperAdmin can manage User', () => {
		expect(canManageUser(['SuperAdmin'], ['User'])).toBe(true);
	});

	it('Admin can manage User', () => {
		expect(canManageUser(['Admin'], ['User'])).toBe(true);
	});

	it('Admin cannot manage SuperAdmin', () => {
		expect(canManageUser(['Admin'], ['SuperAdmin'])).toBe(false);
	});

	it('User cannot manage Admin', () => {
		expect(canManageUser(['User'], ['Admin'])).toBe(false);
	});

	it('User cannot manage User — equal rank is not sufficient', () => {
		expect(canManageUser(['User'], ['User'])).toBe(false);
	});

	it('Admin cannot manage Admin — equal rank is not sufficient', () => {
		expect(canManageUser(['Admin'], ['Admin'])).toBe(false);
	});

	it('SuperAdmin cannot manage SuperAdmin — equal rank is not sufficient', () => {
		expect(canManageUser(['SuperAdmin'], ['SuperAdmin'])).toBe(false);
	});

	it('empty caller roles cannot manage anyone', () => {
		expect(canManageUser([], ['User'])).toBe(false);
	});

	it('any role can manage a target with empty roles', () => {
		expect(canManageUser(['User'], [])).toBe(true);
	});

	it('empty caller cannot manage empty target — both rank 0', () => {
		expect(canManageUser([], [])).toBe(false);
	});

	it('uses highest role when caller has multiple roles', () => {
		expect(canManageUser(['User', 'Admin'], ['User'])).toBe(true);
	});

	it('uses highest role when target has multiple roles', () => {
		// Admin (rank 2) cannot manage target whose highest is SuperAdmin (rank 3)
		expect(canManageUser(['Admin'], ['User', 'SuperAdmin'])).toBe(false);
	});

	it('unknown caller roles cannot manage known target roles', () => {
		expect(canManageUser(['Moderator'], ['User'])).toBe(false);
	});

	it('known caller roles can manage unknown target roles', () => {
		// User (rank 1) > unknown (rank 0)
		expect(canManageUser(['User'], ['Moderator'])).toBe(true);
	});
});

describe('getAssignableRoles', () => {
	const allRoles = ['SuperAdmin', 'Admin', 'User'];

	it('SuperAdmin can assign Admin and User', () => {
		const result = getAssignableRoles(['SuperAdmin'], allRoles);
		expect(result).toEqual(['Admin', 'User']);
	});

	it('Admin can assign only User', () => {
		const result = getAssignableRoles(['Admin'], allRoles);
		expect(result).toEqual(['User']);
	});

	it('User cannot assign any role', () => {
		const result = getAssignableRoles(['User'], allRoles);
		expect(result).toEqual([]);
	});

	it('empty caller roles yield no assignable roles', () => {
		const result = getAssignableRoles([], allRoles);
		expect(result).toEqual([]);
	});

	it('unknown caller role yields no assignable roles from standard set', () => {
		const result = getAssignableRoles(['Moderator'], allRoles);
		expect(result).toEqual([]);
	});

	it('filters from arbitrary allRoles list', () => {
		// Admin (rank 2) can assign anything with rank < 2
		const result = getAssignableRoles(['Admin'], ['User', 'Viewer', 'SuperAdmin']);
		// User has rank 1 (< 2), Viewer has rank 0 (< 2), SuperAdmin has rank 3 (not < 2)
		expect(result).toEqual(['User', 'Viewer']);
	});

	it('returns empty when allRoles is empty', () => {
		const result = getAssignableRoles(['SuperAdmin'], []);
		expect(result).toEqual([]);
	});

	it('never includes the caller own rank level', () => {
		// SuperAdmin (rank 3) should not be able to assign SuperAdmin (rank 3)
		const result = getAssignableRoles(['SuperAdmin'], ['SuperAdmin']);
		expect(result).toEqual([]);
	});

	it('multi-role caller uses highest rank for filtering', () => {
		// Caller has User + Admin, highest is Admin (rank 2)
		const result = getAssignableRoles(['User', 'Admin'], allRoles);
		expect(result).toEqual(['User']);
	});
});
