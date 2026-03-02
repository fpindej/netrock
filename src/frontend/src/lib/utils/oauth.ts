import { browserClient } from '$lib/api';
import { toast } from '$lib/components/ui/sonner';
import * as m from '$lib/paraglide/messages';

/**
 * Initiates an OAuth challenge by requesting an authorization URL from the
 * backend and redirecting the browser. Shows a toast on failure.
 *
 * Returns `true` when the browser is navigating to the provider (callers
 * should keep their loading state active). Returns `false` on failure.
 * All error reporting is handled internally - callers only need to check
 * the return value.
 */
export async function startOAuthChallenge(provider: string): Promise<boolean> {
	try {
		const redirectUri = `${window.location.origin}/oauth/callback`;
		const { response, data } = await browserClient.POST('/api/auth/external/challenge', {
			body: { provider, redirectUri }
		});

		if (response.ok && data?.authorizationUrl) {
			window.location.href = data.authorizationUrl;
			return true;
		}
	} catch {
		// Network error - fall through to toast
	}

	toast.error(m.oauth_challengeError());
	return false;
}
