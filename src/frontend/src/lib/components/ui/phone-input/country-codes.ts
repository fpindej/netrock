/**
 * Country code configuration for phone number input.
 * Uses flag-icons CSS library for flag display.
 *
 * @see https://flagicons.lipis.dev/
 */

export interface CountryCode {
	/** ISO 3166-1 alpha-2 country code (for flag) */
	code: string;
	/** Country name for display */
	name: string;
	/** International dialing code (with +) */
	dialCode: string;
}

/**
 * Common European and international country codes.
 * Ordered by likely usage frequency for European users.
 */
export const COUNTRY_CODES: CountryCode[] = [
	{ code: 'cz', name: 'Czech Republic', dialCode: '+420' },
	{ code: 'sk', name: 'Slovakia', dialCode: '+421' },
	{ code: 'de', name: 'Germany', dialCode: '+49' },
	{ code: 'at', name: 'Austria', dialCode: '+43' },
	{ code: 'pl', name: 'Poland', dialCode: '+48' },
	{ code: 'gb', name: 'United Kingdom', dialCode: '+44' },
	{ code: 'us', name: 'United States', dialCode: '+1' },
	{ code: 'fr', name: 'France', dialCode: '+33' },
	{ code: 'it', name: 'Italy', dialCode: '+39' },
	{ code: 'es', name: 'Spain', dialCode: '+34' },
	{ code: 'nl', name: 'Netherlands', dialCode: '+31' },
	{ code: 'be', name: 'Belgium', dialCode: '+32' },
	{ code: 'ch', name: 'Switzerland', dialCode: '+41' },
	{ code: 'hu', name: 'Hungary', dialCode: '+36' },
	{ code: 'ro', name: 'Romania', dialCode: '+40' },
	{ code: 'ua', name: 'Ukraine', dialCode: '+380' }
];

/**
 * Finds a country code entry by dial code.
 */
export function findCountryByDialCode(dialCode: string): CountryCode | undefined {
	return COUNTRY_CODES.find((c) => c.dialCode === dialCode);
}

/**
 * Extracts the dial code from a full phone number.
 * Returns the matched country and the remaining number.
 */
export function parsePhoneNumber(phone: string): {
	country: CountryCode | undefined;
	nationalNumber: string;
} {
	const cleaned = phone.trim();

	if (!cleaned.startsWith('+')) {
		return { country: undefined, nationalNumber: cleaned };
	}

	// Try to match longest dial codes first (e.g., +380 before +38)
	const sortedCodes = [...COUNTRY_CODES].sort((a, b) => b.dialCode.length - a.dialCode.length);

	for (const country of sortedCodes) {
		if (cleaned.startsWith(country.dialCode)) {
			return {
				country,
				nationalNumber: cleaned.slice(country.dialCode.length).trim()
			};
		}
	}

	return { country: undefined, nationalNumber: cleaned };
}

/**
 * Formats a phone number with dial code.
 */
export function formatPhoneNumber(dialCode: string, nationalNumber: string): string {
	const cleanedNumber = nationalNumber.trim();
	if (!cleanedNumber) return '';
	return `${dialCode}${cleanedNumber}`;
}
