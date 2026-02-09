/**
 * API error handling utilities for ASP.NET Core backends.
 *
 * Provides type-safe parsing and mapping of validation errors
 * from ASP.NET Core's ProblemDetails format, and localized error
 * message resolution from backend error codes via paraglide-js.
 *
 * @remarks Pattern documented in src/frontend/AGENTS.md — update both when changing.
 */

import * as m from '$lib/paraglide/messages';

/**
 * Extended ProblemDetails with validation errors.
 * ASP.NET Core returns field-level errors in an `errors` object.
 *
 * @see https://tools.ietf.org/html/rfc7807
 */
export interface ValidationProblemDetails {
	type?: string | null;
	title?: string | null;
	status?: number | null;
	detail?: string | null;
	instance?: string | null;
	errors?: Record<string, string[]>;
}

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
	AvatarUrl: 'avatarUrl',
	Email: 'email',
	Password: 'password',
	ConfirmPassword: 'confirmPassword',
	CurrentPassword: 'currentPassword',
	NewPassword: 'newPassword'
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
 * Maps backend error codes (dot-separated) to paraglide message functions.
 * When the backend returns an `errorCode` field, this map resolves it to a
 * localized string. Unknown codes fall through to the raw message or fallback.
 *
 * @see ErrorCodes in MyProject.Domain for the canonical list of codes.
 */
const errorCodeMessages: Record<string, () => string> = {
	// Auth — login
	'auth.login.invalidCredentials': m.apiError_auth_login_invalidCredentials,
	'auth.login.accountLocked': m.apiError_auth_login_accountLocked,
	// Auth — registration
	'auth.register.failed': m.apiError_auth_register_failed,
	'auth.register.roleAssignFailed': m.apiError_auth_register_roleAssignFailed,
	// Auth — tokens
	'auth.token.missing': m.apiError_auth_token_missing,
	'auth.token.notFound': m.apiError_auth_token_notFound,
	'auth.token.invalidated': m.apiError_auth_token_invalidated,
	'auth.token.reused': m.apiError_auth_token_reused,
	'auth.token.expired': m.apiError_auth_token_expired,
	'auth.token.userNotFound': m.apiError_auth_token_userNotFound,
	// Auth — general
	'auth.notAuthenticated': m.apiError_auth_notAuthenticated,
	'auth.userNotFound': m.apiError_auth_userNotFound,
	// Auth — password
	'auth.password.incorrect': m.apiError_auth_password_incorrect,
	'auth.password.changeFailed': m.apiError_auth_password_changeFailed,
	// User — self-service
	'user.notAuthenticated': m.apiError_user_notAuthenticated,
	'user.notFound': m.apiError_user_notFound,
	'user.updateFailed': m.apiError_user_updateFailed,
	'user.delete.invalidPassword': m.apiError_user_delete_invalidPassword,
	'user.delete.lastRole': m.apiError_user_delete_lastRole,
	// Admin — user management
	'admin.user.notFound': m.apiError_admin_user_notFound,
	'admin.hierarchy.insufficient': m.apiError_admin_hierarchy_insufficient,
	// Admin — roles
	'admin.role.notExists': m.apiError_admin_role_notExists,
	'admin.role.alreadyAssigned': m.apiError_admin_role_alreadyAssigned,
	'admin.role.notAssigned': m.apiError_admin_role_notAssigned,
	'admin.role.rankTooHigh': m.apiError_admin_role_rankTooHigh,
	'admin.role.selfRemove': m.apiError_admin_role_selfRemove,
	'admin.role.lastRole': m.apiError_admin_role_lastRole,
	'admin.role.assignFailed': m.apiError_admin_role_assignFailed,
	'admin.role.removeFailed': m.apiError_admin_role_removeFailed,
	// Admin — lock/unlock
	'admin.lock.selfAction': m.apiError_admin_lock_selfAction,
	'admin.lock.failed': m.apiError_admin_lock_failed,
	'admin.unlock.failed': m.apiError_admin_unlock_failed,
	// Admin — delete
	'admin.delete.selfAction': m.apiError_admin_delete_selfAction,
	'admin.delete.lastRole': m.apiError_admin_delete_lastRole,
	'admin.delete.failed': m.apiError_admin_delete_failed,
	// Pagination
	'pagination.invalidPage': m.apiError_pagination_invalidPage,
	'pagination.invalidPageSize': m.apiError_pagination_invalidPageSize,
	// Rate limiting
	'rateLimit.exceeded': m.apiError_rateLimit_exceeded,
	// Server
	'server.internalError': m.apiError_server_internalError,
	// Entity (generic repository errors)
	'entity.addFailed': m.apiError_entity_addFailed,
	'entity.notFound': m.apiError_entity_notFound,
	'entity.notDeleted': m.apiError_entity_notDeleted
};

/**
 * Extracts a user-friendly, localized error message from an API error response.
 *
 * Resolution order:
 * 1. `errorCode` field → localized paraglide message (if code is mapped)
 * 2. `message` field → raw backend message (ErrorResponse shape)
 * 3. `detail` field → raw ProblemDetails detail
 * 4. `title` field → raw ProblemDetails title
 * 5. Fallback string
 *
 * @param error - The error object from the API response
 * @param fallback - Fallback message if no error message can be extracted
 * @returns A user-friendly error message, localized when possible
 */
export function getErrorMessage(error: unknown, fallback: string): string {
	if (typeof error === 'object' && error !== null) {
		// 1. Try errorCode → localized message
		if ('errorCode' in error && typeof error.errorCode === 'string') {
			const messageFn = errorCodeMessages[error.errorCode];
			if (messageFn) return messageFn();
		}
		// 2. Try ErrorResponse shape (message field)
		if ('message' in error && typeof error.message === 'string') {
			return error.message;
		}
		// 3. Try ProblemDetails shape (detail/title)
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
 *     return new Response('Backend unavailable', { status: 503 });
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
 * Extracts the error code from a fetch error's cause, if present.
 *
 * @returns The error code string, or undefined if not a fetch error with cause
 */
export function getFetchErrorCode(error: unknown): string | undefined {
	if (typeof error !== 'object' || error === null) return undefined;
	const cause = (error as { cause?: FetchErrorCause }).cause;
	return cause?.code;
}
