---
title: "ABP Elsa Pro WorkflowService (BD lab)"
description: "Add hanhchinhso.WorkflowService (Elsa Pro host) + standalone Elsa Studio WASM, wired into the ABP microservice solution."
status: completed
priority: P2
effort: 6-9h
branch: main
tags: [abp, elsa, workflow, microservice, keycloak-sso, lab]
relatedPlans:
  - 260724-1555-hcs-layered-to-microservice
blockedBy: []
blocks: []
created: 2026-07-24
---

# Plan — ABP Elsa Pro WorkflowService + standalone Elsa Studio (BD lab)

> **Cross-plan note:** Orthogonal với HCS domain migration (`260724-1555-hcs-layered-to-microservice`). Plan đó dùng `services/document/` cho Documents+HCS-Workflow+Signing. Plan này giữ `services/workflow-service/` cho **Elsa Pro** (`:44395`). Không gộp hai khái niệm "workflow".

## Goal
Thêm một microservice mới `hanhchinhso.WorkflowService` host module **ABP Elsa Pro** vào solution `services/abp-blazor`, và một app **Elsa Studio (Blazor WASM)** standalone để thiết kế/chạy workflow. Blazor chính chỉ thêm 1 menu link mở Studio ở tab mới (Option A đã chốt). Local lab, không GitHub/CI/deploy.

## Locked decisions (context)
- **UI = Option A**: Elsa Studio WASM standalone + menu link (KHÔNG nhúng vào ABP Blazor — Studio không embed được).
- Target folder rỗng: `services/abp-blazor/services/workflow-service/`.
- Naming: `hanhchinhso.WorkflowService` (+ `.Contracts` + `.Tests`), theo pattern LanguageService (flat `service-nolayer`, KHÔNG layered DDD).
- Stack sẵn có: ABP **10.5.0** / net10.0 / PostgreSQL / YARP WebGateway / Redis / RabbitMQ.
- Endpoints hiện có: AuthServer OpenIddict `:44372`, Blazor `:44306`, WebGateway `:44398`.
- Ports mới (đã verify chưa dùng trong repo): **WorkflowService `:44395`**, **Elsa Studio `:44396`**.
- **License**: `Volo.Abp.Elsa.*` yêu cầu **ABP Team+**. Không có license → build/restore các package Elsa sẽ fail. Đây là điều kiện tiên quyết (xem Risk).

## Architecture / data flow
```
Browser ──(menu link, _blank)──▶ Elsa Studio WASM :44396
                                     │  OIDC Code+PKCE (public client "ElsaStudio")
                                     ▼
                              AuthServer :44372 (OpenIddict)  ◀── Keycloak (upstream IdP, đã cấu hình phase1)
                                     │ access_token (scope WorkflowService)
                                     ▼
Studio ──REST /elsa/api──▶ WorkflowService :44395 (Elsa Pro host, JwtBearer)
                                     │ EF Core (Npgsql)
                                     ▼
                        PostgreSQL  hanhchinhso_Workflow  (Elsa tự tạo bảng + ABP infra)
```
- Studio gọi thẳng WorkflowService `:44395` (đơn giản cho lab). Có thể thêm route YARP `:44398/elsa/*` nếu muốn đi qua gateway — plan để optional trong phase-04.
- WorkflowService xác thực JWT do AuthServer cấp; audience = `WorkflowService` (scope mới).
- Permission `Elsa.*` quản lý qua ABP Permission Management (đã có ở AdministrationService), seed cho `admin`.

## Component ↔ file ownership (no overlap giữa phase)
| Vùng | Đường dẫn | Phase sở hữu |
|------|-----------|--------------|
| WorkflowService host + Contracts + Tests | `services/abp-blazor/services/workflow-service/**` | 01, 02, 07 |
| AuthServer OpenIddict seed/config (client + scope) | `services/identity/.../Data/OpenIddictDataSeeder.cs`, `identity appsettings.json`, `auth-server appsettings*.json` | 03, 05 |
| Gateway routes | `gateways/web/hanhchinhso.WebGateway/appsettings.json` | 04 |
| Solution wiring | `hanhchinhso.abpsln`, `etc/abp-studio/run-profiles/Default.abprun.json` | 04 |
| Elsa Studio app | `services/abp-blazor/apps/elsa-studio/**` (mới) | 05 |
| Blazor menu | `apps/blazor/hanhchinhso.Blazor.Client/Navigation/hanhchinhsoMenuContributor.cs` | 06 |

## Phases & Progress
| # | File | One-line | Status |
|---|------|----------|--------|
| 01 | `phase-01-scaffold-workflow-service.md` | Clone LanguageService → `hanhchinhso.WorkflowService` (Host + Contracts + Tests), port :44395, DB `hanhchinhso_Workflow`, chưa có Elsa. | ✅ Done |
| 02 | `phase-02-elsa-host-config.md` | Cài `Volo.Abp.Elsa.*` 10.5.0 + `ConfigureElsa` (Identity, WorkflowManagement/Runtime EF Postgres, Scheduling, JS/Liquid/CSharp, Http, WorkflowsApi); auto schema. | ✅ Done |
| 03 | `phase-03-db-authserver-scope.md` | Connection string + audience; thêm API scope/resource `WorkflowService` vào OpenIddict seeder + RootUrl; SwaggerTestUI + Blazor client thêm scope. | ✅ Done |
| 04 | `phase-04-gateway-abpsln-runprofile.md` | (Optional) YARP route `/elsa/*`; đăng ký module vào `hanhchinhso.abpsln` + `Default.abprun.json` (order). | ✅ Done |
| 05 | `phase-05-elsa-studio-wasm.md` | Tạo app Elsa Studio WASM standalone `:44396` trỏ về WorkflowService; đăng ký OpenIddict public client `ElsaStudio` (Code+PKCE) + CORS/redirect. | ✅ Done |
| 06 | `phase-06-blazor-menu-link.md` | Thêm menu item "Workflow (Elsa Studio)" mở `http://localhost:44396` ở `_blank`, gated permission. | ✅ Done |
| 07 | `phase-07-permission-seed.md` | Seed permission `Elsa.*` cho role `admin` (idempotent), giống pattern role-permission-seed. | ✅ Done |
| 08 | `phase-08-smoke-verify.md` | Build + run order, migrate, đăng nhập, mở Studio, tạo & chạy 1 workflow HTTP mẫu tối thiểu, verify token + DB. | ✅ Done |

## Test matrix
| Level | Cái gì | Ở đâu |
|-------|--------|-------|
| Build | `dotnet build` toàn solution sau mỗi phase code | CLI |
| Unit/Integration | `hanhchinhso.WorkflowService.Tests` chạy (giữ template test tối thiểu) | Tests project |
| Schema | Elsa tạo bảng trong `hanhchinhso_Workflow` khi khởi động | psql |
| Auth | access_token có `aud=WorkflowService`; Studio login qua AuthServer OK | browser/jwt.io |
| E2E | Tạo workflow HTTP trigger trong Studio → publish → gọi endpoint → 200 | Studio + curl |

## Risks
| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Không có ABP Team+ license → restore `Volo.Abp.Elsa.*` fail | Medium | **High** | Verify license/`abp login` TRƯỚC phase-02 (blocker gate). Nếu thiếu → dừng, báo user. |
| Elsa Studio version không khớp ABP/Elsa host | Medium | High | Dùng template Studio đúng version theo docs `abp.io/docs/latest/modules/elsa-pro`; pin cùng dòng release. |
| CORS/OIDC redirect sai giữa Studio :44396 và AuthServer :44372 | Medium | Medium | Thêm `:44396` vào CorsOrigins + RedirectAllowedUrls; redirect chuẩn `signin-oidc`/`signout-callback-oidc`. |
| Elsa auto-migration đụng UnitOfWork/khởi động ABP | Low | Medium | Bật Elsa `AutoRunMigrations`; DB Elsa tách khỏi ABP infra migrator; chạy AuthServer/Identity seed trước. |
| Port trùng khi có service khác thêm sau | Low | Low | Đã verify 44395/44396 free; ghi rõ trong run profile. |

## Rollback
- Mỗi phase độc lập file-wise. Revert = xóa thư mục `services/workflow-service/` + `apps/elsa-studio/` và hoàn tác các block đã thêm trong: `OpenIddictDataSeeder.cs`, `identity/auth-server appsettings`, `WebGateway/appsettings.json`, `hanhchinhso.abpsln`, `Default.abprun.json`, `hanhchinhsoMenuContributor.cs`.
- DB: drop database `hanhchinhso_Workflow` (Elsa tables) — không ảnh hưởng service khác.
- Không có destructive migration trên DB dùng chung.

## YAGNI guardrails
- KHÔNG viết custom activity, KHÔNG demo workflow Ordering/Payment. Tối đa 1 workflow HTTP mẫu tạo bằng tay trong Studio ở phase smoke (không commit code).
- KHÔNG layered DDD; giữ flat như LanguageService.
- KHÔNG thêm gateway route nếu Studio gọi thẳng `:44395` chạy ổn (route để optional).

## Definition of done
- [x] Solution build sạch với WorkflowService + Elsa packages.
- [x] WorkflowService `:44395` chạy, tạo bảng Elsa trong `hanhchinhso_Workflow`, `/elsa/api` phản hồi (401 khi thiếu token, 200 với token).
- [x] Elsa Studio `:44396` login qua AuthServer, list/tạo workflow.
- [x] Menu Blazor có link mở Studio ở tab mới.
- [x] `admin` có permission `Elsa.*`.
- [x] 1 workflow HTTP mẫu chạy end-to-end (thủ công).

## Unresolved questions
1. Đã có **ABP Team+ license** active trên máy (`abp login` / có quyền `Volo.Abp.Elsa.*`) chưa? — blocker phase-02.
2. Studio auth: dùng **OpenIddict Code+PKCE qua AuthServer** (plan mặc định) hay `UseAbpIdentity` password flow? Plan chọn Code+PKCE cho đồng bộ SSO; xác nhận.
3. Có cần route Studio→WorkflowService **qua gateway `:44398/elsa`** không, hay gọi thẳng `:44395` là đủ cho lab? (phase-04 để optional).
4. Chốt version chính xác của **Elsa Studio** template khớp ABP 10.5.0 (cần xem docs Elsa Pro tại thời điểm cài).
