---
status: in-progress
blocks: [260814-1000-hcs-blazorise-localization]
notes: "2026-08-19 Workflow slice: Kind CRUD, wizard 4 bước, assignment VIEW/blocking, Start signer/scope, SubmitWorkflowModal, TaskTree/modals, NotificationToast poll. Không đánh DONE cả module Work/Calendar/Survey."
---

# HCS Free feature parity — menu, auth, catalogs and business modules

## Evidence

- The paid reference at `https://hanhchinhso.benhvien199.vn/` accepts the supplied administrator account and exposes these top-level groups: Workspace, Văn bản, Quy trình, Dự án & công việc, Lịch & Sự kiện, Khảo sát, Danh mục, Quản trị, SaaS.
- The paid source implements the pages against its own commercial ABP packages plus `HC.HttpApi` application services. The free solution uses different projects (`HCS.Blazor.Client`, BFF gateway, separate Platform/Organization/Document/Work services) and therefore cannot safely copy the paid Razor pages as-is.
- The free client already has working routes for Organization, the eight document master-data types, chat and the custom administration page. The current top navigation does not expose the nested structure.
- The free login page is rendered by `apps/auth-server/HCS.AuthServer` (ABP MVC Account/Login), not `src/HCS.Blazor.Client/Pages/Login.razor`; styling the latter does not change the actual login screen.

## Phase 1 — foundation and access

1. Diagnose each observed 401 with an authenticated browser session and service logs; fix issuer/audience/permission propagation at the owning service, never by bypassing authorization.
2. Rebuild the HCS top navigation as accessible dropdown groups. Keep direct links only for routes that have an implemented, authorized backend.
3. Add an AuthServer login-page theme matching the supplied reference: brand mark, restrained patterned background, accessible labelled fields, password visibility and responsive layout.
4. Verify fresh login → BFF profile → top menu → protected API calls, including admin permission claims.

## Phase 2 — catalog parity

1. Stabilize existing Organization and eight document catalog CRUD screens with common list/create/edit/delete UX, confirmation and proper empty/loading/error states.
2. Port paid-source *contracts and business rules*, not commercial UI/package dependencies, for remaining catalogs: survey locations, survey criteria, signature settings and reports.
3. Create the matching free-service API/domain/data migrations and gateway routes before exposing each menu item.
4. Apply granular HCS permissions and seed the admin recovery role with every enabled permission.

## Phase 3 — core business parity

1. Documents and signing: document list/detail/assignment workflow, signing policy and audit trail.
   - **Slice done (2026-08-19):** SourceType query reload (3 menu tabs); LibreOffice Word→PDF on DocumentService (`INSTALL_LIBREOFFICE` chỉ image `document`); wizard bước 2 Word+PDF 2 cột; bảng bước/phân công SLA/pills; trình ký tab màu + count + overdue + CSV; `SubmitWorkflowModal` attach; `WorkflowInfoModal`; preview Giao việc. Không copy LeptonX/PdfViewer/RichTextEdit.
2. Workflow: definitions and instances.
   - **Slice done (2026-08-18):** `/workflow-detail` wizard (Type/Assignee, Word/PDF upload, PDF iframe); list New/Edit đi detail.
   - **Slice done (2026-08-19):** Kind vs List tách entity; wizard 4 bước (Kind, template, step+SLA, assignment VIEW/blocking); Start nhận signer/scope; VIEW skip; Return/Resubmit; SubmitWorkflowModal + signing filter; NotificationToast poll (`WORKFLOW` → `/document-signing`).
   - Still open: SLA worker escalation, watermark PDF thương mại.
3. Work: projects and tasks.
   - **Slice done (2026-08-18):** Workspace DatePicker range; CatalogSelect2 (Code/Name); create project on `/project-detail` then forceLoad members+tasks; auto `CalendarEvent` on project/task CRUD + related links.
   - **Slice done (2026-08-19):** TaskTree đệ quy `ParentTaskId`; ProjectTask View/Create modals (General/Assignments/Documents) trên Projects, Tasks, ProjectDetail, Calendar (TASK).
   - Still open: remaining survey vertical and menu/permission exposure (Phase 2).
4. Calendar/events and surveys.

Each module is independently migrated only after its API, data migration, permissions and browser CRUD tests exist. Commercial ABP Pro/SaaS-only pages remain intentionally excluded unless a free replacement is implemented.

## Acceptance criteria for the current increment

- Admin sees an accessible top dropdown menu with only implemented routes enabled.
- Admin can open all exposed catalog CRUD pages without HTTP 401/403.
- Login styling is served by AuthServer and works at desktop and mobile widths.
- No tokens, passwords or external account settings are committed.
