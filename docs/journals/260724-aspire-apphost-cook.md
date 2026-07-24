# Aspire AppHost cook complete — light/full CLI runner

**Date**: 2026-07-24 17:25  
**Severity**: Low  
**Component**: abp-blazor / Aspire 13.4.6  
**Status**: Completed (WorkflowService caveat)  

## What Happened

Cooked plan `260724-1700-aspire-apphost-run`: AppHost + `run.sh` light|full, pin ports, docs/skills, Elsa in `Default.abprun.json`.

## The Brutal Truth

Light smoke sạch. Full orchestration OK nhưng **WorkflowService vẫn crash** vì DI Elsa Identity sẵn có — AppHost không phải thủ phạm. Đừng báo “full Elsa E2E xanh” khi Workflow chết.

## Technical Details

- Path: `services/abp-blazor/aspire/`
- SDK: Aspire.AppHost.Sdk **13.4.6**
- Pin: `launchProfileName: null` + `isProxied: false` + `ASPNETCORE_URLS`
- Light PASS: 44372/44392/44323/44391/44398/44306
- Full: extras + Elsa `:44396` PASS; Workflow `:44395` FTL IdentityUserManager DI
- Review High fixed: `ASPIRE_ALLOW_UNSECURED_TRANSPORT` in launchSettings `http`

## Lessons Learned

1. Aspire `$"..."` trong `WithEnvironment` = ReferenceExpression trap → dùng concat.
2. Smoke full ≠ Workflow healthy — tách orchestration vs app bugs.

## Next Steps

- Fix WorkflowService Elsa Identity DI (riêng).
- Optional: `docker compose --wait` trong `run.sh`.
- Hỏi user có muốn commit không.
