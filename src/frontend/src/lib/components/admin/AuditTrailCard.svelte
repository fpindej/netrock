<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Pagination } from '$lib/components/admin';
	import { browserClient } from '$lib/api/client';
	import { History } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { AuditEvent } from '$lib/types';

	interface Props {
		userId: string;
	}

	let { userId }: Props = $props();

	let events = $state<AuditEvent[]>([]);
	let loading = $state(true);
	let pageNumber = $state(1);
	let totalPages = $state(0);
	let hasPreviousPage = $state(false);
	let hasNextPage = $state(false);

	const pageSize = 10;

	async function loadEvents(page: number): Promise<void> {
		loading = true;
		const { data } = await browserClient.GET('/api/v1/admin/users/{id}/audit', {
			params: {
				path: { id: userId },
				query: { PageNumber: page, PageSize: pageSize }
			}
		});

		if (data) {
			events = (data.items as AuditEvent[]) ?? [];
			pageNumber = data.pageNumber ?? 1;
			totalPages = data.totalPages ?? 0;
			hasPreviousPage = data.hasPreviousPage ?? false;
			hasNextPage = data.hasNextPage ?? false;
		}
		loading = false;
	}

	function handlePageChange(page: number): void {
		loadEvents(page);
	}

	function formatDate(date: string | null | undefined): string {
		if (!date) return '-';
		return new Date(date).toLocaleString();
	}

	function getActionLabel(action: string | undefined): string {
		switch (action) {
			case 'LoginSuccess':
				return m.audit_action_loginSuccess();
			case 'LoginFailure':
				return m.audit_action_loginFailure();
			case 'Logout':
				return m.audit_action_logout();
			case 'Register':
				return m.audit_action_register();
			case 'PasswordChange':
				return m.audit_action_passwordChange();
			case 'PasswordResetRequest':
				return m.audit_action_passwordResetRequest();
			case 'PasswordReset':
				return m.audit_action_passwordReset();
			case 'EmailVerification':
				return m.audit_action_emailVerification();
			case 'ResendVerificationEmail':
				return m.audit_action_resendVerificationEmail();
			case 'ProfileUpdate':
				return m.audit_action_profileUpdate();
			case 'AccountDeletion':
				return m.audit_action_accountDeletion();
			case 'AdminCreateUser':
				return m.audit_action_adminCreateUser();
			case 'AdminLockUser':
				return m.audit_action_adminLockUser();
			case 'AdminUnlockUser':
				return m.audit_action_adminUnlockUser();
			case 'AdminDeleteUser':
				return m.audit_action_adminDeleteUser();
			case 'AdminVerifyEmail':
				return m.audit_action_adminVerifyEmail();
			case 'AdminSendPasswordReset':
				return m.audit_action_adminSendPasswordReset();
			case 'AdminAssignRole':
				return m.audit_action_adminAssignRole();
			case 'AdminRemoveRole':
				return m.audit_action_adminRemoveRole();
			case 'AdminCreateRole':
				return m.audit_action_adminCreateRole();
			case 'AdminUpdateRole':
				return m.audit_action_adminUpdateRole();
			case 'AdminDeleteRole':
				return m.audit_action_adminDeleteRole();
			case 'AdminSetRolePermissions':
				return m.audit_action_adminSetRolePermissions();
			default:
				return action ?? '-';
		}
	}

	function getActionVariant(
		action: string | undefined
	): 'default' | 'secondary' | 'destructive' | 'outline' {
		if (!action) return 'outline';
		if (action.includes('Delete') || action === 'AccountDeletion' || action === 'LoginFailure')
			return 'destructive';
		if (action.startsWith('Admin')) return 'secondary';
		return 'default';
	}

	$effect(() => {
		loadEvents(1);
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.audit_trail_title()}</Card.Title>
		<Card.Description>{m.audit_trail_description()}</Card.Description>
	</Card.Header>
	<Card.Content class="p-0">
		{#if loading}
			<div class="flex items-center justify-center py-12">
				<div
					class="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent"
				></div>
			</div>
		{:else if events.length === 0}
			<div class="flex flex-col items-center justify-center py-12 text-center">
				<div class="mb-3 rounded-full bg-muted p-3">
					<History class="h-6 w-6 text-muted-foreground" />
				</div>
				<p class="text-sm text-muted-foreground">{m.audit_trail_empty()}</p>
			</div>
		{:else}
			<!-- Mobile: card list -->
			<div class="divide-y md:hidden">
				{#each events as event (event.id)}
					<div class="space-y-1 p-4">
						<div class="flex items-center justify-between">
							<span class="text-xs text-muted-foreground">
								{formatDate(event.createdAt)}
							</span>
							<Badge variant={getActionVariant(event.action)}>
								{getActionLabel(event.action)}
							</Badge>
						</div>
						{#if event.targetEntityType}
							<p class="text-xs text-muted-foreground">
								{m.audit_trail_col_target()}: {event.targetEntityType}
							</p>
						{/if}
						{#if event.metadata}
							<p class="truncate text-xs text-muted-foreground">{event.metadata}</p>
						{/if}
					</div>
				{/each}
			</div>

			<!-- Desktop: table -->
			<div class="hidden overflow-x-auto md:block">
				<table class="w-full text-sm">
					<thead>
						<tr class="border-b bg-muted/50 text-start">
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.audit_trail_col_timestamp()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.audit_trail_col_action()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.audit_trail_col_target()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.audit_trail_col_details()}
							</th>
						</tr>
					</thead>
					<tbody>
						{#each events as event (event.id)}
							<tr class="border-b">
								<td class="px-4 py-3 text-muted-foreground">
									{formatDate(event.createdAt)}
								</td>
								<td class="px-4 py-3">
									<Badge variant={getActionVariant(event.action)}>
										{getActionLabel(event.action)}
									</Badge>
								</td>
								<td class="px-4 py-3 text-muted-foreground">
									{event.targetEntityType ?? '-'}
								</td>
								<td class="max-w-xs truncate px-4 py-3 text-muted-foreground">
									{event.metadata ?? '-'}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

			<div class="p-4">
				<Pagination
					{pageNumber}
					{totalPages}
					{hasPreviousPage}
					{hasNextPage}
					onPageChange={handlePageChange}
				/>
			</div>
		{/if}
	</Card.Content>
</Card.Root>
