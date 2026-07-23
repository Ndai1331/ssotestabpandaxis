import type { Knex } from 'knex';
import { merge } from 'lodash-es';

export async function up(knex: Knex): Promise<void> {
	await knex('axis_relations')
		.delete()
		.where('many_collection', 'like', 'axis_%')
		.andWhere('one_collection', 'like', 'axis_%');
}

export async function down(knex: Knex): Promise<void> {
	const defaults = {
		many_collection: 'axis_users',
		many_field: null,
		many_primary: null,
		one_collection: null,
		one_field: null,
		one_primary: null,
		junction_field: null,
	};

	const systemRelations = [
		{
			many_collection: 'axis_users',
			many_field: 'role',
			many_primary: 'id',
			one_collection: 'axis_roles',
			one_field: 'users',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_users',
			many_field: 'avatar',
			many_primary: 'id',
			one_collection: 'axis_files',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_revisions',
			many_field: 'activity',
			many_primary: 'id',
			one_collection: 'axis_activity',
			one_field: 'revisions',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_presets',
			many_field: 'user',
			many_primary: 'id',
			one_collection: 'axis_users',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_presets',
			many_field: 'role',
			many_primary: 'id',
			one_collection: 'axis_roles',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_folders',
			many_field: 'parent',
			many_primary: 'id',
			one_collection: 'axis_folders',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_files',
			many_field: 'folder',
			many_primary: 'id',
			one_collection: 'axis_folders',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_files',
			many_field: 'uploaded_by',
			many_primary: 'id',
			one_collection: 'axis_users',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_fields',
			many_field: 'collection',
			many_primary: 'id',
			one_collection: 'axis_collections',
			one_field: 'fields',
			one_primary: 'collection',
		},
		{
			many_collection: 'axis_activity',
			many_field: 'user',
			many_primary: 'id',
			one_collection: 'axis_users',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_settings',
			many_field: 'project_logo',
			many_primary: 'id',
			one_collection: 'axis_files',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_settings',
			many_field: 'public_foreground',
			many_primary: 'id',
			one_collection: 'axis_files',
			one_primary: 'id',
		},
		{
			many_collection: 'axis_settings',
			many_field: 'public_background',
			many_primary: 'id',
			one_collection: 'axis_files',
			one_primary: 'id',
		},
	].map((row) => {
		for (const [key, value] of Object.entries(row)) {
			if (value !== null && (typeof value === 'object' || Array.isArray(value))) {
				(row as any)[key] = JSON.stringify(value);
			}
		}

		return merge({}, defaults, row);
	});

	await knex.insert(systemRelations).into('axis_relations');
}
