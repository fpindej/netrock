import type { components } from '$lib/api/v1';

declare global {
	namespace App {
		interface Locals {
			user: components['schemas']['UserResponse'] | null;
			locale: string;
		}
	}

	interface Window {
		turnstile?: {
			render: (container: HTMLElement, options: Record<string, unknown>) => string;
			reset: (widgetId: string) => void;
			remove: (widgetId: string) => void;
		};
	}
}

export {};
