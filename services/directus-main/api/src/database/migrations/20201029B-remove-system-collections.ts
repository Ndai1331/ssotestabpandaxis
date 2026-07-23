import type { Knex } from 'knex';
import { merge } from 'lodash-es';

export async function up(knex: Knex): Promise<void> {
	await knex('axis_collections').delete().where('collection', 'like', 'axis_%');
}

export async function down(knex: Knex): Promise<void> {
	const defaults = {
		collection: null,
		hidden: false,
		singleton: false,
		icon: null,
		note: null,
		translations: null,
		display_template: null,
	};

	const systemCollections = [
		{
			collection: 'axis_activity',
			note: 'Accountability logs for all events',
		},
		{
			collection: 'axis_collections',
			icon: 'list_alt',
			note: 'Additional collection configuration and metadata',
		},
		{
			collection: 'axis_fields',
			icon: 'input',
			note: 'Additional field configuration and metadata',
		},
		{
			collection: 'axis_files',
			icon: 'folder',
			note: 'Metadata for all managed file assets',
		},
		{
			collection: 'axis_folders',
			note: 'Provides virtual directories for files',
		},
		{
			collection: 'axis_permissions',
			icon: 'admin_panel_settings',
			note: 'Access permissions for each role',
		},
		{
			collection: 'axis_presets',
			icon: 'bookmark_border',
			note: 'Presets for collection defaults and bookmarks',
		},
		{
			collection: 'axis_relations',
			icon: 'merge_type',
			note: 'Relationship configuration and metadata',
		},
		{
			collection: 'axis_revisions',
			note: 'Data snapshots for all activity',
		},
		{
			collection: 'axis_roles',
			icon: 'supervised_user_circle',
			note: 'Permission groups for system users',
		},
		{
			collection: 'axis_sessions',
			note: 'User session information',
		},
		{
			collection: 'axis_settings',
			singleton: true,
			note: 'Project configuration options',
		},
		{
			collection: 'axis_users',
			archive_field: 'status',
			archive_value: 'archived',
			unarchive_value: 'draft',
			icon: 'people_alt',
			note: 'System users for the platform',
		},
		{
			collection: 'axis_webhooks',
			note: 'Configuration for event-based HTTP requests',
		},
	].map((row) => {
		for (const [key, value] of Object.entries(row)) {
			if (value !== null && (typeof value === 'object' || Array.isArray(value))) {
				(row as any)[key] = JSON.stringify(value);
			}
		}

		return merge({}, defaults, row);
	});

	await knex.insert(systemCollections).into('axis_collections');
}
