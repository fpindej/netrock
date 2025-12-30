<!--
╔══════════════════════════════════════════════════════════════════════════════╗
║                           🏠 DASHBOARD PAGE                                   ║
║                                                                              ║
║  TO REMOVE THE GETTING STARTED CONTENT:                                      ║
║  ──────────────────────────────────────                                      ║
║  1. Delete: src/lib/components/getting-started/ folder                       ║
║  2. Replace <GettingStarted> below with your own dashboard content           ║
║  3. Remove translation keys from src/messages/*.json:                        ║
║     - gettingStarted_*                                                       ║
║     - dashboard_authenticated, dashboard_welcome_*, dashboard_feature_*      ║
║                                                                              ║
╚══════════════════════════════════════════════════════════════════════════════╝
-->

<script lang="ts">
	import * as m from '$lib/paraglide/messages';
	import { GettingStarted } from '$lib/components/getting-started';

	let { data } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_dashboard_title() })}</title>
	<meta name="description" content={m.meta_dashboard_description()} />
</svelte:head>

<!-- Remove this component and the getting-started folder when you're ready to build your own dashboard -->
<GettingStarted username={data.user?.username} />
