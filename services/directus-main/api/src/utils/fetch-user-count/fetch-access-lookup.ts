import type { PrimaryKey } from '@directus/types';
import type { Knex } from 'knex';

export interface AccessLookup {
	role: string | null;
	user: string | null;
	app_access: boolean | number;
	admin_access: boolean | number;
	user_status: 'active' | string;
	user_role: string | null;
}

export interface FetchAccessLookupOptions {
	excludeAccessRows?: PrimaryKey[];
	excludePolicies?: PrimaryKey[];
	excludeUsers?: PrimaryKey[];
	excludeRoles?: PrimaryKey[];
	adminOnly?: boolean;
	knex: Knex;
}

export async function fetchAccessLookup(options: FetchAccessLookupOptions): Promise<AccessLookup[]> {
	let query = options.knex
		.select(
			'axis_access.role',
			'axis_access.user',
			'axis_policies.app_access',
			'axis_policies.admin_access',
			'axis_users.status as user_status',
			'axis_users.role as user_role',
		)
		.from('axis_access')
		.leftJoin('axis_policies', 'axis_access.policy', 'axis_policies.id')
		.leftJoin('axis_users', 'axis_access.user', 'axis_users.id');

	if (options.excludeAccessRows && options.excludeAccessRows.length > 0) {
		query = query.whereNotIn('axis_access.id', options.excludeAccessRows);
	}

	if (options.excludePolicies && options.excludePolicies.length > 0) {
		query = query.whereNotIn('axis_access.policy', options.excludePolicies);
	}

	if (options.excludeUsers && options.excludeUsers.length > 0) {
		query = query.where((q) =>
			q.whereNotIn('axis_access.user', options.excludeUsers!).orWhereNull('axis_access.user'),
		);
	}

	if (options.excludeRoles && options.excludeRoles.length > 0) {
		query = query.where((q) =>
			q.whereNotIn('axis_access.role', options.excludeRoles!).orWhereNull('axis_access.role'),
		);
	}

	if (options.adminOnly) {
		query = query.where('axis_policies.admin_access', 1);
	}

	return query;
}
