export const breadcrumbState = $state({ dynamicLabel: '' });

export function setDynamicLabel(label: string) {
	breadcrumbState.dynamicLabel = label;
}

export function clearDynamicLabel() {
	breadcrumbState.dynamicLabel = '';
}
