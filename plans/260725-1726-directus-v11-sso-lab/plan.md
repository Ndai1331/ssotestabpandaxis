---
title: "Directus v11 SSO lab — replace v12"
description: "Port Keycloak SSO + bd-app-axis gate + Docker compose lab sang directus-main-v11; cập nhật docs SoT."
status: completed
priority: P1
effort: 5h
branch: main
tags: [auth, infra, docs, feature]
blockedBy: []
blocks: []
created: 2026-07-25
brainstorm: plans/reports/brainstorm-260725-1721-directus-v11-sso-keycloak.md
---

# Directus v11 SSO lab — replace v12

## Overview

Chuyển lab Directus SoT từ `services/directus-main` (v12.1.1 + license SSO gate) sang `services/directus-main-v11` (11.13.4, không runtime license SSO). Port compose lab, hook `bd-app-axis`, env OpenID; reuse Keycloak bootstrap; cập nhật runbook/docs. **Không** mang `BD_LAB_ALLOW_SSO` / `api/src/license/*`.

## Cross-Plan Dependencies

| Relationship | Plan | Status |
|-------------|------|--------|
| Builds on | [BD SSO Phase 1](../260723-1419-bd-sso-phase1/plan.md) | completed |
| Builds on | [BD App Access Gate](../260723-1555-bd-app-access-gate/plan.md) | completed |
| Orthogonal | [HCS→MS roadmap](../260724-1555-hcs-layered-to-microservice/plan.md) | pending — no file overlap |

## Decisions (locked)

| # | Decision |
|---|----------|
| D1 | SoT lab = `directus-main-v11`; v12 = archive |
| D2 | Gate `bd-app-axis` bắt buộc (parity ABP) |
| D3 | Docker compose lab (PG+Redis+KC+Axis) |
| D4 | Không port license bypass code |

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | [Port lab artifacts](./phase-01-port-lab-artifacts.md) | Completed |
| 2 | [Boot + roles + ROLE_MAPPING](./phase-02-boot-roles-mapping.md) | Completed |
| 3 | [Smoke SSO + app gate](./phase-03-smoke-sso-gate.md) | Completed |
| 4 | [Docs SoT + archive v12](./phase-04-docs-sot-archive.md) | Completed |

Phase effort (sum ≈ 5h): P1 1h · P2 1.5h · P3 1h · P4 1.5h

## Success criteria (plan-level)

- [x] `docker compose -f docker-compose.bd-lab.yml up -d` trong v11
- [x] Login KC users → đúng role; thiếu `bd-app-axis` → deny
- [x] ABP cùng realm/clients không đổi
- [x] Docs/runbook chỉ path `directus-main-v11`
- [x] Không có `BD_LAB_ALLOW_SSO` trên v11

## Key source → target

| Source (v12) | Target (v11) |
|--------------|--------------|
| `services/directus-main/docker-compose.bd-lab.yml` | `services/directus-main-v11/docker-compose.bd-lab.yml` |
| `services/directus-main/bd-lab-extensions/...` | same path under v11 |
| `services/directus-main/.env.sso.example` | same under v11 |
| `scripts/keycloak_bootstrap_bd_realm.py` | reuse (no move) |

## Out of scope

- Zimbra LDAP, SLO, multi-group priority sort trong OpenID driver
- Đổi ABP AuthServer / clients / secrets
- Xóa vật lý thư mục `directus-main` (chỉ archive bằng docs)

## Cook

```text
/ck:cook --auto /Users/user/Documents/bd-workspace/plans/260725-1726-directus-v11-sso-lab/plan.md
```
