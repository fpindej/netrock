import { describe, expect, it } from 'vitest';
import {
	getErrorMessage,
	getRetryAfterSeconds,
	isFetchErrorWithCode,
	isRateLimited,
	isValidationProblemDetails,
	mapFieldErrors
} from './error-handling';

// ── isValidationProblemDetails ──────────────────────────────────────

describe('isValidationProblemDetails', () => {
	it('object with errors record — returns true', () => {
		const error = { errors: { Email: ['Required'] } };
		expect(isValidationProblemDetails(error)).toBe(true);
	});

	it('full ProblemDetails with errors — returns true', () => {
		const error = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
			title: 'Validation failed',
			status: 400,
			detail: 'One or more validation errors occurred.',
			errors: { Email: ['Invalid email'], Password: ['Too short'] }
		};
		expect(isValidationProblemDetails(error)).toBe(true);
	});

	it('empty errors object — returns true', () => {
		const error = { errors: {} };
		expect(isValidationProblemDetails(error)).toBe(true);
	});

	it('ProblemDetails without errors — returns false', () => {
		const error = { title: 'Not Found', status: 404 };
		expect(isValidationProblemDetails(error)).toBe(false);
	});

	it('null — returns false', () => {
		expect(isValidationProblemDetails(null)).toBe(false);
	});

	it('undefined — returns false', () => {
		expect(isValidationProblemDetails(undefined)).toBe(false);
	});

	it('string — returns false', () => {
		expect(isValidationProblemDetails('error')).toBe(false);
	});

	it('number — returns false', () => {
		expect(isValidationProblemDetails(42)).toBe(false);
	});

	it('boolean — returns false', () => {
		expect(isValidationProblemDetails(true)).toBe(false);
	});

	it('array — returns false', () => {
		expect(isValidationProblemDetails([1, 2, 3])).toBe(false);
	});

	it('errors set to null — returns true (typeof null is "object")', () => {
		// typeof null === 'object' in JS, so the type guard passes
		const error = { errors: null };
		expect(isValidationProblemDetails(error)).toBe(true);
	});

	it('errors set to string — returns false', () => {
		const error = { errors: 'not an object' };
		expect(isValidationProblemDetails(error)).toBe(false);
	});

	it('errors set to number — returns false', () => {
		const error = { errors: 123 };
		expect(isValidationProblemDetails(error)).toBe(false);
	});

	it('errors set to array — returns true (arrays are objects)', () => {
		// typeof [] === 'object', so this passes the type guard
		const error = { errors: [] };
		expect(isValidationProblemDetails(error)).toBe(true);
	});
});

// ── mapFieldErrors ──────────────────────────────────────────────────

describe('mapFieldErrors', () => {
	it('PascalCase keys from default map — maps to camelCase', () => {
		const errors = {
			FirstName: ['Required'],
			LastName: ['Too long'],
			PhoneNumber: ['Invalid format']
		};
		const result = mapFieldErrors(errors);
		expect(result).toEqual({
			firstName: 'Required',
			lastName: 'Too long',
			phoneNumber: 'Invalid format'
		});
	});

	it('multiple messages per field — takes the first message', () => {
		const errors = { Email: ['Required', 'Invalid format', 'Already taken'] };
		const result = mapFieldErrors(errors);
		expect(result).toEqual({ email: 'Required' });
	});

	it('empty messages array — maps to empty string', () => {
		const errors = { Email: [] };
		const result = mapFieldErrors(errors);
		expect(result).toEqual({ email: '' });
	});

	it('key not in default map — falls back to lowercase first char', () => {
		const errors = { CustomField: ['Some error'] };
		const result = mapFieldErrors(errors);
		expect(result).toEqual({ customField: 'Some error' });
	});

	it('single-char key not in map — lowercases it', () => {
		const errors = { X: ['Error'] };
		const result = mapFieldErrors(errors);
		expect(result).toEqual({ x: 'Error' });
	});

	it('already lowercase key not in map — keeps as-is', () => {
		const errors = { alreadyLower: ['Error'] };
		const result = mapFieldErrors(errors);
		expect(result).toEqual({ alreadyLower: 'Error' });
	});

	it('custom field map — overrides default mapping', () => {
		const errors = { Email: ['Custom error'] };
		const result = mapFieldErrors(errors, { Email: 'emailAddress' });
		expect(result).toEqual({ emailAddress: 'Custom error' });
	});

	it('custom field map — does not affect unmapped keys', () => {
		const errors = {
			Email: ['Error 1'],
			FirstName: ['Error 2'],
			UnknownField: ['Error 3']
		};
		const result = mapFieldErrors(errors, { Email: 'emailAddress' });
		expect(result).toEqual({
			emailAddress: 'Error 1',
			firstName: 'Error 2',
			unknownField: 'Error 3'
		});
	});

	it('empty errors object — returns empty object', () => {
		const result = mapFieldErrors({});
		expect(result).toEqual({});
	});

	it('all default-mapped fields — maps correctly', () => {
		const errors = {
			FirstName: ['a'],
			LastName: ['b'],
			PhoneNumber: ['c'],
			Bio: ['d'],
			Email: ['e'],
			Password: ['f'],
			ConfirmPassword: ['g'],
			CurrentPassword: ['h'],
			NewPassword: ['i'],
			Token: ['j']
		};
		const result = mapFieldErrors(errors);
		expect(result).toEqual({
			firstName: 'a',
			lastName: 'b',
			phoneNumber: 'c',
			bio: 'd',
			email: 'e',
			password: 'f',
			confirmPassword: 'g',
			currentPassword: 'h',
			newPassword: 'i',
			token: 'j'
		});
	});
});

// ── getErrorMessage ─────────────────────────────────────────────────

describe('getErrorMessage', () => {
	it('error with detail — returns detail', () => {
		const error = { detail: 'Invalid credentials', title: 'Unauthorized' };
		expect(getErrorMessage(error, 'fallback')).toBe('Invalid credentials');
	});

	it('error with title only — returns title', () => {
		const error = { title: 'Not Found' };
		expect(getErrorMessage(error, 'fallback')).toBe('Not Found');
	});

	it('error with both detail and title — prefers detail', () => {
		const error = { detail: 'Specific message', title: 'Generic title' };
		expect(getErrorMessage(error, 'fallback')).toBe('Specific message');
	});

	it('error with neither detail nor title — returns fallback', () => {
		const error = { status: 500 };
		expect(getErrorMessage(error, 'Something went wrong')).toBe('Something went wrong');
	});

	it('null error — returns fallback', () => {
		expect(getErrorMessage(null, 'fallback')).toBe('fallback');
	});

	it('undefined error — returns fallback', () => {
		expect(getErrorMessage(undefined, 'fallback')).toBe('fallback');
	});

	it('string error — returns fallback', () => {
		expect(getErrorMessage('error string', 'fallback')).toBe('fallback');
	});

	it('number error — returns fallback', () => {
		expect(getErrorMessage(42, 'fallback')).toBe('fallback');
	});

	it('boolean error — returns fallback', () => {
		expect(getErrorMessage(false, 'fallback')).toBe('fallback');
	});

	it('empty object — returns fallback', () => {
		expect(getErrorMessage({}, 'fallback')).toBe('fallback');
	});

	it('detail is non-string — returns fallback', () => {
		const error = { detail: 123 };
		expect(getErrorMessage(error, 'fallback')).toBe('fallback');
	});

	it('title is non-string — returns fallback', () => {
		const error = { title: 456 };
		expect(getErrorMessage(error, 'fallback')).toBe('fallback');
	});

	it('detail is empty string — returns empty string', () => {
		const error = { detail: '' };
		expect(getErrorMessage(error, 'fallback')).toBe('');
	});

	it('detail is null, title is string — returns title', () => {
		const error = { detail: null, title: 'Title message' };
		expect(getErrorMessage(error, 'fallback')).toBe('Title message');
	});
});

// ── isFetchErrorWithCode ────────────────────────────────────────────

describe('isFetchErrorWithCode', () => {
	it('error with matching cause code — returns true', () => {
		const error = { cause: { code: 'ECONNREFUSED' } };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(true);
	});

	it('error with non-matching cause code — returns false', () => {
		const error = { cause: { code: 'ECONNREFUSED' } };
		expect(isFetchErrorWithCode(error, 'ETIMEDOUT')).toBe(false);
	});

	it('error with no cause — returns false', () => {
		const error = { message: 'Failed to fetch' };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(false);
	});

	it('error with cause but no code — returns false', () => {
		const error = { cause: { message: 'connection failed' } };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(false);
	});

	it('null error — returns false', () => {
		expect(isFetchErrorWithCode(null, 'ECONNREFUSED')).toBe(false);
	});

	it('undefined error — returns false', () => {
		expect(isFetchErrorWithCode(undefined, 'ECONNREFUSED')).toBe(false);
	});

	it('string error — returns false', () => {
		expect(isFetchErrorWithCode('error', 'ECONNREFUSED')).toBe(false);
	});

	it('number error — returns false', () => {
		expect(isFetchErrorWithCode(42, 'ECONNREFUSED')).toBe(false);
	});

	it('cause is null — returns false', () => {
		const error = { cause: null };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(false);
	});

	it('cause is a string — returns false', () => {
		const error = { cause: 'not an object' };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(false);
	});

	it('real TypeError with cause — returns true', () => {
		const error = new TypeError('fetch failed');
		Object.defineProperty(error, 'cause', {
			value: { code: 'ECONNREFUSED', errno: -61, syscall: 'connect' }
		});
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(true);
	});

	it('ETIMEDOUT code — matches correctly', () => {
		const error = { cause: { code: 'ETIMEDOUT' } };
		expect(isFetchErrorWithCode(error, 'ETIMEDOUT')).toBe(true);
	});

	it('empty code string — does not match non-empty code', () => {
		const error = { cause: { code: '' } };
		expect(isFetchErrorWithCode(error, 'ECONNREFUSED')).toBe(false);
	});

	it('empty code string — matches empty code', () => {
		const error = { cause: { code: '' } };
		expect(isFetchErrorWithCode(error, '')).toBe(true);
	});
});

// ── isRateLimited ───────────────────────────────────────────────────

describe('isRateLimited', () => {
	it('429 status — returns true', () => {
		const response = new Response(null, { status: 429 });
		expect(isRateLimited(response)).toBe(true);
	});

	it('200 status — returns false', () => {
		const response = new Response(null, { status: 200 });
		expect(isRateLimited(response)).toBe(false);
	});

	it('400 status — returns false', () => {
		const response = new Response(null, { status: 400 });
		expect(isRateLimited(response)).toBe(false);
	});

	it('500 status — returns false', () => {
		const response = new Response(null, { status: 500 });
		expect(isRateLimited(response)).toBe(false);
	});

	it('503 status — returns false', () => {
		const response = new Response(null, { status: 503 });
		expect(isRateLimited(response)).toBe(false);
	});
});

// ── getRetryAfterSeconds ────────────────────────────────────────────

describe('getRetryAfterSeconds', () => {
	/** Creates a Response with the given Retry-After header value. */
	function responseWithRetryAfter(value: string): Response {
		return new Response(null, {
			status: 429,
			headers: { 'Retry-After': value }
		});
	}

	it('valid integer header — returns the number', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter('60'))).toBe(60);
	});

	it('zero — returns 0', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter('0'))).toBe(0);
	});

	it('large value — returns the number', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter('3600'))).toBe(3600);
	});

	it('negative value — returns null', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter('-1'))).toBeNull();
	});

	it('non-numeric string — returns null', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter('abc'))).toBeNull();
	});

	it('empty string — returns null', () => {
		expect(getRetryAfterSeconds(responseWithRetryAfter(''))).toBeNull();
	});

	it('floating-point string — returns integer part', () => {
		// parseInt('30.5', 10) returns 30
		expect(getRetryAfterSeconds(responseWithRetryAfter('30.5'))).toBe(30);
	});

	it('missing header — returns null', () => {
		const response = new Response(null, { status: 429 });
		expect(getRetryAfterSeconds(response)).toBeNull();
	});

	it('header with leading whitespace — returns the number', () => {
		// parseInt trims leading whitespace
		expect(getRetryAfterSeconds(responseWithRetryAfter(' 120'))).toBe(120);
	});

	it('HTTP-date format — returns null (not supported)', () => {
		// The backend only sends delta-seconds; HTTP-date is intentionally unsupported
		expect(
			getRetryAfterSeconds(responseWithRetryAfter('Wed, 21 Oct 2025 07:28:00 GMT'))
		).toBeNull();
	});
});
