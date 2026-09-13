# HCS Layout

## Topbar actions

- **Ngôn ngữ** (`#lang-pop`): dropdown VI / EN, lưu `localStorage hcs-lang`.
- **Thông báo** (`#notify-pop`): badge chưa đọc, danh sách, đánh dấu đã đọc (`sessionStorage`).
- **Tin nhắn**: link `chat.html`.

Không dùng `data-lang` trên chip (sẽ set cứng VI). Chọn ngôn ngữ bằng `[data-set-lang]`.

## Shell nội bộ (`body.app-page`)

```
┌─────────────────────────────────────────────┐
│ topbar  64px  logo · lang · bell · user     │
├─────────────────────────────────────────────┤
│ nav     52px  teal  #007f7c  items + mega   │
├─────────────────────────────────────────────┤
│ main#main   padding, max content width      │
│   .page-head (h1 + .page-actions) — trừ Workspace   │
│   cards / grid                                      │
└─────────────────────────────────────────────┘
```

Bắt buộc trên mọi page nội bộ:

1. `<a class="skip-link" href="#main">`
2. `<header class="topbar">` — brand `home.html`, logo `assets/logo-hcs.svg`
3. `<nav class="nav" id="app-nav">` — copy nguyên từ `home.html`
4. `<main id="main">`
5. Toast: `#toast` (xem `PATTERNS.md`)
6. `<script src="js/app.js" defer></script>`

## Nav hiện tại

| Mục | Href | Ghi chú |
|-----|------|---------|
| Workspace | `home.html` | Lưới 12 cột: KPI 3+3+3+3; Lịch 8 + Thông báo 4; hàng dưới 4+8 |
| Văn bản | `van-ban.html` | sidebar + bảng; `#tao`/`#np`: form trái, **chọn file + PDF** cột phải |
| Trình ký | `ky-duyet.html` | |
| Chat | `chat.html` | |
| Bảng tin | `bang-tin.html` | |
| Lịch | `lich.html` | |
| Sự kiện | `su-kien.html` | `#danh-sach`, `#tao`, `#e1`/`#e2` chi tiết (`#view-event-detail`); QR → `check-in.html#…` |
| Quy trình | `quy-trinh.html` | Hồ sơ; `#tao` / `#np-tpl2` editor (chrome + tab + PDF phải) |
| Dự án | `du-an.html` | Danh sách dự án |
| Công việc | `cong-viec.html` | Kanban, `#danh-sach` |
| Danh mục | `danh-muc.html` | **Chuẩn list**: Phòng ban (search luôn hiện, table + modal thêm/sửa) |

Item đang mở: `class="nav-item is-active"` + `aria-current="page"`.

## Page head

```html
<div class="page-head">
  <h1>Tên trang</h1>
  <div class="page-actions">
    <button class="btn btn-outline" type="button">…</button>
    <button class="btn btn-primary" type="button">…</button>
  </div>
</div>
```

- `h1`: 22px, semibold, `var(--color-primary)`
- Actions: flex, gap 10px, nút pill 40px, **căn đáy** với tiêu đề
- Khoảng cách tới khối dưới: `--space-5` (cả khi `.page-head` nằm trong section)
- **Tìm kiếm**: nút `[data-search-toggle]` trong `.page-actions` — click mới mở form `.list-search` (ẩn mặc định)
- **Workspace** (`home.html`): không có `.page-head`. Thanh `.ws-filter` (khoảng ngày `type="date"`) luôn hiện, nút Tìm kiếm không bị ẩn.
- **Bộ lọc**: `.filter-pop` cùng hàng với các nút, không nằm trong thanh tìm

## Grid

| Class | Cột |
|-------|-----|
| `.grid-2` | 1.35fr / 1fr, gap `--space-4` |
| `.ws-grid` | 12 cột. Workspace: `.ws-span-3` KPI, `.ws-span-8` + `.ws-span-4` (Lịch/Thông báo), `.ws-span-4` + `.ws-span-8` (hàng dưới) |
| `.docs-layout` | sidebar 220px + nội dung (văn bản) |

Social: `.social-wrap` max-width **1280px**, `margin-inline: auto`.

## Sự kiện — chi tiết (`su-kien.html`)

Hash điều hướng view: `#danh-sach` · `#tao` · `#e1` / `#e2` (mở `#view-event-detail`).

```
#view-event-detail
├── .ed-back → #danh-sach
├── .ed-hero (tên, meta, .badge-live|…)
├── .ed-stats (KPI tham dự / check-in)
└── .ed-split
    ├── .ed-card form + QR (#ed-checkin-open → check-in.html#id)
    ├── .ed-card file đính kèm
    └── .ed-card khách mời (+ modal chọn user hệ thống)
```

CSS: `css/events.css` (prefix `.ed-`, `.guest-dialog`). Modal thêm khách: checkbox + “Chọn tất cả”, search user.

## Check-in khách / QR (`check-in.html`)

Trang **ngoài shell**: `body.page-checkin`, không topbar/nav. CSS: `css/events.css` (prefix `.ci-`). Hash `#e1` / `#e2`.

```
page-checkin
├── #ci-panel-login     màn 1 — chỉ đăng nhập (+ tóm tắt tên/thời gian/địa điểm)
├── #ci-panel-event     màn 2 — sau login: trạng thái, meta, file, banner, CTA
└── #ci-empty           sự kiện không tồn tại
```

**Luồng**

1. **Đăng nhập** — `#ci-panel-login` (form `#ci-login`). Session: `hcs-user`.
2. **Xác nhận tham dự (RSVP)** — trên `#ci-panel-event`, nút `#ci-action` mode `rsvp`. Key: `hcs-rsvp-{id}`.
3. **Check-in có mặt** — chỉ khi đã RSVP **và** phase `live`. Key: `hcs-checkin-{id}`.

**Phase sự kiện** (mock `CI_NOW` = 11/09/2026 10:00 trong `js/app.js`): `upcoming` · `live` · `ended` → badge + banner (`is-wait` / mặc định / `is-warn` / `is-done`).

Stepper `.ci-steps` chỉ hiện trên màn sự kiện (bước 1 đánh `is-done` sau login). Không gộp form login vào thẻ sự kiện — sự kiện giữ chỗ cho status + file + CTA.

## Breakpoint

Mobile: hamburger `#menu-toggle` + overlay. Không tắt zoom. Không scroll ngang.

Tham chiếu CSS: `css/app.css` (`.topbar`, `.nav`, `.page-head`); sự kiện/check-in: `css/events.css`.
