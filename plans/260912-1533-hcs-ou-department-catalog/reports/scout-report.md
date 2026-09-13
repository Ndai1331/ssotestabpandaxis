# Scout report

## Current implementation

- `src/HCS.Blazor.Client/Pages/Organization/Departments.razor` renders the generic custom `OrganizationCatalog` for `/departments`.
- `OrganizationCatalogClient` calls `/api/organization/departments`; that service owns a separate `hcs_organization` database.
- `src/HCS.EntityFrameworkCore/EntityFrameworkCore/HCSDbContext.cs` already exposes ABP Identity `OrganizationUnits`, `Users`, and the initial migration creates OU membership tables.
- The Platform host includes the application, EF Core, and HttpApi modules and is already reached by Gateway route `/api/identity/{**catch-all}`.
- Commercial reference implements a flat OU-to-tree mapping and selection pattern, but its Pro-only OU app service contracts are unavailable in Community 10.6.

## Relevant constraints

- The Community package provides the domain/repository/manager primitives but no OU application service contract.
- `OrganizationUnitManager.MoveAsync` does not protect against moving an OU under its own descendant; the HCS service must validate this.
- Existing custom department IDs are consumed by units, documents, work management, and user mappings. A full replacement would require a separate migration plan.
- Worktree has unrelated dirty edits, including organization catalog and localization files. New work must avoid reverting or broad-reformatting them.

## Chosen approach

Add a small HCS OU management application service and explicit HttpApi controller under `/api/identity/organization-units`. Add a dedicated Blazor client/page/component instead of mutating the generic custom catalog implementation. Reuse `HCS.Organization.Departments` permission and current BFF client.
