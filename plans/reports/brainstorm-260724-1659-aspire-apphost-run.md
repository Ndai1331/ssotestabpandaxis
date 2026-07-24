---
title: Brainstorm — Aspire AppHost run-once for abp-blazor
date: 2026-07-24
status: approved
slug: 260724-1659-aspire-apphost-run
sources:
  - services/abp-blazor
  - services/abp-blazor/etc/abp-studio/run-profiles/Default.abprun.json
  - services/abp-blazor/etc/docker
  - https://abp.io/docs/latest/solution-templates/microservice/aspire-integration
---

# Brainstorm — Aspire AppHost chạy 1 lần (`abp-blazor`)

**Date:** 2026-07-24  
**Status:** Approved (Approach A)  
**Project:** BD / hanhchinhso microservice — local DX

---

## Problem

`services/abp-blazor` = ABP microservice (~11–12 .NET apps + Elsa Studio). “Run all” hiện:

| Cơ chế | Gap |
|--------|-----|
| ABP Studio + `Default.abprun.json` | Cần GUI; ElsaStudio **chưa** trong profile |
| `etc/docker/up.ps1` | Chỉ infra |
| Nhiều `dotnet run` | Không 1 lệnh CLI |

**Không có** .NET Aspire AppHost. Lab cần chạy **không phụ thuộc Studio**, có **profile nhẹ** + khả năng **full + Elsa**.

## Requirements (user)

| # | Yêu cầu |
|---|---------|
| R1 | Aspire **AppHost thật** (không chỉ script) |
| R2 | Profile **full + ElsaStudio** (`:44396`) |
| R3 | Không cần ABP Studio GUI |
| R4 | Có **profile nhẹ** (default) để tiết kiệm RAM |

## Decisions (approved 2026-07-24)

| ID | Quyết định | Giá trị |
|----|------------|---------|
| D1 | Approach | **A — AppHost-only** (+ optional ServiceDefaults phase-2) |
| D2 | Infra v1 | Giữ `etc/docker` compose; **không** migrate AddPostgres/AddRedis vào Aspire |
| D3 | Ports | **Pin cứng** khớp `launchSettings` / appsettings (OIDC + YARP) |
| D4 | Profiles | `light` (default) + `full` qua AppHost args/env |
| D5 | Keycloak | Ngoài AppHost — compose Directus `:5110` |
| D6 | Studio | Giữ `Default.abprun.json`; SoT CLI = AppHost |
| D7 | ServiceDefaults | Optional phase-2 — không bắt buộc mọi `Program.cs` ở v1 |

## Approaches evaluated

| | Approach | Verdict |
|--|----------|---------|
| A | AppHost orchestrate .NET; infra docker ngoài; light/full args | **Chọn** — KISS, đáp ứng R1–R4, ít đụng Module |
| B | Full ABP Aspire template (containers code-first + OTEL mọi service) | Reject v1 — scope L–XL, dễ phá connection/port |
| C | Script spawn only (pseudo-Aspire) | Reject — lệch R1 |

## Target layout

```
services/abp-blazor/
  aspire/
    hanhchinhso.AppHost/           # entry: dotnet run
    hanhchinhso.ServiceDefaults/   # optional phase-2
    run.sh                         # light|full → ensure docker + AppHost
```

## Profile map

### `light` (default)

| Component | Port |
|-----------|------|
| AuthServer | 44372 |
| IdentityService | 44392 |
| AdministrationService | 44323 |
| LanguageService | 44391 |
| WebGateway | 44398 |
| Blazor | 44306 |

Infra tối thiểu: **postgres + redis + rabbitmq**.

### `full`

light + AuditLogging `44302`, Gdpr `44348`, AIManagement `44318`, Organization `44370`, WorkflowService `44395`, **ElsaStudio `44396`**.

Infra: như `up.ps1` khi cần (ES/Grafana/MinIO/Ollama…).

### Lệnh mục tiêu

```bash
cd services/abp-blazor
./aspire/run.sh light    # default
./aspire/run.sh full
# hoặc:
dotnet run --project aspire/hanhchinhso.AppHost -- --profile full
```

## Hard rules (AppHost)

1. Mọi `AddProject` **pin port** — không để Aspire proxy đổi URL.
2. Start order / `WaitFor`: Identity+Admin(+Language) → AuthServer → Gateway → Blazor / (full: Workflow → ElsaStudio).
3. v1 **không** rewrite connection strings — giả định localhost docker đã up.
4. Thêm service mới → cập nhật AppHost (+ ideally sync note với `Default.abprun.json`).

## Out of scope v1

- Aspire-owned infra containers thay `etc/docker`
- Bắt buộc ServiceDefaults / OpenTelemetry mọi service
- Xóa ABP Studio run profile
- Dockerize apps
- Nhúng Keycloak vào AppHost

## Risks

| Risk | Mitigation |
|------|------------|
| Aspire đổi port → SSO/YARP fail | Pin port + smoke URL sau start |
| Full ~12 process — RAM Mac | Default `light` |
| Drift AppHost vs `abprun` | SoT CLI = AppHost; comment sync khi add service |
| ElsaStudio WASM quirks | Treat as project; smoke `:44396` |

## Success metrics

- [ ] `./aspire/run.sh light` → Blazor login AuthServer **không** mở Studio
- [ ] `./aspire/run.sh full` → ElsaStudio `:44396` + Workflow `:44395` sống
- [ ] Port map không đổi so với hiện tại
- [ ] Keycloak vẫn start riêng (document trong run.sh / README)

## Next steps

1. `/ck:plan` — phase: scaffold AppHost → wire light → wire full+Elsa → run.sh + docs → smoke
2. Optional: thêm ElsaStudio vào `Default.abprun.json` (parity Studio)
3. Phase-2: ServiceDefaults nếu cần dashboard OTEL sâu hơn

## Dependencies

- .NET 10 SDK + Aspire workload / packages tương thích
- Docker local (infra)
- Keycloak (SSO lab) ngoài stack
- ABP license (Elsa packages) — đã có cho Workflow/Studio

---

*Approved design A — 2026-07-24*
