# Phase 5 — Polish + verify

## Overview

- Priority: P2
- Status: pending
- Estimate: 0.5 day
- Goal: L10n, cleanup config, smoke end-to-end, cập nhật wiki/hot nếu cần.

## Requirements

1. Keys VI/EN: page title, field labels, password SSO warning, avatar errors, signature section.
2. Deprecate/document `Bff:AccountUrl` nếu không còn dùng (hoặc giữ cho link “advanced” ẩn — mặc định **không** expose trên UI).
3. Manual smoke checklist (local):
   - Dropdown: không Workspace; Manage → `/account`
   - Profile save + password local user
   - External user: password section disabled + message
   - Avatar upload → topbar img; delete → initials
   - Signature self không cần SigningExecute
   - Hard refresh Ctrl+Shift+R
4. Regression: `/document-signing` vẫn 403/redirect khi thiếu Execute; Administration signatures elevated ok.
5. Ghi chú ngắn trong `wiki/hot.md` nếu team dùng wiki cho HCS runtime.

## Success criteria

- Checklist smoke pass trên lab local.
- Không còn deep-link Manage AuthServer từ topbar.

## Handoff

Sau khi complete: cook/report + restart Platform + Document + Blazor host nếu cần; báo URL app cho user hard-refresh.
