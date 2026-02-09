/**
 * Dynamic paraglide message lookup utilities.
 *
 * Paraglide generates named exports (one function per key) but provides no
 * runtime API for looking up messages by computed key. This module performs
 * the cast once at the module boundary and exposes a clean lookup function.
 *
 * @see https://inlang.com/m/gerre34r/library-inlang-paraglideJs — no dynamic lookup API exists.
 * @remarks Pattern documented in src/frontend/AGENTS.md — update both when changing.
 */

import * as m from '$lib/paraglide/messages';

/**
 * Paraglide message namespace typed for dynamic key access.
 *
 * Each value is a no-arg function returning a localized string, or undefined
 * if the key does not exist. The cast lives here — at the module boundary —
 * so that consumers never need to know about the underlying type mismatch.
 */
const messages = m as unknown as Record<string, (() => string) | undefined>;

/**
 * Resolves a backend error code to a localized paraglide message.
 *
 * Transforms dot-separated error codes to paraglide key format and looks
 * up the corresponding message function at runtime:
 *   "auth.login.invalidCredentials" → messages["apiError_auth_login_invalidCredentials"]()
 *
 * Adding a new error code only requires adding the `apiError_*` key to
 * en.json and cs.json — no frontend code changes needed.
 *
 * @param errorCode - Dot-separated error code from the backend (e.g. "auth.login.invalidCredentials")
 * @returns The localized error message, or undefined if no translation exists for this code
 *
 * @see ErrorCodes in MyProject.Domain for the canonical list of codes.
 */
export function resolveErrorCode(errorCode: string): string | undefined {
	const key = `apiError_${errorCode.replaceAll('.', '_')}`;
	return messages[key]?.();
}
