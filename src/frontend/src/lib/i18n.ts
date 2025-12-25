import { register, init, getLocaleFromNavigator, locale } from 'svelte-i18n';
import { browser } from '$app/environment';

register('en', () => import('./locales/en.json'));
register('cs', () => import('./locales/cs.json'));

const defaultLocale = 'en';

export function initI18n(serverLocale?: string) {
	if (!browser) {
		init({ fallbackLocale: defaultLocale, initialLocale: serverLocale || defaultLocale });
		return;
	}

	const savedLocale = localStorage.getItem('locale');
	const browserLocale = getLocaleFromNavigator();

	// Simple logic: if it starts with 'cs', use 'cs', otherwise default to 'en'
	// You can expand this logic if you add more languages
	let initialLocale = savedLocale || serverLocale;

	if (!initialLocale) {
		if (browserLocale?.startsWith('cs')) {
			initialLocale = 'cs';
		} else {
			initialLocale = 'en';
		}
	}

	init({
		fallbackLocale: defaultLocale,
		initialLocale: initialLocale
	});
}

export function setLanguage(newLocale: string) {
	locale.set(newLocale);
	if (browser) {
		localStorage.setItem('locale', newLocale);
		document.documentElement.setAttribute('lang', newLocale);
	}
}
