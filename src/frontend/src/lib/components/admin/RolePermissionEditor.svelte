<script lang="ts">
	import { Checkbox } from '$lib/components/ui/checkbox';
	import { Label } from '$lib/components/ui/label';
	import type { PermissionGroup } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		permissionGroups: PermissionGroup[];
		selected: string[];
		disabled?: boolean;
		onchange: (permissions: string[]) => void;
	}

	let { permissionGroups, selected, disabled = false, onchange }: Props = $props();

	function togglePermission(permission: string) {
		if (disabled) return;
		const next = selected.includes(permission)
			? selected.filter((p) => p !== permission)
			: [...selected, permission];
		onchange(next);
	}
</script>

<div class="space-y-6">
	{#each permissionGroups as group (group.category)}
		<div>
			<h4 class="mb-3 text-sm font-semibold">{group.category}</h4>
			<div class="grid gap-3 sm:grid-cols-2">
				{#each group.permissions ?? [] as permission (permission)}
					<div class="flex items-center gap-2">
						<Checkbox
							id="perm-{permission}"
							checked={selected.includes(permission)}
							{disabled}
							onCheckedChange={() => togglePermission(permission)}
						/>
						<Label for="perm-{permission}" class="text-sm font-normal">
							{permission}
						</Label>
					</div>
				{/each}
			</div>
		</div>
	{/each}
	{#if disabled}
		<p class="text-sm text-muted-foreground">{m.admin_roles_permissionsReadonly()}</p>
	{/if}
</div>
