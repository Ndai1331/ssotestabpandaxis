# Handoff: chạy và deploy HCS bằng Docker Compose

## Trạng thái

- Runtime mặc định: Docker Compose tại `services/HCS_web_free_license/docker-compose.yml`.
- Kubernetes/Kind đã được dừng bằng `scripts/docker-up.sh`; PVC Kubernetes không bị xoá.
- Browser entrypoint: `https://hcs.localhost`.
- BFF login phải chuyển hướng tới `https://auth.hcs.localhost/connect/authorize` — không bao giờ là `http://web-gateway/...`.

## Chạy local

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

## Deploy Docker host

1. Cài Docker Engine + Compose v2; DNS `hcs.localhost` và `auth.hcs.localhost` phải trỏ tới Docker host.
2. Copy `.env.k8s.example` thành `.env`, đặt secret qua secret manager của host.
3. Cấp TLS certificate hợp lệ cho Caddy khi deploy ngoài local (thay `tls internal` trong `deploy/docker/Caddyfile`).
4. Chạy `./scripts/docker-up.sh`, kiểm tra `docker compose ps` và `curl -k -I https://hcs.localhost/bff/login`.
5. Rollback ứng dụng: dùng image tag trước đó trong Compose, `docker compose up -d`. Database migration là forward-only; backup volume PostgreSQL trước migration có thay đổi schema.

## Ubuntu 24 — 2 server (production/intranet)

Tách PostgreSQL/Redis/RabbitMQ/MinIO sang `10.17.227.64` và app + Nginx + Keycloak sang `10.17.227.58`. Không dùng Caddy. Hướng dẫn đầy đủ (3 domain, UFW, Let's Encrypt DNS-01, Nginx):

[`../../services/HCS_web_free_license/docs/runbooks/hcs-ubuntu24-two-server.md`](../../services/HCS_web_free_license/docs/runbooks/hcs-ubuntu24-two-server.md)

File Compose/Nginx: `services/HCS_web_free_license/deploy/ubuntu/`.

## Kubernetes (chỉ khôi phục khi cần)

Kubernetes không còn là runtime mặc định. Dữ liệu cũ vẫn tồn tại trong namespace `hcs` nhưng workload được scale về 0. Khôi phục chỉ khi có chủ đích:

```bash
kubectl -n hcs scale statefulset postgres --replicas=1
kubectl -n hcs scale deployment --all --replicas=1
cd services/HCS_web_free_license && ./scripts/k8s-up.sh --kind
```
