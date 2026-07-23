---
phase: 4
title: "E2E SSO verify + runbook"
status: pending
effort: 3h
dependsOn: [2, 3]
---

# Phase 04 — E2E SSO verify + runbook

## Goal

Chứng minh SSO thật giữa Directus và ABP; ghi runbook local; cập nhật wiki.

## E2E test script

### A. Cold start

1. Restart Keycloak + Directus + AuthServer + Blazor  
2. Browser **clean profile** / incognito  

### B. Directus first

1. Mở http://localhost:8080 (hoặc login URL Directus)  
2. Login Keycloak `bacsi@benhvien.vn`  
3. Confirm role BacSi  
4. **Không** đóng browser  

### C. ABP second (SSO)

1. Tab mới → http://localhost:44306  
2. Login → Keycloak  
3. **Expect:** không hỏi password (session KC còn)  
4. Confirm IdentityUser + role `bacsi`  

### D. Reverse order (optional)

Incognito: ABP first → Directus second → cùng SSO behavior.

### E. Role matrix

Lặp B–C cho `admin@`, `lanhdao@`, `nhanvien@`.

### F. Logout best-effort

1. Keycloak Admin → Sessions → logout user  
2. Refresh Directus/ABP → expect re-auth (document actual behavior; full SLO = phase 1.5)

## Runbook deliverable

Tạo `docs/runbooks/local-sso-lab.md`:

- Start commands (KC, Directus, ABP infra/apps)  
- Ports table  
- Realm/clients/groups/users  
- Env keys (no secrets)  
- Callback URIs  
- Troubleshooting: RS256, redirect_uri, ROLE_MAPPING json:, discovery fail  

Update:

- `wiki/hot.md` — E2E result  
- `wiki/concepts/Keycloak Local Lab.md` — ports/clients final  
- `docs/workspace-architecture.md` — ports section nếu lệch  

## Success criteria

- [ ] SSO Directus→ABP không re-prompt password  
- [ ] 4 roles verified both apps  
- [ ] Runbook committed trong docs  
- [ ] Wiki hot updated  

## Exit criteria for Phase 1 complete

Tất cả success criteria `plan.md` checked → status `completed`.  
Zimbra LDAP = **plan riêng phase 2** (không nhét vào đây).
