# Validation report — free workflow signing placeholders

Date: 2026-08-26

## Acceptance status

| Area | Status | Evidence |
|---|---|---|
| Active electronic signature check before workflow start | Pass | `WorkflowSubmissionPreparationService` rejects missing/inactive/not-yet-valid/expired/empty signature image before `StartReview` and workflow persistence. |
| Prepared aliases | Pass | DOCX and detectable-PDF paths cover `PreparedBySign`, `PreparedFullName`, `VitriVieclam`, `ViTriLamViec`, `Position`, `PositionName`, `Department`, `PhongBan`, and `ContentToBeApproved`. |
| Position/department resolution | Pass with runtime configuration | Organization lookup now returns department and position names; DocumentService forwards the bearer token. Compose config points to `http://organization:8080/`. |
| Per-step name/note merge | Pass for supported PDF | `FullNameNN`, `NoteContentNN`, and plain `NoteContent` are merged before provider execution; provider receives the bounded note. |
| DOCX/PDF pair selection | Pass | Source duplication now preserves `PairedFileId`; Word templates are preferred for preparation and a PDF pair is generated when needed. |
| Canonical signed file | Pass | The original file is not changed before provider success; successful output updates the document file and signing blob. Concurrent file changes are rejected. |
| Automated tests/builds | Pass | Results below. |
| End-to-end Docker/provider verification | Not run | Requires running service stack, LibreOffice, blob backend, auth claims and configured provider endpoints. |

## Commands and results

- `dotnet test services/HCS_web_free_license/services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore` — **66/66 passed**.
- `dotnet test services/HCS_web_free_license/services/organization/HCS.OrganizationService.Tests/HCS.OrganizationService.Tests.csproj --no-restore` — **22/22 passed**.
- `dotnet test services/HCS_web_free_license/gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/HCS.WebGateway.Tests.csproj --no-restore` — **116/116 passed**.
- DocumentService Release build — **0 warnings, 0 errors**.
- OrganizationService.Host Release build — **0 warnings, 0 errors**.
- Blazor.Client Release build — **0 warnings, 0 errors**.
- `./scripts/audit-license-clean.sh` from `services/HCS_web_free_license` — **passed**.
- Targeted `git diff --check` for the implementation paths — **passed**.

## Runtime fixes verified after the initial report

- The Docker log for `POST /api/signing/attempts` showed .NET 10 rejecting
  validation metadata placed on the positional-record property. The metadata
  is now attached to the `IdempotencyKey` constructor parameter and the
  regression test plus DocumentService suite pass (**67/67**).
- The Docker log for the approval-management user lookup showed
  `A second operation was started on this context instance` at
  `WorkflowAssigneeCandidatesController.LookupUsersAsync`. The endpoint was
  issuing concurrent `FindAsync` calls against one scoped EF Core context. It
  now reads the requested users sequentially, so the UI can receive full names
  instead of its GUID fallback.
- Platform Release build — **0 warnings, 0 errors**.
- Docker platform image — **built successfully**; the platform container was
  recreated and startup logs show it listening on port 8080. No new lookup
  request was made during this post-restart check, so the browser retry below
  remains the final smoke test.
- A signed approval retry is still required from the browser to provide a
  real provider-backed end-to-end result; the Docker checks above validate the
  fixed services and startup path.

## Runtime assumptions and remaining boundary

- `Services:Organization:BaseUrl` must resolve from DocumentService and the forwarded bearer token must be accepted by OrganizationService.
- DOCX submission preparation requires LibreOffice to be available in the document container.
- PDF replacement is a safe overlay over detectable, contiguous PDF glyphs; it does not rewrite embedded PDF text streams. The original placeholder may remain extractable underneath the whiteout, while the visible document contains the replacement.
- Blob storage and database writes are coordinated with best-effort cleanup. A full distributed transaction cannot be proven by the current unit tests.
- The full repository `git diff --check` still reports pre-existing trailing whitespace in `apps/auth-server/HCS.AuthServer/Pages/Account/Login.cshtml`; it is outside this task's implementation paths and was left unchanged.
