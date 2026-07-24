# Aspire AppHost plan — run abp-blazor một lệnh

**Date**: 2026-07-24 17:00  
**Severity**: Low  
**Component**: abp-blazor DX / .NET Aspire  
**Status**: Planned  

## What Happened

Brainstorm chốt Approach A (AppHost-only, infra docker ngoài, light/full). Viết plan `plans/260724-1700-aspire-apphost-run` (5 phases, 4–6h).

## The Brutal Truth

Aspire mặc định proxy/đổi port sẽ **phá** OIDC + YARP nếu không pin. Toàn bộ plan xoay quanh `launchProfileName: null` + `isProxied: false` — bỏ qua = smoke fail ngay phase 02.

## Technical Details

- SoT CLI: `aspire/hanhchinhso.AppHost` + `./aspire/run.sh`
- light = 6 apps; full = +Audit/Gdpr/AI/Org/Workflow/ElsaStudio
- Không retrofit ServiceDefaults / Aspire-owned Postgres ở v1
- Cross-plan: không block HCS migration; related Elsa plan đã completed

## Lessons Learned

1. Brainstorm trước → plan cookable nhanh (fast mode đủ vì decision đã lock).
2. Pin-port phải là hard rule trong phase, không “nice to have”.

## Next Steps

```bash
/cook /Users/user/Documents/bd-workspace/plans/260724-1700-aspire-apphost-run/plan.md --auto
```
