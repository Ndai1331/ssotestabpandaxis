---
title: "Hiển thị avatar user trong /chat"
description: "Dùng ảnh đại diện của user trong các avatar trên trang chat và giữ initials khi avatar thiếu hoặc tải lỗi."
status: in-progress
progress: "CSS isolation/RenderTreeBuilder root cause fixed with ::deep; client build and 38/38 tests pass; browser /chat E2E pending"
priority: P2
effort: 3-5h
branch: main
tags: [feature, frontend, chat, avatar, accessibility, testing]
blockedBy: []
blocks: []
created: 2026-08-24
---

# Hiển thị avatar user trong `/chat`

## Overview

API avatar đã tồn tại; `/chat` hiện chỉ render initials. Bổ sung một luồng render avatar dùng URL same-origin, không đổi schema/MinIO/upload, không tải ảnh bằng C# trước khi render. Avatar không tồn tại, URL rỗng hoặc ảnh lỗi phải quay về initials ổn định.

## Progress

- Implementation complete in `ChatWorkspace.razor`, `ChatWorkspace.razor.css`, and `ChatContactsController.cs`.
- Shared `UserAvatar`/`ConversationAvatar` path handles contact URLs, direct-user fallback URLs, initials fallback, image `onerror`, and presence badges.
- Contact projection uses one batched `UserAvatars` lookup and emits nullable relative URLs; no per-contact blob fetch.
- Scope deviation: `ChatContactsController.cs` changed (not verify-only) to avoid emitting avatar URLs for users without an avatar.
- Confirmed root cause: CSS isolation selectors did not reach avatar elements emitted through `RenderTreeBuilder`, so the image styling was not applied at runtime.
- Fix: added the scoped CSS `::deep` selector required for the `RenderTreeBuilder`-generated avatar elements.
- Remaining blocker: manual authenticated `/chat` E2E; owner: workspace operator; unblock: run avatar/no-avatar/404/broken-image/accessibility matrix with local services available.

## Khảo sát codebase và data flow

- `services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs`: `GET /api/chat/contacts` đã trả `ChatContactDto.AvatarUrl = /api/identity/users/{id}/avatar`.
- `services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs`: `ChatContactDto` đã có `AvatarUrl` nullable; `ConversationDto`/`ConversationMemberDto` chỉ có member IDs.
- `services/platform/HCS.PlatformService/Controllers/ProfileAvatarController.cs` + `Identity/UserAvatarAppService.cs`: endpoint đọc avatar, trả `404` khi chưa có ảnh; yêu cầu authenticated user.
- `gateways/web/HCS.WebGateway/appsettings.json`: `/api/identity/{**catch-all}` đã proxy Platform và giữ BFF cookie.
- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor`: contact picker và add-member picker render initials trực tiếp; conversation list/thread/info, message sender và forward list đi qua `ConversationAvatar`/`UserAvatar`, nhưng các helper hiện không nhận/render URL.
- `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor.css`: avatar đã có kích thước circle; cần bổ sung object-fit/overflow cho `img`, không đổi layout.

Data flow sau thay đổi:

`ChatContactsController` → `ChatContactDto.AvatarUrl` (contact pickers); `ConversationMemberDto.UserId`/`ChatMessageDto.SenderUserId`/direct conversation target → resolver URL `/api/identity/users/{id}/avatar` → browser request qua BFF → Platform/MinIO → `<img>`; `404`, empty URL hoặc `error` → initials `<span>`.

## Phạm vi file/component/model/test

| File | Action | Mục đích |
|---|---|---|
| `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor` | Modify | Tập trung render avatar vào helper dùng URL nullable; áp dụng cho contacts, add-members, direct conversation, message sender, member info, thread/list/forward; giữ icon cho group/project/task. |
| `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor.css` | Modify | Style `img` cùng kích thước/circle với initials, `object-fit: cover`, clipping và không làm lệch presence badge. |
| `services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs` | Verify only | Không đổi contract nếu JSON compatibility giữ nguyên `AvatarUrl`; chỉ sửa khi test chứng minh consumer thiếu field. |
| `services/platform/HCS.PlatformService/Controllers/ChatContactsController.cs` | Verify only | Giữ URL nullable/relative hiện có; không query blob/database cho từng contact. |
| `gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/GatewayConfigurationTests.cs` | Modify test | Khẳng định route `/api/identity/.../avatar` vẫn về Platform nếu route contract test phù hợp. |
| `test/HCS.Application.Tests/PlatformRouteContractTests.cs` hoặc test project phù hợp | Modify/add test | Kiểm tra route/response contract của contacts và avatar endpoint theo harness hiện có; không tạo test project UI mới nếu không cần. |
| `src/HCS.Blazor.Client/Pages/ChatWorkspace.razor` (testability seam) | Add focused test only if existing harness supports | Unit-test resolver/fallback/initials; nếu không có Blazor component test harness, ghi nhận manual E2E là kiểm chứng bắt buộc, không kéo thêm framework. |

## TODO tasks

- [x] Xác định một API nội bộ duy nhất cho `UserAvatar(userId, displayName, avatarUrl, ...)` và `ConversationAvatar`; không copy logic fallback tại từng call site (DRY).
- [x] Resolver ưu tiên `ChatContactDto.AvatarUrl` khi có; với member/sender/direct user dùng URL endpoint theo `Guid`; reject/null qua `string.IsNullOrWhiteSpace`.
- [x] Render `<img>` với `alt="" aria-hidden="true"` vì tên user đã hiển thị cạnh avatar; initials/icon vẫn là decorative. Không để `alt` trùng tên gây đọc lặp.
- [x] Trên `onerror`, chuyển đúng avatar instance về initials; tránh retry loop và không làm mất presence badge. Fallback state keyed theo avatar URL.
- [x] Bổ sung CSS image rule: circle, `object-fit: cover`, `display`, kích thước kế thừa theo modifier small/large; không đổi layout.
- [x] Không thêm endpoint, migration, blob fetch client-side, package UI hoặc thay đổi quyền; dùng relative same-origin URL qua BFF.
- [ ] Viết test contract cho `AvatarUrl` nullable/relative và gateway route; existing gateway suite passed, nhưng chưa có avatar-specific assertions/initials edge-case tests.
- [ ] Nếu có component test harness hiện hữu, test matrix: URL hợp lệ, null/empty, `404`, lỗi mạng/ảnh hỏng, direct/group conversation, message/member/contact picker.
- [ ] Manual E2E tại `/chat`: user có avatar thấy ảnh ở contact/direct/message/member surfaces; user không có avatar thấy initials; xóa avatar hoặc trả 404 không có broken-image icon. Pending local authenticated run.
- [ ] Accessibility check: screen reader không đọc ảnh hai lần, status/presence vẫn có accessible label, avatar không thay thế text name, keyboard flow unchanged.
- [ ] Sau khi triển khai, hard-refresh/reload `/chat`; rollback bằng revert hai file UI/CSS và không cần migration/data rollback.

## Dependencies and risks

- **Dependencies:** existing authenticated BFF session, Platform route, MinIO avatar object, `ChatContactDto.AvatarUrl`; không phụ thuộc plan chat khác (`260815-1550-hcs-chat-audit`, `260814-1721-hcs-header-chat`) vì không thay đổi API/permission.
- **Medium likelihood / medium impact:** `<img>` request bị `401/403/404`, CORS hoặc cookie không đi kèm → dùng relative same-origin URL, giữ initials fallback, kiểm tra gateway route trước E2E.
- **Medium likelihood / low impact:** URL ảnh hợp lệ nhưng content hỏng/unsupported → `onerror` fallback, không retry vô hạn.
- **Low likelihood / medium impact:** render fragment dùng state chung khiến một ảnh lỗi làm fallback tất cả avatar → state theo `userId + url` hoặc DOM instance, test nhiều user cùng lúc.
- **Low likelihood / low impact:** ảnh lớn gây layout shift/băng thông → kích thước cố định CSS, browser cache; không thêm client-side blob caching trong MVP.

## Test and rollback gates

## Verification results

- Root cause confirmed: CSS isolation did not target avatar elements emitted by `RenderTreeBuilder`.
- Fix verified: scoped CSS `::deep` selector reaches the generated avatar elements and applies the image styling.
- Client build: passed.
- Automated tests: `38/38` passed.
- Browser E2E: pending authenticated `/chat` run; avatar/no-avatar/404/broken-image/accessibility matrix remains the release gate.
- Risk state: build and automated test coverage green; browser rendering, 401/403/404 fallback, mobile layout, and accessibility remain unverified until E2E.

- **Unit/contract:** DTO serialization preserves nullable `AvatarUrl`; controller emits relative avatar URL; gateway maps identity/avatar; initials edge cases.
- **Component/integration:** all avatar call sites select image vs initials and preserve icons/presence; failed image does not throw or loop.
- **E2E/manual:** authenticated `/chat` with avatar, without avatar, deleted avatar, broken response; desktop/mobile; keyboard + screen reader inspection.
- **Success criteria:** zero initials where a valid avatar loads; zero broken-image icon when avatar is absent/fails; all existing group/project/task icons and presence labels unchanged; no new API/schema/migration; relevant tests pass.
- **Rollback:** revert `ChatWorkspace.razor` and `.razor.css`; retain existing API/contract and data. No destructive rollback operation required.

## Unresolved questions

- Có yêu cầu hiển thị avatar của chính user trong mọi nơi của `/chat` không, hay chỉ các user khác? Plan mặc định hỗ trợ mọi user ID; `isMine` hiện không render sender avatar.
- Có sẵn Blazor component-test harness trong nhánh triển khai không? Nếu không, không mở rộng dependency; dùng contract tests + manual E2E.
