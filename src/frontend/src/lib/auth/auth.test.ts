import { describe, expect, it, vi } from 'vitest';
import { getUser } from './auth';

const ORIGIN = 'http://localhost';

/** Creates a JSON response with the given body and status. */
function jsonResponse(body: unknown, status = 200): Response {
	return new Response(JSON.stringify(body), {
		status,
		headers: { 'Content-Type': 'application/json' }
	});
}

/** Creates a non-JSON response with the given status. */
function emptyResponse(status: number): Response {
	return new Response(null, { status });
}

/** Minimal user payload matching `UserResponse`. */
const MOCK_USER = {
	id: '00000000-0000-0000-0000-000000000001',
	username: 'test@example.com',
	email: 'test@example.com',
	firstName: 'Test',
	lastName: 'User',
	roles: ['User'],
	permissions: [],
	emailConfirmed: true
};

describe('getUser', () => {
	// ── Basic response handling (no refresh involved) ───────────────

	it('200 with user data — returns { user, error: null }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(jsonResponse(MOCK_USER));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: MOCK_USER, error: null });
	});

	it('401 — returns { user: null, error: null }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		// Initial request returns 401
		fetchFn.mockResolvedValueOnce(emptyResponse(401));
		// Middleware triggers refresh — refresh also fails with 401
		fetchFn.mockResolvedValueOnce(emptyResponse(401));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: null, error: null });
	});

	it('403 — returns { user: null, error: null }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(emptyResponse(403));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: null, error: null });
	});

	it('5xx — returns { user: null, error: backend_unavailable }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(emptyResponse(500));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: null, error: 'backend_unavailable' });
	});

	it('network error — returns { user: null, error: backend_unavailable }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockRejectedValueOnce(new TypeError('Failed to fetch'));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: null, error: 'backend_unavailable' });
	});

	// ── Server-side refresh integration (middleware wired) ──────────

	it('401 → refresh succeeds → retry returns user', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		// 1. Initial GET /api/users/me → 401
		fetchFn.mockResolvedValueOnce(emptyResponse(401));
		// 2. Middleware POSTs /api/auth/refresh → 200 (refresh succeeds)
		fetchFn.mockResolvedValueOnce(emptyResponse(200));
		// 3. Middleware retries GET /api/users/me → 200 with user data
		fetchFn.mockResolvedValueOnce(jsonResponse(MOCK_USER));

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: MOCK_USER, error: null });
		expect(fetchFn).toHaveBeenCalledTimes(3);
	});

	it('401 → refresh fails → returns { user: null, error: null }', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		// 1. Initial GET /api/users/me → 401
		fetchFn.mockResolvedValueOnce(emptyResponse(401));
		// 2. Middleware POSTs /api/auth/refresh → 401 (refresh fails)
		fetchFn.mockResolvedValueOnce(emptyResponse(401));
		// Middleware returns undefined → original 401 passes through

		const result = await getUser(fetchFn, ORIGIN);

		expect(result).toEqual({ user: null, error: null });
		expect(fetchFn).toHaveBeenCalledTimes(2);
	});
});
