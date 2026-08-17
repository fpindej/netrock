/**
 * API error handling utilities for ASP.NET Core backends.
 *
 * All error responses use ProblemDetails (RFC 9457). Provides type-safe
 * parsing and mapping of validation errors, and user-friendly error
 * message extraction from ProblemDetails responses.
 *
 * @remarks Pattern documented in .claude/skills/frontend-conventions/SKILL.md.
 */

/**
 * ProblemDetails (RFC 9457) as returned by the backend.
 *
 * Every error response carries a stable, machine-readable `code` (snake_case)
 * alongside the human-readable `detail`. Branch on `code` - never on `detail` text.
 *
 * @see https://tools.ietf.org/html/rfc9457
 */
export interface ProblemDetails {
	type?: string | null;
	title?: string | null;
	status?: number | null;
	detail?: string | null;
	instance?: string | null;
	code?: string | null;
}

/**
 * Extended ProblemDetails with validation errors.
 * ASP.NET Core returns field-level errors in an `errors` object.
 */
export interface ValidationProblemDetails extends ProblemDetails {
	errors?: Record<string, string[]>;
}

/**
 * Translated messages keyed by backend error `code`.
 * Values are Paraglide message functions so translation happens lazily in the current locale.
 *
 * @example
 * ```ts
 * const messages: ErrorMessagesByCode = {
 *   auth_login_account_locked: m.auth_login_accountLocked
 * };
 * ```
 */
export type ErrorMessagesByCode = Record<string, () => string>;

/**
 * Type guard to check if an error response is a ValidationProblemDetails.
 */
export function isValidationProblemDetails(
	error: unknown
): error is ValidationProblemDetails & { errors: Record<string, string[]> } {
	return (
		typeof error === 'object' &&
		error !== null &&
		'errors' in error &&
		typeof (error as ValidationProblemDetails).errors === 'object'
	);
}

/**
 * Default mapping of PascalCase backend field names to camelCase frontend field names.
 * Extend this map as needed for your application.
 */
const DEFAULT_FIELD_MAP: Record<string, string> = {
	FirstName: 'firstName',
	LastName: 'lastName',
	PhoneNumber: 'phoneNumber',
	Bio: 'bio',
	Email: 'email',
	Password: 'password',
	ConfirmPassword: 'confirmPassword',
	CurrentPassword: 'currentPassword',
	NewPassword: 'newPassword',
	Token: 'token'
};

/**
 * Maps backend field names (PascalCase) to frontend field names (camelCase).
 *
 * @param errors - The errors object from ValidationProblemDetails
 * @param customFieldMap - Optional custom field name mapping to override defaults
 * @returns A record of field names to their first error message
 *
 * @example
 * ```ts
 * const errors = { PhoneNumber: ["Invalid format"] };
 * const mapped = mapFieldErrors(errors);
 * // Result: { phoneNumber: "Invalid format" }
 * ```
 */
export function mapFieldErrors(
	errors: Record<string, string[]>,
	customFieldMap?: Record<string, string>
): Record<string, string> {
	const fieldMap = { ...DEFAULT_FIELD_MAP, ...customFieldMap };
	const mapped: Record<string, string> = {};

	for (const [key, messages] of Object.entries(errors)) {
		// Use custom mapping, fall back to default, then to lowercase
		const fieldName = fieldMap[key] ?? key.charAt(0).toLowerCase() + key.slice(1);
		mapped[fieldName] = messages[0] ?? '';
	}

	return mapped;
}

/**
 * Extracts the machine-readable error `code` from a ProblemDetails API error response.
 *
 * @param error - The error object from the API response
 * @returns The snake_case error code, or `null` when the response carries none
 */
export function getErrorCode(error: unknown): string | null {
	if (
		typeof error === 'object' &&
		error !== null &&
		'code' in error &&
		typeof error.code === 'string' &&
		error.code.length > 0
	) {
		return error.code;
	}
	return null;
}

/**
 * Extracts a user-friendly error message from a ProblemDetails API error response.
 *
 * Resolution order:
 * 1. `code` field with a matching entry in `messagesByCode` -> translated message
 * 2. `detail` field -> ProblemDetails detail (the specific English message)
 * 3. `title` field -> ProblemDetails title (generic status description)
 * 4. Fallback string
 *
 * Prefer passing `messagesByCode` for errors the UI wants to translate - the
 * backend `code` is a stable contract, while `detail` text may change.
 *
 * @param error - The error object from the API response
 * @param fallback - Fallback message if no error message can be extracted
 * @param messagesByCode - Optional translated messages keyed by backend error code
 * @returns A user-friendly error message
 */
export function getErrorMessage(
	error: unknown,
	fallback: string,
	messagesByCode?: ErrorMessagesByCode
): string {
	const code = getErrorCode(error);
	const translated = code === null ? undefined : messagesByCode?.[code];
	if (translated) {
		return translated();
	}
	if (typeof error === 'object' && error !== null) {
		if ('detail' in error && typeof error.detail === 'string') {
			return error.detail;
		}
		if ('title' in error && typeof error.title === 'string') {
			return error.title;
		}
	}
	return fallback;
}

/**
 * Represents a fetch error with a typed cause containing the error code.
 * Node.js fetch errors (and some browser implementations) include a `cause`
 * property with additional error details.
 */
export interface FetchErrorCause {
	code?: string;
	errno?: number;
	syscall?: string;
	hostname?: string;
	message?: string;
}

/**
 * Type guard to check if an error has a fetch error cause with a code.
 * Useful for detecting network errors like ECONNREFUSED, ETIMEDOUT, etc.
 *
 * @example
 * ```ts
 * try {
 *   await fetch(url);
 * } catch (err) {
 *   if (isFetchErrorWithCode(err, 'ECONNREFUSED')) {
 *     return Response.json(
 *       { title: 'Service Unavailable', status: 503, detail: 'Backend unavailable' },
 *       { status: 503, headers: { 'Content-Type': 'application/problem+json' } }
 *     );
 *   }
 * }
 * ```
 */
export function isFetchErrorWithCode(error: unknown, code: string): boolean {
	if (typeof error !== 'object' || error === null) return false;
	const cause = (error as { cause?: FetchErrorCause }).cause;
	return cause?.code === code;
}

/**
 * Returns true when the response is a 429 Too Many Requests.
 */
export function isRateLimited(response: Response): boolean {
	return response.status === 429;
}

/**
 * Extracts the integer seconds from the `Retry-After` response header.
 *
 * The backend always sends `Retry-After` as an integer (delta-seconds).
 * Returns `null` when the header is absent or not a valid integer.
 *
 * @param response - The HTTP response (typically a 429)
 * @returns Retry delay in seconds, or null
 */
export function getRetryAfterSeconds(response: Response): number | null {
	const header = response.headers.get('Retry-After');
	if (!header) return null;
	const seconds = parseInt(header, 10);
	return Number.isNaN(seconds) || seconds < 0 ? null : seconds;
}
