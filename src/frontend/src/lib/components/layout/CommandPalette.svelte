<script lang="ts">
	import * as Command from '$lib/components/ui/command';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { hasPermission, Permissions } from '$lib/utils';
	import { shortcutsState, ShortcutAction, getShortcutSymbol } from '$lib/state/shortcuts.svelte';
	import { toggleTheme } from '$lib/state/theme.svelte';
	import { logout } from '$lib/auth';
	import * as m from '$lib/paraglide/messages';
	import {
		LayoutDashboard,
		User,
		Settings,
		Users,
		Shield,
		Clock,
		KeyRound,
		Sun,
		LogOut
	} from '@lucide/svelte';
	import type { Component } from 'svelte';
	import type { IconProps } from '@lucide/svelte';
	import type { User as UserType } from '$lib/types';

	interface Props {
		user: UserType | null | undefined;
	}

	let { user }: Props = $props();

	type CommandItem = {
		label: () => string;
		icon: Component<IconProps>;
		action: () => void;
		shortcut?: string;
	};

	type AdminCommandItem = CommandItem & { permission: string };

	const navItems: CommandItem[] = [
		{
			label: m.nav_dashboard,
			icon: LayoutDashboard,
			action: () => navigate(resolve('/'))
		},
		{
			label: m.profile_title,
			icon: User,
			action: () => navigate(resolve('/profile'))
		},
		{
			label: m.nav_settings,
			icon: Settings,
			action: () => navigate(resolve('/settings')),
			shortcut: getShortcutSymbol(ShortcutAction.Settings)
		}
	];

	const adminItems: AdminCommandItem[] = [
		{
			label: m.nav_adminUsers,
			icon: Users,
			action: () => navigate(resolve('/admin/users')),
			permission: Permissions.Users.View
		},
		{
			label: m.nav_adminRoles,
			icon: Shield,
			action: () => navigate(resolve('/admin/roles')),
			permission: Permissions.Roles.View
		},
		{
			label: m.nav_adminJobs,
			icon: Clock,
			action: () => navigate(resolve('/admin/jobs')),
			permission: Permissions.Jobs.View
		},
		{
			label: m.nav_adminOAuthProviders,
			icon: KeyRound,
			action: () => navigate(resolve('/admin/oauth-providers')),
			permission: Permissions.OAuthProviders.View
		}
	];

	const actionItems: CommandItem[] = [
		{
			label: m.commandPalette_toggleTheme,
			icon: Sun,
			action: () => {
				toggleTheme();
				shortcutsState.isCommandPaletteOpen = false;
			}
		},
		{
			label: m.nav_logout,
			icon: LogOut,
			action: () => {
				shortcutsState.isCommandPaletteOpen = false;
				logout();
			},
			shortcut: getShortcutSymbol(ShortcutAction.Logout)
		}
	];

	let visibleAdminItems = $derived(
		adminItems.filter((item) => hasPermission(user, item.permission))
	);

	function navigate(href: string) {
		shortcutsState.isCommandPaletteOpen = false;
		goto(href);
	}
</script>

<Command.Dialog
	bind:open={shortcutsState.isCommandPaletteOpen}
	title={m.shortcuts_commandPalette()}
	description={m.commandPalette_placeholder()}
>
	<Command.Input placeholder={m.commandPalette_placeholder()} />
	<Command.List>
		<Command.Empty>{m.commandPalette_noResults()}</Command.Empty>

		<Command.Group heading={m.commandPalette_navigation()}>
			{#each navItems as item (item.label())}
				<Command.Item onSelect={item.action}>
					<item.icon />
					<span>{item.label()}</span>
					{#if item.shortcut}
						<Command.Shortcut>{item.shortcut}</Command.Shortcut>
					{/if}
				</Command.Item>
			{/each}
		</Command.Group>

		{#if visibleAdminItems.length > 0}
			<Command.Group heading={m.commandPalette_admin()}>
				{#each visibleAdminItems as item (item.label())}
					<Command.Item onSelect={item.action}>
						<item.icon />
						<span>{item.label()}</span>
					</Command.Item>
				{/each}
			</Command.Group>
		{/if}

		<Command.Separator />

		<Command.Group heading={m.commandPalette_actions()}>
			{#each actionItems as item (item.label())}
				<Command.Item onSelect={item.action}>
					<item.icon />
					<span>{item.label()}</span>
					{#if item.shortcut}
						<Command.Shortcut>{item.shortcut}</Command.Shortcut>
					{/if}
				</Command.Item>
			{/each}
		</Command.Group>
	</Command.List>
</Command.Dialog>
