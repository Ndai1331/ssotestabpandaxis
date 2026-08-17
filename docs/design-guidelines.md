# design-guidelines.md — BD

Phase lab SSO — chưa có brand UI riêng cho BD portal.

## Principles
- Ưu tiên UI upstream Directus Studio và ABP Blazor theme.
- Không áp dụng design system Task9 / BootstrapBlazor Task9 pages.
- Khi thiết kế landing/login custom: bám skill `frontend-design` + guideline user (tránh purple-gradient AI cliché).

## Auth UX
- Login chỉ qua Keycloak (không form password riêng trên app nếu SSO bật).
- Thông báo lỗi auth rõ (redirect URI, realm sai).
