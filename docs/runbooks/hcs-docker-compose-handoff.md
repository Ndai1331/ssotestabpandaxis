# Handoff: chạy và deploy HCS bằng Docker Compose

## Trạng thái

- Runtime mặc định: Docker Compose tại `services/HCS_web_free_license/docker-compose.yml`.
- Kubernetes/Kind đã được dừng bằng `scripts/docker-up.sh`; PVC Kubernetes không bị xoá.
- Browser entrypoint: `https://hcs.localhost`.
- BFF login phải chuyển hướng tới `https://auth.hcs.localhost/connect/authorize` — không bao giờ là `http://web-gateway/...`.

## Chạy local

**Thứ tự khuyến nghị:** Keycloak (`:5110`) → bootstrap realm `bd` → `./scripts/docker-up.sh`. Chi tiết SSO: [`../../docs/runbooks/hcs-docker-compose-handoff.md`](../../docs/runbooks/hcs-docker-compose-handoff.md#keycloak--login-sso-bắt-buộc-trước-khi-test).

```bash
cd services/HCS_web_free_license
chmod +x scripts/docker-up.sh scripts/docker-down.sh
env YARN_IGNORE_ENGINES=1 abp install-libs
./scripts/docker-up.sh
docker compose ps
```

Script build chín image, khởi động hạ tầng, chạy DbMigrator idempotent (seed `HCS_App`), rồi khởi động các app và Caddy HTTPS proxy. Docker Compose lưu dữ liệu ở named volumes riêng; không dùng PVC Kubernetes.

Lệnh `install-libs` là bắt buộc trước lần build sạch hoặc sau khi nâng package theme của AuthServer. Nó sinh ABP static assets (ví dụ `/libs/abp/core/abp.css`) vào `apps/auth-server/HCS.AuthServer/wwwroot/libs`; các assets này không được Git theo dõi. `YARN_IGNORE_ENGINES=1` chỉ bỏ qua metadata Node của dependency `select2`, không đổi dependency đã khoá trong `yarn.lock`.

Để dừng nhưng giữ dữ liệu:

```bash
./scripts/docker-down.sh
```

Không dùng `docker compose down -v` trừ khi chủ đích xoá database/MinIO local.

## Secrets

`.env` không được commit. Nó phải chứa tất cả biến của `.env.k8s.example`, đặc biệt:

- `HCS_ADMIN_PASSWORD`
- `HCS_GATEWAY_CLIENT_SECRET`
- `HCS_KEYCLOAK_CLIENT_SECRET`
- mật khẩu PostgreSQL, RabbitMQ và MinIO.

Keycloak lab tiếp tục chạy tại `http://localhost:5110`. Client `hcs-free-auth` cần redirect URI `https://auth.hcs.localhost/signin-oidc`.

## Keycloak + Login SSO (bắt buộc trước khi test)

HCS Compose **không** gồm Keycloak. AuthServer chạy trong Docker cần Keycloak trên **host**, port **5110**.

### 1. Khởi động Keycloak

Khuyến nghị: container độc lập `bd-keycloak` (image `26.3`), bind `0.0.0.0:5110` để container HCS reach được qua `host.docker.internal`, và **`KC_HOSTNAME=http://localhost:5110`** để trình duyệt không bị redirect sang `host.docker.internal` (lỗi `DNS_PROBE_FINISHED_NXDOMAIN`).

```bash
docker rm -f bd-keycloak 2>/dev/null
docker run -d --name bd-keycloak -p 5110:8080 \
  -e KC_BOOTSTRAP_ADMIN_USERNAME=admin \
  -e KC_BOOTSTRAP_ADMIN_PASSWORD=secret \
  -e KC_HOSTNAME=http://localhost:5110 \
  -e KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true \
  -e KC_HTTP_ENABLED=true \
  quay.io/keycloak/keycloak:26.3 \
  start-dev --http-port=8080 --http-host=0.0.0.0
```

Chờ Keycloak sẵn sàng (lần đầu có thể **12–18 phút** do Quarkus build; lần sau ~1–2 phút):

```bash
curl -sf http://127.0.0.1:5110/realms/master >/dev/null && echo "Keycloak OK"
```

Admin UI: http://localhost:5110 (`admin` / `secret`).

**Không** bind chỉ `127.0.0.1:5110` nếu AuthServer chạy trong Docker — container sẽ không gọi được metadata qua `host.docker.internal`.

Tuỳ chọn: Keycloak trong `etc/docker-compose/local-infra.yml` (`--profile keycloak`) cũng đã cấu hình `KC_HOSTNAME` tương tự.

### 2. Bootstrap realm `bd`

Chạy từ repo root; `HCS_AUTH_CLIENT_SECRET` phải **trùng** `HCS_KEYCLOAK_CLIENT_SECRET` trong `.env` của HCS:

```bash
cd services/HCS_web_free_license
# thay bằng giá trị thật trong .env
export HCS_AUTH_CLIENT_SECRET='dev-hcs-free-auth-secret-2026'

KEYCLOAK_URL=http://127.0.0.1:5110 \
  python3 ../../scripts/keycloak_bootstrap_bd_realm.py
```

Script tạo realm `bd`, groups (`bd-app-hcs`, role groups), client `hcs-free-auth`, và user lab.

Kiểm tra discovery **phải** trả URL `localhost` (không phải `host.docker.internal`):

```bash
curl -s http://127.0.0.1:5110/realms/bd/.well-known/openid-configuration \
  | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['issuer']); print(d['authorization_endpoint'])"
# Kỳ vọng:
# http://localhost:5110/realms/bd
# http://localhost:5110/realms/bd/protocol/openid-connect/auth
```

### 3. Cấu hình AuthServer (Docker Compose)

`docker-compose.yml` tách URL browser và metadata nội bộ:

| Biến | Giá trị lab | Vai trò |
|------|-------------|---------|
| `Authentication__Keycloak__Authority` | `http://localhost:5110/realms/bd` | Redirect OIDC trên browser |
| `Authentication__Keycloak__MetadataAddress` | `http://host.docker.internal:5110/realms/bd/.well-known/openid-configuration` | Discovery từ container |
| `Authentication__Keycloak__ClientId` | `hcs-free-auth` | Client confidential |
| `Authentication__Keycloak__ClientSecret` | `${HCS_KEYCLOAK_CLIENT_SECRET}` | Khớp bootstrap |
| `Authentication__Keycloak__Enabled` | `true` | Bật nút **Login với SSO** |

Sau khi đổi code hoặc env AuthServer:

```bash
cd services/HCS_web_free_license
docker compose build auth-server
docker compose up -d --no-deps auth-server
```

### 4. Smoke test SSO

1. Mở https://hcs.localhost (hard refresh `Ctrl+Shift+R`).
2. Luồng: Blazor → `/bff/login` → `https://auth.hcs.localhost/connect/authorize` → trang login AuthServer → **Login với SSO**.
3. Trình duyệt redirect tới `http://localhost:5110/realms/bd/protocol/openid-connect/auth?...` (không có `host.docker.internal`).
4. User lab (password `Passw0rd!`, group `bd-app-hcs` bắt buộc):

| Email | Role HCS |
|-------|----------|
| `admin@benhvien.vn` | admin |
| `bacsi@benhvien.vn` | bacsi |
| `lanhdao@benhvien.vn` | lanhdao |
| `nhanvien@benhvien.vn` | nhanvien |

### 6. Smoke test Account và chữ ký cá nhân

Sau khi đăng nhập, kiểm tra:

1. Mở `https://hcs.localhost/account`; profile hiển thị banner, avatar fallback initials và hai tab.
2. Upload/remove avatar tại tab **Thông tin cá nhân**; header avatar phải cập nhật sau mutation.
3. Mở `https://hcs.localhost/account?tab=signatures`; upload ảnh hợp lệ dưới 2 MB.
4. Đổi tên hoặc thay ảnh, đặt mặc định, rồi xóa chữ ký. Khi xóa chữ ký mặc định, chữ ký mới nhất còn lại phải được chọn mặc định.
5. Mở `/user-signatures` và xác nhận redirect tới `/account?tab=signatures`; `/signature-settings` vẫn mở trang credential riêng.

Ảnh chữ ký chỉ nhận JPEG/PNG/WebP/GIF và giới hạn 2 MB. Nếu cần kiểm tra MinIO, xem bucket `hcs-avatars` và `hcs-signing`; không dùng `docker compose down -v` vì sẽ xóa dữ liệu local.

### 5. Troubleshooting SSO

| Triệu chứng | Nguyên nhân | Cách xử lý |
|-------------|-------------|------------|
| `DNS_PROBE_FINISHED_NXDOMAIN` trên `host.docker.internal:5110` | Keycloak/AuthServer dùng hostname Docker trên browser | Set `KC_HOSTNAME=http://localhost:5110`; AuthServer `Authority` = `localhost`, `MetadataAddress` = `host.docker.internal` |
| AuthServer không load metadata OIDC | Keycloak bind `127.0.0.1` only | Publish `-p 5110:8080` (0.0.0.0), `--http-host=0.0.0.0` |
| Nút SSO không hiện | `Authentication__Keycloak__Enabled=false` hoặc secret thiếu | Kiểm tra `docker inspect hcs-community-auth-server-1` env Keycloak |
| Login SSO OK nhưng bị từ chối | User thiếu group `bd-app-hcs` | Gán group trong Keycloak Admin hoặc chạy lại bootstrap |
| Bootstrap lỗi giữa chừng | Keycloak chưa ready | Chờ `curl` master realm = 200, chạy lại script (idempotent) |

Runbook SSO tổng thể (Directus + ABP): [`local-sso-lab.md`](./local-sso-lab.md).

## Deploy Docker host

1. Cài Docker Engine + Compose v2; DNS `hcs.localhost` và `auth.hcs.localhost` phải trỏ tới Docker host.
2. Copy `.env.k8s.example` thành `.env`, đặt secret qua secret manager của host.
3. Cấp TLS certificate hợp lệ cho Caddy khi deploy ngoài local (thay `tls internal` trong `deploy/docker/Caddyfile`).
4. Chạy `./scripts/docker-up.sh`, kiểm tra `docker compose ps` và `curl -k -I https://hcs.localhost/bff/login`.
5. Rollback ứng dụng: dùng image tag trước đó trong Compose, `docker compose up -d`. Database migration là forward-only; backup volume PostgreSQL trước migration có thay đổi schema.

## Ubuntu 24 — 2 server (production/intranet)

Tách PostgreSQL/Redis/RabbitMQ/MinIO sang data server và app + Nginx + Keycloak sang apps server. Không dùng Caddy.

**Hướng dẫn deploy đầy đủ (CI/CD Docker Hub + step-by-step):** [`../../services/HCS_web_free_license/docs/runbooks/deploy-server.md`](../../services/HCS_web_free_license/docs/runbooks/deploy-server.md)

Reference UFW/backup/diagram: [`../../services/HCS_web_free_license/docs/runbooks/hcs-ubuntu24-two-server.md`](../../services/HCS_web_free_license/docs/runbooks/hcs-ubuntu24-two-server.md)

File Compose/Nginx: `services/HCS_web_free_license/deploy/ubuntu/`.

## Kubernetes (chỉ khôi phục khi cần)

Kubernetes không còn là runtime mặc định. Dữ liệu cũ vẫn tồn tại trong namespace `hcs` nhưng workload được scale về 0. Khôi phục chỉ khi có chủ đích:

```bash
kubectl -n hcs scale statefulset postgres --replicas=1
kubectl -n hcs scale deployment --all --replicas=1
cd services/HCS_web_free_license && ./scripts/k8s-up.sh --kind
```
