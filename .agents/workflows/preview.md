---
description: Preview — BD local (Keycloak / apps). Codespace Task9 đã tắt.
---

# Preview — BD Local Lab

> Workflow Codespace `task9-ui` **đã sunset**. Dùng local:

## Keycloak
```bash
cd /Users/user/Documents/bd-workspace/services/directus-main
docker compose up -d keycloak
# http://localhost:5110  admin/secret
```

## Directus / ABP
Theo README từng service; báo URL cho user hard-refresh.

Không push GitHub / không tạo Codespace trừ khi user yêu cầu sau khi có remote.
