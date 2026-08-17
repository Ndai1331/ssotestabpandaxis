# Phase 02 — Wire light profile (pin ports)

## Context

- Parent: [plan.md](./plan.md)
- Depends: Phase 01 done
- Ports SoT: launchSettings + appsettings (AuthServer CORS đã list các cổng này)

## Overview

| | |
|--|--|
| Priority | P2 |
| Status | Completed |
| Effort | ~60–90m |
| Goal | `light` start 6 apps đúng cổng cố định; order WaitFor hợp lý |

## Requirements

**Apps (light):**

| Resource name | Project path | Port |
|---------------|--------------|------|
| auth-server | `apps/auth-server/hanhchinhso.AuthServer/hanhchinhso.AuthServer.csproj` | 44372 |
| identity | `services/identity/hanhchinhso.IdentityService/hanhchinhso.IdentityService.csproj` | 44392 |
| administration | `services/administration/hanhchinhso.AdministrationService/hanhchinhso.AdministrationService.csproj` | 44323 |
| language | `services/language/hanhchinhso.LanguageService/hanhchinhso.LanguageService.csproj` | 44391 |
| web-gateway | `gateways/web/hanhchinhso.WebGateway/hanhchinhso.WebGateway.csproj` | 44398 |
| blazor | `apps/blazor/hanhchinhso.Blazor/hanhchinhso.Blazor.csproj` | 44306 |

**Pin-port pattern (bắt buộc mỗi project):**

```csharp
// Suppress launch-profile endpoint injection; disable Aspire proxy.
var auth = builder.AddProject<Projects.hanhchinhso_AuthServer>(
        "auth-server",
        launchProfileName: null)
    .WithHttpEndpoint(port: 44372, targetPort: 44372, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:44372")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
```

*(Tên `Projects.*` theo Aspire project reference naming — điều chỉnh sau khi AddProjectReference.)*

**Start order / WaitFor:**

1. identity, administration, language (parallel OK)
2. auth-server waits for identity (+ admin nếu seed phụ thuộc)
3. web-gateway waits for auth-server + backends light
4. blazor waits for web-gateway (+ auth-server)

**Assumptions:** Docker infra (postgres/redis/rabbitmq) **đã chạy** — chưa có run.sh (phase 04).

## Related files

**Modify:**

- `aspire/hanhchinhso.AppHost/Program.cs`
- `aspire/hanhchinhso.AppHost/hanhchinhso.AppHost.csproj` (ProjectReference × 6)

**Create (optional helper):**

- `aspire/hanhchinhso.AppHost/Hosting/PinnedProjectExtensions.cs` — helper `AddPinnedHttpProject(...)` tránh lặp

## Implementation steps

1. Add ProjectReference từ AppHost tới 6 csproj trên.
2. Implement helper `AddPinnedHttpProject` (path hoặc generic) với `port`, `isProxied: false`, `ASPNETCORE_URLS`.
3. Register 6 resources; gắn `WaitFor` theo order trên.
4. Parse profile: nếu chưa có arg, default **light** (chỉ register 6 apps — full ở phase 03).
5. `dotnet run` AppHost → verify từng URL:
   - `http://localhost:44372/health-status` (hoặc `/`)
   - `http://localhost:44398/health-status`
   - `http://localhost:44306/`
6. Confirm Aspire Dashboard **không** gán port khác (URL = cổng bảng).

## Todo

- [x] ProjectReferences × 6
- [x] Pin-port helper + wire light apps
- [x] WaitFor chain
- [x] Manual smoke ports (curl/browser)
- [x] Document: infra must be up before run

## Success criteria

- [x] Tất cả 6 process Running trên đúng port
- [x] `curl -s -o /dev/null -w '%{http_code}' http://localhost:44306` không connection-refused
- [x] Không đổi bất kỳ appsettings URL nào

## Risks

| Risk | Mitigation |
|------|------------|
| Proxy vẫn bật → conflict bind | `launchProfileName: null` + `isProxied: false` + set `ASPNETCORE_URLS` |
| Project name có dấu `.` → Projects class lạ | Dùng alias / kiểm tra generated Projects namespace |
| App fail vì thiếu DB | Nhắc user chạy docker trước; không block phase nếu lỗi connection (ghi note) |

## Security

- Chỉ HTTP localhost lab; không expose external endpoints.

## Next

→ [Phase 03 — Full + Elsa](./phase-03-wire-full-elsa.md)
