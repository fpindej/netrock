<script lang="ts">
	import { untrack } from 'svelte';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { ChevronDown, Check } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import { cn } from '$lib/utils';
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
		/** ID of the element describing this input (for errors) */
		'aria-describedby'?: string;
		/** Additional CSS classes for the input */
		class?: string;
	}

	let {
		value = $bindable(),
		placeholder = '123 456 789',
		id,
		disabled = false,
		'aria-invalid': ariaInvalid,
		'aria-describedby': ariaDescribedby,
		class: className
	}: Props = $props();

	// Parse the initial value to extract country and national number
	// COUNTRY_CODES is a constant non-empty array, so index 0 always exists
	let selectedCountry = $state<CountryCode>(COUNTRY_CODES[0] ?? { code: 'cz', dialCode: '+420' });
	let nationalNumber = $state('');

	// Track the last value we produced internally so we can distinguish
	// external prop changes from our own updateValue() writes.
	let lastEmittedValue = '';

	// Sync internal state only when value prop changes externally.
	// Reading `value` subscribes to the prop; writing internal state inside
	// `untrack` avoids creating a circular reactive dependency.
	$effect(() => {
		const current = value;

		untrack(() => {
			if (current === lastEmittedValue) return;

			const parsed = parsePhoneNumber(current);
			if (parsed.country) {
				selectedCountry = parsed.country;
			}
			nationalNumber = parsed.nationalNumber;
			lastEmittedValue = current;
		});
	});

	/**
	 * Gets the localized country name for a given country code.
	 */
	function getCountryName(code: string): string {
		const countryNames: Record<string, () => string> = {
			cz: m.country_cz,
			sk: m.country_sk,
			de: m.country_de,
			at: m.country_at,
			pl: m.country_pl,
			gb: m.country_gb,
			us: m.country_us,
			fr: m.country_fr,
			it: m.country_it,
			es: m.country_es,
			nl: m.country_nl,
			be: m.country_be,
			ch: m.country_ch,
			hu: m.country_hu,
			ro: m.country_ro,
			ua: m.country_ua
		};
		return countryNames[code]?.() ?? code.toUpperCase();
	}

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
		lastEmittedValue = value;
	}
</script>

<div class="flex gap-1">
	<DropdownMenu.Root>
		<DropdownMenu.Trigger {disabled}>
			{#snippet child({ props })}
				<Button
					variant="outline"
					class="flex w-20 shrink-0 items-center justify-between gap-1 px-2 sm:w-[100px]"
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
					<span class="flex-1 truncate">{getCountryName(country.code)}</span>
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
		aria-describedby={ariaDescribedby}
		class={cn('flex-1', className)}
	/>
</div>
