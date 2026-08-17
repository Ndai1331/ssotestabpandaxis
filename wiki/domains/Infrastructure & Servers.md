---
type: domain
title: "Infrastructure & Servers"
created: 2026-06-27
updated: 2026-06-27
tags:
  - infrastructure
  - devops
status: mature
related:
  - "[[CI/CD Pipeline]]"
  - "[[do-187]]"
  - "[[do-122]]"
  - "[[do-136]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Infrastructure & Servers

Task9 chạy trên **3 DigitalOcean droplets**.

## Server Map

| Alias | IP | Vai trò |
|-------|----|---------|
| **do-187** | 206.189.147.187 | **Main Production** — UI, API, N8N, Metabase, Minio, status |
| **do-122** | 167.71.200.122 | **AI & Workers + Test/Staging** — Agent, Worker, GeoBlock, Ahrefs MCP, Worker-Lite, CI/CD, 2 bộ test+staging |
| **do-136** | 152.42.250.136 | **LLM Inference** — Ollama, port 11434 public |

## do-187 — Main Production

Subdomains: `task9.pro` (UI), `api.task9.pro`, `dashboard.task9.pro` (Metabase), `workflow.task9.pro` (N8N), `minio.task9.pro`, `status.task9.pro`

## do-122 — AI & Workers

- SSH alias: `do-122`, user `tobi`, home `/home/tobi`
- Chạy **2 bộ container song song**: `task9-{ui,api}-test` (ports 8085/7093, DB `qcadmin_test`) + `task9-{ui,api}-staging` (ports 8086/7094, DB `qcadmin_stag`)
- Route qua nginx-proxy-manager theo domain
- Auto-deploy: CI → webhook N8N `deploy-staging` → `deploy-listener.py` (port 9998, systemd) → `docker run` tag tương ứng
- DB clone: N8N workflow `TxrqpeGZi9OERH5K` chạy 03:00 hàng ngày, clone prod → test/stag
- Subdomains: `agent`, `cpd`, `geoblock`, `wl` (worker-lite), `ahref-api`, `cicd`, `proxy`, `subsytem`, `test/stag.task9.pro`, `testapi/stagapi.task9.pro`

> ⚠️ **Laptop migration backup** tại `~/laptop-migration/` (3 bundles, SHA256 documented trong `docs/handoff/laptop-migration-260613-config-backup.md`). Xóa sau khi restore xong.

## do-136 — LLM / Ollama

- SSH alias: `do-136`
- Ollama systemd service, port 11434 bind `0.0.0.0` (public, không auth)
- OpenAI-compatible API: `https://llm.task9.pro/v1` (public) hoặc `http://152.42.250.136:11434/v1` (internal)
- RAM: 7.8 GB, Disk: 155 GB, models chiếm ~42.5 GB

**Models local:**
- `gemma4:31b-it-q4_K_M` (19 GB, vision+tools+thinking)
- `gemma4:e4b` (9.6 GB), `gemma4:e2b` (7.2 GB)
- `qwen3.5:4b` (3.4 GB, tools+thinking), `qwen3-vl:4b-instruct` (3.3 GB, vision)

**Models cloud (proxy Ollama Cloud):** `kimi-k2.6:cloud`, `qwen3.5:cloud`, `gemma4:31b-cloud`, `minimax-m2.7:cloud`, `gpt-oss:120b-cloud`

> ⚠️ Port 11434 hiện open public, chưa có auth. Cân nhắc thêm firewall nếu cần.

## MySQL Databases

| DB | Metabase ID | Dùng cho |
|----|-------------|---------|
| `qcadmin` | 2 | **Operational data** — mọi CRUD table task9-api mới → đây |
| `seo_data` | 3 | SEO analytics — chỉ đọc từ API |
| `task9_ai` | 5 | AI/Agent data |
| `warehousedata` | 4 | Data warehouse |

> ⚠️ **Incident 2026-04-22:** Tạo nhầm `domain_price_eval_session` trong `seo_data` thay vì `qcadmin`. Xem [[DB Separation Rule]].

## MySQL Connection Note

Connection string **PHẢI có** `AllowPublicKeyRetrieval=True` — MySQL 8.0 dùng `caching_sha2_password`.
