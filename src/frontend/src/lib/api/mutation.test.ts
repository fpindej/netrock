/**
 * Tests for handleMutationError — the orchestrator that routes mutation failures
 * to the appropriate UI response (rate-limit toast, validation error mapping,
 * or generic error toast).
 *
 * The underlying utilities (isRateLimited, mapFieldErrors, etc.) are tested
 * in error-handling.test.ts; these tests focus on the branching and delegation logic.
 */
import { describe, expect, it, vi } from 'vitest';
import { handleMutationError, type MutationErrorOptions } from './mutation';

vi.mock('$lib/components/ui/sonner', () => ({
	toast: {
		error: vi.fn(),
		success: vi.fn()
	}
}));

vi.mock('$lib/paraglide/messages', () => ({
	error_rateLimited: vi.fn(() => 'Too many requests'),
	error_rateLimitedDescription: vi.fn(() => 'Please try again later.'),
	error_rateLimitedDescriptionWithRetry: vi.fn(
		({ seconds }: { seconds: number }) => `Please wait ${seconds} seconds and try again.`
	)
}));

import { toast } from '$lib/components/ui/sonner';

/** Creates a Response with the given status and optional Retry-After header. */
function mockResponse(status: number, retryAfter?: string): Response {
	const headers: Record<string, string> = {};
	if (retryAfter !== undefined) {
		headers['Retry-After'] = retryAfter;
	}
	return new Response(null, { status, headers });
}

/** Creates default MutationErrorOptions with a spy cooldown. */
function defaultOptions(overrides: Partial<MutationErrorOptions> = {}): MutationErrorOptions {
	return {
		cooldown: { start: vi.fn() },
		fallback: 'Something went wrong',
		...overrides
	};
}

// ── 502/503 suppression ─────────────────────────────────────────────

describe('handleMutationError — 502/503 suppression', () => {
	it('502 status — suppresses toast entirely', () => {
		const options = defaultOptions();
		handleMutationError(mockResponse(502), null, options);
		expect(toast.error).not.toHaveBeenCalled();
	});

	it('503 status — suppresses toast entirely', () => {
		const options = defaultOptions();
		handleMutationError(mockResponse(503), null, options);
		expect(toast.error).not.toHaveBeenCalled();
	});

	it('502 status — does not call onError', () => {
		const onError = vi.fn();
		handleMutationError(mockResponse(502), null, defaultOptions({ onError }));
		expect(onError).not.toHaveBeenCalled();
	});

	it('503 status — does not call onRateLimited', () => {
		const onRateLimited = vi.fn();
		handleMutationError(mockResponse(503), null, defaultOptions({ onRateLimited }));
		expect(onRateLimited).not.toHaveBeenCalled();
	});
});

// ── 429 rate-limit path ─────────────────────────────────────────────

describe('handleMutationError — 429 rate-limit', () => {
	it('429 with Retry-After — starts cooldown with parsed seconds', () => {
		const options = defaultOptions();
		handleMutationError(mockResponse(429, '30'), null, options);
		expect(options.cooldown.start).toHaveBeenCalledWith(30);
	});

	it('429 with Retry-After — shows rate-limit toast with retry description', () => {
		handleMutationError(mockResponse(429, '30'), null, defaultOptions());
		expect(toast.error).toHaveBeenCalledWith('Too many requests', {
			description: 'Please wait 30 seconds and try again.'
		});
	});

	it('429 without Retry-After — shows rate-limit toast with generic description', () => {
		handleMutationError(mockResponse(429), null, defaultOptions());
		expect(toast.error).toHaveBeenCalledWith('Too many requests', {
			description: 'Please try again later.'
		});
	});

	it('429 without Retry-After — does not start cooldown', () => {
		const options = defaultOptions();
		handleMutationError(mockResponse(429), null, options);
		expect(options.cooldown.start).not.toHaveBeenCalled();
	});

	it('429 with onRateLimited callback — calls it after toast', () => {
		const onRateLimited = vi.fn();
		handleMutationError(mockResponse(429, '10'), null, defaultOptions({ onRateLimited }));
		expect(onRateLimited).toHaveBeenCalledOnce();
	});

	it('429 without onRateLimited — does not throw', () => {
		expect(() => {
			handleMutationError(mockResponse(429, '10'), null, defaultOptions());
		}).not.toThrow();
	});
});

// ── Validation error path ───────────────────────────────────────────

describe('handleMutationError — validation errors', () => {
	it('validation error with onValidationError — calls handler with mapped field errors', () => {
		const onValidationError = vi.fn();
		const error = {
			title: 'Validation failed',
			status: 422,
			errors: { Email: ['Required'], FirstName: ['Too short'] }
		};
		handleMutationError(mockResponse(422), error, defaultOptions({ onValidationError }));
		expect(onValidationError).toHaveBeenCalledWith({
			email: 'Required',
			firstName: 'Too short'
		});
	});

	it('validation error with onValidationError — does not show toast', () => {
		const onValidationError = vi.fn();
		const error = { errors: { Email: ['Required'] } };
		handleMutationError(mockResponse(422), error, defaultOptions({ onValidationError }));
		expect(toast.error).not.toHaveBeenCalled();
	});

	it('validation error without onValidationError — falls through to generic error', () => {
		const error = { errors: { Email: ['Required'] }, detail: 'Validation failed' };
		handleMutationError(mockResponse(422), error, defaultOptions());
		expect(toast.error).toHaveBeenCalledWith('Validation failed');
	});

	it('non-validation error with onValidationError — falls through to generic error', () => {
		const onValidationError = vi.fn();
		const error = { detail: 'Not found' };
		handleMutationError(mockResponse(404), error, defaultOptions({ onValidationError }));
		expect(onValidationError).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith('Not found');
	});
});

// ── Generic error path ──────────────────────────────────────────────

describe('handleMutationError — generic errors', () => {
	it('generic error without onError — shows toast with error message', () => {
		const error = { detail: 'Invalid credentials' };
		handleMutationError(mockResponse(401), error, defaultOptions());
		expect(toast.error).toHaveBeenCalledWith('Invalid credentials');
	});

	it('generic error without detail — shows toast with fallback message', () => {
		handleMutationError(mockResponse(500), {}, defaultOptions({ fallback: 'Save failed' }));
		expect(toast.error).toHaveBeenCalledWith('Save failed');
	});

	it('generic error with onError — calls onError instead of toast', () => {
		const onError = vi.fn();
		handleMutationError(mockResponse(400), { detail: 'Bad request' }, defaultOptions({ onError }));
		expect(onError).toHaveBeenCalledOnce();
		expect(toast.error).not.toHaveBeenCalled();
	});

	it('null error — shows toast with fallback', () => {
		handleMutationError(mockResponse(500), null, defaultOptions({ fallback: 'Unexpected error' }));
		expect(toast.error).toHaveBeenCalledWith('Unexpected error');
	});
});
