import { ForbiddenError } from '@directus/errors';
import type { AbstractServiceOptions } from '@directus/types';
import {
	AccessService,
	ActivityService,
	CommentsService,
	DashboardsService,
	DeploymentProjectsService,
	DeploymentRunsService,
	DeploymentService,
	FilesService,
	FlowsService,
	FoldersService,
	ItemsService,
	NotificationsService,
	OperationsService,
	PanelsService,
	PermissionsService,
	PoliciesService,
	PresetsService,
	RevisionsService,
	RolesService,
	SettingsService,
	SharesService,
	TranslationsService,
	UsersService,
	VersionsService,
} from '../services/index.js';

/**
 * Select the correct service for the given collection. This allows the individual services to run
 * their custom checks (f.e. it allows `UsersService` to prevent updating TFA secret from outside).
 */
export function getService(collection: string, opts: AbstractServiceOptions): ItemsService {
	switch (collection) {
		case 'axis_access':
			return new AccessService(opts);
		case 'axis_activity':
			return new ActivityService(opts);
		case 'axis_comments':
			return new CommentsService(opts);
		case 'axis_dashboards':
			return new DashboardsService(opts);
		case 'axis_files':
			return new FilesService(opts);
		case 'axis_flows':
			return new FlowsService(opts);
		case 'axis_folders':
			return new FoldersService(opts);
		case 'axis_notifications':
			return new NotificationsService(opts);
		case 'axis_operations':
			return new OperationsService(opts);
		case 'axis_panels':
			return new PanelsService(opts);
		case 'axis_permissions':
			return new PermissionsService(opts);
		case 'axis_presets':
			return new PresetsService(opts);
		case 'axis_policies':
			return new PoliciesService(opts);
		case 'axis_revisions':
			return new RevisionsService(opts);
		case 'axis_roles':
			return new RolesService(opts);
		case 'axis_settings':
			return new SettingsService(opts);
		case 'axis_shares':
			return new SharesService(opts);
		case 'axis_translations':
			return new TranslationsService(opts);
		case 'axis_users':
			return new UsersService(opts);
		case 'axis_versions':
			return new VersionsService(opts);
		case 'axis_deployments':
			return new DeploymentService(opts);
		case 'axis_deployment_projects':
			return new DeploymentProjectsService(opts);
		case 'axis_deployment_runs':
			return new DeploymentRunsService(opts);
		default:
			// Deny usage of other system collections via ItemsService
			if (collection.startsWith('axis_')) throw new ForbiddenError();

			return new ItemsService(collection, opts);
	}
}
