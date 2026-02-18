<script lang="ts">
	import { onMount } from 'svelte';
	import { browser } from '$app/environment';

	interface Props {
		siteKey: string;
		onVerified?: (token: string) => void;
		onError?: () => void;
		resetRef?: (fn: () => void) => void;
	}

	let { siteKey, onVerified, onError, resetRef }: Props = $props();
	let container: HTMLDivElement | undefined = $state();
	let widgetId: string | undefined = $state();

	const SCRIPT_ID = 'cf-turnstile-script';
	const SCRIPT_SRC = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
	const LOAD_TIMEOUT_MS = 10_000;

	function getWidgetTheme(): 'light' | 'dark' {
		if (!browser) return 'light';
		return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
	}

	function reset() {
		if (widgetId !== undefined && window.turnstile) {
			window.turnstile.reset(widgetId);
			onVerified?.('');
		}
	}

	function renderWidget() {
		if (!window.turnstile || !container) return;
		widgetId = window.turnstile.render(container, {
			sitekey: siteKey,
			size: 'flexible',
			theme: getWidgetTheme(),
			callback: (token: string) => onVerified?.(token),
			'expired-callback': () => onVerified?.(''),
			'error-callback': () => onError?.()
		});
		resetRef?.(reset);
	}

	function loadScript(): Promise<void> {
		return new Promise((resolve, reject) => {
			const existing = document.getElementById(SCRIPT_ID);
			if (existing) {
				// Script tag exists — it may still be loading
				if (window.turnstile) {
					resolve();
				} else {
					existing.addEventListener('load', () => resolve(), { once: true });
					existing.addEventListener(
						'error',
						() => reject(new Error('Failed to load Turnstile script')),
						{
							once: true
						}
					);
				}
				return;
			}

			const script = document.createElement('script');
			script.id = SCRIPT_ID;
			script.src = SCRIPT_SRC;
			script.async = true;
			script.onload = () => resolve();
			script.onerror = () => reject(new Error('Failed to load Turnstile script'));
			document.head.appendChild(script);
		});
	}

	onMount(() => {
		let aborted = false;
		const timeout = setTimeout(() => {
			if (!widgetId && !aborted) {
				onError?.();
			}
		}, LOAD_TIMEOUT_MS);

		async function init() {
			try {
				await loadScript();
				if (!aborted) renderWidget();
			} catch {
				if (!aborted) onError?.();
			}
		}

		init();

		return () => {
			aborted = true;
			clearTimeout(timeout);
			if (widgetId && window.turnstile) {
				window.turnstile.remove(widgetId);
			}
		};
	});
</script>

<div class="w-full" bind:this={container}></div>
