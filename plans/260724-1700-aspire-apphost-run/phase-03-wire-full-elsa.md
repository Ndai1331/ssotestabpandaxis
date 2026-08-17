# Phase 03 — Wire full profile + ElsaStudio

## Context

- Parent: [plan.md](./plan.md)
- Depends: Phase 02
- Elsa Studio path: `apps/elsa-studio/HanhChinhSo.ElsaStudio/HanhChinhSo.ElsaStudio.csproj` (`:44396`)
- WorkflowService: `services/workflow-service/hanhchinhso.WorkflowService/` (`:44395`)

## Overview

| | |
|--|--|
| Priority | P2 |
| Status | Pending |
| Effort | ~45–60m |
| Goal | `--profile full` start thêm 6 apps; ElsaStudio trong full only |

## Requirements

**Additional apps (full only):**

| Resource | Project | Port |
|----------|---------|------|
| audit-logging | `services/audit-logging/hanhchinhso.AuditLoggingService/...csproj` | 44302 |
| gdpr | `services/gdpr/hanhchinhso.GdprService/...csproj` | 44348 |
| ai-management | `services/ai-management/hanhchinhso.AIManagementService/...csproj` | 44318 |
| organization | `services/organization/hanhchinhso.OrganizationService/...csproj` | 44370 |
| workflow | `services/workflow-service/hanhchinhso.WorkflowService/...csproj` | 44395 |
| elsa-studio | `apps/elsa-studio/HanhChinhSo.ElsaStudio/...csproj` | 44396 |

**Profile switch:**

```csharp
// Prefer: args after -- ; fallback env HCS_RUN_PROFILE
var profile = GetProfile(args); // "light" | "full", default light
if (profile == "full") { /* register extras */ }
```

CLI:

```bash
dotnet run --project aspire/hanhchinhso.AppHost -- --profile full
```

**WaitFor (full extras):**

- workflow waits for auth-server (+ identity)
- elsa-studio waits for workflow + auth-server
- gateway (đã có) — optional `WaitFor` organization/workflow nếu YARP cần lúc start (thường không bắt buộc)

Cùng pin-port pattern như phase 02.

## Related files

**Modify:**

- `aspire/hanhchinhso.AppHost/Program.cs`
- `aspire/hanhchinhso.AppHost/hanhchinhso.AppHost.csproj` (+6 ProjectReference)

**Create (optional):**

- `aspire/hanhchinhso.AppHost/Hosting/RunProfile.cs` — parse `--profile` / env

## Implementation steps

1. Add ProjectReferences cho 6 projects full.
2. Implement `GetProfile(args)` — default `light`; accept `full`; reject unknown → fail fast message.
3. Wrap full-only `AddPinnedHttpProject` trong `if (profile == "full")`.
4. WaitFor: ElsaStudio after Workflow + AuthServer.
5. Run `--profile light` → Dashboard **không** list Elsa/Workflow (hoặc not started).
6. Run `--profile full` → verify `:44395/health-status`, `:44396/`.

## Todo

- [x] ProjectReferences × 6 full apps
- [x] Profile parser (`--profile` + env)
- [x] Conditional registration + WaitFor
- [x] Verify light excludes full apps
- [x] Verify full includes ElsaStudio `:44396`

## Success criteria

- [x] `light` = đúng 6 apps (± infra)
- [x] `full` = light + 6 apps kể cả ElsaStudio
- [x] Ports không đổi so với bảng plan

## Risks

| Risk | Mitigation |
|------|------------|
| Blazor WASM Studio needs different hosting | Vẫn `AddProject` như host WASM; smoke phase 05 |
| AIManagement needs Ollama/pgvector | Document full infra; app có thể degrade — không fail AppHost nếu optional |
| Organization chưa migrate xong | Vẫn wire; crash riêng không block AppHost design |

## Security

- Không thêm redirect URI mới — dùng config hiện có.

## Next

→ [Phase 04 — run.sh + docs](./phase-04-run-script-docs.md)
