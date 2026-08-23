# design-guidelines.md — BD

Phase lab SSO — chưa có brand UI riêng cho BD portal.

## Principles
- Ưu tiên UI upstream Directus Studio và ABP Blazor theme.
- Không áp dụng design system Task9 / BootstrapBlazor Task9 pages.
- Khi thiết kế landing/login custom: bám skill `frontend-design` + guideline user (tránh purple-gradient AI cliché).

## Auth UX
- Login chỉ qua Keycloak (không form password riêng trên app nếu SSO bật).
- Thông báo lỗi auth rõ (redirect URI, realm sai).

## HCS UI foundation (mobile-first)

- Font giao diện: Be Vietnam Pro; body mặc định 16px với line-height tối thiểu 1.5.
- Dùng token chung trong `services/HCS_web_free_license/src/HCS.Blazor.Client/wwwroot/hcs-tokens.css` và primitive trong `hcs-components.css`; page/domain CSS chỉ giữ phần đặc thù.
- Spacing theo nhịp 4/8px: 4, 8, 12, 16, 24, 32, 40 và 48px. Page gutter là 16px trên mobile, tăng dần theo viewport.
- Breakpoint chuẩn: 576px, 768px, 992px và 1200px. App drawer chuyển sang mobile ở khoảng 992px; không thu gọn label navigation ở tablet.
- Button, input, menu item, icon button và vùng tương tác chính có kích thước tối thiểu 44px; input text không nhỏ hơn 16px trên mobile.
- Luôn có `:focus-visible`, trạng thái disabled và thông báo text/icon đi kèm màu trạng thái. Text thường cần contrast tối thiểu 4.5:1.
- Dùng `min-height: 100dvh`, safe-area inset và một scroll region chính; không dùng `100vh` cho layout mobile.
- Bảng đơn giản tối đa 4 cột dùng `.hcs-table-wrap--stacked` cùng `data-label` để chuyển thành card trên mobile. Bảng cần so sánh nhiều cột dùng `.hcs-table-wrap--scroll`, `min-width` và chỉ dẫn “Vuốt ngang để xem thêm”; không tạo horizontal scroll cho page container.
- Với bảng rộng trong CSS Grid, grid item và data surface phải có `min-width: 0`; đặt `overflow-x: auto` ở card body/table wrapper, không để `min-width` của table làm nở `.hcs-document-layout` hoặc page header.
- Form nhiều cột chuyển thành một cột dưới 768px. Page header, toolbar, filter và action phụ xếp dọc khi không còn đủ chiều rộng.
- Animation chỉ dùng cho `transform`/`opacity` trong 150–300ms và phải giảm/tắt khi `prefers-reduced-motion: reduce`.
