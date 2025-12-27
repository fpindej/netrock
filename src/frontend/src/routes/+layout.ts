import { setLocale, locales } from '$lib/paraglide/runtime';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async ({ data }) => {
	setLocale(data.locale as (typeof locales)[number]);
	return data;
};
