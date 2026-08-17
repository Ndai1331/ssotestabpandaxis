---
type: concept
title: "Authz & Permission Model"
created: 2026-06-29
updated: 2026-07-16
tags:
  - security
  - authz
  - rbac
  - tech-debt
confidence: high
source_audit: "plans/reports/brainstorm-260626-1042-permission-user-team-authz-audit.md"
related:
  - "[[Codebase — task9-api]]"
  - "[[Codebase — task9-ui]]"
  - "[[Blazor Page Creation Checklist]]"
---

# Authz & Permission Model

> Hiện trạng phân quyền task9-ui + task9-api. Nguồn: audit `plans/reports/brainstorm-260626-1042-permission-user-team-authz-audit.md` (2026-06-26) + RBAC ship commits (2026-07-14). Đọc page này TRƯỚC khi chạm bất cứ thứ gì liên quan auth/user/role. Liên quan: [[Codebase — task9-api]], [[Codebase — task9-ui]], [[Blazor Page Creation Checklist]].

## TL;DR cần nhớ

- **RBAC ĐÃ SHIP (2026-07-14):** API `3f46c6d` + UI `6744e3a` thêm permission RBAC, SUPER_ADMIN role, login-as-user impersonation, user-manager/team redesign. Tình trạng đã cải thiện đáng kể so với audit ban đầu.
- **Audit gốc (2026-06-26) — bối cảnh lịch sử:** 55/60 controller chỉ `[Authorize]` trơn → Broken Access Control (OWASP A01). Tình trạng hiện tại đã khác, cần verify lại coverage.
- **Per-role default login landing page** (UI `f447e1a`, API `3cc303e`): mỗi role có trang đích riêng sau login.
- **Runtime menu order/module reassignment** (API `0b84163`): menu có thể reorder/reassign module lúc runtime, không cần hardcode.
- **Auth hardening (API `bc5c572`):** block refresh token cho inactive/deleted users, revoke session khi deactivate.
- **User-manager fixes (UI `f86c1d6`, API `d330b59`):** null-guard role/position/team loads trên auth race; surface real create-user errors.

## Cơ chế hiện tại (sau RBAC ship 2026-07-14)

| Tầng | Cơ chế | Cập nhật |
|------|--------|----------|
| UI | `UrlAuthorizationService` + permission-matrix load | Đã redesign user-manager/team, thêm permission RBAC UI, login-as-user impersonation |
| API | `[Authorize]` + RBAC policies + SUPER_ADMIN | Đã thêm RBAC permission enforcement, SUPER_ADMIN role, impersonation |

### RBAC đã ship (commit `3f46c6d` API, `6744e3a` UI)
- Permission RBAC: API endpoint có kiểm soát quyền theo permission/role
- SUPER_ADMIN role: quyền toàn năng (impersonation, quản lý mọi user)
- Login-as-user impersonation: SUPER_ADMIN có thể đăng nhập thay user khác
- User-manager/team redesign: UI quản lý user + team mới

### Per-role login landing page (commit `3cc303e` API, `f447e1a` UI)
- Mỗi role có trang mặc định sau khi login (configurable)
- API trả về `defaultLandingPage` theo role

### Runtime menu order/module reassignment (commit `0b84163` API)
- Menu order và module assignment có thể thay đổi runtime, không cần hardcode
- Đã remove "SEO Thái" khỏi menu mặc định

### Auth hardening (commit `bc5c572` API)
- Block refresh token cho inactive/deleted users
- Revoke session khi user deactivate
- Tránh token被盗 sau khi user bị khóa

## Audit gốc (2026-06-26) — phát hiện lịch sử

> Đây là kết quả audit ban đầu. RBAC đã ship cải thiện nhiều vấn đề. Cần verify lại coverage từng điểm để biết cái nào còn mở.

- **A1 🔴** 55/60 controller không enforce role — cần verify lại sau RBAC ship
- **A2 🔴** Policy `EmployeeOnly` không định nghĩa — cần verify đã được thay thế hay fixed
- **A4 🟠** `GenerateTokenByUser` nhét mọi role vào JWT, UI chỉ đọc 1 — cần verify
- **A5 🟠** `role.Contains("SEO")` mong manh — cần verify đã được thay bằng RBAC thật
- **A6 🟡** `sign-up` + `set-password` `[AllowAnonymous]` — cần verify
- **B1 🟠** Menu sync 2 chỗ (`PageLayout.razor.cs` + `UrlAuthorizationService`) — runtime menu reassignment có thể đã giải quyết
- **B3** `RoleClaimEnum.cs` tàn dư y tế (`DOCTOR/ADMIN/PATIENT/CS`) — role thật: `ADMIN/HEAD/ASSISTANT/SEO*/QC/IC/HR` + SUPER_ADMIN mới

## 3 trục không liên kết authz
`Role` (chỉ ảnh hưởng UI), `Team` (chỉ là nhãn gán việc/lọc PIC), `Department`, `UserType` (`Inhouse/Remote/OSZ/OS`). Hiện **không có row-level/scope authz** ("user chỉ thấy data team mình") — có thể là chủ đích, cần user xác nhận.

## Hướng cải thiện — cập nhật
- **PA1 — vá chỗ chảy máu:** ✅ Phần lớn đã thực thi qua RBAC ship. Cần audit lại coverage còn thiếu.
- **PA2 ⭐ — RBAC claim-driven:** ✅ Đã ship (commit `3f46c6d` + `6744e3a`). Cần verify mức độ hoàn thiện.
- **PA3 — RBAC + scope theo Team:** chưa làm, YAGNI nếu nghiệp vụ không cần.

## Câu hỏi mở (cần verify lại sau RBAC ship)
1. Tất cả 55 controller cũ đã được gắn RBAC chưa, hay chỉ một phần?
2. Policy `EmployeeOnly` đã được thay thế bằng RBAC policy mới?
3. `role.Contains("SEO")` đã được thay bằng RBAC-driven menu chưa?
4. `sign-up` + `set-password` đã có protection chưa?
5. Impersonation có audit log không?

> ⚠️ Khi user nhờ "thêm trang/endpoint mới": vẫn phải chủ động gắn RBAC permission ở controller. Đừng giả định mặc định đã enforce.
