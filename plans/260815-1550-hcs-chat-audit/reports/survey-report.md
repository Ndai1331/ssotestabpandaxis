---
type: scout-report
status: complete
created: "2026-08-15"
scope: "HCS_web_free_license Trao đổi/chat audit"
---

# Survey report — HCS Trao đổi

## Tóm tắt

Đã khảo sát source, plan/rules, Docker snapshot và service logs theo đúng phạm vi. Không file source nào bị sửa, không restart/reconfigure service và không thay đổi runtime/data.

Root cause HTTP 500 khi F5 `https://hcs.localhost/chat` đã xác nhận:

```text
System.InvalidOperationException:
The AuthorizationPolicy named: 'Collaboration.Chat' was not found.
```

Lỗi phát sinh ở `hcs-community-blazor-1` trong authorization middleware, trước khi `ChatWorkspace` render và trước khi client gọi gateway/SignalR. Các container Collaboration, gateway, Redis, MinIO, PostgreSQL và RabbitMQ hiện đều có mặt trong runtime snapshot; chưa có evidence SignalR là nguyên nhân của HTTP 500 này.

## Phạm vi và phương pháp

Đã đọc root/service README, `AGENTS.md`, canonical rules, architecture docs và các plan liên quan trước khi khảo sát. Đã dùng source search/file inspection, read-only Docker `ps`/logs, HTTPS GET tới local route và read-only PostgreSQL queries. Đã sử dụng scout findings cho frontend/delete, permission/API/gateway và Docker/runtime/license.

Không chạy build/test hoặc full license script trong lượt này vì request chỉ yêu cầu plan/survey và không đổi runtime.

## 1. Delete/confirm inventory

| Surface | Hiện trạng | Kế hoạch |
|---|---|---|
| Chat Leave | `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor:645-666` gọi `JS.InvokeAsync<bool>("confirm", ...)` | inject `IUiMessageService`, Blazorise confirm modal, busy guard |
| Organization Catalog | `src/HCS.Blazor.Client/Pages/OrganizationCatalog.Mutations.cs:159-197` còn native JS confirm | thay confirm path; giữ JS riêng cho export nếu cần |
| Projects | `src/HCS.Blazor.Client/Pages/Projects.razor` còn JS confirm | thay bằng shared UI message confirm |
| ProjectTasks | `src/HCS.Blazor.Client/Pages/ProjectTasks.razor` còn JS confirm | thay bằng shared UI message confirm |
| CalendarEvents | `src/HCS.Blazor.Client/Pages/CalendarEvents.razor` còn JS confirm | thay bằng shared UI message confirm |
| Administration users | `src/HCS.Blazor.Client/Pages/Administration.razor:101-110,496-510` đã dùng `IUiMessageService.Confirm` | giữ làm reference, kiểm tra busy/error consistency |
| Chat message delete | backend endpoint tồn tại nhưng free client chưa có method/button | ngoài scope trừ khi user yêu cầu |

Blazorise packages hiện được pin ở 2.3.0 trong `HCS.Blazor.csproj` và `HCS.Blazor.Client.csproj`; client module đã đăng ký Blazorise provider. Không cần thêm package cho confirm modal.

## 2. Permission/policy/claim/API trace

### UI và client

- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor:1-5`: routes `/chat`, `/chat/{guid}` và aliases `/chat1`; page policy là `CollaborationPermissions.Chat`.
- `src/HCS.Application.Contracts/Permissions/HCSPermissions.cs:65-71`: exact values là `Collaboration`, `Collaboration.Chat`, `Collaboration.Notifications`, `Collaboration.Administration`.
- `src/HCS.Application.Contracts/Permissions/HCSPermissionDefinitionProvider.cs:49-52`: central permission group hiện đã có Collaboration/Chat; finding cũ trong report trước đây nói thiếu catalog đã stale.
- `src/HCS.Blazor.Client/Authentication/PermissionAuthorizationPolicyProvider.cs:17-37`: các prefix dynamic gồm `Collaboration.`; generated policy yêu cầu authenticated user + claim `permission` với exact policy name.
- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor:368-450`: 401/403 của conversations, contacts và selected conversation map tới `Chat:NoPermission`; lỗi khác map tới `Chat:LoadError` hoặc tương đương localized status.
- `src/HCS.Domain.Shared/Localization/HCS/vi.json:270-271`: exact text là `Tài khoản chưa được cấp quyền trao đổi.` và `Không thể tải dữ liệu trao đổi qua gateway.`.

### Client, gateway và backend

- `src/HCS.Blazor.Client/Collaboration/CollaborationClient.cs:25-40`: contacts dùng `/api/chat/contacts`; conversations dùng `/api/chat/conversations`.
- Cùng client gọi messages, create/send, mark-read, pin, rename, members, leave và upload dưới `/api/chat/...`; attachment URL là `/api/chat/attachments/{id}`.
- `gateways/web/HCS.WebGateway/appsettings.json:41,56-58`: route `/api/chat/contacts/{**catch-all}` order -1 đi Platform; `/api/chat/{**catch-all}` đi Collaboration; `/hubs/chat/{**catch-all}` đi Collaboration.
- `services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs:10-38`: policy `HCSPermissions.Collaboration.Chat`, trả active contacts theo least privilege.
- `services/collaboration/HCS.CollaborationService/Api/ChatController.cs:10-80`: controller route `api/chat`, policy `CollaborationPermissions.Chat`, có conversation/member/message/attachment endpoints; backend DELETE message tồn tại.
- `services/collaboration/HCS.CollaborationService/Hubs/ChatHub.cs:11-31`: hub cũng dùng `CollaborationPermissions.Chat`.
- `services/collaboration/HCS.CollaborationService/HCSCollaborationServiceModule.cs:51-56`: Collaboration service đăng ký policy exact bằng `RequireClaim("permission", CollaborationPermissions.Chat)`; lines 60-65 đăng ký SignalR + Redis; lines 87-99 map `/hubs/chat`.

### Role → permission claim

- `apps/auth-server/HCS.AuthServer/Authentication/PermissionClaimsHandler.cs:20-69`: đọc local user roles, lấy `PermissionManager.GetAllForRoleAsync(role)`, lọc `IsGranted`, phát claim type `permission`; access-token destination được set ở handler lines 76-98.
- AuthServer module đăng ký sign-in handler tại `HCSAuthServerModule.cs:53-57` và handler service tại lines 113-118.
- `gateways/web/HCS.WebGateway/Bff/BffEndpoints.cs:38-67,93-94`: `/bff/user` yêu cầu auth và public claims có `permission`.
- `gateways/web/HCS.WebGateway/Bff/BffAccessTokenTransform.cs:8-21`: BFF access token được chuyển thành Bearer khi proxy request.
- `src/HCS.Blazor.Client/Authentication/BffAuthenticationStateProvider.cs:31-69,99-123`: client đọc `/bff/user` và giữ permission claim.

`BffAccessTokenMiddleware` có thể trả 401/503 khi cookie/token thiếu hoặc refresh thất bại; đây là lý do cần phân biệt status thật thay vì chỉ đọc localized message. Keycloak group `bd-app-hcs` và mapper quyết định role đầu vào; Collaboration membership còn cần smoke để xác minh `sub` được ánh xạ đúng sang `ICurrentUser.Id`, như Platform đã cấu hình tường minh.

Read-only DB evidence: role `admin` tồn tại; hai user hiện được gán role admin; `AbpPermissionGrants` có `Collaboration.Chat` với `ProviderName=R`, `ProviderKey=admin` cùng các Collaboration grants. `HCSRoleDataSeedContributor` chỉ seed mặc định cho provider key `admin`; `bacsi`, `lanhdao`, `nhanvien` phải được cấp riêng. Tuy nhiên không có browser cookie/session authenticated trong lượt survey, nên chưa kết luận session hiện tại đã có claim. Cần fresh sign-in và inspect `/bff/user` hoặc network/access-token evidence sau host-policy fix.

## 3. F5/HTTP 500 runtime evidence

Container snapshot:

- `auth-server`, `blazor`, `web-gateway`, `collaboration`, `platform`, `organization`, `document`, `work-management`, `caddy`: Up.
- `postgres`, `redis`, `rabbitmq`, `minio`: Up/healthy.
- `db-migrator`: Exited (0), phù hợp migration job đã hoàn tất.

Read-only request tới `https://hcs.localhost/chat` trả HTTP/2 500 từ Kestrel, có correlation ID trong response. Blazor log ghi request `GET /chat` kết thúc 500 với exception policy not found. Các log `/Error`, application-configuration và localization gateway call không cho thấy lỗi culture là nguyên nhân.

Route source xác nhận `deploy/docker/Caddyfile:5-16`: `/chat` đi `blazor:8080`; chỉ `/api/*`, `/hubs/*` và BFF đi `web-gateway:8080`. Vì vậy Caddy đang route đúng tới host, và 500 hiện tại không phải gateway route failure.

`ChatRealtimeConnection.cs:24-51` chỉ gọi SignalR sau init component (`ChatWorkspace.razor:332-357`). Do host policy fail trước render, SignalR chưa thể là nguyên nhân trực tiếp. Caddy có một số log 502 lịch sử tới auth-server cho route khác; không trùng nguyên nhân của request `/chat` hiện tại.

### Root cause code boundary

- Client module đăng ký `PermissionAuthorizationPolicyProvider` ở `HCSBlazorClientModule.cs:104-108`.
- Server host `HCSBlazorModule.cs` có `UseAuthorization()` ở lines 295-300 nhưng không đăng ký provider tương đương.
- `Authorize(Policy = "Collaboration.Chat")` trên route metadata vì thế không resolve được trong Blazor host và ném `InvalidOperationException` thay vì trả NotAuthorized.

Implementation gate: reuse/factor provider, đăng ký ở server host, rồi mới chạy authenticated browser smoke. Không bypass bằng cách bỏ `[Authorize]` khỏi page.

## 4. Culture, SignalR và dependency findings

- Supported cultures `en`/`vi` được cấu hình ở `HCSBlazorModule.cs:70-73`; request localization middleware nằm trong pipeline. Không có exception culture trong F5 stack.
- Collaboration module dùng internal AuthServer authority, JWT bearer, query-string token cho `/hubs/chat`, SignalR Redis backplane và MinIO. Source wiring đầy đủ.
- Current containers healthy snapshot không chứng minh negotiate/reconnect đã hoạt động; cần test sau policy fix.
- REST fallback trong `ChatWorkspace` và realtime status/retry đã có; implementation không nên làm page phụ thuộc cứng vào SignalR.

## 5. License audit

- `README.md` của free service pin Blazorise 2.3.0 và ghi production release blocker.
- `docs/dependency-license-decisions.md` nêu Blazorise.Licensing từ nuget.org không phải OSI; production cho tổ chức/government cần valid commercial license hoặc thay bằng OSS.
- `scripts/audit-license-clean.sh` định nghĩa boundary reject Commercial/Pro/private feed/secret content. Full script đã không được coi là pass vì scan generated/vendor bị dừng; chỉ có scoped source/package inspection.
- Không dùng `services/HCS_web_with_license` làm dependency; paid source chỉ là read-only behavior/layout reference.

License là blocker phát hành độc lập, không phải nguyên nhân HTTP 500. Bất kỳ implementation tiếp theo nào cũng phải giữ Blazorise 2.3.0 boundary và không thêm paid package/assets.

## 6. Recommended next implementation sequence

1. Đăng ký dynamic policy provider dùng chung cho Blazor host; thêm focused test cho `Collaboration.Chat` và direct `/chat`.
2. Re-login admin, kiểm tra `/bff/user` có exact permission claim; smoke API contacts/conversations và expected 401/403.
3. Chuẩn hóa confirm modal cho Chat Leave và toàn bộ delete surfaces đã inventory; thêm busy/error handling.
4. Smoke Caddy → BFF → gateway → Platform/Collaboration; kiểm tra SignalR negotiate, reconnect, Redis và REST fallback.
5. Kiểm tra culture qua F5, full build/test và chạy license audit trong môi trường có scope phù hợp; cập nhật acceptance/plan status.

## Unresolved questions

- Delete scope có bao gồm toàn bộ page surfaces ngoài Chat không?
- Có muốn expose message-delete UI trong lượt sau không?
- Chọn public/reuse provider hay factor shared implementation ở đâu để tránh dependency vòng?
- Cần browser session authenticated để xác nhận claim và SignalR thực tế.
- Duplicate nullable-tenant grants có cần data cleanup plan riêng không?
