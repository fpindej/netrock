import { browserClient } from '$lib/api';
import { toast } from '$lib/components/ui/sonner';
import * as m from '$lib/paraglide/messages';

/**
 * Initiates an OAuth challenge by requesting an authorization URL from the
 * backend and redirecting the browser. Shows a toast on failure.
 */
export async function startOAuthChallenge(provider: string): Promise<void> {
	const redirectUri = `${window.location.origin}/oauth/callback`;
	const { response, data } = await browserClient.POST('/api/auth/external/challenge', {
		body: { provider, redirectUri }
	});

	if (response.ok && data?.authorizationUrl) {
		window.location.href = data.authorizationUrl;
		return;
	}

	toast.error(m.oauth_challengeError());
}
