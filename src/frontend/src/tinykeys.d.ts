declare module 'tinykeys' {
	export function tinykeys(
		target: Window | HTMLElement,
		keyBindingMap: Record<string, (event: KeyboardEvent) => void>,
		options?: { event?: string }
	): () => void;
}
