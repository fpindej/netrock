<script lang="ts">
	import * as Sidebar from '$lib/components/ui/sidebar';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { hasPermission, Permissions } from '$lib/utils';
	import {
		LayoutDashboard,
		Users,
		Shield,
		Clock,
		KeyRound,
		Package2,
		PanelLeft,
		type IconProps
	} from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import { ThemeToggle, LanguageSelector, UserNav } from '$lib/components/layout';
	import type { Component } from 'svelte';
	import type { User } from '$lib/types';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	const sidebar = Sidebar.useSidebar();
	let collapsed = $derived(sidebar.state === 'collapsed');

	type NavItem = { title: () => string; href: string; icon: Component<IconProps> };
	type AdminNavItem = NavItem & { permission: string };

	const items: NavItem[] = [
		{
			title: m.nav_dashboard,
			href: resolve('/'),
			icon: LayoutDashboard
		}
	];

	const adminItems: AdminNavItem[] = [
		{
			title: m.nav_adminUsers,
			href: resolve('/admin/users'),
			icon: Users,
			permission: Permissions.Users.View
		},
		{
			title: m.nav_adminRoles,
			href: resolve('/admin/roles'),
			icon: Shield,
			permission: Permissions.Roles.View
		},
		{
			title: m.nav_adminJobs,
			href: resolve('/admin/jobs'),
			icon: Clock,
			permission: Permissions.Jobs.View
		},
		{
			title: m.nav_adminOAuthProviders,
			href: resolve('/admin/oauth-providers'),
			icon: KeyRound,
			permission: Permissions.OAuthProviders.View
		}
	];

	let visibleAdminItems = $derived(
		adminItems.filter((item) => hasPermission(user, item.permission))
	);

	function isActive(href: string): boolean {
		const pathname = page.url.pathname;
		if (href === resolve('/')) {
			return pathname === href;
		}
		return pathname.startsWith(href);
	}

	function handleNavigate() {
		if (sidebar.isMobile) {
			sidebar.setOpenMobile(false);
		}
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() in items array -->
<Sidebar.Root collapsible="icon" class="ps-[env(safe-area-inset-left,0px)]">
	<Sidebar.Header>
		<Sidebar.Menu>
			<Sidebar.MenuItem>
				<Sidebar.MenuButton size="lg">
					{#snippet child({ props })}
						<a href={resolve('/')} onclick={handleNavigate} {...props}>
							<div
								class="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground"
							>
								<Package2 class="size-4" />
							</div>
							<span class="truncate font-semibold">{m.app_name()}</span>
						</a>
					{/snippet}
				</Sidebar.MenuButton>
			</Sidebar.MenuItem>
		</Sidebar.Menu>
	</Sidebar.Header>
	<Sidebar.Content>
		<Sidebar.Group>
			<Sidebar.GroupContent>
				<Sidebar.Menu>
					{#each items as item (item.href)}
						<Sidebar.MenuItem>
							<Sidebar.MenuButton tooltipContent={item.title()} isActive={isActive(item.href)}>
								{#snippet child({ props })}
									<a
										href={item.href}
										onclick={handleNavigate}
										aria-current={isActive(item.href) ? 'page' : undefined}
										{...props}
									>
										<item.icon />
										<span>{item.title()}</span>
									</a>
								{/snippet}
							</Sidebar.MenuButton>
						</Sidebar.MenuItem>
					{/each}
				</Sidebar.Menu>
			</Sidebar.GroupContent>
		</Sidebar.Group>
		{#if visibleAdminItems.length > 0}
			<Sidebar.Group>
				<Sidebar.GroupLabel>{m.nav_admin()}</Sidebar.GroupLabel>
				<Sidebar.GroupContent>
					<Sidebar.Menu>
						{#each visibleAdminItems as item (item.href)}
							<Sidebar.MenuItem>
								<Sidebar.MenuButton tooltipContent={item.title()} isActive={isActive(item.href)}>
									{#snippet child({ props })}
										<a
											href={item.href}
											onclick={handleNavigate}
											aria-current={isActive(item.href) ? 'page' : undefined}
											{...props}
										>
											<item.icon />
											<span>{item.title()}</span>
										</a>
									{/snippet}
								</Sidebar.MenuButton>
							</Sidebar.MenuItem>
						{/each}
					</Sidebar.Menu>
				</Sidebar.GroupContent>
			</Sidebar.Group>
		{/if}
	</Sidebar.Content>
	<Sidebar.Footer class="pb-[max(0.5rem,env(safe-area-inset-bottom,0px))]">
		{#if !sidebar.isMobile}
			<div class="flex items-center gap-1 group-data-[collapsible=icon]:flex-col">
				<LanguageSelector />
				<ThemeToggle {collapsed} />
				<div class="flex-1 group-data-[collapsible=icon]:hidden"></div>
				{#if user}
					<UserNav {user} />
				{/if}
			</div>
			<Sidebar.Separator />
			<Sidebar.Menu>
				<Sidebar.MenuItem>
					<Sidebar.MenuButton
						tooltipContent={collapsed ? m.nav_expand() : m.nav_collapse()}
						onclick={() => sidebar.toggle()}
					>
						<PanelLeft />
						<span>{collapsed ? m.nav_expand() : m.nav_collapse()}</span>
					</Sidebar.MenuButton>
				</Sidebar.MenuItem>
			</Sidebar.Menu>
		{/if}
	</Sidebar.Footer>
</Sidebar.Root>
