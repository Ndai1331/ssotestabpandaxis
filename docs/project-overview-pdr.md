# Project Overview & PDR — BD SSO Lab

> **Project:** Bình Dương (BD) — SSO Zimbra + Keycloak + Directus + ABP  
> **Status:** Local proof-of-concept  
> **Updated:** 2026-08-10

---

## 1. Vision

Cho phép nhân viên / bác sĩ / lãnh đạo đăng nhập **một lần bằng tài khoản mail Zimbra** và dùng được cả:

1. **Directus** — quản lý dữ liệu lâm sàng  
2. **ABP** — hành chính số (văn bản, phê duyệt)

Identity trung tâm: **Keycloak** (OIDC/OAuth2).

---

## 2. Goals (POC local)

| ID | Goal | Acceptance |
|----|------|------------|
| G1 | Keycloak chạy local | Admin UI `:5110` reachable |
| G2 | Realm + 2 OIDC client paths | Directus + HCS AuthServer external client configured |
| G3 | Directus login via Keycloak | Redirect → login → session Directus |
| G4 | HCS login via BFF | Protected UI → BFF → AuthServer → Keycloak → same HTTPS deep link |
| G5 | SSO giữa 2 app | Login app A, mở app B không nhập lại password (cùng session Keycloak) |
| G6 | (Optional) LDAP Zimbra | User Federation sync user từ Zimbra khi có LDAP |
| G7 | Chat authorization | `/chat` is visible/usable only with `Collaboration.Chat` |

---

## 3. Non-goals (hiện tại)

- GitHub org, CI/CD, deploy staging/prod  
- Migrate full nghiệp vụ bệnh viện vào Directus/ABP  
- Thay thế hoàn toàn OpenIddict nội bộ ABP trong ngày 1 (có thể dùng external IdP song song)  
- Giữ vận hành stack Task9  

---

## 4. Actors

| Actor | Nhu cầu |
|-------|---------|
| End user (NV/BS/LĐ) | Login bằng mail Zimbra, vào cả 2 hệ thống |
| Admin IT | Quản lý user/group/role tại Keycloak |
| Developer / Agent | Cấu hình OIDC, document, test local |

---

## 5. Constraints

- Local machine + Docker  
- Zimbra LDAP có thể chưa sẵn — POC cho phép user Keycloak local trước  
- Docs/rules agent phải phản ánh runtime `services/directus-main-v11` + `services/HCS_web_free_license`; `services/abp-blazor` chỉ là tham chiếu lịch sử.

---

## 6. Success metrics (lab)

- [ ] 2 apps authenticate qua cùng Keycloak realm  
- [ ] Role mapping demo (ít nhất 1 role Keycloak → 1 role mỗi app)  
- [ ] Docs (`workspace-architecture`, `CLAUDE.md`, wiki) khớp thực tế  

---

## 7. Risks

| Risk | Mitigation |
|------|------------|
| ABP AuthServer vs Keycloak trùng vai trò IdP | Chọn rõ: Keycloak = IdP; ABP AuthServer trust external; document quyết định |
| Redirect URI mismatch | Checklist client settings trước mỗi test |
| Open redirect hoặc token exposure | Gateway validates configured return origins; browser uses an HTTP-only BFF cookie |
| Docs Task9 gây nhiễu agent | Sunset rules trong CLAUDE; wiki index chỉ BD |

---

## 8. References

- `system-sso-guideline.png`  
- `docs/workspace-architecture.md`  
- Directus OpenID docs (upstream)  
- ABP external provider / OpenIddict docs (upstream)  
