---
type: domain
title: "Task9 Platform Overview"
created: 2026-06-27
updated: 2026-06-27
tags:
  - task9/platform
  - architecture
status: mature
related:
  - "[[Infrastructure & Servers]]"
  - "[[Database Architecture]]"
  - "[[CI/CD Pipeline]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Task9 Platform Overview

Nền tảng **quản lý SEO toàn diện** dạng microservices. Meta repo `task9org/task9-workspace` chứa config/docs, code nằm trong git submodules.

## Service Catalog

| Service | Path | Port | Tech | Vai trò |
|---------|------|------|------|---------|
| **UI** | `services/ui/` | 8080 | .NET 9 Blazor Server | Frontend dashboard |
| **API** | `services/api/` | 7093 | .NET REST | Backend CRUD + business logic |
| **Agent** | `services/agent/` | 3001 | Node.js/TypeScript | AI sidecar → Claude via SSE |
| **Worker** | `services/worker/` | 5000 | Python/Flask + SeleniumBase | CPD browser automation |
| **Worker-Lite** | `services/worker-lite/` | 5005 | Python/FastAPI | Realtime GP/TL QC, multi-tier AI |
| **GeoBlock** | `services/geoblock/` | — | Python | Domain geo accessibility check |
| **Ahrefs MCP** | `services/ahrefs-mcp/` | 3000 | Node.js/Fastify | Ahrefs domain analysis API |
| **Metabase** | `services/metabase/` | — | Metabase | SEO dashboards embedded vào UI |
| **N8N** | `n8n/` + subsytem.task9.pro | — | N8N self-hosted | Workflow automation |

## Core vs Worker

| | Core | Worker |
|---|---|---|
| Services | `ui`, `api` | `agent`, `worker`, `geoblock`, `ahrefs-mcp`, `worker-lite`, `metabase` |
| Worktree | ✅ Riêng per feature | ❌ Không |
| launch.json | ✅ | ❌ |
| Cách chạy | `dotnet run` / Claude preview | `docker compose up <svc>` |

## Communication Flow

```
UI ──(HTTP/JWT)──► API ──► MySQL (qcadmin)
 │                  ▲
 ├──(HTTP/SSE)──► Agent ──► Claude API
 │                  └──(MCP tools)──► API
 ├──(Webhook)──► N8N ──► MySQL (direct) / Worker / Telegram
 ├──(Direct SQL)──► ETL ──► MySQL (qcadmin)
 └──(HTTP/REST)──► Worker-Lite ──► Ollama (do-136) / Claude API
```

## Feature Modules (UI)

- SEO Management (keywords, rankings, reports, market share, budget eval)
- CPD — Content Performance Dashboard (link/banner checking)
- Payment Workflow (IC → Assistant → Manager approval)
- Realtime GP/TL QC (qua worker-lite, AI multi-tier)
- Geo Block Checker
- AI Chat (qua sidecar → Claude)
- IC SEO Resource management
- SSO Login (Google Workspace OAuth2/OIDC)
- User/Team management, Metabase embedding, ETL reporting

## Authentication

**SSO:** User → Google OAuth2/OIDC (PKCE) → `SsoCallbackController` → JWT → LocalStorage → `HttpClientService` Bearer  
**Legacy:** User → API `/api/auth/login` → JWT → LocalStorage  
**Authorization:** `UrlAuthorizationService.cs` — role-based URL whitelist. **Trang mới PHẢI được khai báo ở đây.**

## API Endpoints Chính

`/api/keywords`, `/api/keyword-rankings`, `/api/seo-payment-tickets`, `/api/cpd-links`, `/api/users`, `/api/teams`, `/api/brands`, `/api/seo-costs`, `/api/geo-blocks`, `/api/ic-seo-resources`

## URLs Production

| Service | URL |
|---------|-----|
| UI | https://task9.pro |
| API | https://api.task9.pro |
| Agent | https://agent.task9.pro |
| N8N | https://subsytem.task9.pro |
| Metabase | https://dashboard.task9.pro |
