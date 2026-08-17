# Plan: BD App Access Gate (Directus / ABP)

> Status: **completed**  
> Date: 2026-07-23

## Goal

Keycloak user có thể: chỉ Directus | chỉ ABP | cả 2. Default role vẫn **nhanvien** khi có app entitlement nhưng thiếu role group.

## Design

| Layer | Mechanism |
|-------|-----------|
| KC groups | `bd-app-axis`, `bd-app-hcs` + existing `bd-*` roles |
| Bootstrap | Create app groups; lab users get **both** app groups + role group |
| ABP | `OnTokenValidated`: thiếu `bd-app-hcs` → `context.Fail` |
| Directus | Hook extension: `auth.create`/`auth.update` reject nếu thiếu `bd-app-axis` |
| Default role | Giữ nhanvien (compose DEFAULT + ABP mapper fallback) **chỉ khi đã qua app gate** |

## Done checklist

- [x] Bootstrap app groups + multi-group users
- [x] ABP mapper + events gate
- [x] Directus extension + compose mount
- [x] Runbook / handoff / wiki
- [x] Re-bootstrap KC + recreate Directus; AuthServer build OK

## Out of scope

Keycloak SPI plugins, Zimbra, SLO, full permission matrix.
