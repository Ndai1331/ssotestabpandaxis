---
title: "HCS Trao đổi — permission, F5 runtime và destructive-confirmation audit"
description: "Khảo sát và lập kế hoạch xử lý confirm modal, permission/API mapping và lỗi HTTP 500 khi deep-link /chat trong free-license."
status: in-progress
priority: P1
effort: 1-2d
branch: main
tags: [audit, plan, auth, collaboration, gateway, signalr, blazorise, license]
blockedBy: []
blocks: [260814-1721-hcs-header-chat]
relatedPlans: [260813-1200-hcs-free-feature-parity, 260803-hcs-community-microservice, 260812-0926-hcs-admin-navigation-catalogs, 260814-1000-hcs-blazorise-localization]
created: "2026-08-15"
createdBy: "Codex"
notes: "2026-08-18: Platform AddPolicy Collaboration.Chat + ChatWorkspace map 401/403/502. Chưa đóng hết audit F5/destructive confirm."
---

# HCS Trao đổi — permission, F5 runtime và destructive-confirmation audit

## Quyết định khảo sát

Đây là plan khảo sát riêng cho `services/HCS_web_free_license`; lượt này không implement code, không build/test, không restart/reconfigure service và không đổi dữ liệu runtime.

Lỗi F5 `/chat` đã được xác nhận bằng log: Blazor host không tìm thấy policy `Collaboration.Chat`, nên trả HTTP 500 tại authorization middleware trước khi `ChatWorkspace` khởi động, gọi gateway hoặc mở SignalR. Database hiện có grant `Collaboration.Chat` cho provider role `admin`, nhưng chưa có authenticated browser evidence chứng minh session BFF hiện tại đã mang claim này.

## Phạm vi

1. Chuẩn hóa toàn bộ delete/leave action đang có trong free UI thành confirm modal qua `IUiMessageService`/Blazorise; không dùng `window.confirm`, JS popup hoặc JS runtime cho quyết định destructive.
2. Trace từ UI policy và localized error đến BFF, gateway route, typed client, backend controller/service policy, AuthServer role-to-permission claim và free-license permission catalog.
3. Xử lý root cause deep-link/F5 `HTTP 500` và xác minh riêng các dependency culture, Caddy/gateway, Collaboration, Redis/SignalR, MinIO và database.
4. Kiểm tra license boundary: chỉ dùng Blazorise 2.3.0 và mã free; `HCS_web_with_license` chỉ là behavioral/UI reference read-only.

### Ngoài phạm vi lượt này

- Không sửa source, package, compose, Caddy, database grant hoặc runtime.
- Không thêm message-delete UI mới: backend có endpoint DELETE message nhưng free client hiện chưa có method/button; chỉ đưa vào scope nếu user xác nhận.
- Không dọn duplicate permission rows trong database; đây là data-quality follow-up riêng.
- Không copy Commercial/Pro module, asset, DTO hoặc package từ `HCS_web_with_license`.

## Evidence và chẩn đoán hiện tại

### F5 `/chat` — root cause đã xác nhận

- `ChatWorkspace.razor:1-5` khai báo `/chat`, các alias `/chat1` và `[Authorize(Policy = CollaborationPermissions.Chat)]`.
- `HCSBlazorModule.cs:295-300` bật `UseAuthorization()`, nhưng server host không đăng ký `PermissionAuthorizationPolicyProvider`.
- `HCSBlazorClientModule.cs:104-108` chỉ replace provider trong client/WASM service collection.
- `PermissionAuthorizationPolicyProvider.cs:26-37` có thể tạo dynamic policy cho prefix `Collaboration.` với claim `permission`, nhưng class/provider chưa được reuse ở server host.
- Docker log của `hcs-community-blazor-1` ghi rõ: `System.InvalidOperationException: The AuthorizationPolicy named: 'Collaboration.Chat' was not found.`; request `/chat` kết thúc 500.

Kết luận: plan implementation cần đăng ký cùng dynamic policy provider ở Blazor host (hoặc factor một implementation dùng chung trong project hợp lệ), sau đó mới đánh giá lỗi API/SignalR từ authenticated browser. Culture và proxy không phải nguyên nhân trực tiếp; SignalR chưa được gọi vì component chưa qua được route authorization.

### Permission, claim và API map

| Boundary | Evidence hiện tại | Contract cần giữ |
|---|---|---|
| UI route/menu | `ChatWorkspace.razor`; `HCSMenuContributor.cs` | `Collaboration.Chat` |
| Client policy | `PermissionAuthorizationPolicyProvider.cs` | authenticated + claim `permission=Collaboration.Chat` |
| Localized status | `ChatWorkspace.razor`; `vi.json` | 401/403 → `Chat:NoPermission`; lỗi khác → `Chat:LoadError` |
| Contacts API | `CollaborationClient` → `/api/chat/contacts` | gateway order ưu tiên Platform `ChatContactsController` |
| Chat API | client → `/api/chat/{**catch-all}` | WebGateway → Collaboration `ChatController` |
| Realtime | client → `/hubs/chat` | Caddy/WebGateway → `ChatHub`, policy cùng tên |
| Backend authorization | `ChatController`, `ChatHub`, `HCSCollaborationServiceModule` | require claim `permission=Collaboration.Chat` |
| Claim emission | AuthServer `PermissionClaimsHandler` | local role grants → access-token permission claims → BFF transform |
| Free catalog | `HCSPermissions` và `HCSPermissionDefinitionProvider` | Chat/Notifications/Administration đã có trong source hiện tại |

DB read-only evidence cho thấy role `admin` và hai user hiện có grant `Collaboration.Chat` (cùng Notifications/Administration). `HCSRoleDataSeedContributor` chỉ seed mặc định cho provider key `admin`; các role `bacsi`, `lanhdao`, `nhanvien` phải được cấp riêng. Có duplicate rows do nullable `TenantId` và unique index hiện tại; cần ghi nhận nhưng chưa coi là root cause. Cần sign-out/sign-in hoặc kiểm tra authenticated `/bff/user` để chứng minh claim thực tế trong session.

### Delete behavior hiện tại

- `ChatWorkspace.razor`: nút Leave dùng `JS.InvokeAsync<bool>("confirm", ...)`; cần đổi sang `IUiMessageService.Confirm` và busy guard.
- `OrganizationCatalog.Mutations.cs`: còn native JS confirm; page dùng Blazorise danger button. Export vẫn có thể cần JS, nên chỉ thay confirm path.
- `Projects.razor`, `ProjectTasks.razor`, `CalendarEvents.razor`: còn JS confirm.
- `Administration.razor`: delete user đã dùng `IUiMessageService.Confirm`; giữ làm reference và kiểm tra busy/error behavior.
- Chưa có delete message button trong free ChatWorkspace; không tự mở rộng scope.

## File dự kiến khi implement

### UI confirm/modal

- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor`
- `src/HCS.Blazor.Client/Pages/Administration.razor` — chỉ nếu cần đồng nhất busy/error behavior
- file page/code-behind của Organization Catalog, gồm `OrganizationCatalog.Mutations.cs` và page markup
- `src/HCS.Blazor.Client/Pages/Projects.razor`
- `src/HCS.Blazor.Client/Pages/ProjectTasks.razor`
- `src/HCS.Blazor.Client/Pages/CalendarEvents.razor`
- shared `IUiMessageService`/Blazorise provider chỉ khi khảo sát implementation cho thấy thiếu capability; hiện đã có usage hoạt động.

### Host policy và authorization

- `src/HCS.Blazor.Client/Authentication/PermissionAuthorizationPolicyProvider.cs` — public/reuse hoặc chuyển implementation vào boundary shared hợp lệ.
- `src/HCS.Blazor/HCSBlazorModule.cs` — đăng ký provider cho server host.
- focused authorization test project/file nếu test harness hiện hữu cho phép; không thêm package chỉ để test.

### Permission/API chỉ chỉnh khi verification chứng minh mismatch

- `src/HCS.Application.Contracts/Permissions/HCSPermissions.cs`
- `src/HCS.Application.Contracts/Permissions/HCSPermissionDefinitionProvider.cs`
- `apps/auth-server/HCS.AuthServer/Authentication/PermissionClaimsHandler.cs`
- `apps/auth-server/HCS.AuthServer/HCSAuthServerModule.cs`
- `gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs`
- `gateways/web/HCS.WebGateway/Bff/BffAccessTokenTransform.cs`
- `gateways/web/HCS.WebGateway/appsettings.json`
- `services/collaboration/HCS.CollaborationService/Api/ChatController.cs`
- `services/collaboration/HCS.CollaborationService/Hubs/ChatHub.cs`
- `services/collaboration/HCS.CollaborationService/HCSCollaborationServiceModule.cs`
- `services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs`
- `src/HCS.Blazor.Client/Collaboration/CollaborationClient.cs` và `ChatRealtimeConnection.cs`

## Dependency/API gap và cách xử lý

1. **Host dynamic policy gap — confirmed.** Đăng ký provider ở host là source fix tối thiểu; cần test direct `/chat` và một route policy khác để tránh policy bypass.
2. **BFF session claim — evidence gap.** Grant trong database không đảm bảo cookie/access token đang dùng đã refresh. Sau khi host 500 được sửa, fresh login và `/bff/user`/network trace phải xác nhận exact claim.
3. **Gateway mapping — source đã khớp, runtime phải smoke.** `/api/chat/contacts` đi Platform trước; các route `/api/chat/*` đi Collaboration; `/hubs/chat/*` đi Collaboration. Không đổi route prefix khi chưa có failing request evidence. Paid source có route/permission khác, nên không dùng làm contract.
4. **Role/claim propagation — partial gap.** Seed mặc định chỉ đảm bảo `admin`; cần kiểm tra role thực tế, Keycloak group `bd-app-hcs`, local ABP role grant và fresh access-token/BFF claim. Không suy ra mọi tài khoản quản trị từ một DB grant.
5. **Identity/dependency gap — runtime dependent.** Cần xác minh Collaboration có ánh xạ `sub` sang `ICurrentUser.Id` như Platform và membership checks hoạt động; CORS/config drift cũng chỉ kết luận sau request thực tế.
6. **Status message ambiguity.** `NoPermission` hiện gộp 401/403; `LoadError` gộp 404/5xx/network/serialization. Implementation nên log correlation/status/route, và chỉ tách message nếu UX requirement yêu cầu.
7. **SignalR/dependency health — not current root cause.** Containers Collaboration, Redis, MinIO, RabbitMQ, PostgreSQL đang Up/healthy trong snapshot; cần authenticated negotiate/reconnect smoke sau policy fix.
8. **License.** Blazorise 2.3.0 là production blocker đã được ghi trong `docs/dependency-license-decisions.md`; full `audit-license-clean.sh` chưa được coi là pass vì scan generated/vendor bị dừng. Giữ acceptance audit riêng, không dùng paid source.

## Phân chia implement ngay và phụ thuộc backend/runtime

### Có thể implement ngay sau khi user cho phép code

- Host policy provider registration/reuse và focused test.
- Thay toàn bộ native confirm đã định danh bằng Blazorise `IUiMessageService.Confirm`.
- Busy/error guard cho destructive UI và regression check không còn `confirm(` trong product source.
- Bổ sung structured logging/status evidence quanh Collaboration client nếu cần, không đổi API contract.

### Phụ thuộc backend, session hoặc runtime

- Chứng minh BFF session có `permission=Collaboration.Chat` sau fresh login.
- Xác minh role/grant propagation qua AuthServer access token.
- Authorized 200 và unauthorized 401/403 cho contacts/conversations/messages.
- SignalR negotiate, hub authorize, reconnect và REST fallback.
- Caddy/WebGateway routing thực tế, culture cookie khi hard refresh, và dependency health.
- Full build/test/package và license audit sạch trong môi trường được phép.

## Acceptance criteria cho lượt implementation/validation

- F5 trực tiếp `https://hcs.localhost/chat` không còn HTTP 500 và không còn log `AuthorizationPolicy ... was not found`.
- Admin fresh session thấy exact `permission=Collaboration.Chat` tại `/bff/user`; gateway/backend trả 200 cho route hợp lệ.
- User thiếu claim nhận 401/403 và UI hiển thị đúng `Tài khoản chưa được cấp quyền trao đổi.`; lỗi gateway thật vẫn có correlation/status evidence và hiển thị thông báo tương ứng.
- Contacts, conversations, thread, attachment và `/hubs/chat` dùng đúng route map; SignalR failure không làm mất REST fallback.
- Mọi delete/leave button user-facing mở Blazorise confirm modal; không còn native JS popup/confirm. Export JS không bị phá.
- Blazorise vẫn ở 2.3.0; không có Commercial/Pro package, asset hoặc paid source dependency.
- Build/test/audit được chạy trong lượt implementation; lượt này không chạy vì user yêu cầu chỉ plan.

## Unresolved questions / blockers

- “Các nút delete” có bao gồm cả Organization Catalog, Projects, ProjectTasks và CalendarEvents ngoài Chat Leave không? Khuyến nghị giữ toàn bộ các surface đã định danh để không còn native confirm trong free UI.
- Có yêu cầu thêm nút xóa message trong ChatWorkspace không? Hiện backend có endpoint nhưng client/UI chưa expose.
- Provider nên được public/reuse trực tiếp hay factor sang project shared hiện có? Cần chọn phương án không tạo dependency vòng và vẫn để client/server dùng cùng policy semantics.
- Cần authenticated browser session để chụp `/bff/user`, API response và SignalR negotiate sau khi host policy được sửa.
- Duplicate `AbpPermissionGrants` có cần một DB/data cleanup plan riêng không? Chưa xử lý trong scope này.
- License production blocker của Blazorise và full audit script vẫn là release blocker dù source scan hiện tại không thấy Commercial/Pro dependency.

## Handoff

Khi được phép implement, dùng plan này làm gate: `/ck:cook --auto /Users/nguyenlong/Documents/Projects/bd-workspace/plans/260815-1550-hcs-chat-audit/plan.md`. Lượt hiện tại chỉ hoàn tất survey/plan.
