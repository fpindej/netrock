/**
 * Tests for the global API health polling state.
 *
 * Covers state transitions (online/offline), adaptive polling intervals,
 * and cleanup. Visibility-based pause/resume is a browser integration —
 * not tested here since it would just verify addEventListener wiring.
 */
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';

// Mock document before importing the module — node environment has no DOM.
vi.stubGlobal('document', {
	visibilityState: 'visible',
	addEventListener: vi.fn(),
	removeEventListener: vi.fn()
});

const { healthState, initHealthCheck } = await import('./health.svelte');

describe('health state', () => {
	let cleanup: () => void;

	beforeEach(() => {
		vi.useFakeTimers();
		healthState.online = false;
	});

	afterEach(() => {
		cleanup?.();
		vi.useRealTimers();
	});

	it('sets online to true when health check succeeds', async () => {
		vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }));

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);

		expect(healthState.online).toBe(true);
	});

	it('sets online to false when health check returns non-ok', async () => {
		vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }));

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);

		expect(healthState.online).toBe(false);
	});

	it('sets online to false when fetch throws', async () => {
		vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')));

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);

		expect(healthState.online).toBe(false);
	});

	it('recovers when backend comes back online', async () => {
		const mockFetch = vi
			.fn()
			.mockResolvedValueOnce({ ok: false })
			.mockResolvedValueOnce({ ok: true });
		vi.stubGlobal('fetch', mockFetch);

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);
		expect(healthState.online).toBe(false);

		// Offline interval is 5s — next check should recover
		await vi.advanceTimersByTimeAsync(5_000);
		expect(healthState.online).toBe(true);
	});

	it('polls at 30s when online, 5s when offline', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: true });
		vi.stubGlobal('fetch', mockFetch);

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);
		expect(mockFetch).toHaveBeenCalledTimes(1);

		// Should NOT poll at 5s when online
		await vi.advanceTimersByTimeAsync(5_000);
		expect(mockFetch).toHaveBeenCalledTimes(1);

		// Should poll at 30s
		await vi.advanceTimersByTimeAsync(25_000);
		expect(mockFetch).toHaveBeenCalledTimes(2);
	});

	it('polls at 5s when offline', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: false });
		vi.stubGlobal('fetch', mockFetch);

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);
		expect(mockFetch).toHaveBeenCalledTimes(1);

		await vi.advanceTimersByTimeAsync(5_000);
		expect(mockFetch).toHaveBeenCalledTimes(2);
	});

	it('cleanup stops polling and removes visibility listener', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: true });
		vi.stubGlobal('fetch', mockFetch);

		cleanup = initHealthCheck();
		await vi.advanceTimersByTimeAsync(0);
		expect(mockFetch).toHaveBeenCalledTimes(1);

		cleanup();

		// No further polls after cleanup
		await vi.advanceTimersByTimeAsync(60_000);
		expect(mockFetch).toHaveBeenCalledTimes(1);

		expect(document.removeEventListener).toHaveBeenCalledWith(
			'visibilitychange',
			expect.any(Function)
		);
	});
});
