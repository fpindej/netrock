import { env } from '$env/dynamic/private';

export const SERVER_CONFIG = {
	API_URL: env.API_URL || 'http://localhost:{INIT_API_PORT}',
	/** Cloudflare Turnstile site key — read at runtime so a single image works across environments. */
	TURNSTILE_SITE_KEY: env.TURNSTILE_SITE_KEY || env.PUBLIC_TURNSTILE_SITE_KEY || '',
	/** Additional origins allowed through the API proxy CSRF check (e.g. ngrok, reverse proxy). */
	ALLOWED_ORIGINS:
		env.ALLOWED_ORIGINS?.split(',')
			.map((o) => o.trim())
			.filter(Boolean) ?? []
};

/** Validate API_URL is a well-formed URL at startup. */
try {
	new URL(SERVER_CONFIG.API_URL);
} catch {
	throw new Error(`Invalid API_URL: ${SERVER_CONFIG.API_URL}`);
}
