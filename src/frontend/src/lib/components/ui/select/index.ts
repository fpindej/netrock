import { Select as SelectPrimitive } from 'bits-ui';
import Content from './select-content.svelte';
import Item from './select-item.svelte';
import Trigger from './select-trigger.svelte';

const Root = SelectPrimitive.Root;
const Group = SelectPrimitive.Group;
const GroupHeading = SelectPrimitive.GroupHeading;

export {
	Root,
	Group,
	GroupHeading,
	Content,
	Item,
	Trigger,
	//
	Root as Select,
	Group as SelectGroup,
	GroupHeading as SelectGroupHeading,
	Content as SelectContent,
	Item as SelectItem,
	Trigger as SelectTrigger
};
