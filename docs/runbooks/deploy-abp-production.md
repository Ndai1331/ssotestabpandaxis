# Runbook — Deploy ABP (hanhchinhso) Production

> **Mục tiêu:** Đưa `services/abp-blazor` lên server **Ubuntu 24.04+** với một trong hai mô hình:
>
> 1. **Docker Compose + Nginx** (proxy TLS) — đơn giản, 1–2 VM  
> 2. **Kubernetes + Helm + Ingress NGINX** — scale, multi-node  
>
> **Nguồn code / chart:** `services/abp-blazor`  
> **Local lab (không dùng cho prod):** `docs/runbooks/local-sso-lab.md`, `aspire/README.md`  
> **Cập nhật:** 2026-07-24

---

## 0. Quyết định nhanh

| Tiêu chí | Docker + Nginx | Kubernetes (Helm) |
|----------|----------------|-------------------|
| Số máy | 1 VM đủ (hoặc tách DB) | Cluster ≥ 1 control + workers |
| Độ phức tạp ops | Thấp–trung | Trung–cao |
| Có sẵn trong repo | Dockerfiles + `etc/docker` (infra local) | `etc/helm/hanhchinhso` (local/staging chart) |
| Phù hợp khi | POC prod / bệnh viện nhỏ | Nhiều service, HA, CI/CD image |
| Khuyến nghị BD hiện tại | **Ưu tiên bắt đầu** | Khi đã ổn định image + secret |

Cả hai mô hình đều dùng **cùng pattern BFF**:

```
Browser
  ├─ https://app.<domain>          → Blazor
  ├─ https://auth.<domain>         → AuthServer (OpenIddict + optional Keycloak)
  └─ https://gateway.<domain>      → WebGateway (YARP → microservices)
         │
         └─ (internal only) Identity, Administration, Language, …
```

**Không** expose Postgres / Redis / RabbitMQ / Elasticsearch / MinIO ra internet.

---

## 1. Thành phần cần chạy

### 1.1 Public (browser / OIDC)

| Thành phần | Project | Container listen | Ghi chú |
|------------|---------|------------------|---------|
| Blazor UI | `apps/blazor/hanhchinhso.Blazor` | `:80` | UI chính |
| AuthServer | `apps/auth-server/hanhchinhso.AuthServer` | `:80` | Login, token, `/signin-oidc` (Keycloak) |
| WebGateway | `gateways/web/hanhchinhso.WebGateway` | `:80` | BFF `/api/*` |

### 1.2 Internal (chỉ Docker network / ClusterIP)

| Service | Port local (dev) | DB gợi ý |
|---------|------------------|----------|
| Identity | 44392 | `hanhchinhso_Identity` |
| Administration | 44323 | `hanhchinhso_Administration` (+ BlobStoring) |
| Language | 44391 | `hanhchinhso_Language` |
| AuditLogging | 44302 | `hanhchinhso_AuditLogging` |
| Gdpr | 44348 | `hanhchinhso_Gdpr` |
| AIManagement | 44318 | `hanhchinhso_AIManagement` (+ pgvector nếu dùng AI) |
| Organization | 44370 | DB riêng — **chưa có Helm chart** |
| WorkflowService | 44395 | `hanhchinhso_Workflow` — **chưa có Helm chart** |
| Elsa Studio | 44396 | WASM — **chưa có Dockerfile/Helm** |

### 1.3 Infrastructure

| Infra | Local compose | Ghi chú prod |
|-------|---------------|--------------|
| PostgreSQL 16 | `etc/docker/containers/postgresql.yml` `:5432` | Bắt buộc; đổi password; backup |
| Redis 7 | `containers/redis.yml` `:6379` | Cache / distributed |
| RabbitMQ 3.12 | `containers/rabbitmq.yml` `:5672` / `:15672` | Event bus; **khóa management UI** |
| Elasticsearch / Kibana | optional | Logging; admin-only |
| MinIO | `containers/minio.yml` | Object storage HCS; **đổi credential** |
| Keycloak | `services/directus-main-v11` compose lab `:5110` | SSO Zimbra — có thể VM/container riêng |

Helm chart hiện có (`etc/helm/hanhchinhso/charts/`): authserver, blazorwebapp, webgateway, administration, identity, language, auditlogging, gdpr, aimanagement, postgresql, redis, rabbitmq, elasticsearch, kibana, grafana, prometheus.

**Gap production (cần bổ sung trước khi coi “full HCS”):**

- Không có Helm/build-all cho **Organization**, **WorkflowService**, **Elsa Studio**
- Helm WebGateway **chưa** có YARP cluster Organization / Workflow
- Connection strings Helm **chưa** có DB Organization / Workflow
- Keycloak chỉ cấu hình rõ trong `appsettings.Development.json` — prod phải inject env / secret

---

## 2. DNS & TLS (bắt buộc trước khi chạy app)

Giả sử domain `hcs.benhvien.vn` (thay bằng domain thật):

| Hostname | Trỏ tới |
|----------|---------|
| `app.hcs.benhvien.vn` | Blazor |
| `auth.hcs.benhvien.vn` | AuthServer |
| `gateway.hcs.benhvien.vn` | WebGateway |
| `sso.hcs.benhvien.vn` (tuỳ chọn) | Keycloak |
| `studio.hcs.benhvien.vn` (tuỳ chọn) | Elsa Studio — khi đã ship |

TLS:

- **Docker + Nginx:** Certbot (Let’s Encrypt) trên host, hoặc terminate TLS trên Nginx
- **Kubernetes:** `cert-manager` + ClusterIssuer `letsencrypt` (chart local đã có annotation `cert-manager.io/cluster-issuer: letsencrypt`)

Sau khi có URL thật, cập nhật (seed / config):

- `App__SelfUrl`, `AuthServer__Authority`
- OpenIddict client redirect: `{BlazorRoot}/signin-oidc`, `{BlazorRoot}/signout-callback-oidc`
- `App__CorsOrigins`, `App__RedirectAllowedUrls`
- Keycloak client redirect URI / Web origins (nếu federation)

---

## 3. Chuẩn bị server Ubuntu 24.04+

### 3.1 Hệ thống cơ bản

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y ca-certificates curl gnupg ufw fail2ban
sudo timedatectl set-timezone Asia/Ho_Chi_Minh
```

Firewall (chỉ mở cần thiết):

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### 3.2 Docker Engine (cả hai mô hình đều cần để build/pull image)

```bash
# Official Docker CE — Ubuntu 24.04
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker "$USER"
# re-login sau khi add group
```

### 3.3 Chỉ cho Kubernetes

```bash
# kubectl
curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.31/deb/Release.key \
  | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
# … theo docs kubernetes hiện hành, rồi:
sudo apt install -y kubectl

# Helm
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

Cluster: k3s / kubeadm / managed (tuỳ infra bệnh viện). Trên single-node lab có thể dùng **k3s**:

```bash
curl -sfL https://get.k3s.io | sh -
# kubectl config: /etc/rancher/k3s/k3s.yaml
```

Ingress NGINX (bắt buộc cho chart ABP):

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.config.enable-underscores-in-headers="true"
```

---

## 4. Secrets & certificate AuthServer (critical)

### 4.1 OpenIddict signing certificate

Prod **không** dùng `dotnet dev-certs` của máy dev. Tạo PFX riêng, mount vào container AuthServer:

```bash
# Trên máy build an toàn (ví dụ)
openssl req -x509 -newkey rsa:4096 -sha256 -days 825 \
  -keyout openiddict.key -out openiddict.crt -nodes \
  -subj "/CN=hanhchinhso-authserver"
openssl pkcs12 -export -out openiddict.pfx \
  -inkey openiddict.key -in openiddict.crt \
  -passout pass:'<STRONG_PASSPHRASE>'
```

AuthServer non-Development load `openiddict.pfx` với passphrase từ config `AuthServer:CertificatePassPhrase` (xem module AuthServer). **Đổi passphrase lab** (`5ffe2f0a-…` trong README local).

### 4.2 Secrets bắt buộc đổi trước prod

| Secret | Nơi thường gặp (lab) | Hành động |
|--------|----------------------|-----------|
| Postgres password | `myPassword` trong Helm values / docker | Random mạnh, secret store |
| Identity admin | `admin@abp.io` / `1q2w3E*` | Đổi ngay sau seed |
| Blazor client secret | `1q2w3e*` | Rotate + re-seed OpenIddict |
| StringEncryption | `TSUF1eP9nPuNEgwi` | Generate mới **trước** data thật |
| MinIO | `hcsadmin` / `hcsadminpassword` | Chỉ lab |
| Keycloak client secret | `bd-abp-auth-lab-secret` | Rotate trên Keycloak + AuthServer |

Không commit `.env`, PFX, hay secret vào git.

### 4.3 Environment tối thiểu (mọi app)

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
# Forwarded headers sau Nginx / Ingress
# (ABP/ASP.NET thường cần X-Forwarded-Proto / Host — bật middleware tương ứng nếu chưa)
```

Connection string mẫu (thay host/password):

```text
Host=postgres;Port=5432;Database=hanhchinhso_Identity;User ID=hcs;Password=<SECRET>;Timeout=240;
```

Redis / RabbitMQ: hostname nội bộ (`redis`, `rabbitmq` hoặc K8s service name).

---

## 5. Option A — Docker Compose + Nginx (khuyến nghị bắt đầu)

**Template sẵn trong repo:** [`services/abp-blazor/etc/docker-prod/`](../../services/abp-blazor/etc/docker-prod/)

| File | Vai trò |
|------|---------|
| `docker-compose.prod.yml` | Infra + core (Blazor/Auth/Gateway/Identity/Admin/Language); `--profile full` = Organization/Workflow/… |
| `.env.example` | Copy → `.env` (chmod 600) |
| `nginx/hcs.conf.example` | TLS terminate → `127.0.0.1:8080/8081/8082` |
| `postgres/init-databases.sql` | Tạo DB ABP lần đầu |
| `certs/` | Đặt `openiddict.pfx` |

### 5.1 Trên server Ubuntu

```bash
# Clone/copy thư mục docker-prod lên /opt/hcs (hoặc làm việc trực tiếp trong repo)
cd /opt/hcs   # hoặc: services/abp-blazor/etc/docker-prod
cp .env.example .env && chmod 600 .env
# Sửa IMAGE_REGISTRY, IMAGE_TAG, PUBLIC_*_URL, passwords, PFX passphrase
mkdir -p certs data
# copy openiddict.pfx → certs/
```

### 5.2 Build images

Pattern giống `etc/helm/build-image.ps1`:

```bash
dotnet publish apps/auth-server/hanhchinhso.AuthServer \
  -c Release -o apps/auth-server/hanhchinhso.AuthServer/bin/Release/net10.0/publish
docker build -t registry.example/hcs/authserver:1.0.0 \
  apps/auth-server/hanhchinhso.AuthServer
```

Script: `etc/helm/build-all-images.ps1` (9 image template; organization/workflow build riêng). Mapping tên image: xem `etc/docker-prod/README.md`.

### 5.3 Chạy Compose + Nginx

```bash
docker compose -f docker-compose.prod.yml --env-file .env up -d
# Full HCS extras:
docker compose -f docker-compose.prod.yml --env-file .env --profile full up -d

sudo cp nginx/hcs.conf.example /etc/nginx/sites-available/hcs.conf
# sửa server_name → domain thật
sudo ln -sf /etc/nginx/sites-available/hcs.conf /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d app.YOUR.DOMAIN -d auth.YOUR.DOMAIN -d gateway.YOUR.DOMAIN
```

Loopback: Blazor `:8080`, AuthServer `:8081`, WebGateway `:8082`, Workflow `:8085` (profile full).

### 5.4 Vận hành

```bash
docker compose -f docker-compose.prod.yml --env-file .env ps
docker compose -f docker-compose.prod.yml --env-file .env logs -f authserver
```

Migration / seed: lần đầu Identity + Administration theo ABP docs; **cập nhật OpenIddict redirect URLs** sang domain prod; đổi admin password ngay.

---

## 6. Option B — Kubernetes + Helm

Repo đã có chart local: `services/abp-blazor/etc/helm/`.  
Guide local: `etc/helm/README.md` (Docker Desktop / mkcert). Dưới đây là **hướng production trên Ubuntu**.

### 6.1 Khác biệt so với chart local

| Local (`values.hanhchinhso-local.yaml`) | Production |
|-----------------------------------------|------------|
| Hosts `hanhchinhso-local-*` | FQDN thật (`app.`, `auth.`, `gateway.`) |
| `dotnetEnvironment: Staging` | `Production` |
| Password lab `myPassword` | Secret / ExternalSecret |
| TLS mkcert + secret `hanhchinhso-local-tls` | cert-manager Let’s Encrypt |
| `enablePII: true` | `false` |
| ABP Studio link | Tắt |

Tạo file values riêng (không commit secret), ví dụ `values.production.yaml`:

```yaml
global:
  hosts:
    authserver: "auth.hcs.benhvien.vn"
    webgateway: "gateway.hcs.benhvien.vn"
    blazorwebapp: "app.hcs.benhvien.vn"
    # kibana/grafana/prometheus: chỉ nội bộ / VPN — cân nhắc disable ingress
  connectionStrings:
    administration: "Host=hanhchinhso-postgresql;Port=5432;Database=hanhchinhso_Administration;User ID=hcs;Password=<FROM_SECRET>;Timeout=240;"
    # … identity, language, …
  dotnetEnvironment: "Production"
  enablePII: "false"
  stringEncryptionDefaultPassPhrase: "<GENERATE_NEW>"
  tlsSecret: "hcs-tls"   # hoặc để cert-manager annotate từng ingress
```

### 6.2 Build & push images

Trên CI hoặc build host:

```bash
cd services/abp-blazor/etc/helm
# Dev: ./build-all-images.ps1 (Windows)
# Prod Linux: publish + docker build + docker push registry.example/hcs/<name>:<tag>
```

Cập nhật image repository/tag trong values từng chart (`hanhchinhso/authserver` → `registry.example/hcs/authserver:1.0.0`).

### 6.3 TLS secret / cert-manager

```bash
kubectl create namespace hcs
# Option 1: cert-manager ClusterIssuer letsencrypt (khuyến nghị)
# Option 2: kubectl create secret tls hcs-tls --cert=fullchain.pem --key=privkey.pem -n hcs
```

AuthServer PFX:

```bash
kubectl create secret generic authserver-openiddict \
  --from-file=openiddict.pfx=./openiddict.pfx \
  --from-literal=passphrase='<STRONG>' \
  -n hcs
```

(Cần chỉnh Deployment AuthServer mount secret — chart local chưa mount PFX prod; bổ sung volume khi harden.)

### 6.4 Install chart

Tham chiếu script local `etc/helm/install.ps1`:

```bash
helm upgrade --install hanhchinhso ./hanhchinhso \
  --namespace hcs --create-namespace \
  -f ./hanhchinhso/values.yaml \
  -f ./values.production.yaml
```

Ingress class: `nginx` (đã cài ở §3.3).

### 6.5 Kiểm tra

```bash
kubectl -n hcs get pods,svc,ingress
kubectl -n hcs logs deploy/hanhchinhso-authserver -f
curl -I https://app.hcs.benhvien.vn
curl -I https://auth.hcs.benhvien.vn
curl -I https://gateway.hcs.benhvien.vn/health-status   # nếu có endpoint
```

Uninstall: `helm uninstall hanhchinhso -n hcs` (xem `uninstall.ps1`).

---

## 7. Keycloak / SSO Zimbra (BD)

Keycloak **không** nằm trong Helm ABP. Prod gợi ý:

1. Chạy Keycloak (Docker/K8s) riêng, domain `sso.<domain>`, TLS riêng
2. Realm + User Federation LDAP/Zimbra (xem `docs/workspace-architecture.md`)
3. Client OIDC cho AuthServer (`abp-auth`) — redirect `https://auth.<domain>/signin-oidc`
4. Inject vào AuthServer:
   - `Keycloak__Authority`
   - `Keycloak__ClientId`
   - `Keycloak__ClientSecret`
5. Group gate `bd-app-hcs` (lab) — giữ convention hoặc đổi tên prod có chủ đích

AuthServer vẫn là OpenIddict IdP cho Blazor; Keycloak là **external login** upstream.

---

## 8. Checklist go-live

### Trước deploy

- [ ] Ubuntu 24.04+, Docker (và K8s nếu chọn B)
- [ ] DNS A/AAAA cho app / auth / gateway (+ sso)
- [ ] TLS hợp lệ
- [ ] PFX OpenIddict mới + passphrase mạnh
- [ ] Đổi mọi password/secret lab
- [ ] Postgres backup strategy (pg_dump / volume snapshot)
- [ ] Image tag immutable (`1.0.0`, không dùng `latest` trên prod)
- [ ] CORS / RedirectAllowedUrls / OpenIddict seed khớp domain thật
- [ ] Keycloak clients cập nhật URL prod (nếu SSO)

### Sau deploy

- [ ] Login Blazor qua AuthServer (và Keycloak nếu bật)
- [ ] API qua gateway (không gọi thẳng service)
- [ ] Upload / blob (nếu dùng MinIO) hoạt động
- [ ] Log không lộ PII (`enablePII=false`)
- [ ] Grafana/Kibana/Prometheus **không** public internet
- [ ] UFW / security group chỉ 22 (VPN), 80, 443
- [ ] Document runbook nội bộ: ai giữ secret, ai rotate cert

### Chưa sẵn sàng “full HCS” trên Helm

- [ ] Thêm chart + image Organization, Workflow, Elsa Studio
- [ ] Bổ sung YARP routes Organization (+ Workflow nếu đi qua gateway)
- [ ] Connection strings DB tương ứng
- [ ] Elsa Studio: quyết định public URL vs VPN-only

---

## 9. Tài liệu liên quan trong repo

| File | Nội dung |
|------|----------|
| `services/abp-blazor/README.md` | Overview, cert local, Helm Studio |
| `services/abp-blazor/etc/helm/README.md` | Local Kubernetes |
| `services/abp-blazor/etc/docker/README.md` | Infra docker-compose local |
| `services/abp-blazor/etc/docker-prod/` | Compose + Nginx + `.env` production templates |
| `services/abp-blazor/aspire/README.md` | Chạy local 1 lệnh (không dùng prod) |
| `docs/runbooks/local-sso-lab.md` | Lab Keycloak + ports |
| `docs/workspace-architecture.md` | SSO Zimbra → Keycloak → ABP |
| [ABP Deployment — Microservice](https://abp.io/docs/latest/deployment/distributed-microservice) | Official ABP |

---

## 10. Gợi ý lộ trình BD

1. **Phase lab** (hiện tại): Aspire / ABP Studio + Keycloak local — giữ nguyên  
2. **Phase staging 1 VM:** Option A (Compose + Nginx) với 3 host public + infra nội bộ  
3. **Phase prod harden:** secret manager, backup, monitoring, tắt ingress observability  
4. **Phase scale:** chuyển Option B (K8s), bổ sung chart Organization/Workflow/Elsa  

---

*ABP hanhchinhso — production deploy runbook. Không chứa credential thật; mọi giá trị lab chỉ để đối chiếu.*
