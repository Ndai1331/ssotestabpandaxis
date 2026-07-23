import type { MergeCoreCollection } from '../index.js';

export type DirectusFolder<Schema = any> = MergeCoreCollection<
	Schema,
	'axis_folders',
	{
		id: string;
		name: string;
		parent: DirectusFolder<Schema> | string | null;
	}
>;
