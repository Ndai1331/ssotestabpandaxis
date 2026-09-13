# Phase 1 — Expose OU management API

## Context Links

- [Scout report](./reports/scout-report.md)
- [Research summary](./reports/research-summary.md)
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.EntityFrameworkCore/EntityFrameworkCore/HCSDbContext.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.HttpApi/Controllers/Localization/LanguagesController.cs`

## Overview

Create the Community-compatible application contract/service and Platform-discovered controller for OU tree and direct member management.

## Requirements

- List all tenant-scoped OUs with `Id`, `ParentId`, `Code`, `DisplayName`, and `ConcurrencyStamp`.
- List direct members with paging/filter and stable user display fields.
- List available users and add/remove a user from the selected OU.
- Create root/child, update display name, move, delete, move all members to another OU.
- Guard self/descendant moves and invalid source/target combinations.
- Apply department permission to every endpoint; use typed DTOs and bounded page sizes.

## Related Code Files

Create:

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Application.Contracts/OrganizationUnits/OrganizationUnitManagementDtos.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Application.Contracts/OrganizationUnits/IOrganizationUnitManagementAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Application/OrganizationUnits/OrganizationUnitManagementAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.HttpApi/Controllers/Identity/OrganizationUnitManagementController.cs`

Modify:

- Domain shared localization JSON only for new error keys if runtime messages need them.

## Implementation Steps

1. Define DTO/input contracts with validation and paged member result.
2. Implement list/member mapping via Identity repositories; avoid the custom organization DB.
3. Implement mutations through `OrganizationUnitManager` and `IdentityUserManager`.
4. Add explicit controller routes under `/api/identity/organization-units` and permission attributes.
5. Verify controller discovery and Gateway route assumptions with reflection/route contract tests.

## Success Criteria

- API compiles and exposes the six required operations.
- No new database migration or custom organization entity is introduced.
- Invalid hierarchy moves are rejected before manager mutation.

## Todo List

- [x] Define OU/member DTOs and application contract.
- [x] Implement tree, member, available-member, and mutation operations.
- [x] Add protected Platform controller under the existing Gateway identity route.
- [x] Build Application, HttpApi, and Platform projects.

## Risk Assessment / Security

- Permission checks must remain server-side; UI checks are only affordance control.
- Do not expose password, claims, role memberships, or audit fields in member DTOs.
- Bound list/page sizes to prevent an unbounded identity query.
