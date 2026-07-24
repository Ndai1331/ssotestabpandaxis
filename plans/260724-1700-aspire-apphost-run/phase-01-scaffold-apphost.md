# Phase 01 — Scaffold Aspire AppHost

## Context

- Brainstorm: [brainstorm-260724-1659-aspire-apphost-run.md](../reports/brainstorm-260724-1659-aspire-apphost-run.md)
- Parent: [plan.md](./plan.md)
- ABP docs: Aspire integration (reference only — **không** full template retrofit)

## Overview

| | |
|--|--|
| Priority | P2 |
| Status | Completed |
| Effort | ~45–60m |
| Goal | Project AppHost tồn tại, restore/build OK, `dotnet run` mở Aspire Dashboard (chưa AddProject apps) |

## Requirements

- Path: `services/abp-blazor/aspire/hanhchinhso.AppHost/`
- Target: `net10.0`
- Aspire AppHost SDK + `Aspire.Hosting.AppHost` (pin version ổn định tương thích SDK 10 — verify lúc scaffold bằng `dotnet new aspire-apphost` hoặc docs hiện tại)
- **Không** thêm ServiceDefaults project ở phase này (YAGNI v1)
- **Không** sửa app/service Program.cs

## Related files

**Create:**

- `services/abp-blazor/aspire/hanhchinhso.AppHost/hanhchinhso.AppHost.csproj`
- `services/abp-blazor/aspire/hanhchinhso.AppHost/Program.cs` (minimal `DistributedApplication.CreateBuilder` → `Build().Run()`)
- `services/abp-blazor/aspire/hanhchinhso.AppHost/Properties/launchSettings.json` (dashboard URLs)
- `services/abp-blazor/aspire/hanhchinhso.AppHost/appsettings.json` (optional empty)

**Modify:** none outside `aspire/` (có thể thêm `.slnx` local cho AppHost nếu cần — optional)

## Implementation steps

1. Tạo thư mục `aspire/hanhchinhso.AppHost`.
2. Scaffold bằng một trong hai:
   - `dotnet new aspire-apphost -n hanhchinhso.AppHost -o aspire/hanhchinhso.AppHost` (nếu template có), **hoặc**
   - Hand-write csproj theo Aspire AppHost SDK docs hiện tại.
3. Pin Aspire package/SDK version trong csproj; ghi version đã chọn vào comment đầu `Program.cs`.
4. `Program.cs` tối thiểu:
   ```csharp
   var builder = DistributedApplication.CreateBuilder(args);
   builder.Build().Run();
   ```
5. `dotnet restore` + `dotnet build` project AppHost.
6. `dotnet run --project aspire/hanhchinhso.AppHost` → Aspire Dashboard mở (không lỗi).

## Todo

- [x] Scaffold AppHost project under `aspire/`
- [x] Pin Aspire SDK/package versions
- [x] Minimal Program.cs builds & runs dashboard
- [x] Record chosen Aspire version in comment / phase note

## Success criteria

- [x] `dotnet build` AppHost = 0 error
- [x] Dashboard reachable khi run AppHost trống
- [x] Không đụng `services/**` / `apps/**` code

## Risks

| Risk | Mitigation |
|------|------------|
| Template Aspire thiếu / version mismatch net10 | Fallback hand-write csproj; thử version từ nuget.org tương thích |
| Workload chưa cài | `dotnet workload install aspire` nếu CLI yêu cầu |

## Security

- Không commit secrets; AppHost chỉ orchestration local.

## Next

→ [Phase 02 — Wire light profile](./phase-02-wire-light-profile.md)
