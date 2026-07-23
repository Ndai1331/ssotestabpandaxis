---
type: concept
title: "CI/CD Pipeline"
created: 2026-06-27
updated: 2026-07-16
tags:
  - devops
  - cicd
status: mature
complexity: basic
related:
  - "[[Git Flow 3 Tầng]]"
  - "[[Infrastructure & Servers]]"
sources:
  - "[[workspace-architecture.md]]"
---

# CI/CD Pipeline

Image UI/API build **trong GitHub Actions** (`.github/workflows/docker-build-push.yml`). Không build local.

## Trigger Matrix

| Prefix commit | Branch | Docker tag | Env | Domain | DB |
|---------------|--------|-----------|-----|--------|----|
| `[WEB]`/`[API]` | `test` | `test` | Test | test/testapi.task9.pro | `qcadmin_test` |
| `[WEB]`/`[API]` | `staging` | `staging` | Staging | stag/stagapi.task9.pro | `qcadmin_stag` |
| `[WEB]`/`[API]` | `main` | `net9` | Prod | task9.pro / api.task9.pro | `qcadmin` |
| *(thiếu prefix)* | bất kỳ | ❌ | không build | — | — |

> ⚠️ **Thiếu `[WEB]` hoặc `[API]` trong commit message → CI không kích hoạt → code KHÔNG được deploy.**

## Self-hosted Runner (2026-07-14)

Từ commit `705b4fa` (UI) / `f674f79` (API): GitHub Actions dùng **self-hosted runner trên do-122** (`cicd.task9.pro`) thay vì GitHub-hosted runner. Lý do: GitHub-hosted IPs bị n8n webhook allowlist block → deploy webhook không kích được. Self-hosted runner trên do-122 cùng mạng với n8n → webhook hoạt động.

## Auto-deploy Flow (do-122)

```
GitHub Actions (self-hosted runner, do-122) → push Docker Hub → webhook N8N "deploy-staging"
→ deploy-listener.py (port 9998, systemd trên do-122)
→ docker run container theo tag
```

Containers:
- `task9-ui-test` (port 8085) + `task9-api-test` (port 7093)
- `task9-ui-staging` (port 8086) + `task9-api-staging` (port 7094)
- NPM route 4 domains → containers

## Agent / Worker Riêng

**Agent (task9-agent):** Build local → push Docker Hub → SSH pull trên do-122

```bash
# Build (từ services/)
docker buildx build --platform linux/amd64 -t andywilly/task9-agent:<tag> -f agent/Dockerfile.unified --push .

# Deploy
ssh do-122 "cd /home/tobi/task9-agent && docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d --force-recreate"
```

> ⚠️ Build PHẢI chạy từ `services/` (context cần `agent/` folder). `--platform linux/amd64` bắt buộc (VPS linux amd64, local macOS ARM). OpenClaw gateway cần ~30s để start.

**Worker (qc_tray):** Image `andywilly/worker-slim`, script `platforms/linux-docker/build-push-slim.sh`
- Tag `test` = staging, tag `latest` = production
- Flow: agent branch → `test` → `main` (KHÔNG skip test)

## Checklist Trước Deploy

- [ ] Commit UI có prefix `[WEB]`
- [ ] Commit API có prefix `[API]`
- [ ] Đã merge latest `origin/main` vào feat branch trước
- [ ] Merge vào đúng target branch
- [ ] Submodule reference ở workspace root đã update
