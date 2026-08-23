---
title: "HCS free license: đổi logo và xác minh favicon"
description: "Xác định toàn bộ điểm branding của HCS Community free-license, thay logo an toàn và kiểm tra favicon/PWA icon sau khi có binary đầu vào."
status: blocked
priority: P2
effort: 2-4h
branch: main
tags: [hcs, free-license, branding, logo, favicon, blazor, abp]
blockedBy: []
blocks: []
created: 2026-08-23
---

# Kế hoạch triển khai

## Phạm vi và kết luận khảo sát

Service đúng là `services/HCS_web_free_license` — HCS Community / ABP Community free-license, .NET 10; `services/abp-blazor` chỉ là template lịch sử. Lần này chỉ lập kế hoạch, chưa sửa code hoặc asset.

Các điểm đã xác định:

| Mục | Vị trí hiện tại | Vai trò | Dự kiến xử lý |
|---|---|---|---|
| Logo dùng chung | `src/HCS.Blazor/wwwroot/images/logo/logo.png` | Boot screen, header Blazor, login AuthServer | Thay binary sau khi có logo mới; giữ nguyên đường dẫn để giảm phạm vi |
| Logo theme | `src/HCS.Blazor/wwwroot/images/logo/leptonxlite/logo-{light,dark}{,-thumbnail}.png` | Asset theme Lepton/ABP | Kiểm tra bằng browser; chỉ thay nếu thực tế còn được theme sử dụng |
| Logo login | `apps/auth-server/HCS.AuthServer/wwwroot/auth-login.css` | CSS trỏ `/images/logo/logo.png` | Không cần đổi selector nếu giữ contract đường dẫn; kiểm tra render |
| Cấp asset login | `apps/auth-server/HCS.AuthServer/HCS.AuthServer.csproj` | Link/copy `logo.png` từ Blazor vào AuthServer `wwwroot` | Không đổi nếu tiếp tục dùng cùng file; build kiểm tra copy |
| Favicon browser | `src/HCS.Blazor/wwwroot/favicon.ico` | URL mặc định `/favicon.ico` của host | Kiểm tra response/render; thay binary nếu yêu cầu favicon mới |
| PWA/install icons | `src/HCS.Blazor.Client/wwwroot/icon-192.png`, `icon-512.png` | Được `manifest.json` tham chiếu | Đồng bộ với brand mới hoặc giữ riêng nếu yêu cầu thiết kế khác |
| PWA metadata | `src/HCS.Blazor.Client/wwwroot/manifest.json` | Tên app + icon install | Kiểm tra; chỉ sửa tên/màu/paths khi có quyết định branding |
| HTML head | `src/HCS.Blazor/Components/App.razor` | Hiện có `<title>HCS</title>` nhưng chưa khai báo `<link rel="icon">` | Xác nhận fallback `/favicon.ico`; cân nhắc khai báo rõ link để test ổn định |

### Blocker bắt buộc

`/Users/nguyenlong/Downloads/logo.png` hiện **không tồn tại**. Asset thay thế `/Users/nguyenlong/Downloads/logo-dark.png` có checksum `839a0aaa27772cb22d24757efc9c5b2038f9198810e38016e6a4b45e42d6468f`, trùng với cả 5 logo PNG đã modified trong worktree; vì vậy không cần ghi đè logo lần nữa. Không tạo logo thay thế, không lấy asset từ nguồn ngoài, và không ghi đè các asset đang có trong worktree. Việc đồng bộ favicon/PWA icons vẫn cần user xác nhận.

Worktree hiện đã có thay đổi trước đó trên `logo.png` và các biến thể Lepton; phải bảo toàn các thay đổi này, không dùng `git checkout`, `reset` hoặc thao tác hoàn nguyên.

## Data flow

```text
logo.png đầu vào
  -> kiểm tra định dạng/kích thước/độ trong suốt
  -> logo dùng chung trong HCS.Blazor/wwwroot
  -> build/publish static web assets
  -> (1) Blazor boot screen + header
  -> (2) AuthServer csproj copy -> AuthServer /images/logo/logo.png -> login

favicon.ico / icon-192.png / icon-512.png
  -> static web root hoặc Blazor Client manifest
  -> browser request /favicon.ico hoặc manifest install
  -> favicon tab/bookmark + PWA icon
```

Không có database migration, API contract, user data hoặc OIDC flow thay đổi. Output quan sát được là static asset bytes, HTML/CSS references, HTTP responses và ảnh hiển thị trong browser.

## Dependency graph và phases

### Phase 1 — Chốt input và contract branding (blocked)

**Owner:** frontend/static-assets implementer. **Files:** không sửa; đọc `logo.png`, các icon hiện tại, các reference nêu trên.

1. Nhận logo binary tại `/Users/nguyenlong/Downloads/logo.png` hoặc path mới do user xác nhận.
2. Xác nhận logo dùng chung hay cần bộ favicon/PWA riêng; chốt kích thước nền/alpha và tên hiển thị (HCS hay tên mới).
3. Kiểm tra asset hiện tại trong worktree trước khi thay, ghi checksum/kích thước để rollback.

**Gate:** input tồn tại, mở được, đúng định dạng và user xác nhận không ghi đè thay đổi logo đang có.

### Phase 2 — Cập nhật assets và wiring tối thiểu

**Depends on:** Phase 1. **Owner:** frontend/static-assets implementer. **Ownership:** chỉ các file asset/static và `App.razor`; không song song sửa cùng file.

1. Thay `src/HCS.Blazor/wwwroot/images/logo/logo.png` bằng asset đã chốt, giữ nguyên path để header, boot screen, AuthServer CSS và csproj copy tiếp tục hoạt động.
2. Nếu design yêu cầu, tạo/cập nhật `favicon.ico`, `icon-192.png`, `icon-512.png`; không dùng logo 500x500 làm favicon một cách mù quáng nếu tỷ lệ/độ rõ không phù hợp.
3. Chỉ cập nhật `manifest.json` khi tên, màu hoặc paths thực sự thay đổi.
4. Chỉ thêm `<link rel="icon" href="/favicon.ico">` vào `App.razor` nếu browser kiểm tra cho thấy fallback không ổn định; giữ thay đổi này nhỏ và rõ ràng.
5. Không sửa `auth-login.css` hoặc `HCS.AuthServer.csproj` trừ khi contract path/copy được chứng minh là không còn đúng.

**Failure modes / mitigation:** asset sai màu hoặc alpha → preview trước build; copy thiếu vào AuthServer → build/publish + kiểm tra file output; browser cache asset cũ → query/version hoặc hard refresh, không đổi URL tùy tiện; logo mới phá layout → kiểm tra desktop/mobile và rollback file asset.

### Phase 3 — Build, runtime smoke test và bàn giao

**Depends on:** Phase 2. **Owner:** QA/runtime verifier. Không sửa file implementation.

1. Audit license/secret trước build.
2. Build solution và test theo lệnh chuẩn README.
3. Chạy runtime HCS Docker Compose, mở `https://hcs.localhost` và AuthServer `https://localhost:44401` nếu môi trường đã có dependencies.
4. Kiểm tra tab icon, bookmark/fresh private window, PWA manifest/icon, boot screen, header và `/Account/Login`.
5. DevTools Network: xác nhận `/favicon.ico` trả `200`, MIME đúng; các logo/icon trả `200` và bytes/kích thước khớp asset mới.
6. Báo URL, container/process đã restart và yêu cầu hard refresh theo rule workspace.

## Test matrix

| Layer | Kiểm tra | Tiêu chí |
|---|---|---|
| Unit/static | Không cần unit test cho binary; kiểm tra XML/JSON hợp lệ và reference path bằng `rg`/script | Không còn path chết; manifest parse được |
| Build/integration | `./scripts/audit-license-clean.sh`; `dotnet restore HCS.slnx --configfile NuGet.Config`; `dotnet build HCS.slnx --no-restore` | Audit/build pass; AuthServer publish chứa `/images/logo/logo.png` |
| Existing tests | `dotnet test HCS.slnx --no-build` | Không regression; nếu baseline fail phải ghi rõ, không bỏ qua |
| E2E browser | fresh private window: `/`, `/login`, login AuthServer, deep link, mobile viewport | Logo hiển thị đúng; favicon tab đúng; login/SSO vẫn chạy |
| Cache/install | hard refresh, đổi tab, bookmark, manifest/application panel | Không còn icon cũ sau cache refresh; icon 192/512 đúng nếu PWA scope |
| Failure | thiếu binary, file hỏng, asset copy thiếu, favicon 404, cache cũ, logo tỷ lệ xấu | Có lỗi rõ ràng và rollback được; không làm hỏng login/runtime |

## Backwards compatibility, rollback và security

- Giữ nguyên các URL asset hiện có (`/images/logo/logo.png`, `/favicon.ico`, manifest icon paths) để browser bookmark/cache và code tham chiếu không vỡ.
- Không thay đổi auth endpoint, cookie, token, Keycloak, database hoặc service contract.
- Trước thay đổi, lưu checksum/kích thước các file asset liên quan. Rollback = khôi phục đúng bytes/checksum trước đó và rebuild/restart host; nếu thêm `App.razor` link thì revert riêng dòng đó.
- Rollback runtime Docker: `./scripts/docker-down.sh` (giữ volumes), sau đó chạy lại `./scripts/docker-up.sh` với source asset đã khôi phục. Không xóa volume/namespace.
- Chỉ nhận binary từ user/path đã xác nhận; kiểm tra loại file và không commit secret. Không lấy asset từ `HCS_web_with_license` hoặc nguồn commercial.

## Success criteria đo được

- Asset input tồn tại và được user chốt; blocker được đóng trước khi triển khai.
- Mọi reference logo/favicon/manifest ở bảng trên đều trỏ tới file tồn tại, build copy đúng output.
- `/favicon.ico`, `/images/logo/logo.png`, icon PWA trả HTTP `200` với MIME/kích thước đúng trong runtime.
- Logo mới nhìn đúng ở boot screen, header và AuthServer login trên desktop/mobile; favicon mới hiển thị trong tab sau hard refresh/private window.
- `dotnet build` và `dotnet test --no-build` pass, hoặc baseline failures được ghi kèm bằng chứng.
- Không có thay đổi ngoài các file branding đã chốt; thay đổi logo đang có trước lượt này không bị mất.

## Lệnh dự kiến khi được phép triển khai

```bash
cd /Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license
./scripts/audit-license-clean.sh
dotnet restore HCS.slnx --configfile NuGet.Config
dotnet build HCS.slnx --no-restore
dotnet test HCS.slnx --no-build
./scripts/docker-up.sh
docker compose ps
```

Browser smoke: mở `https://hcs.localhost`, `https://hcs.localhost/login`, và `https://localhost:44401/Account/Login`; dùng DevTools Network/Application để kiểm tra favicon và manifest. Chỉ restart service sau khi asset/code thực sự được cập nhật.

## Unresolved questions

1. Cung cấp `/Users/nguyenlong/Downloads/logo.png` (hoặc path mới) và xác nhận file này được phép thay cho logo hiện tại.
2. Logo mới có cần đồng thời làm favicon và PWA icons không, hay có bộ favicon 16/32/ICO/192/512 riêng?
3. Có đổi tên hiển thị/title từ `HCS` và subtitle `HCS · Hành chính số` không?
4. Có yêu cầu đổi logo Lepton light/dark/thumbnail cùng lượt không?
5. Môi trường Docker HCS hiện đã chạy sẵn để thực hiện browser smoke test chưa?
