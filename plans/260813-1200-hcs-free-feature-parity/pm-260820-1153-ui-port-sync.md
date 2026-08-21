# PM Sync — HCS UI Port (with_license → free license) | 2026-08-20 11:53

**Status:** In Progress  
**Blocker:** None (on track for Phase 2 handoff to 260814-1000-hcs-blazorise-localization)  
**Scope:** UI slice completion; Phase 3 business modules (Survey, Projects, Documents, Workflow, Catalog audit)

---

## Completed Work

### UI/UX Increment Delivery (All 7 todos) — 2026-08-20 14:45

1. ✅ **PDF full-frame iframe** — `HcsPdfFrame` blob iframe, không Blazorise.PdfViewer
2. ✅ **Workflow wizard 3+4 merge + RoleInSubmitterOu resolver** — Resolve role trên DocumentService qua Platform HTTP; Submit modal preset/dropdown
3. ✅ **Chat leave/transfer admin + conversation type icons** — Sole admin chọn `transferAdminTo`; icon User/Group/Project/Task
4. ✅ **Notification copy + topbar badges** — Title ≠ body; badge chuông + unread chat
5. ✅ **Signing tabs above grid** — All / Sent to me / Sent by me, một hàng trên DataGrid, `--hcs-primary`
6. ✅ **Login → /workspace, workspace buttons --hcs-primary** — Index redirect + AuthServer ReturnUrl
7. ✅ **Project roles + DateTime + calendar icon** — Manager/Supervisor/Member; `HcsDatePicker` DateTime + addon lịch

### Earlier Work (from prior sync)

### 1. Survey Module
- ✅ Split `SurveyLocations` + `SurveyCriterias` entity handling
- ✅ NavLink added (no dual-route navigation collision)
- ✅ Removed `NavigateSurvey` / `SurveyCatalogs` dual-route ambiguity

### 2. Projects Module
- ✅ GET by-project endpoint (404 if missing or not member)
- ✅ Create Type=Project business rule
- ✅ Navigate to `/chat/{id}` on project creation
- ✅ `WorkSubjectNotProvisioned` toast added

### 3. Documents Module
- ✅ Filter bar + pager + type sidebar UI
- ✅ `GetList` filters on existing aggregate fields only (no spurious lookups)
- ✅ Detail page save/cancel CSS classes
- ✅ Signing pager implementation

### 4. Workflow Module
- ✅ `ReplaceSteps` unique index constraint fix (RemoveRange + SaveChanges, then AddRange in transaction)
- ✅ List page Kind/Active/status columns in advanced filter + DataGrid pager
- ✅ `SaveSteps` toast if definition Id is null

### 5. Catalog Audit
- ✅ `SurveySessions` pager + filter chrome
- ✅ `SurveyResults` filter UI
- ✅ `CalendarEvents` list search

### 6. Test Coverage (re-run 2026-08-20)
- ✅ DocumentService.Tests: 45 passed
- ✅ CollaborationService.Tests: 30 passed
- ✅ WorkManagementService.Tests: 39 passed
- ✅ Build clean: HCS.Blazor.Client, HCS.AuthServer, HCS.Blazor, Collaboration/Document/Work hosts
- ⚠️ Hard-refresh browser (Ctrl+Shift+R) khi test UI local

---

## Plan Update

**File updated:** `/plans/260813-1200-hcs-free-feature-parity/plan.md`

- Updated frontmatter notes with 2026-08-20 completion summary
- **Did NOT mark Phase 3 modules as DONE** (per instruction: UI slice only, not full Work/Calendar/Survey modules)
- Explicitly documented: "Không đánh DONE cả module Work/Calendar/Survey; chỉ đánh UI items chính xác"

---

## Risks & Blockers

| Risk | Status | Mitigation |
|------|--------|-----------|
| Phase 2 handoff alignment (260814-1000) | 🟢 Clear | No scope creep; UI slice standalone; Blazorise localization can start fresh |
| Docker URLs migration (http → https) | 🟢 Resolved | All services rebuilt; hard-refresh required on client |
| Test regressions (44+28+39+109 tests) | 🟢 Monitored | CI pipeline must validate full suite before next phase |

---

## Next Actions

1. **User validation** (blocking Phase 2 start):
   - Hard-refresh browser → verify https://hcs.localhost loads
   - Test authenticated flows (login → menu → catalog CRUD)
   - Confirm no 401/403 on exposed routes

2. **Phase 2 prerequisites** (260814-1000 / Blazorise localization):
   - Confirm plan.md acceptance criteria met
   - Verify test suite passes in CI
   - No scope changes to Work/Calendar/Survey modules unless explicitly planned

3. **Catalog audit completion** (Phase 2):
   - Full parity audit for Danh mục (Organization, Signature Settings, Reports) + Survey/Calendar
   - Permission seed review (admin recovery role)

---

## Unresolved Questions

- Are the Docker URLs (https://hcs.localhost) resolving without certificate warnings in your test browser?
- Have you validated the full login → menu → protected API flow post-hard-refresh?
