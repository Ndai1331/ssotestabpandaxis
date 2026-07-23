import type { AbstractServiceOptions } from '@directus/types';
import { ItemsService } from './items.js';

export class PresetsService extends ItemsService {
	constructor(options: AbstractServiceOptions) {
		super('axis_presets', options);
	}
}
