---
type: domain
title: "N8N Workflows"
created: 2026-06-27
updated: 2026-06-27
tags:
  - n8n
  - automation
status: mature
related:
  - "[[CPD Auto-Detect System]]"
  - "[[Ahrefs Integration]]"
  - "[[Infrastructure & Servers]]"
sources:
  - "[[workspace-architecture.md]]"
  - "[[Handoff Phase3 Ahrefs Site Explorer]]"
---

# N8N Workflows

## Access

- **Base URL:** `https://subsytem.task9.pro`
- **API Key:** `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1YTVjYzYyYi03MDZmLTRkNjQtYmFhNi0zMTVkNjA3NDFhY2EiLCJpc3MiOiJuOG4iLCJhdWQiOiJwdWJsaWMtYXBpIiwianRpIjoiY2FjZDZlZWYtODNjYS00NTU3LTk0MTAtNTA3YjUxZWE0YzBhIiwiaWF0IjoxNzc4NTU5MjE3fQ.4mOVE0uS_Ylq_-UoGYkfNnyb3EvlNb7bGp4KAICfbUo`
- **API Docs:** `https://subsytem.task9.pro/api/v1/docs`

## Inventory Workflows

| ID | Tên | Schedule | Status | Ghi chú |
|----|-----|----------|--------|---------|
| `ZcSMgWpbZ27k8xgV` | CPD Batch Lite | Daily 7AM | Active | Quét banner tự động |
| `5GVPcmzoJJsTkpg3` | Ahrefs Site Explorer Weekly (Mainsites v2) | Mon 6AM | Active | DR + traffic + backlinks |
| `TxrqpeGZi9OERH5K` | DB Clone Orchestration | Daily 3AM | Active | Clone prod → test/stag |
| `deploy-staging` (webhook) | Auto Deploy do-122 | On CI push | Active | docker run theo tag |
| `n8n_tlgp_ai_import` | Telegram TLGP Import | Webhook | — | Import qua Telegram |
| `ahref_api` | Ahrefs API | — | — | Ahrefs integration cũ |

## Credential IDs Thường Dùng

| Credential | ID | Dùng bởi |
|-----------|-----|---------|
| QCADMIN PRO (MySQL) | `HAodCNFUpt4MrPhb` | Nhiều workflows |
| seo_data (MySQL) | `AJOxfO4I7aFS17G3` | Ahrefs Site Explorer |
| Ahrefs httpBearerAuth | `xHrdmsbyjaWQEYKb` | Ahrefs workflows |

## Update Workflow qua API

```bash
# PUT update
curl -X PUT "https://subsytem.task9.pro/api/v1/workflows/<ID>" \
  -H "X-N8N-API-KEY: <KEY>" \
  -H "Content-Type: application/json" \
  -d @/tmp/n8n_payload.json
```

## Gotchas

- `N8N_BLOCK_ENV_ACCESS_IN_NODE=true` → Code node không đọc được `process.env` → dùng HTTP Request nodes + credential
- MySQL credential PHẢI có `AllowPublicKeyRetrieval=True` (MySQL 8.0)
- `mainsites` không có cột `deleted` → filter: `WHERE domain IS NOT NULL AND domain != ''`
