---
type: domain
title: "Codebase — task9-agent"
created: 2026-06-28
updated: 2026-06-28
tags:
  - task9/agent
  - codebase
  - nodejs
status: mature
related:
  - "[[Task9 Platform Overview]]"
  - "[[Codebase — task9-api]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Codebase — task9-agent

AI sidecar Node.js/TypeScript (port 3001, prod `agent.task9.pro`). Repo `services/agent`. Stack: **Hono** (HTTP), **@modelcontextprotocol/sdk** (MCP), **ws** (WebSocket gateway), **mysql2**, **node-cron**, **zod**. ESM (`"type": "module"`), chạy bằng `tsx`, test bằng `vitest`.

## Cấu trúc `src/`
| File / folder | Vai trò |
|---------------|---------|
| `index.ts` | Entry — bootstrap Hono server. |
| `claude-runner.ts` | Gọi Claude (Anthropic SDK), stream SSE về UI. |
| `gateway-client.ts` / `ws-gateway-client.ts` | Kết nối **OpenClaw** gateway (port 18800 internal). |
| `system-prompt.txt` | System prompt cho agent. |
| `routes/` | HTTP endpoints (xem dưới). |
| `services/` | Logic nghiệp vụ (xem dưới). |
| `jobs/` | Cron jobs. |
| `middleware/`, `templates/`, `__tests__/` | Hỗ trợ. |

## Routes (`src/routes/`)
`chat-routes.ts` (chat SSE chính), `agent-admin-routes.ts`, `config-routes.ts`, `dashboard-routes.ts`, `file-routes.ts`, `metabase-routes.ts`, `notify-routes.ts`, `schedule-routes.ts`, `user-memory-routes.ts`.

## Services (`src/services/`)
- `schedule-engine.ts` — chạy scheduled task (node-cron).
- `conversation-service.ts` / `conversation-db-service.ts` — lưu hội thoại (MySQL).
- `session-service.ts`, `user-agent-manager.ts` — quản session & agent per-user.
- `agent-config-db-service.ts`, `config-store.ts` — config động.
- `file-storage-service.ts`, `workspace-service.ts`.

## Vai trò trong hệ thống
- UI → Agent qua **HTTP + SSE** (streaming chat).
- Agent → **Claude** qua Anthropic SDK (`ANTHROPIC_API_KEY`).
- Agent → **API** qua **MCP tools** (`mcp/task9-api-mcp.ts`).
- Agent → **Metabase** qua `@cognitionai/metabase-mcp-server`.

## Deploy (KHÁC UI/API — build local → push → pull VPS)
1. Merge feature → `main`, commit conventional (`feat: ...`).
2. Build local cross-platform: `docker buildx build --platform linux/amd64 -t andywilly/task9-agent:<tag> -f agent/Dockerfile.unified --push .` (chạy từ `services/`, context cần cả folder `agent/`). Tag: `main`=`latest`.
3. VPS do-122: `ssh do-122 "cd /home/tobi/task9-agent && docker compose -f docker-compose.prod.yml pull && ... up -d --force-recreate"`.
4. Chờ ~30s cho OpenClaw gateway start rồi test `POST :3001/api/chat`.
