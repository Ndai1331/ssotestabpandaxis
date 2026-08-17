# Phase 08 — Smoke verify (end-to-end)

**Goal:** Xác nhận toàn luồng chạy: build → migrate → login → Studio → tạo & chạy 1 workflow HTTP mẫu → token/DB đúng. Không commit code demo (workflow tạo thủ công trong Studio).

**Depends on:** tất cả phase trước.

## Pre-req (infra containers)
- [ ] `docker compose` các container: PostgreSQL(5432), Redis(6379), RabbitMQ(5672/15672) đang chạy (từ `etc/docker/containers` hoặc Directus compose cho Keycloak nếu cần SSO upstream).
- [ ] Keycloak `:5110` chạy (nếu login đi qua Keycloak upstream, theo phase1 SSO).

## Run order (khớp Default.abprun.json)
1. [ ] AuthServer `:44372`
2. [ ] IdentityService `:44392` (chạy OpenIddict seeder — tạo scope `WorkflowService` + client `ElsaStudio`)
3. [ ] AdministrationService `:44323` (permission seed — phase-07)
4. [ ] WorkflowService `:44395` (Elsa host — tạo schema)
5. [ ] WebGateway `:44398`
6. [ ] Blazor `:44306`
7. [ ] Elsa Studio `:44396`

## Checks
- [ ] **Build**: `dotnet build hanhchinhso.WorkflowService.slnx` + build Studio project OK.
- [ ] **Schema**: psql `\dt` trên `hanhchinhso_Workflow` → có bảng Elsa (workflow definitions/instances/bookmarks...) + `__WorkflowService_Migrations` cho ABP infra.
- [ ] **API auth**:
  - `curl http://localhost:44395/elsa/api/workflow-definitions` (no token) → **401**.
  - Lấy token qua Swagger `SwaggerTestUI` (scope WorkflowService) → decode `aud` chứa `WorkflowService`.
  - `curl -H "Authorization: Bearer <token>" .../elsa/api/workflow-definitions` → **200**.
- [ ] **Studio login**: `:44396` → redirect AuthServer → login admin → callback OK → dashboard hiển thị (không CORS/redirect error).
- [ ] **Menu**: Blazor `:44306` admin → menu "Workflow (Elsa Studio)" mở tab mới `:44396`.
- [ ] **Permission**: admin không bị 403 trong Studio (phase-07).
- [ ] **E2E workflow (thủ công, tối thiểu)**:
  - Trong Studio tạo workflow: `HTTP Endpoint` (path `/test`, method GET) → `HTTP Response` (body "ok", 200). Publish.
  - `curl http://localhost:44395/test` (hoặc path Elsa http prefix theo cấu hình) → trả "ok".
  - Kiểm workflow instance xuất hiện trong Studio (đã executed).

## Sign-off (Definition of done — plan.md)
- [ ] Build sạch, service + Studio chạy, schema tạo, auth đúng, menu OK, permission OK, workflow mẫu chạy.

## Notes / reload rule (CLAUDE.md)
- Đổi contract OIDC (client/scope/redirect) → restart **AuthServer + IdentityService + Studio**.
- Đổi config Elsa/host → restart WorkflowService.
- Test UI → hard refresh (Ctrl+Shift+R).
