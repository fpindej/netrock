<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { ChevronDown, Check } from '@lucide/svelte';
	import {
		COUNTRY_CODES,
		parsePhoneNumber,
		formatPhoneNumber,
		type CountryCode
	} from './country-codes';

	interface Props {
		/** The full phone number value (with dial code) */
		value: string;
		/** Placeholder text for the input */
		placeholder?: string;
		/** ID for the input element */
		id?: string;
		/** Whether the input is disabled */
		disabled?: boolean;
		/** Whether the field has an error */
		'aria-invalid'?: boolean;
	}

	let {
		value = $bindable(),
		placeholder = '123 456 789',
		id,
		disabled = false,
		'aria-invalid': ariaInvalid
	}: Props = $props();

	// Parse the initial value to extract country and national number
	let selectedCountry = $state<CountryCode>(COUNTRY_CODES[0]);
	let nationalNumber = $state('');

	// Sync internal state when value prop changes externally
	$effect(() => {
		const parsed = parsePhoneNumber(value);
		if (parsed.country) {
			selectedCountry = parsed.country;
		}
		nationalNumber = parsed.nationalNumber;
	});

	function handleCountrySelect(country: CountryCode) {
		selectedCountry = country;
		updateValue();
	}

	function handleNumberInput(e: Event) {
		const input = e.target as HTMLInputElement;
		nationalNumber = input.value;
		updateValue();
	}

	function updateValue() {
		value = formatPhoneNumber(selectedCountry.dialCode, nationalNumber);
	}
</script>

<div class="flex gap-1">
	<DropdownMenu.Root>
		<DropdownMenu.Trigger {disabled}>
			{#snippet child({ props })}
				<Button
					variant="outline"
					class="flex w-[100px] shrink-0 items-center justify-between gap-1 px-2"
					{...props}
				>
					<span class={`fi fi-${selectedCountry.code} h-3 w-4 shrink-0 rounded-sm`}></span>
					<span class="text-xs font-normal text-muted-foreground">{selectedCountry.dialCode}</span>
					<ChevronDown class="h-3 w-3 shrink-0 opacity-50" />
				</Button>
			{/snippet}
		</DropdownMenu.Trigger>
		<DropdownMenu.Content class="max-h-[300px] overflow-y-auto">
			{#each COUNTRY_CODES as country (country.code)}
				<DropdownMenu.Item onclick={() => handleCountrySelect(country)}>
					<span class={`fi fi-${country.code} me-2 h-3 w-4 rounded-sm`}></span>
					<span class="flex-1 truncate">{country.name}</span>
					<span class="ms-2 text-xs text-muted-foreground">{country.dialCode}</span>
					{#if selectedCountry.code === country.code}
						<Check class="ms-2 h-4 w-4 shrink-0" />
					{/if}
				</DropdownMenu.Item>
			{/each}
		</DropdownMenu.Content>
	</DropdownMenu.Root>

	<Input
		{id}
		type="tel"
		autocomplete="tel-national"
		value={nationalNumber}
		oninput={handleNumberInput}
		{placeholder}
		{disabled}
		aria-invalid={ariaInvalid}
		class="flex-1"
	/>
</div>
