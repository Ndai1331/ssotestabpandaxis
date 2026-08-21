# HCS Free UI/UX increment — 2026-08-20

**Phạm vi:** `services/HCS_web_free_license`  
**Ranh giới:** LICENSE chỉ đọc; không port `Blazorise.PdfViewer`.

## Đã giao

1. **PDF full-frame** — `HcsPdfFrame` (iframe + blob URL) fill 100% trên DocumentDetail, bước template quy trình, modal preview.
2. **Wizard quy trình** — 3 bước (thông tin / template / bước+người). `RoleInSubmitterOu` resolve phía DocumentService qua Platform HTTP (`GET api/identity/workflow-assignees`). Submit modal: 1 ứng viên preset, nhiều thì dropdown.
3. **Chat** — rời nhóm: sole admin còn member phải chọn `transferAdminTo`; icon loại hội thoại (User chữ, Group/Project/Task Font Awesome).
4. **Thông báo** — title ≠ body (chat / gán việc / gán dự án). Topbar badge số chuông + unread chat (`GET api/chat/unread-count` + poll 15s / SignalR).
5. **Ký số** — filter riêng; 3 tab 1 hàng ngay trên DataGrid; tab active `--hcs-primary`.
6. **Landing** — user đã login redirect `/workspace`; AuthServer ReturnUrl mặc định `/workspace`; nút search/action workspace dùng `--hcs-primary`.
7. **Dự án** — vai trò Manager/Supervisor/Member (en/vi); DateTime picker + icon lịch (`HcsDatePicker`).

## Quyết định

- PDF: nâng iframe hiện có, không thêm gói commercial.
- Resolve role trên server (Document → Platform), không chỉ trên Blazor — DocumentService không có Identity DB.
- CSS runtime nằm ở `main.css` (bundle host); `hcs-catalog.css` / `hcs-workspace.css` giữ bản nguồn song song.

## Tests đã chạy

- CollaborationService.Tests: 30 passed
- DocumentService.Tests: 45 passed
- WorkManagementService.Tests: 39 passed
- Build: HCS.Blazor.Client, HCS.AuthServer
