---
type: decision
title: "SSO Phase 1 Approach A"
updated: 2026-07-23
---

# Decision — SSO Phase 1 Approach A

**Approved:** 2026-07-23

| | |
|--|--|
| ABP | AuthServer federate Keycloak (Approach A) |
| IdP users | Keycloak local trước; Zimbra sau |
| Hosts | localhost ports |
| Roles | admin, bác sĩ, lãnh đạo, nhân viên |

KC groups: `bd-admin`, `bd-bacsi`, `bd-lanhdao`, `bd-nhanvien`

Report: `plans/reports/brainstorm-260723-1415-bd-sso-login-flow.md`  
Related: [[BD SSO Architecture]], [[OIDC Client Mapping]], [[Keycloak Local Lab]]
