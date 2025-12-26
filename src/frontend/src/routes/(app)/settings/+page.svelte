<script lang="ts">
	import WorkInProgress from '$lib/components/common/WorkInProgress.svelte';
	import { t } from '$lib/i18n';
</script>

<svelte:head>
	<title>{$t('common.meta.titleTemplate').replace('%s', $t('common.meta.settings.title'))}</title>
	<meta name="description" content={$t('common.meta.settings.description')} />
</svelte:head>

<WorkInProgress
	title={$t('settings.title')}
	description={$t('settings.description')}
	wipDescription={$t('settings.wipDescription')}
/>
