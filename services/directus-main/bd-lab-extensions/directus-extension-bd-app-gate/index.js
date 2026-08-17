/**
 * BD lab: reject Keycloak OpenID login unless user has group bd-app-axis.
 * Mounted into Axis extensions via docker-compose.bd-lab.yml.
 */
export default ({ filter }, { logger }) => {
	const APP_GROUP = 'bd-app-axis';
	const PROVIDER = 'keycloak';

	const extractGroups = (userInfo) => {
		if (!userInfo || typeof userInfo !== 'object') {
			return [];
		}

		if (Array.isArray(userInfo.groups)) {
			return userInfo.groups.map(String);
		}

		// OpenID driver flattens userInfo before auth.* filters
		const fromFlat = Object.entries(userInfo)
			.filter(([key]) => key === 'groups' || key.startsWith('groups.'))
			.map(([, value]) => value)
			.filter((v) => v != null && v !== '')
			.map(String);

		if (fromFlat.length > 0) {
			return fromFlat;
		}

		return [];
	};

	const assertAppAccess = (meta) => {
		if (meta?.provider !== PROVIDER) {
			return;
		}

		const userInfo = meta?.providerPayload?.userInfo ?? {};
		const groups = extractGroups(userInfo);

		if (!groups.some((g) => g.toLowerCase() === APP_GROUP)) {
			logger?.warn?.(`[BD] Keycloak login denied: missing group ${APP_GROUP}`);
			throw new Error(`Access denied: Keycloak group '${APP_GROUP}' is required for Axis.`);
		}
	};

	filter('auth.create', (payload, meta) => {
		assertAppAccess(meta);
		return payload;
	});

	filter('auth.update', (payload, meta) => {
		assertAppAccess(meta);
		return payload;
	});
};
