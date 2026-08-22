# Deploy HCS trên Baota Panel (`/www/server/panel/data/compose/`)

Bundle **standalone** — không cần clone full repo trên server. Cùng pattern với `cms`, `axis`:

```
/www/server/panel/data/compose/
├── cms/
├── axis/
├── hcs-data/          ← PostgreSQL, Redis, RabbitMQ, MinIO
└── hcs/                 ← Apps: Blazor, Gateway, Auth, microservices, Keycloak
```

## Một server vs hai server

| Kịch bản | Folder trên panel | `HCS_DATA_HOST` |
|----------|-------------------|-----------------|
| **Một máy** (cms/axis/hcs chung server) | Cả `hcs-data` + `hcs` trên cùng host | IP private của server (vd. `10.17.227.58`) — **không** dùng `127.0.0.1` vì container apps không reach được |
| **Hai máy** (data tách) | `hcs-data` trên server DB; `hcs` trên server apps | IP private server data trên `.env` của `hcs/` |

---

## Bước 1 — Copy bundle lên server

Từ máy dev (có repo):

```bash
cd services/HCS_web_free_license
./deploy/panel/install-on-server.sh root@<IP-server>
```

Hoặc copy thủ công:

```bash
rsync -av deploy/panel/hcs-data/ root@<IP>:/www/server/panel/data/compose/hcs-data/
rsync -av deploy/panel/hcs/       root@<IP>:/www/server/panel/data/compose/hcs/
```

Trên server:

```bash
chmod +x /www/server/panel/data/compose/hcs-data/up.sh
chmod +x /www/server/panel/data/compose/hcs/up.sh
```

---

## Bước 2 — `.env`

### `hcs-data/.env`

```bash
cd /www/server/panel/data/compose/hcs-data
cp .env.example .env
chmod 600 .env
nano .env
```

Điền mật khẩu Postgres/Redis/Rabbit/MinIO. `HCS_DATA_HOST` = IP mà **container apps** dùng để kết nối (thường IP LAN server).

### `hcs/.env`

```bash
cd /www/server/panel/data/compose/hcs
cp .env.example .env
chmod 600 .env
nano .env
```

**Copy cùng** mật khẩu data từ `hcs-data/.env`. Domain HTL Tech mẫu đã có sẵn trong `.env.example`.

Chỉnh WASM client nếu cần: `config/blazor-client.appsettings.json`.

---

## Bước 3 — Cert PFX (chỉ server `hcs`)

```bash
sudo mkdir -p /etc/hcs/certs && sudo chmod 750 /etc/hcs/certs
# Tạo openiddict.pfx + dataprotection.pfx — xem deploy-server.md § B.3
```

Đường dẫn trong `.env`: `HCS_OPENIDDICT_PFX=/etc/hcs/certs/openiddict.pfx`.

---

## Bước 4 — Khởi động stack

**Thứ tự:** data trước, apps sau.

```bash
# Data
cd /www/server/panel/data/compose/hcs-data
./up.sh

# Apps (pull Docker Hub + up)
cd /www/server/panel/data/compose/hcs
docker login   # nếu Hub private
./up.sh
```

### Qua Baota Panel UI

1. **Docker** → **Compose** → **Add project**
2. Project path: `/www/server/panel/data/compose/hcs-data` → compose file `docker-compose.yml`
3. Lặp lại với `/www/server/panel/data/compose/hcs`
4. Env file: panel thường đọc `.env` cùng thư mục nếu có

---

## Bước 5 — Nginx / SSL (4 domain)

Container bind loopback:

| Port | Service |
|------|---------|
| `127.0.0.1:8081` | Blazor |
| `127.0.0.1:8082` | Gateway / BFF |
| `127.0.0.1:8083` | AuthServer |
| `127.0.0.1:8084` | Keycloak |

**Cách A — Nginx system** (khuyến nghị, config đầy đủ):

```bash
sudo cp /path/to/repo/deploy/ubuntu/nginx/hcs-proxy.inc /etc/nginx/hcs-proxy.inc
sudo cp /path/to/repo/deploy/ubuntu/nginx/hcs.conf /etc/nginx/sites-available/hcs.conf
sudo ln -sfn /etc/nginx/sites-available/hcs.conf /etc/nginx/sites-enabled/hcs.conf
sudo nginx -t && sudo systemctl reload nginx
```

**Cách B — Baota Website:** tạo 4 site HTTPS, reverse proxy tới `127.0.0.1:8081–8084` theo bảng domain trong [`docs/runbooks/deploy-server.md`](../runbooks/deploy-server.md).

DNS A record: `hanhchinhso`, `api-hcs`, `auth-hcs`, `sso-hcs` → IP server apps.

---

## Bước 6 — Bootstrap Keycloak

Cần script bootstrap từ repo (một lần):

```bash
git clone <repo> /opt/hcs-src   # hoặc scp scripts/keycloak_bootstrap_bd_realm.py

export HCS_AUTH_CLIENT_SECRET='<khớp HCS_KEYCLOAK_CLIENT_SECRET trong hcs/.env>'
export HCS_AUTH_PUBLIC_HOST='auth-hcs.htltech.vn'
export KEYCLOAK_URL='https://sso-hcs.htltech.vn'
export KEYCLOAK_ADMIN='admin'
export KEYCLOAK_ADMIN_PASSWORD='<khớp .env>'

python3 /opt/hcs-src/scripts/keycloak_bootstrap_bd_realm.py
```

---

## Cập nhật sau CI (push Docker Hub)

```bash
cd /www/server/panel/data/compose/hcs
./up.sh
```

`up.sh` tự `docker compose pull` khi `HCS_PULL_IMAGES=true`.

---

## Tài liệu đầy đủ

- [`docs/runbooks/deploy-server.md`](../runbooks/deploy-server.md) — CI/CD, smoke test, troubleshooting
- [`deploy/ubuntu/nginx/hcs.conf`](../ubuntu/nginx/hcs.conf) — mẫu Nginx 4 vhost
