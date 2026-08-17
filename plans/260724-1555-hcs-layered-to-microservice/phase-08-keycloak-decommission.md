---
phase: 8
title: Keycloak deepen and decommission HCS
status: pending
effort: 1-2w
dependsOn: [3, 4, 5, 6, 7]
---

# Phase 08 — Keycloak deepen + decommission HCS_web

## Goal

1. Đánh giá / triển khai bước “tiến tới Keycloak” (không bắt buộc Keycloak-only).
2. Tắt HCS_web khi parity checklist = 100% (hoặc accepted gaps documented).
3. Archive / README pointer sang `abp-blazor`.

## Keycloak options (chọn 1 khi cook)

| Option | Mô tả | Khi nào |
|--------|-------|---------|
| A | Giữ Approach A; harden groups/mappers; tắt `prompt=login` lab nếu muốn silent SSO | Default an toàn |
| B | Mobile clients lấy token KC trực tiếp (audience API) | Nếu mobile team yêu cầu |
| C | Giảm trách nhiệm OpenIddict (advanced) | Chỉ sau B ổn |

Zimbra LDAP = **plan SSO Phase 2 riêng** — có thể parallel nhưng không block decommission nếu lab users KC đủ.

## Decommission checklist

- [ ] Parity checklist all rows done hoặc waived w/ ticket
- [ ] Mobile E2E trên MS
- [ ] Data ETL production (nếu có) verified
- [ ] Stop HCS AuthServer/HttpApi/Blazor/workers
- [ ] Update `wiki/hot.md`, handoff, README workspace
- [ ] Optional: move `HCS_web` → `archive/` hoặc git submodule note

## Success criteria

- [ ] Chỉ `abp-blazor` + Directus + KC phục vụ HCS nghiệp vụ
- [ ] Decision recorded (A/B/C)
- [ ] HCS_web không còn trong cold-start runbook chính

## Risks

- Waive quá nhiều gaps → debt; require sign-off user
- Decommission sớm trước Phase 6/7 nếu product không cần chat/report — **allowed** nếu user waive
