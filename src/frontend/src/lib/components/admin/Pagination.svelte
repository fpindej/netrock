<script lang="ts">
	import * as PaginationUI from '$lib/components/ui/pagination';

	interface Props {
		pageNumber: number;
		totalPages: number;
		onPageChange: (page: number) => void;
	}

	let { pageNumber, totalPages, onPageChange }: Props = $props();
</script>

{#if totalPages > 1}
	<PaginationUI.Root
		count={totalPages * 10}
		perPage={10}
		page={pageNumber}
		onPageChange={(page) => onPageChange(page)}
		siblingCount={1}
	>
		{#snippet children({ pages })}
			<PaginationUI.Content>
				<PaginationUI.Item>
					<PaginationUI.PrevButton />
				</PaginationUI.Item>

				{#each pages as page (page.key)}
					<PaginationUI.Item>
						{#if page.type === 'page'}
							<PaginationUI.Link {page} isActive={page.value === pageNumber} />
						{:else}
							<PaginationUI.Ellipsis />
						{/if}
					</PaginationUI.Item>
				{/each}

				<PaginationUI.Item>
					<PaginationUI.NextButton />
				</PaginationUI.Item>
			</PaginationUI.Content>
		{/snippet}
	</PaginationUI.Root>
{/if}
