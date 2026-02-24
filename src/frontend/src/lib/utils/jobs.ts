import * as m from '$lib/paraglide/messages';

type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline';

/**
 * Format a nullable date string for job-related display.
 *
 * @param date   - ISO date string, null, or undefined
 * @param fallback - Text to show when the date is absent (defaults to '-')
 */
export function formatJobDate(date: string | null | undefined, fallback: string = '-'): string {
	if (!date) return fallback;
	return new Date(date).toLocaleString();
}

/**
 * Map a job execution status (and optional pause state) to a Badge variant.
 */
export function getJobStatusVariant(
	status: string | null | undefined,
	isPaused?: boolean
): BadgeVariant {
	if (isPaused) return 'outline';
	switch (status) {
		case 'Succeeded':
			return 'default';
		case 'Failed':
			return 'destructive';
		case 'Processing':
			return 'secondary';
		default:
			return 'outline';
	}
}

/**
 * Map a job execution status (and optional pause state) to a human-readable i18n label.
 */
export function getJobStatusLabel(status: string | null | undefined, isPaused?: boolean): string {
	if (isPaused) return m.admin_jobs_status_paused();
	switch (status) {
		case 'Succeeded':
			return m.admin_jobs_status_succeeded();
		case 'Failed':
			return m.admin_jobs_status_failed();
		case 'Processing':
			return m.admin_jobs_status_running();
		default:
			return status ?? m.admin_jobs_status_idle();
	}
}

/**
 * Format an HH:MM:SS duration string into a compact human-readable form.
 */
export function formatJobDuration(duration: string | null | undefined): string {
	if (!duration) return '-';
	const match = duration.match(/(\d+):(\d+):(\d+)/);
	if (!match) return duration;
	const hours = parseInt(match[1]);
	const minutes = parseInt(match[2]);
	const seconds = parseInt(match[3]);
	if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
	if (minutes > 0) return `${minutes}m ${seconds}s`;
	return `${seconds}s`;
}
