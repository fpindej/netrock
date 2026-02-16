/**
 * Mutation error handling with standardized rate-limit and validation support.
 *
 * Builds on the pure utilities in `error-handling.ts` by adding UI concerns
 * (toast notifications, i18n messages) for the common mutation response pattern.
 *
 * @remarks Pattern documented in src/frontend/AGENTS.md — update both when changing.
 */

import { toast } from '$lib/components/ui/sonner';
import * as m from '$lib/paraglide/messages';
import {
	isRateLimited,
	getRetryAfterSeconds,
	isValidationProblemDetails,
	mapFieldErrors,
	getErrorMessage
} from './error-handling';

/**
 * Options for {@link handleMutationError}.
 */
export interface MutationErrorOptions {
	/** Cooldown timer to start on 429 rate-limit responses. */
	cooldown: { start(seconds: number): void };
	/** Default error message shown via toast when no {@link onError} handler is provided. */
	fallback: string;
	/** Called after the standard rate-limit toast is shown (e.g., to trigger a shake animation). */
	onRateLimited?: () => void;
	/**
	 * Called with mapped field errors on validation failure (422 with `errors` object).
	 * When absent, validation errors fall through to the generic error path.
	 */
	onValidationError?: (errors: Record<string, string>) => void;
	/**
	 * Called on generic (non-rate-limit, non-validation) errors.
	 * When absent, a toast with the extracted error message (or {@link fallback}) is shown.
	 */
	onError?: () => void;
}

/**
 * Handles non-OK mutation responses with standardized rate-limit toasts,
 * optional validation error mapping, and a generic error fallback.
 *
 * Call this in the `else` branch after checking `response.ok`.
 *
 * @example
 * ```ts
 * // Simple: rate-limit + toast with fallback
 * if (response.ok) {
 *   toast.success(m.saved());
 * } else {
 *   handleMutationError(response, error, {
 *     cooldown,
 *     fallback: m.saveError()
 *   });
 * }
 *
 * // Full: rate-limit + validation + custom error
 * if (response.ok) {
 *   toast.success(m.saved());
 * } else {
 *   handleMutationError(response, error, {
 *     cooldown,
 *     fallback: m.saveError(),
 *     onValidationError(errors) {
 *       fieldErrors = errors;
 *       fieldShakes.triggerFields(Object.keys(errors));
 *     }
 *   });
 * }
 * ```
 */
export function handleMutationError(
	response: Response,
	error: unknown,
	{ cooldown, fallback, onRateLimited, onValidationError, onError }: MutationErrorOptions
): void {
	if (isRateLimited(response)) {
		const retryAfter = getRetryAfterSeconds(response);
		if (retryAfter) cooldown.start(retryAfter);
		toast.error(m.error_rateLimited(), {
			description: retryAfter
				? m.error_rateLimitedDescriptionWithRetry({ seconds: retryAfter })
				: m.error_rateLimitedDescription()
		});
		onRateLimited?.();
		return;
	}

	if (onValidationError && isValidationProblemDetails(error)) {
		onValidationError(mapFieldErrors(error.errors));
		return;
	}

	if (onError) {
		onError();
	} else {
		toast.error(getErrorMessage(error, fallback));
	}
}
