import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

/** Reason values that the login page recognizes and shows toasts for. */
const VALID_REASONS = ['session_expired', 'password_changed'] as const;
type LoginReason = (typeof VALID_REASONS)[number];

export const load: PageServerLoad = async ({ parent, url }) => {
	const { user } = await parent();

	if (user) {
		throw redirect(303, '/');
	}

	const raw = url.searchParams.get('reason');
	const reason: LoginReason | null = VALID_REASONS.includes(raw as LoginReason)
		? (raw as LoginReason)
		: null;

	return { reason };
};
