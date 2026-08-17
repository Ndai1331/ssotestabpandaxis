---
title: Journal — Directus v11 SSO Keycloak design approved
date: 2026-07-25
tags: [brainstorm, directus, keycloak, sso, architecture]
---

# Journal — Directus v11 SSO design approved (thay v12 lab)

## Context

Lab hiện tại dùng `services/directus-main` (v12.1.1, MSCL). Vấn đề: v12 yêu cầu license gate (`BD_LAB_ALLOW_SSO`=true) để bật runtime SSO, làm phức tạp local dev. v11.13.4 có OpenID driver built-in, **không runtime license gate**.

## What happened

Brainstorm hôm nay (2026-07-25 17:21) duyệt thiết kế chuyển lab sang v11. User chốt 3 quyết định:
1. **SoT mới**: thay v12 bằng v11 (`directus-main-v11`)
2. **App gate**: bắt buộc keep parity với ABP (`bd-app-axis` ≡ `bd-app-hcs`)
3. **Runtime**: Docker compose lab (Postgres + Redis + Keycloak + Axis)

## Technical decision

**Approach C** được duyệt:
- Port `docker-compose.bd-lab.yml` từ v12 → v11; bỏ `BD_LAB_ALLOW_SSO` env
- Copy `bd-lab-extensions/directus-extension-bd-app-gate/` + hook `auth.create` (đã test v11 compat)
- Port `.env.sso.example` không bypass license
- Keep `scripts/keycloak_bootstrap_bd_realm.py` (client/secret/groups không đổi)
- **Không port** `api/src/license/*` hay bypass code
- Archive v12 path; update docs SoT → v11

## Why this matters

**v11 thích hợp hơn v12 cho lab**:
- OpenID driver built-in (`api/src/auth/drivers/openid.ts`) → không cần ngoại patch
- Role mapping + group claim = pattern v12 lab
- **Bỏ được** runtime license bypass → code sạch, không đụng tới license layer, tương lai upgrade v13+ dễ hơn

**Design cost**:
- Docker build v11 cần verify Dockerfile
- Role UUID lần đầu cần manual seed sau first boot
- Extension port dù compatibility check OK, cần smoke test compose up

## The truth

Cảm giác là đúng quyết định. v12 đang "work" nhưng license bypass trong Directus codebase là **technical debt hôn nhân**. User muốn clean lab, v11 cung cấp đường thoát tự nhiên. Brainstorm short + focused, không quanh co, quyết định rõ ràng.

Chi phí implement dự kiến ~4-6h (port compose + extension + docs + smoke); mitigatable.

## Lessons extracted

1. **Built-in feature > runtime bypass**: Chọn version có native feature đồng nghĩa clean architecture. Tiếp theo, ưu tiên feature built-in > custom gate nếu có.
2. **License layer nên isolated**: Nếu cần gate license, tách thành decorator/middleware riêng, không làm stain auth flow.
3. **Docs SoT phải跟 tech decision**: v12 archive, v11 active → docs chỉ v11; không để 2 SoT gây confuse sau.

## Next steps

1. User decide `/ck:plan` vs `/cook` → phase 1 (compose + gate) vs full roadmap
2. Pending: Implement (ck:plan hoặc straight cook)
3. Success: Compose v11 lab up, KC bootstrap, gate require `bd-app-axis`, login via Keycloak ✓

## References

- Brainstorm report: `plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md`
- Phase OpenID (v1 design): `plans/260723-1419-bd-sso-phase1/phase-02-directus-openid.md`
- v12 compose (archive): `services/directus-main/docker-compose.bd-lab.yml`
- v11 fork: `services/directus-main-v11/` (ready structure)
