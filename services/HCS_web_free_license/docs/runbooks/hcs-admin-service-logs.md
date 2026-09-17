# HCS admin service logs runbook

Màn hình `/administration/service-logs` gom **runtime Serilog** (Error / Warning / Information) từ các host HCS qua Seq. Đây **không** phải trang Audit logs.

| | Audit logs | Service logs |
|---|---|---|
| Route | `/administration/audit-logs` | `/administration/service-logs` |
| Nguồn | `HcsAuditRecordProjections` (HTTP/business audit) | Seq (stdout/Serilog) |
| Filter | user, IP, HTTP status, correlation | service All/một host, Error/Warning/Information, keyword |
| Permission | `HCS.AuditViewer` | `HCS.ServiceLogs` |

## Seq

- Local infra: `etc/docker-compose/local-infra.yml` bind `127.0.0.1:5341` (UI/ingestion). Không public qua Caddy/`hcs.localhost`.
- Full stack: service `seq` chỉ trong Docker network. App dùng `Seq__ServerUrl=http://seq`.
- Local `dotnet run`: `Seq:ServerUrl=http://localhost:5341` trong appsettings host.
- Lab mặc định `SEQ_FIRSTRUN_NOAUTHENTICATION=true`. Production nên đặt `HCS_SEQ_API_KEY` / `Seq:ApiKey`.
- Retention: cấu hình trong Seq UI (Settings). Local nên giữ khoảng 7 ngày để tránh đầy disk.

```bash
docker compose --env-file .env -f etc/docker-compose/local-infra.yml up -d seq
# Seq UI (ops only): http://127.0.0.1:5341
```

Mỗi host enrich property `Application` (allow-list): `HCS.AuthServer`, `HCS.WebGateway`, `HCS.Blazor`, `HCS.PlatformService`, `HCS.OrganizationService`, `HCS.DocumentService`, `HCS.WorkManagementService`, `HCS.CollaborationService`.

## API

- BFF: `GET /api/service-logs?applications=&levels=&filter=&maxResultCount=`
- Platform alias: `GET /api/hcs/service-logs`
- Browser **không** gọi Seq. Filter Seq được dựng server-side; raw Seq query từ client bị từ chối.
- Seq down: page báo lỗi, Platform không crash.

Admin role nhận `HCS.ServiceLogs` qua `HCSRolePermissionSynchronizer`. Sign out/in lại sau khi đổi quyền.
