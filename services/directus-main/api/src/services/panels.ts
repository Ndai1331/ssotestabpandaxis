import type { AbstractServiceOptions } from '@directus/types';
import { ItemsService } from './items.js';

export class PanelsService extends ItemsService {
	constructor(options: AbstractServiceOptions) {
		super('axis_panels', options);
	}
}
