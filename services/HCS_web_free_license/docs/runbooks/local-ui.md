# Chạy HCS local (Blazor + API + seed DB)

Hướng dẫn chạy **Blazor trên máy** (`https://localhost:44403`) gọi Gateway/Auth/services local. Không trỏ production (`api-hcs.htltech.vn`).

Stack Docker Compose `https://hcs.localhost` (`./scripts/docker-up.sh`) là luồng khác — xem README gốc.

## Cần có sẵn

- .NET 10 SDK
- Docker Desktop
- `python3` (script tách process host)
- Copy `.env` (không commit)

```bash
cd services/HCS_web_free_license
dotnet dev-certs https --trust
cp .env.example .env
chmod 600 .env
```

Sửa mọi `change-me` trong `.env`. Các biến **bắt buộc** cho local UI:

| Biến | Dùng cho |
|------|----------|
| `HCS_POSTGRES_PASSWORD` | Postgres |
| `HCS_RABBITMQ_USER` / `HCS_RABBITMQ_PASSWORD` | RabbitMQ |
| `HCS_MINIO_ROOT_USER` / `HCS_MINIO_ROOT_PASSWORD` | MinIO |
| `HCS_GATEWAY_CLIENT_SECRET` | OpenIddict client `HCS_App` + Gateway |
| `HCS_ADMIN_PASSWORD` | Seed user `admin` / `admin@abp.io` |

`HCS_GATEWAY_CLIENT_SECRET` và secret Gateway phải **trùng nhau** (script map cả hai từ cùng biến).

## Lệnh nhanh (khuyến nghị)

Từ `services/HCS_web_free_license`:

```bash
chmod +x scripts/run-local-ui.sh scripts/dev-local-env.sh
./scripts/run-local-ui.sh
```

Lệnh này lần lượt:

1. Bật Postgres / Redis / RabbitMQ / MinIO
2. Chạy DbMigrator (schema + seed)
3. Chạy Auth, Platform, Organization, Document, Work, Collaboration, Gateway
4. `dotnet watch` Blazor

Mở **https://localhost:44403** → login `admin@abp.io` / giá trị `HCS_ADMIN_PASSWORD`.

Dừng host .NET (giữ volume Docker):

```bash
./scripts/run-local-ui.sh stop
```

## Lệnh từng bước

Khi muốn tách infra / seed / API / UI:

```bash
# 1. Infra
./scripts/run-local-ui.sh infra

# 2. Migrate + seed (Identity, OpenIddict HCS_App, roles)
./scripts/run-local-ui.sh migrate

# 3. Auth + Gateway + microservices
./scripts/run-local-ui.sh backend

# 4. Blazor hot reload (terminal riêng)
source scripts/dev-local-env.sh
dotnet watch --project src/HCS.Blazor/HCS.Blazor.csproj
```

Seed thủ công (nếu không dùng script `migrate`) — **phải `cd` vào project DbMigrator**, không chạy `dotnet run --project` từ root:

```bash
source scripts/dev-local-env.sh
cd src/HCS.DbMigrator
dotnet run --no-launch-profile
```

Chạy DbMigrator từ root repo sẽ **không** load `appsettings.json` của migrator → bảng `OpenIddictApplications` trống → login lỗi `client_id is invalid` (OpenIddict ID2052).

## Cổng

| URL | Dịch vụ |
|-----|---------|
| https://localhost:44403 | Blazor UI |
| https://localhost:44402 | Gateway / BFF |
| https://localhost:44401 | AuthServer |
| https://localhost:44411 | Platform |
| https://localhost:44412 | Organization |
| https://localhost:44413 | Document |
| https://localhost:44414 | Work Management |
| https://localhost:44415 | Collaboration |
| `127.0.0.1:5432` hoặc `:5433` | Postgres |
| `127.0.0.1:6379` | Redis |
| `127.0.0.1:5672` | RabbitMQ |
| `127.0.0.1:9000` | MinIO |

Nếu máy đã có Postgres khác trên `5432`, script tự dùng **`5433`**. Ghi đè:

```bash
export HCS_POSTGRES_PORT=5433
./scripts/run-local-ui.sh
```

Script `dev-local-env.sh` export connection string, Redis, OpenIddict `HCS_App`, MinIO, RabbitMQ và **tắt Keycloak** (`Authentication__Keycloak__Enabled=false`) để login local bằng user seed.

## Seed tạo gì

DbMigrator migrate `hcs_identity` rồi seed:

- User `admin` / `admin@abp.io` (password = `HCS_ADMIN_PASSWORD`)
- OpenIddict client **`HCS_App`** (confidential) — redirect `https://localhost:44402/signin-oidc`
- Client `HCS_Swagger`, `hcs-mobile` (nếu cấu hình trong appsettings)
- Scope `HCS`

Các DB khác (`hcs_organization`, `hcs_document`, `hcs_work`, `hcs_collaboration`) được `CREATE DATABASE` lúc Postgres init; schema do từng service migrate khi start.

Kiểm tra client sau seed:

```bash
source scripts/dev-local-env.sh
# psql hoặc GUI: SELECT "ClientId" FROM "OpenIddictApplications";
# kỳ vọng có HCS_App
```

Chạy `migrate` lại được (idempotent). Đổi `HCS_GATEWAY_CLIENT_SECRET` hoặc `HCS_ADMIN_PASSWORD` rồi seed lại để cập nhật.

## Log

```text
Logs/local-ui/auth-server.log
Logs/local-ui/web-gateway.log
Logs/local-ui/platform.log
Logs/local-ui/work-management.log
...
```

## Reset Postgres local

Volume cũ giữ password Postgres **lúc init**. Đổi `HCS_POSTGRES_PASSWORD` trong `.env` mà không xóa volume → `password authentication failed for user "hcs"`.

```bash
docker compose --env-file .env -f etc/docker-compose/local-infra.yml down
docker volume rm hcs-community-local_hcs-postgres
./scripts/run-local-ui.sh migrate
```

Xóa volume **mất data** local identity.

## Troubleshooting

| Hiện tượng | Cách xử lý |
|------------|------------|
| `The specified 'client_id' is invalid` (ID2052) | Seed lại: `./scripts/run-local-ui.sh migrate`. Không chạy DbMigrator từ root repo. |
| `The action field is required` lúc bấm Login | Hard refresh trang Auth. Đã fix trong `Login.js` (không disable nút trước khi POST `Action`). |
| Postgres `password authentication failed` | Reset volume như trên, khớp `.env`. |
| `HCS_KEYCLOAK_ADMIN is missing` | Không cần Keycloak cho local UI. Compose dùng default nếu thiếu biến. |
| Cổng `5432` bị chiếm | Script chuyển `5433`. Đừng trỏ connection string nhầm cổng. |
| Blazor không login / cookie | Blazor `44403` và Gateway `44402` cùng host `localhost`. Không trỏ `Bff:PublicOrigin` sang production. |

Sau khi đổi Auth/Gateway, restart host rồi **hard refresh** (Cmd+Shift+R).
