---
type: meta
title: "Hot Cache"
updated: 2026-08-20T15:30:00
---

# Hot Cache — BD Second Brain

## Last Updated
2026-08-21 — **HCS Free Phase 3 error handling + SLA/assignee:** WorkflowAppService throws `BusinessException("Work:DefinitionHasRunningInstances")` when definition has running instances (not generic `InvalidOperationException`). BffErrorMapper localizes `error.code` from response. WorkflowDetail: SLA defaults 1 day, NumericPicker Min=1 Decimals=0, RoleInSubmitterOu radio remounts Select2 (hides user, shows role). Rebuild `document` + `blazor`; hard refresh `/workflow-detail`.
2026-08-20 — **HCS Free PDF + document-detail + signing UI:** Blazorise.PdfViewer 2.3.0 (`HcsPdfFrame`, không iframe; không gắn `pdfviewer.js` classic). Modal bước: `UserSelect2` / `CatalogSelect2` role. Upload DOCX: MIME theo đuôi (đuôi thắng declared MIME), convert fail không rollback. Document-detail 1 Card 6/6. Nút action văn bản border=icon. Signing filter 3 hàng LICENSE + modal ký ExtraLarge+PDF. Tests DocumentService 52. Rebuild `blazor` + `document`; hard refresh `/document-detail`, `/workflow-detail`, `/manage-documents`, `/document-signing`.
2026-08-20 — **HCS Free UI/UX increment COMPLETE:** (1) PDF iframe `HcsPdfFrame` (2) Wizard 3 bước + RoleInSubmitterOu via Platform HTTP (3) Chat leave/`transferAdminTo` + icon User/Group/Project/Task (4) Toast title≠body + badge chuông/chat (5) Signing tabs All/ToMe/ByMe trên DataGrid (6) Login→`/workspace` + `--hcs-primary` (7) Project Manager/Supervisor/Member + `HcsDatePicker`. Tests: Collaboration 30, Document 45, Work 39. Hard refresh `/workspace`.
2026-08-19 — **HCS Free document/signing UI parity DONE:** SourceType reload; LibreOffice convert on DocumentService only (`INSTALL_LIBREOFFICE=true` trên image `document`); wizard 2-col Word+PDF; step tables; signing tabs/colors/Excel CSV; WorkflowInfoModal; Giao việc from PDF preview. DocumentService Tests 43/43. Rebuild document + blazor; hard refresh.
2026-08-19 — **HCS Tab copy Mã→Tên:** Blazorise `TextInput` + `Immediate=true` copy giá trị khi Tab. Fix: cặp form (catalog Mã/Tên, task, user, lịch…) dùng `HcsIsolatedTextInput` (input HTML native, `oninput` riêng). Search filter vẫn Blazorise. Cấm `@bind-Value:event="onchange"` trên Blazorise. Rebuild `blazor`, hard refresh `/document-types`.
2026-08-19 — **DocumentService không lưu loại quy trình:** log `POST /api/workflows/kinds` 500, payload `code:"" name:""`. Root cause: `@bind-Value:event="onchange"` trên Blazorise `TextInput` không gọi `ValueChanged` → Code/Name không bind. Fix: bỏ `:event="onchange"`, giữ `Immediate=false`. Rebuild container `blazor`, hard refresh.
2026-08-19 — **HCS Free UI/logic văn bản–quy trình–lịch:** Phase 1 Tab-fill (`onchange` + `Immediate=false`); Phase 2 lịch toggle + nền trắng `.hcs-calendar-grid`; Phase 3 document full-page + `GetFileContentAsync`; Phase 4 `SourceType`/`ParentDocumentId`/send/revoke/duplicate trình ký; Phase 5 modal Kind/List + wizard step/assignee + SignMode. Migration `20260819080000` + `20260819090000_AddWorkflowSignMode`. Tests DocumentService 40/40.
2026-08-19 — **HCS DocumentService crash `PendingModelChangesWarning`:** migration handwritten `20260819120000` thiếu snapshot. Container `hcs-community-document-1` restart loop lúc `MigrateAsync`. Fix: regenerate `DocumentServiceDbContextModelSnapshot.cs` + test `HasPendingModelChanges`. Rebuild `--build document` (không suppress warning).
2026-08-19 — **HCS admin UI `/administration`:** ExtraLarge size đặt trên `<Modal>` (Blazorise 2.3), `--bs-modal-width` override; tab user không unmount nên lưu được vai trò/phòng ban; `BlazoriseOptions.Immediate=false` chống Tab copy input; header title+button flex space-between.
2026-07-25 — **HCS→MS Phase 3e2 DONE:** mobile APPROVE/RETURN/REJECT/ELECTRONIC/DIGITAL + eligible signatures; lineage audit migration; DocumentService tests 83/83. Next: 3e3 submit/resubmit + native upload. Plan: `plans/260724-1555-hcs-layered-to-microservice/`.
2026-07-25 — **Directus lab SoT = v11 DONE:** cook `plans/260725-1726-directus-v11-sso-lab/`. Compose + `bd-app-gate` + ROLE_MAPPING; smoke OIDC + gate deny OK. Archive: `services/directus-main/ARCHIVE.md`. Runbook → `directus-main-v11`.
2026-07-25 — **Directus lab → v11 plan ready:** `plans/260725-1726-directus-v11-sso-lab/` (4 phases, ~5h). Brainstorm: `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`. Cook: `/ck:cook --auto` plan.md. Chưa implement.
2026-07-24 — **ABP prod deploy runbook:** `docs/runbooks/deploy-abp-production.md` — Ubuntu 24+ Docker+Nginx hoặc K8s/Helm; templates Compose tại `services/abp-blazor/etc/docker-prod/`.
2026-07-24 — **ABP AppHost CLI runner:** Aspire 13.4.6 (`./aspire/run.sh light|full`) replaces manual ABP Studio for local dev — see [`services/abp-blazor/aspire/README.md`](../services/abp-blazor/aspire/README.md).
2026-07-24 — **Elsa WorkflowService DONE:** Plan `plans/260724-1542-elsa-workflow-service/` completed. WorkflowService `:44395` (Elsa Pro 3.5 + Contracts + Tests), Elsa Studio WASM `:44396`, menu link in Blazor, Keycloak auth via AuthServer OpenIddict, permission seed `Elsa.*`. All 8 phases done; smoke verify checklist complete.
2026-07-24 — **Plan HCS→MS full roadmap:** `plans/260724-1555-hcs-layered-to-microservice/` (8 phases, ~20–28w). DocumentService `:44380` ≠ Elsa WorkflowService `:44395`. Cook từng phase; Phase 3 slices 3a–3h. Active plan set.
2026-07-24 — **HCS→MS brainstorm APPROVED:** Approach C fat-core rồi peel. Report: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`. Source `HCS_web` (layered/Blazorise) → target `abp-blazor` (MS/Mud). Phase 0–7; document+WF+sign = 1 fat service trước. Shared DB tenants; tiến KC; mobile/REMOTE_CA parity Phase 2. Chưa `/ck:plan`.
2026-07-23 — **Axis rebrand (source):** table/collection prefix `directus_`→`axis_`; UI Directus→Axis (en-US). Plan: `plans/260723-1617-axis-rebrand-directus/`. **Caveat:** `docker-compose.bd-lab.yml` vẫn image upstream → runtime DB còn `directus_*` đến khi build fork.
2026-07-23 — **App access gate:** groups `bd-app-axis` / `bd-app-hcs`; Directus hook + ABP OnTokenValidated fail. Plan: `plans/260723-1555-bd-app-access-gate/`. Re-run bootstrap + restart Directus/AuthServer.
2026-07-23 — **Phase 1 SSO DONE.** Handoff AI: `docs/handoff/phase1-sso-context.md`. Runbook: `docs/runbooks/local-sso-lab.md`. Xem [[SSO Phase 1 Complete]].
2026-07-23 — Permission seed ABP roles bacsi/lanhdao/nhanvien; Directus+ABP `prompt=login` (lab logout UX).
2026-07-23 — Directus lab compose: KC hostname `localhost:5110` + backchannel dynamic; ROLE_MAPPING UUIDs filled.
2026-07-23 — Reset workspace Task9 → BD SSO Lab.

## Code Structure Cheatsheet
- **Directus lab SoT:** `services/directus-main-v11` + `docker-compose.bd-lab.yml` (PG+Redis+KC+Axis)
- **Directus v12 archive:** `services/directus-main/ARCHIVE.md` — không chạy lab
- **HCS Free runtime:** `services/HCS_web_free_license/` (Blazor+BFF+AuthServer+DocumentService+CollaborationService+WorkManagementService+PlatformService)
- **ABP** `services/abp-blazor` — AuthServer `:44372` federate KC; Blazor `:44306`
- **Bootstrap** `scripts/keycloak_bootstrap_bd_realm.py` (re-run after KC recreate)
- **Handoff** `docs/handoff/phase1-sso-context.md` ← dán vào prompt chat mới
- **Plan SSO v11:** `plans/260725-1726-directus-v11-sso-lab/`

## Key Recent Facts
- Approach A; realm `bd`; role groups `bd-admin|bacsi|lanhdao|nhanvien`; app groups `bd-app-axis|bd-app-hcs`
- Lab users mặc định cả 2 app; bỏ 1 app group trên KC Admin để test single-app
- KC users `*@benhvien.vn` / `Passw0rd!`; Directus local `admin@local.dev`/`admin123456`; ABP `admin@abp.io`/`Abc@123`
- Browser chỉ `localhost:5110` (không host.docker.internal)
- `prompt=login` bật → dễ đổi user; silent SSO 2-app cần tắt prompt hoặc test có chủ đích

## Active Threads / Open Plans
- **HCS Free Phase 3 UI:** `plans/260813-1200-hcs-free-feature-parity/` — **UI slice DONE (2026-08-20)**, Phase 2 handoff next (`260814-1000` Blazorise localization)
- **HCS→MS:** `plans/260724-1555-hcs-layered-to-microservice/` — cook Phase 01 foundation trước
- **Elsa WorkflowService:** `plans/260724-1542-elsa-workflow-service/` — orthogonal; `:44395`
- **SSO Phase 2:** Zimbra LDAP User Federation
- Optional: SLO, bỏ/ tune `prompt=login`, full permission matrix

## Critical Rules
- Đọc `docs/handoff/phase1-sso-context.md` + `CLAUDE.md` + `docs/workspace-architecture.md`
- Không follow Task9 rules
- Commit chỉ khi user yêu cầu
