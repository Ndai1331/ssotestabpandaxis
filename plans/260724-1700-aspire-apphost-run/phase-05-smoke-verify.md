# Phase 05 — Smoke verify + optional Studio parity

## Context

- Parent: [plan.md](./plan.md)
- Depends: Phase 04
- Optional: `etc/abp-studio/run-profiles/Default.abprun.json` — ElsaStudio chưa có

## Overview

| | |
|--|--|
| Priority | P2 |
| Status | Completed (with WorkflowService caveat) |
| Effort | ~45–60m |
| Goal | Chứng minh success metrics brainstorm; optional sync Elsa vào abprun |

## Requirements

### Smoke — light ✓ PASS

1. `./aspire/run.sh light` → all 6 apps started ✓
2. All apps healthy (Dashboard + manual curl) ✓
3. Browser: `http://localhost:44306` → Blazor responsive ✓
4. Confirm ports via lsof — all listening ✓

### Smoke — full (PARTIAL)

1. `./aspire/run.sh full` → light + 6 additional apps ✓
2. ElsaStudio `:44396` — **PASS** ✓
3. WorkflowService `:44395` — **FAILED** (known DI issue, out-of-scope) 
   - Pre-existing: AbpIdentityAccessTokenIssuer + IdentityUserManager not properly injected
   - This is a service-level concern, not AppHost orchestration
   - Does NOT block light profile or ElsaStudio
   - TODO: File separate issue for WorkflowService DI in identity layer

### Optional Studio parity ✓ DONE

ElsaStudio now in `Default.abprun.json`:

```json
"HanhChinhSo.ElsaStudio": {
  "type": "dotnet-project",
  "path": "../../../apps/elsa-studio/HanhChinhSo.ElsaStudio/HanhChinhSo.ElsaStudio.csproj",
  "launchUrl": "http://localhost:44396",
  "folder": "apps",
  "execution": { "order": 5 }
}
```

(Không bắt buộc cho SoT CLI; làm nếu còn thời gian — checkbox riêng.)

## Related files

**Modify (optional):**

- `etc/abp-studio/run-profiles/Default.abprun.json`

**Verify only:**

- Tất cả apps đã wire — không sửa code trừ bug pin-port tìm thấy khi smoke

## Implementation steps

1. Run light smoke checklist; ghi kết quả (pass/fail) vào phase todo.
2. Run full smoke checklist.
3. Nếu pin-port fail → fix trong AppHost (không đổi appsettings URLs).
4. Optional: thêm ElsaStudio vào abprun.
5. Tick success metrics trên plan.md / brainstorm.

## Todo

- [x] Light smoke: Blazor + AuthServer ports — PASS
- [x] Full smoke: ElsaStudio — PASS; WorkflowService known issue, out-of-scope
- [x] Fix bất kỳ port/proxy regression — none found
- [x] (Optional) ElsaStudio trong `Default.abprun.json` — DONE
- [x] Mark plan phases done / status completed khi OK

## Success criteria

- [x] `./aspire/run.sh light` → Blazor reachable, **không** cần Studio GUI
- [x] `./aspire/run.sh full` → ElsaStudio `:44396` working; WorkflowService `:44395` issue documented
- [x] Port map = bảng plan (verified all light + ElsaStudio)
- [x] Docs nhắc Keycloak riêng

## Risks

| Risk | Mitigation |
|------|------------|
| First-run migrate chậm / fail | Chạy services 1 lần trước hoặc chờ health; document |
| License Elsa restore fail | Đã biết từ plan Elsa — không phải regression AppHost |

## Security

- Smoke chỉ localhost; không ghi password vào report.

## Next

Plan complete → cook done; journal nếu session wrap. Service HCS mới sau này: **bắt buộc** thêm vào AppHost full (và light nếu core).
