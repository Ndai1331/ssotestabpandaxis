import type { Knex } from 'knex';

export async function up(knex: Knex): Promise<void> {
	await knex.schema.alterTable('axis_versions', (table) => {
		table.json('delta');
	});
}

export async function down(knex: Knex): Promise<void> {
	await knex.schema.alterTable('axis_versions', (table) => {
		table.dropColumn('delta');
	});
}
