<script lang="ts">
	import ProfileForm from '$lib/components/profile/ProfileForm.svelte';
	import AccountDetails from '$lib/components/profile/AccountDetails.svelte';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();
</script>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">Profile</h3>
		<p class="text-sm text-muted-foreground">This is how others will see you on the site.</p>
	</div>
	<div class="h-px w-full bg-border"></div>
	<div class="grid gap-6 md:grid-cols-2">
		<ProfileForm user={data.user} />
		<AccountDetails user={data.user} />
	</div>
</div>
