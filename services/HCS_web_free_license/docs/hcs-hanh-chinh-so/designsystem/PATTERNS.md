# HCS Patterns

## Token

```css
/* Đúng */
background: var(--color-primary);
color: var(--color-foreground);

/* Sai */
background: #007f7c;
```

Sửa token ở `designsystem/tokens.css` + `design-tokens.json`, không hardcode ở page CSS.

## Toast + mock

```html
<div class="toast" id="toast" role="status" aria-live="polite"></div>
```

- `data-mock-action` → `showToast(...)` trong `js/app.js`
- Không giả lập bằng `alert`
- Link thật (`href="….html"`) để điều hướng — đừng `preventDefault`

## Icon

- UI: SVG outline, `stroke="currentColor"`, `aria-hidden="true"`
- Nút có chữ: icon 16×16
- Không dùng emoji làm icon chrome (nav, btn, tab)
- Cảm xúc feed: SVG **có màu** trong `REACTS`

## i18n

Chuỗi UI: `data-i18n` + bảng `t()` trong `js/app.js`. Page mới thêm key vào cùng chỗ.

## A11y

- `lang="vi"`
- Skip link
- `:focus-visible` — không xóa ring
- `type="button"` trừ submit
- `sr-only` cho label ẩn
- `prefers-reduced-motion` đã giảm `--duration` trong token

## JS

- Một file: `js/app.js`
- Bọc theo page: `if (document.getElementById("…")) { … }`
- Hôm nay mock lịch: **11 Sep 2026**
- Check-in mock “bây giờ”: `CI_NOW` = **11 Sep 2026, 10:00** (cùng ngày lịch)
- Chat: không sửa comment `<!-- leftover-start` sau `</html>`

### Check-in — session & trạng thái

| Key | Ý nghĩa |
|-----|---------|
| `sessionStorage hcs-user` | Đã đăng nhập (tên hiển thị) |
| `sessionStorage hcs-rsvp-{id}` | Đã xác nhận tham dự (`"1"`) |
| `sessionStorage hcs-checkin-{id}` | Đã check-in có mặt (`"1"`) |

Gate UI: không user → chỉ `#ci-panel-login`. Có user → `#ci-panel-event` (status, file, CTA). Check-in chỉ enable khi RSVP xong và `eventPhase === "live"`.

Demo reset: xóa các key trên trong DevTools. Demo login nhanh: `#ci-quick-login` → `hungtv`.

i18n keys liên quan: `eventCheckin*`, `eventRsvp*` trong bảng `t()`.

## Module CSS

Chỉ tạo `css/<module>.css` khi page có layout riêng (như `social.css`, `events.css`). Scope bằng class body (`.page-social`, `.page-checkin`) để không rò vào Workspace.

## Git

Nhánh `feat/…` hoặc `fix/…`. Không push `main`. Không commit nếu user chưa yêu cầu.
