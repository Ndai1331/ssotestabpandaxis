import type { MergeCoreCollection } from '../index.js';

export type DirectusTranslation<Schema = any> = MergeCoreCollection<
	Schema,
	'axis_translations',
	{
		id: string; // uuid
		language: string;
		key: string;
		value: string;
	}
>;
