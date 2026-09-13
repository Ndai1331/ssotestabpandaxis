# HCS Components

Dùng class có sẵn trong `css/app.css`. Không invent class trùng ý nghĩa.

## Button

Mọi `.btn` **phải có SVG** 16×16 (`stroke="currentColor"`).

| Variant | Class | Dùng khi |
|---------|-------|----------|
| Primary | `.btn.btn-primary` | Hành động chính |
| Outline | `.btn.btn-outline` | Phụ, viền teal |
| Ghost | `.btn.btn-ghost` | Hủy / nhẹ |
| Danger | `.btn.btn-danger` | Xóa / thu hồi |
| Small | thêm `.btn-sm` | Toolbar, comment |

Trên list (`.page-actions`, `.docs-toolbar`): nút cao 40px, radius `--button-radius` (`--radius-xl`, đồng bộ với input/form). **Tìm kiếm** nằm trong `.page-actions` (`data-search-toggle`) — click mới mở form `.list-search` (ẩn mặc định). Ô `.docs-search` trắng, radius `--input-radius`.

```html
<button class="btn btn-primary" type="button">
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
    <path d="M12 5v14M5 12h14"/>
  </svg>
  Tạo mới
</button>
```

| State | Token |
|-------|--------|
| Default | `--button-bg` / `--button-fg`, height 44px |
| Hover | `--button-hover-bg` |
| Active | `translateY(1px)` |
| Disabled / busy | opacity 0.7, `cursor: wait` |

Icon-only: `.icon-btn` 40×40, hover `--color-gray-100`.  
Bảng: `.doc-action` 36×36, `border-radius: 8px` (`--radius-md`), icon căn giữa; xóa thêm `.is-danger` (đỏ).

## Input

```html
<div class="field">
  <label class="field-label" for="q">Nhãn</label>
  <input class="input" id="q" type="text" autocomplete="off">
  <p class="field-error" id="q-err">Lỗi</p>
</div>
```

- Height 40px, radius `--input-radius` (`--radius-xl`, đồng bộ button/form), border `--color-input`
- Focus: border primary + `--shadow-focus`
- Lỗi: `.input.is-invalid` + `.field-error.is-visible`
- Select / textarea: cùng radius `--radius-xl`

## Card

```html
<section class="card">
  <div class="card-head">
    <h2 class="card-title">Tiêu đề</h2>
  </div>
  …
</section>
```

KPI: `.card.kpi` + `.kpi-icon.teal|blue|green|…` + `.kpi-value` + `.kpi-label`.  
Workspace: KPI là link; badge số `.kpi-badge`. Hover nổi: thêm `.card-interactive`.

## Badge

| Class | Nền / chữ | Dùng khi |
|-------|-----------|----------|
| `.badge.badge-info` | xanh dương | Chưa bắt đầu / thông tin |
| `.badge.badge-live` | teal mềm | Đang diễn ra (sự kiện) |
| `.badge.badge-warn` | tím | Cảnh báo nhẹ |
| `.badge.badge-muted` | xám | Đã kết thúc / trung tính |
| `.badge.badge-overdue` | hồng | Quá hạn |

Min-height 24px, 11px semibold.

## Modal

```html
<div class="modal-overlay" id="…" hidden>
  <div class="process-dialog" role="dialog" aria-modal="true" aria-labelledby="…">
    <div class="process-dialog-head">…</div>
    <div class="process-dialog-body">…</div>
    <div class="process-dialog-foot">…</div>
  </div>
</div>
```

Mở: `.is-open` trên overlay + `body.modal-open`. Esc đóng.

Thêm khách sự kiện: `.guest-dialog` + `.ed-pick` (search, `#guest-user-all`, list checkbox). Style trong `css/events.css`.

## Avatar

`.avatar` / `.chat-avatar` — chữ viết tắt 2 ký tự, nền teal mềm.

## Cảm xúc (bảng tin)

Trigger `.react-wrap` + `[data-react-open]`. Hover (hoặc bấm trên comment / touch) hiện `.react-bar`.

Thứ tự icon: like · love · **haha** · smile · clap · think · sad (SVG màu, định nghĩa `REACTS` trong `js/app.js`).

Comment meta dùng `<div class="comment-meta">` — **không** bọc `.react-bar` trong `<p>`.

## Sự kiện — chi tiết (`.ed-*`)

Trong `su-kien.html` + `css/events.css`:

| Khối | Class / id | Ghi chú |
|------|------------|---------|
| Quay lại | `.ed-back` | Link `#danh-sach` |
| Hero | `.ed-hero`, `#ed-title`, `#ed-badge` | Badge phase |
| KPI | `.ed-stats` / `.ed-stat` | Tổng / xác nhận / pending / từ chối / in / out |
| Form + QR | `.ed-split` → `.ed-card`, `#ed-form`, `#ed-checkin-open` | Mở `check-in.html#id` |
| File / khách | bảng `.ed-table`, filter `.ed-guest-filter` | Select filter **không** full-width |

## Check-in khách (`.ci-*`)

Trong `check-in.html` + `css/events.css`. **Hai thẻ tách** — không nhét form login vào thẻ sự kiện.

| Khối | Class / id | Ghi chú |
|------|------------|---------|
| Màn login | `#ci-panel-login` `.ci-card-login` | Form `#ci-login`; context `#ci-login-event` |
| Màn sự kiện | `#ci-panel-event` `.ci-card-event` | Rộng hơn (`min(520px)`); sau `hcs-user` |
| Stepper | `.ci-steps` `[data-ci-step]` | `login` · `rsvp` · `checkin` — `is-current` / `is-done` |
| Meta | `.ci-event`, `#ci-status-badge`, `#ci-when`, `#ci-place` | |
| Banner | `.ci-banner` (+ `is-wait` / `is-warn` / `is-done`) | `#ci-banner-text` |
| File | `.ci-files` / `.ci-file` | Ẩn nếu không có file |
| CTA | `#ci-action` `data-ci-mode` | `rsvp` · `checkin` · `wait` · `ended` · `done` |