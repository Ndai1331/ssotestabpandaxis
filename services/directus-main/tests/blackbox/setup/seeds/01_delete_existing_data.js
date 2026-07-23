export async function seed(knex) {
	if (process.env.TEST_LOCAL) {
		await knex('axis_collections').del();
		await knex('axis_relations').del();
		await knex('axis_roles').del();
		await knex('axis_permissions').del();
		await knex('axis_policies').del();
		await knex('axis_access').del();
		await knex('axis_revisions').del();
		await knex('axis_versions').del();
		await knex('axis_users').del();
	}
}
