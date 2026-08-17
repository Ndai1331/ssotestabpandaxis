---
title: "Aspire AppHost — run abp-blazor một lệnh (light/full)"
description: "Thêm .NET Aspire AppHost orchestrate apps với pin port; profile light (default) + full+Elsa; infra giữ etc/docker; không phụ thuộc ABP Studio."
status: completed
priority: P2
effort: 4-6h
branch: main
tags: [feature, infra, aspire, abp, dx, lab]
blockedBy: []
blocks: []
relatedPlans:
  - 260724-1542-elsa-workflow-service
  - 260724-1555-hcs-layered-to-microservice
created: 2026-07-24
createdBy: ck:plan
source: plans/reports/brainstorm-260724-1659-aspire-apphost-run.md
completedBy: pm:sync-back
completedAt: 2026-07-24
---

# Plan — Aspire AppHost run-once (`abp-blazor`)

## Overview

Thêm **.NET Aspire AppHost** vào `services/abp-blazor` để start stack local **một lệnh CLI**, không cần ABP Studio GUI.

- Approach **A** (đã approve): AppHost orchestrate .NET projects; **infra giữ** `etc/docker`.
- Profiles: **`light`** (default) / **`full`** (+ ElsaStudio `:44396`).
- **Pin port** khớp appsettings (OIDC/YARP) — `isProxied: false`.
- SoT CLI = AppHost; giữ `Default.abprun.json` cho Studio.

## Cross-plan

| Relationship | Plan | Note |
|--------------|------|------|
| Related (done) | [Elsa WorkflowService](../260724-1542-elsa-workflow-service/plan.md) | Full profile phải start Workflow+ElsaStudio |
| Orthogonal | [HCS MS migration](../260724-1555-hcs-layered-to-microservice/plan.md) | Service mới sau này → thêm vào AppHost (convention) |
| None blocking | — | `blockedBy: []` |

## Locked decisions

| ID | Value |
|----|-------|
| D1 | AppHost-only v1 (không Aspire-owned Postgres/Redis) |
| D2 | Pin ports; `AddProject(..., launchProfileName: null)` + `WithHttpEndpoint(..., isProxied: false)` |
| D3 | `light` default; `full` = light + Audit/Gdpr/AI/Org/Workflow/ElsaStudio |
| D4 | Keycloak ngoài AppHost |
| D5 | ServiceDefaults = optional / skip v1 trừ khi scaffold template bắt buộc |
| D6 | Wrapper `aspire/run.sh light\|full` |

## Profile map (SoT)

**light:** AuthServer `44372`, Identity `44392`, Administration `44323`, Language `44391`, WebGateway `44398`, Blazor `44306`  
**Infra light:** postgresql + redis + rabbitmq  

**full:** + AuditLogging `44302`, Gdpr `44348`, AIManagement `44318`, Organization `44370`, WorkflowService `44395`, ElsaStudio `44396`  
**Infra full:** `etc/docker/up.ps1` (all containers)

## Phases

|| Phase | Name | Status |
||-------|------|--------|
|| 1 | [Scaffold AppHost](./phase-01-scaffold-apphost.md) | Completed |
|| 2 | [Wire light profile](./phase-02-wire-light-profile.md) | Completed |
|| 3 | [Wire full + Elsa](./phase-03-wire-full-elsa.md) | Completed |
|| 4 | [run.sh + docs](./phase-04-run-script-docs.md) | Completed |
|| 5 | [Smoke verify](./phase-05-smoke-verify.md) | Completed (with WorkflowService caveat) |

### Phase one-liners

| # | Detail |
|---|--------|
| 01 | Tạo `aspire/hanhchinhso.AppHost` + Aspire SDK; empty host + dashboard. |
| 02 | Wire 6 projects light + pin ports + WaitFor. |
| 03 | `--profile full` + 6 apps còn lại + ElsaStudio. |
| 04 | `run.sh` + docker light/full + README/start-local. |
| 05 | Smoke light + full; optional Elsa trong `Default.abprun.json`. |

## Component ↔ file ownership

| Vùng | Path | Phase |
|------|------|-------|
| AppHost project | `services/abp-blazor/aspire/hanhchinhso.AppHost/**` | 01–03 |
| Wrapper script | `services/abp-blazor/aspire/run.sh` | 04 |
| Docs | `services/abp-blazor/aspire/README.md`, skill/runbook touch nhẹ | 04 |
| Optional Studio parity | `etc/abp-studio/run-profiles/Default.abprun.json` | 05 |
| Apps/services code | **không sửa** Program.cs/modules (v1) | — |

## Test matrix

| Level | What | Where |
|-------|------|-------|
| Build | `dotnet build aspire/hanhchinhso.AppHost` | CLI |
| Light | Dashboard + 6 apps đúng port; Blazor mở `:44306` | Browser |
| Full | + Elsa `:44396`, Workflow `:44395` health | Browser/curl |
| Regression | AuthServer CORS/OIDC vẫn dùng cổng cũ | Login smoke |

## Risks

| Risk | Mitigation |
|------|------------|
| Aspire proxy đổi port → SSO/YARP gãy | `launchProfileName: null` + `isProxied: false` + `ASPNETCORE_URLS` cố định |
| Aspire SDK version drift (.NET 10) | Pin version ổn định khi scaffold; ghi trong phase-01 |
| Full RAM nặng | Default light |
| Drift vs `abprun` | Comment SoT; phase-05 optional sync Elsa |

## Out of scope

- Aspire AddPostgres/AddRedis thay docker
- ServiceDefaults OTEL mọi service
- Keycloak trong AppHost
- Xóa ABP Studio runner
- Dockerize apps

## Dependencies

- .NET 10 SDK, Docker, (optional) `pwsh` cho `up.ps1`
- Keycloak `:5110` khi test SSO (ngoài plan)
- Brainstorm: `plans/reports/brainstorm-260724-1659-aspire-apphost-run.md`

## Cook

```bash
/cook /Users/user/Documents/bd-workspace/plans/260724-1700-aspire-apphost-run/plan.md --auto
```
