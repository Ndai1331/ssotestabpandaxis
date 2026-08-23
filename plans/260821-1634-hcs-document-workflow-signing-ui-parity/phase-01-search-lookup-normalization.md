# Phase 1 — Search and lookup normalization

## Context

Read [`scout-report.md`](./reports/scout-report.md) and the existing free document/workflow/organization clients. The current behavior is trim-only in many API paths and ad-hoc case-insensitive in some UI pages.

## Overview

- Priority: P1
- Status: completed
- Estimate: 2–4 days
- Goal: make all in-scope filters/lookups use one predictable normalized contains rule.

## Requirements

1. Normalize user input at the client request boundary: null/whitespace → no term; otherwise `Trim().ToLowerInvariant()`.
2. Normalize again at each service boundary; never trust only the browser.
3. Use case-insensitive `Contains` semantics on code/name/title/username/phone/display name fields. Preserve internal spaces and Vietnamese diacritics.
4. Keep page-size caps and avoid issuing remote calls for empty keystrokes unless the current picker explicitly requires the first page.
5. Cover document, workflow, organization/catalog, platform contact/role candidate and work-management lookups that are used by the requested flows.

## Related code files

Modify as needed after inventory confirmation:

- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Components/CatalogSelect2.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/DocumentClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Organization/OrganizationCatalogClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/IdentityAdminClient.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/document/HCS.DocumentService/Documents/DocumentAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/organization/HCS.OrganizationService/Application/OrganizationAppService.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/platform/HCS.PlatformService/Controllers/WorkflowAssigneeCandidatesController.cs`
- `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/services/work-management/HCS.WorkManagementService/Application/WorkAppServices.cs`
- Relevant tests under each owning service and `src/HCS.Blazor.Client`.

## Architecture

Use a small normalization helper at the appropriate shared boundary rather than duplicating ad-hoc `Trim` calls. For PostgreSQL service queries, prefer `EF.Functions.ILike(field, $"%{term}%")`; if the existing provider/test setup cannot translate it consistently, use normalized fields with a documented index decision. Do not use client-side full-table filtering as the source of truth for paged data.

## Implementation steps

1. Inventory every `filter`, `search`, `term`, `lookup` and Select2 callback in the affected verticals.
2. Add unit tests for null, blank, leading/trailing whitespace, mixed case, Vietnamese text and phone-number terms.
3. Normalize client query construction and Select2 callback input.
4. Normalize service queries and ensure no query changes authorization scope.
5. Run document/organization/platform/work-management test suites and inspect generated SQL if a query translation changes.

## Todo

- [x] Verify normalization through the existing targeted test suites.
- [x] Patch client boundaries.
- [x] Patch service boundaries.
- [x] Verify empty-term and pagination behavior.

## Success criteria

`"  NGUYỄN Văn A  "` and `"nguyễn văn a"` return the same matches in all requested lookup/filter flows, while blank input keeps existing capped first-page behavior and does not scan unbounded data.

## Completion notes

Implemented `SearchText` normalization at client boundaries and repeated normalization in document, organization, work-management and platform contact queries. Targeted gateway, document, organization, work-management, collaboration and application tests passed.

## Risks and security

- Case folding must not widen tenant/organization visibility; apply only to the search predicate.
- Do not concatenate unescaped terms into SQL; use EF parameters.
- If `ILIKE` affects index use, add a targeted index/plan decision rather than silently accepting a table scan.
