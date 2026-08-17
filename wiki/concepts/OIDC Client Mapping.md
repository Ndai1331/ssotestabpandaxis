---
type: concept
title: "OIDC Client Mapping"
updated: 2026-07-23
---

# OIDC Client Mapping

Keycloak phát hành token → mỗi app map claims sang permission nội bộ.

| App | Client (gợi ý tên) | Map tới |
|-----|--------------------|---------|
| Directus | `directus` | Roles / policies Directus |
| ABP | `abp` / `abp-blazor` | Identity roles / permissions ABP |

## Claims thường cần
- `email` / `preferred_username`  
- `name`  
- groups hoặc realm roles  

## Pitfalls
- Redirect URI không khớp client settings  
- Clock skew / wrong issuer URL (`localhost` vs hostname)  
- ABP AuthServer vs Keycloak: chọn một IdP “chân lý” — Keycloak cho BD lab  

Xem [[Keycloak Local Lab]], [[BD SSO Architecture]]
