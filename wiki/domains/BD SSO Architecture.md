---
type: domain
title: "BD SSO Architecture"
updated: 2026-07-23
---

# BD SSO Architecture

Mô hình: **Zimbra → Keycloak → Directus + ABP** (OIDC).

## Login flow
1. User mở Directus hoặc ABP  
2. Redirect Keycloak  
3. Keycloak auth qua Zimbra  
4. Zimbra OK  
5. Keycloak cấp ID + Access Token  
6. Redirect về app  
7. App verify token → session  

## Sync & mapping
- User Federation: Zimbra LDAP → Keycloak users/groups  
- Role map: Keycloak → Directus roles / ABP Identity roles  
- Xem [[OIDC Client Mapping]]

## DBs
| DB | Owner |
|----|-------|
| Keycloak DB | Identity |
| Directus DB | App data + permissions |
| ABP DBs | IdentityUsers, workflows, docs |

## SoT
`docs/workspace-architecture.md` + `system-sso-guideline.png`
