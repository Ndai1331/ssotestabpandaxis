---
phase: 1
title: Foundation & conventions
status: completed
effort: 1-2w
dependsOn: []
---

# Phase 01 — Foundation & conventions

## Goal

Khóa playbook migrate trước khi port domain. Sau phase: có checklist + sample entity end-to-end qua Gateway + Mud page stub.

## Context

- Target pattern: `services/language/` (Contracts + Host + Tests)
- Brainstorm: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`
- Research: `./research/researcher-01-abp-ms-scaffold.md`
- **Không** đụng `workflow-service/` (Elsa)

## Requirements

1. ADR ngắn: shared DB tenants; DB-per-MS; Document ≠ Elsa
2. Feature inventory → sheet/markdown checklist parity (owner service)
3. Bảng map Blazorise → MudBlazor (components hay dùng)
4. Permission naming: `HanhChinhSo.{Module}.*` (hoặc `Organization.*` / `Document.*` — chốt 1 convention)
5. Infra lab: MinIO (+ optional LibreOffice compose stub) trong `etc/docker` hoặc document path HCS reuse
6. Template “new service wiring checklist” (abpsln, gateway, OpenIddict scope, run profile, DB)
7. Spike: 1 entity mẫu (vd. `MasterData` tối giản hoặc `Book`-style) trong Organization **hoặc** temporary Demo — prefer bắt đầu Organization folder trống + DemoController đã có pattern

## Architecture

```
docs/decisions/  hoặc  wiki/decisions/
plans/.../reports/parity-checklist.md
services/abp-blazor/etc/docker/containers/minio.yml  (nếu chưa)
```

## Implementation steps

1. Xuất/copy feature inventory từ `HCS_web/scripts/feature-inventory/` → `plans/260724-1555-.../reports/parity-checklist.md` (cột: id, name, owner service, status)
2. Viết `docs/decisions/` hoặc wiki ADR: Approach C + ports table + Document vs Elsa
3. Viết `reports/blazorise-mud-map.md` (DataGrid→MudTable, Modal→MudDialog, …)
4. Thêm MinIO docker (nếu thiếu) + note connection pattern ABP BlobStoring.Minio
5. Document OpenIddict: cách thêm API resource/scope (copy Language/Identity seeder pattern) — file path cụ thể khi cook
6. Smoke: Identity + Admin + AuthServer + Gateway + Blazor vẫn chạy sau mọi doc-only change

## Files (expected)

| Path | Action |
|------|--------|
| `plans/260724-1555-hcs-layered-to-microservice/reports/parity-checklist.md` | create |
| `plans/.../reports/blazorise-mud-map.md` | create |
| `wiki/decisions/` hoặc `docs/decisions/` ADR | create |
| `services/abp-blazor/etc/docker/...` MinIO | create/modify if needed |
| `docs/runbooks/` migrate-module.md | create short |

## Success criteria

- [x] Parity checklist tồn tại với owner Phase 2–8
- [x] Mud map + wiring checklist usable bởi cook Phase 2
- [x] ADR DocumentService ≠ WorkflowService/Elsa
- [x] MinIO reachable từ lab

## Verification — 2026-07-24

- OrganizationService health: `Healthy`; PostgreSQL partial unique indexes xác nhận cho host/tenant và `IsDeleted = false`.
- MinIO: container pin theo digest, health endpoint trả thành công.
- Organization integration tests: 2 passed (CRUD + tenant isolation).
- Gateway unauthenticated gate: MasterData trả `401`.
- Authenticated authorization-code/PKCE flow:
  - token scopes: `OrganizationService AdministrationService IdentityService`;
  - Administration remote permission integration trả `true` cho `MasterData` + `Create`;
  - `POST :44398/api/organization-management/master-data` trả `200`;
  - `GET :44398/api/organization-management/master-data` trả `200` và đọc lại đúng record `P1-20260724`.
- MudBlazor page/proxy wiring build thành công ở cả server/client render modes. Browser tích hợp của phiên kiểm thử chặn `localhost`, nên UI click-through sẽ được lặp lại khi Phase 2 hoàn thiện toàn bộ màn hình Organization.

## Risks

- Inventory M01–M67 thiếu file Excel → build từ menu contributor HCS
- Over-doc → giữ ≤4 artifact files

## Cook

```text
/ck:cook --auto .../phase-01-foundation-conventions.md
```
