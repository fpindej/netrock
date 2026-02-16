<script lang="ts">
	import { onMount } from 'svelte';

	interface Props {
		siteKey: string;
		onVerified?: (token: string) => void;
		onError?: () => void;
	}

	let { siteKey, onVerified, onError }: Props = $props();
	let container: HTMLDivElement | undefined = $state();

	onMount(() => {
		const scriptId = 'cf-turnstile-script';
		if (!document.getElementById(scriptId)) {
			const script = document.createElement('script');
			script.id = scriptId;
			script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
			script.async = true;
			document.head.appendChild(script);
		}

		const interval = setInterval(() => {
			if (window.turnstile && container) {
				clearInterval(interval);
				window.turnstile.render(container, {
					sitekey: siteKey,
					callback: (token: string) => onVerified?.(token),
					'error-callback': () => onError?.()
				});
			}
		}, 100);

		return () => clearInterval(interval);
	});
</script>

<div bind:this={container}></div>
