# Runbook: migrate một HCS module sang microservice

Áp dụng cho `services/HCS_web` → `services/abp-blazor`. Target pattern là `services/language`: Contracts + Host + Tests, cấu trúc flat.

## 1. Scaffold và data

1. Copy scaffold LanguageService và rename thành `hanhchinhso.{Module}Service`.
2. Dùng database riêng `hanhchinhso_{Module}`; entity tenant-aware có `TenantId` và ABP data filter.
3. Tạo EF migration mới. Không copy lịch sử migration layered.
4. Không đọc database service khác; dùng HTTP contract hoặc distributed event.

## 2. Wiring bắt buộc

| Vị trí | Việc cần làm |
|---|---|
| `hanhchinhso.abpsln` | Thêm module/project và metadata image/chart nếu dùng. |
| `etc/abp-studio/run-profiles/Default.abprun.json` | Thêm application, project path và launch profile. |
| `gateways/web/.../appsettings.json` | Thêm YARP route `/api/{module}-management/{**catch-all}` và cluster destination. |
| AuthServer `appsettings.json` | Thêm connection string nếu AuthServer chạy migration orchestration. |
| AuthServer module/seeder | Thêm API scope/resource đúng audience `{Module}Service`. Serialize thay đổi với plan Elsa. |
| Blazor module | Thêm Contracts module, remote service base URL và scope `{Module}Service`. |
| service `appsettings.json` | Authority `http://localhost:44372` cho local lab, audience và connection string riêng. |
| `Properties/launchSettings.json` | Dùng port đã khóa trong ADR. |

OpenIddict hiện đăng ký scope client ở `apps/blazor/hanhchinhso.Blazor/hanhchinhsoBlazorModule.cs`; gateway khai báo OAuth scopes trong `gateways/web/hanhchinhso.WebGateway/hanhchinhsoWebGatewayModule.cs`. Luôn đối chiếu LanguageService trước khi thêm service mới.

## 3. Permission và UI

- Permission mới: `HanhChinhSo.{Module}.{Action}`.
- Seed role lab tối thiểu: `admin`, `bacsi`, `lanhdao`, `nhanvien`; Keycloak gate: `bd-app-hcs`.
- Port UI theo `plans/260724-1555-hcs-layered-to-microservice/reports/blazorise-mud-map.md`.
- Cập nhật feature trong parity checklist theo `Pending → API → UI → Verified`.

## MinIO cho DocumentService

Thêm package `Volo.Abp.BlobStoring.Minio`, rồi cấu hình container trong module:

```csharp
Configure<AbpBlobStoringOptions>(options =>
{
    options.Containers.Configure("documents", container =>
    {
        container.UseMinio(minio =>
        {
            minio.EndPoint = configuration["MinIO:EndPoint"];
            minio.AccessKey = configuration["MinIO:AccessKey"];
            minio.SecretKey = configuration["MinIO:SecretKey"];
            minio.BucketName = configuration["MinIO:BucketName"];
            minio.WithSSL = configuration.GetValue<bool>("MinIO:WithSSL");
            minio.CreateBucketIfNotExists = true;
        });
    });
});
```

Local container endpoint là `minio:9000`; process chạy trực tiếp trên host dùng
`localhost:9000`. Credentials phải lấy từ environment/user secrets, không commit
secret production. Phase 3 phải kiểm tra object round-trip (put/get/delete), không
chỉ dựa vào liveness endpoint.

## 4. Verification

1. Build Contracts, Host, Tests và Blazor.
2. Chạy migration trên database service.
3. Start AuthServer, service, WebGateway và Blazor.
4. Kiểm tra health endpoint, Swagger/service API qua gateway, authentication và tenant isolation.
5. Kiểm thử role allow/deny và ít nhất một CRUD flow end-to-end.
6. Không đánh dấu `Verified` nếu chỉ build thành công.

## 5. Cutover

- HCS layered feature-freeze; chỉ bugfix trong thời gian parity.
- Chuyển traffic theo module sau khi feature inventory tương ứng đều `Verified`.
- Chỉ archive/tắt `HCS_web` ở Phase 8 sau khi toàn bộ M01–M67 được chứng minh parity.
