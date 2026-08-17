import type { Knex } from 'knex';

const updates = [
	{
		table: 'axis_fields',
		constraints: [
			{
				column: 'group',
				references: 'axis_fields.id',
			},
		],
	},
	{
		table: 'axis_files',
		constraints: [
			{
				column: 'folder',
				references: 'axis_folders.id',
			},
			{
				column: 'uploaded_by',
				references: 'axis_users.id',
			},
			{
				column: 'modified_by',
				references: 'axis_users.id',
			},
		],
	},
	{
		table: 'axis_folders',
		constraints: [
			{
				column: 'parent',
				references: 'axis_folders.id',
			},
		],
	},
	{
		table: 'axis_permissions',
		constraints: [
			{
				column: 'role',
				references: 'axis_roles.id',
			},
		],
	},
	{
		table: 'axis_presets',
		constraints: [
			{
				column: 'user',
				references: 'axis_users.id',
			},
			{
				column: 'role',
				references: 'axis_roles.id',
			},
		],
	},
	{
		table: 'axis_revisions',
		constraints: [
			{
				column: 'activity',
				references: 'axis_activity.id',
			},
			{
				column: 'parent',
				references: 'axis_revisions.id',
			},
		],
	},
	{
		table: 'axis_sessions',
		constraints: [
			{
				column: 'user',
				references: 'axis_users.id',
			},
		],
	},
	{
		table: 'axis_settings',
		constraints: [
			{
				column: 'project_logo',
				references: 'axis_files.id',
			},
			{
				column: 'public_foreground',
				references: 'axis_files.id',
			},
			{
				column: 'public_background',
				references: 'axis_files.id',
			},
		],
	},
	{
		table: 'axis_users',
		constraints: [
			{
				column: 'role',
				references: 'axis_roles.id',
			},
		],
	},
];

/**
 * NOTE:
 * Not all databases allow (or support) recursive onUpdate/onDelete triggers. MS SQL / Oracle flat out deny creating them,
 * Postgres behaves erratic on those triggers, not sure if MySQL / Maria plays nice either.
 */

export async function up(knex: Knex): Promise<void> {
	for (const update of updates) {
		await knex.schema.alterTable(update.table, (table) => {
			for (const constraint of update.constraints) {
				table.dropForeign([constraint.column]);
				table.foreign(constraint.column).references(constraint.references);
			}
		});
	}
}

export async function down(knex: Knex): Promise<void> {
	for (const update of updates) {
		await knex.schema.alterTable(update.table, (table) => {
			for (const constraint of update.constraints) {
				table.dropForeign([constraint.column]);
			}
		});
	}
}
