# HCS Design System

Nguồn chuẩn để làm **page tiếp theo** đúng style prototype `projects/hcs-hanh-chinh-so`.

Đọc file này trước khi thêm HTML/CSS/JS mới.

## Stack

- HTML tĩnh + `css/app.css` (đã `@import` token)
- Token 3 lớp: primitive → semantic → component
- Font: **Inter** 400–800 (Google Fonts)
- Primary: teal `#007f7c` → `var(--color-primary)`
- Nền app: `#F3F7F8` → `var(--color-background)`
- Không dùng hex trong component mới — luôn `var(--…)`

## File trong thư mục này

| File | Dùng khi |
|------|----------|
| `tokens.css` | Sửa màu / spacing / radius (source of truth) |
| `design-tokens.json` | Token máy đọc, cùng giá trị với CSS |
| `LAYOUT.md` | Khung trang: topbar, nav, main, page-head |
| `COMPONENTS.md` | Button, input, card, badge, modal, KPI |
| `PATTERNS.md` | Toast, icon, cảm xúc, mock action, i18n |

CSS runtime: `css/app.css` → `css/tokens.css` → `designsystem/tokens.css`.

Module riêng (chỉ gắn page cần):

- Mạng xã hội → `css/social.css` + `body.page-social`
- Lịch / sự kiện / check-in → `css/events.css`

## Checklist page mới

1. Copy khung từ `home.html` (skip-link, topbar, nav, `#main`, toast) — **trừ** trang khách QR (`check-in.html`).
2. `lang="vi"`, Inter, `css/app.css`. Chỉ thêm CSS module nếu layout khác hẳn.
3. `body`: `app-page` (nội bộ) · `login-page` (đăng nhập app) · `page-checkin` (QR check-in, không shell).
4. Tiêu đề trang nội bộ: `.page-head h1` màu primary, 22px semibold.
5. Mọi `.btn` có **SVG 16×16** + nhãn. `type="button"` trừ submit.
6. Touch: nút/input tối thiểu **44×44**. Focus: `var(--color-ring)`.
7. Hành vi giả: `data-mock-action` → toast. **Không** `preventDefault` link thật.
8. Gắn nav: đánh `is-active` + `aria-current="page"` đúng mục.
9. JS mới chỉ thêm vào `js/app.js`, không tạo file script rời trừ khi bắt buộc.

## Page đã có (tham chiếu)

| Page | File | Ghi chú |
|------|------|---------|
| Đăng nhập | `login.html` | `login-page`, không topbar |
| Workspace | `home.html` | Lưới 12 cột (3/8/4) |
| Văn bản | `van-ban.html` | sidebar + bảng; thêm/sửa `#tao` / `#np` |
| Trình ký | `ky-duyet.html` | hàng chờ ký |
| Chat | `chat.html` | `page-chat` |
| Bảng tin | `bang-tin.html` | `page-social` |
| Sự kiện | `su-kien.html` | dashboard · `#danh-sach` · `#tao` · `#e1`/`#e2` chi tiết (KPI, QR, file, khách) |
| Check-in | `check-in.html` | `page-checkin`; 2 màn: `#ci-panel-login` → `#ci-panel-event` (RSVP → check-in khi live) |
| Lịch | `lich.html` | tháng / tuần / ngày |
| Quy trình | `quy-trinh.html` | hồ sơ, `#tao` / `#np-tpl2` editor như văn bản |
| Dự án | `du-an.html` | danh sách dự án |
| Công việc | `cong-viec.html` | Kanban, `#danh-sach` |

## Việc không làm

- Không đổi primary sang màu khác.
- Không dùng font khác Inter.
- Không emoji làm icon UI (cảm xúc feed dùng SVG màu trong `REACTS`).
- Không đẩy thẳng `main`. Không commit khi chưa được yêu cầu.
