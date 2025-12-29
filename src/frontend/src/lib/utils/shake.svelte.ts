/**
 * Creates a shake state that can be used to trigger shake animations on error.
 *
 * Usage:
 * ```svelte
 * <script>
 *   import { createShake } from '$lib/utils/shake.svelte';
 *   const shake = createShake();
 *
 *   function handleError() {
 *     shake.trigger();
 *   }
 * </script>
 *
 * <div class={shake.class}>...</div>
 * ```
 */
export function createShake(duration = 500) {
	let isShaking = $state(false);

	return {
		get active() {
			return isShaking;
		},
		get class() {
			return isShaking ? 'animate-shake' : '';
		},
		trigger() {
			isShaking = true;
			setTimeout(() => {
				isShaking = false;
			}, duration);
		}
	};
}
