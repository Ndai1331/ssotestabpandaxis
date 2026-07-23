export async function seed(knex) {
	await knex('axis_settings').update({
		collaborative_editing_enabled: true,
	});
}
