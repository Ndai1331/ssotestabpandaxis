import type { Knex } from 'knex';

export async function up(knex: Knex): Promise<void> {
	await knex.schema.alterTable('axis_fields', (table) => {
		table.dropForeign(['collection']);
	});

	await knex.schema.alterTable('axis_activity', (table) => {
		table.dropForeign(['collection']);
	});

	await knex.schema.alterTable('axis_permissions', (table) => {
		table.dropForeign(['collection']);
	});

	await knex.schema.alterTable('axis_presets', (table) => {
		table.dropForeign(['collection']);
	});

	await knex.schema.alterTable('axis_relations', (table) => {
		table.dropForeign(['one_collection']);
		table.dropForeign(['many_collection']);
	});

	await knex.schema.alterTable('axis_revisions', (table) => {
		table.dropForeign(['collection']);
	});
}

export async function down(knex: Knex): Promise<void> {
	await knex.schema.alterTable('axis_fields', (table) => {
		table.foreign('collection').references('axis_collections.collection');
	});

	await knex.schema.alterTable('axis_activity', (table) => {
		table.foreign('collection').references('axis_collections.collection');
	});

	await knex.schema.alterTable('axis_permissions', (table) => {
		table.foreign('collection').references('axis_collections.collection');
	});

	await knex.schema.alterTable('axis_presets', (table) => {
		table.foreign('collection').references('axis_collections.collection');
	});

	await knex.schema.alterTable('axis_relations', (table) => {
		table.foreign('one_collection').references('axis_collections.collection');
		table.foreign('many_collection').references('axis_collections.collection');
	});

	await knex.schema.alterTable('axis_revisions', (table) => {
		table.foreign('collection').references('axis_collections.collection');
	});
}
