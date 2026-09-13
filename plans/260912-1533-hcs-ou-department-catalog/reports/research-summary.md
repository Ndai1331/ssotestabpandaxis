# Research and design summary

## API

The service wraps `IOrganizationUnitRepository`, `OrganizationUnitManager`, `IdentityUserManager`, and `IIdentityUserRepository`. It returns a complete tree source ordered by `Code` and a paged direct-member list. CRUD methods use the manager so code generation, duplicate sibling validation, recursive deletion, and membership cache invalidation stay in ABP domain logic.

## Authorization

All API actions require `HCSOrganizationPermissions.Departments`. The page and action buttons also check the existing CRUD child permissions, so read-only department users can browse while only granted users mutate.

## UI

Use a dedicated two-column responsive page: left OU tree with a per-node Blazorise dropdown; right selected-unit member panel with tabs/title/search/table. Use existing HCS/Blazorise styles and localization resource. Keep recursive tree as a small component so node action state and page member state are separate.

## Risks and mitigations

- OU membership is separate from legacy custom mapping: document this explicitly and do not delete legacy tables.
- Large OU trees: cap API list size, build the tree once, and preserve expanded/selected IDs across refresh.
- Concurrent edits: send and honor `ConcurrencyStamp` where available; refresh after mutations.
- Invalid hierarchy move: reject self and descendants before calling the ABP manager.
