<script lang="ts">
	import { LoginForm } from '$lib/components/auth';
	import { t } from '$lib/i18n';
	let { data } = $props();
</script>

<svelte:head>
	<title>{$t('common.meta.titleTemplate').replace('%s', $t('common.meta.login.title'))}</title>
	<meta name="description" content={$t('common.meta.login.description')} />
</svelte:head>

<LoginForm apiUrl={data.apiUrl} />
