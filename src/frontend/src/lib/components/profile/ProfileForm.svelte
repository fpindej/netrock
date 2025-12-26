<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Avatar from '$lib/components/ui/avatar';
	import type { components } from '$lib/api/v1';

	type UserType = components['schemas']['MeResponse'];

	let { user }: { user: UserType | null | undefined } = $props();

	// Mock data for placeholders
	let fullName = $state('John Doe');
	let email = $state('john.doe@example.com');
	let bio = $state('Software Engineer based in San Francisco.');
	let isLoading = $state(false);

	function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		// Simulate API call
		setTimeout(() => {
			isLoading = false;
		}, 1000);
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>Personal Information</Card.Title>
		<Card.Description>Update your personal details here.</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSubmit} class="space-y-6">
			<div class="flex flex-col items-center gap-4 sm:flex-row">
				<div class="relative h-24 w-24">
					<Avatar.Root class="h-24 w-24">
						<Avatar.Image src="https://github.com/shadcn.png" alt="@shadcn" class="object-cover" />
						<Avatar.Fallback class="text-lg">
							{user?.username?.substring(0, 2).toUpperCase() ?? 'ME'}
						</Avatar.Fallback>
					</Avatar.Root>
				</div>
				<div class="flex flex-col gap-1 text-center sm:text-left">
					<h3 class="text-lg font-medium">{fullName}</h3>
					<p class="text-sm text-muted-foreground">{email}</p>
					<Button variant="outline" size="sm" class="mt-2 w-full sm:w-auto">Change Avatar</Button>
				</div>
			</div>

			<div class="grid gap-4">
				<div class="grid gap-2">
					<Label for="username">Username</Label>
					<Input id="username" value={user?.username} disabled />
					<p class="text-xs text-muted-foreground"></p>
					<div class="grid gap-2">
						<Label for="fullName">Full Name</Label>
						<Input id="fullName" bind:value={fullName} placeholder="Your full name" />
					</div>

					<div class="grid gap-2">
						<Label for="email">Email</Label>
						<Input id="email" type="email" bind:value={email} placeholder="Your email address" />
					</div>

					<div class="grid gap-2">
						<Label for="bio">Bio</Label>
						<Input id="bio" bind:value={bio} placeholder="Tell us a little bit about yourself" />
					</div>
				</div>

				<div class="flex justify-end">
					<Button type="submit" disabled={isLoading}>
						{isLoading ? 'Saving...' : 'Save Changes'}
					</Button>
				</div>
			</div>
		</form>
	</Card.Content>
</Card.Root>
