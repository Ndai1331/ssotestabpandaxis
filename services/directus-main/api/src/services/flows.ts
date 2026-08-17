import type { AbstractServiceOptions, FlowRaw, Item, MutationOptions, PrimaryKey } from '@directus/types';
import { getFlowManager } from '../flows.js';
import { getEntitlementManager } from '../license/entitlements/manager.js';
import { validateAccess } from '../permissions/modules/validate-access/validate-access.js';
import { ItemsService } from './items.js';

export class FlowsService extends ItemsService<FlowRaw> {
	constructor(options: AbstractServiceOptions) {
		super('axis_flows', options);
	}

	override async createOne(data: Partial<Item>, opts?: MutationOptions): Promise<PrimaryKey> {
		if (!('status' in data) || data['status'] === 'active') {
			await getEntitlementManager().assert('flows', { adding: 1, knex: this.knex });
		}

		const result = await super.createOne(data, opts);

		await getEntitlementManager().clearCache('flows');
		await getFlowManager().reload();

		return result;
	}

	override async updateMany(keys: PrimaryKey[], data: Partial<Item>, opts?: MutationOptions): Promise<PrimaryKey[]> {
		if ('status' in data && data['status'] === 'active') {
			await getEntitlementManager().assert('flows', { adding: keys.length, knex: this.knex });
		}

		const result = await super.updateMany(keys, data, opts);

		await getEntitlementManager().clearCache('flows');
		await getFlowManager().reload();

		return result;
	}

	override async deleteMany(keys: PrimaryKey[], opts?: MutationOptions): Promise<PrimaryKey[]> {
		if (this.accountability) {
			await validateAccess(
				{
					collection: 'axis_flows',
					action: 'delete',
					accountability: this.accountability,
					primaryKeys: keys,
				},
				{ knex: this.knex, schema: this.schema },
			);
		}

		// this is to prevent foreign key constraint error on axis_operations resolve/reject during cascade deletion
		await this.knex('axis_operations').update({ resolve: null, reject: null }).whereIn('flow', keys);

		const result = await super.deleteMany(keys, opts);

		await getEntitlementManager().clearCache('flows');
		await getFlowManager().reload();

		return result;
	}
}
