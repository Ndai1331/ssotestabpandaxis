---
type: domain
title: "Codebase — Workers (Python)"
created: 2026-06-28
updated: 2026-06-28
tags:
  - task9/worker
  - codebase
  - python
status: mature
related:
  - "[[Task9 Platform Overview]]"
  - "[[CPD Auto-Detect System]]"
  - "[[Worker-Lite QC System]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Codebase — Workers (Python)

Nhóm worker Python làm browser automation & QC. Đều deploy qua **Docker image**, KHÔNG dùng worktree, KHÔNG CI prefix.

## services/worker (qc_tray) — CPD browser automation
Flask, port 5000, SeleniumBase. Repo `services/worker`. Image Docker Hub `andywilly/worker-slim`.

Cấu trúc `core/`:
| File | Vai trò |
|------|---------|
| `worker.py` / `auto_worker.py` | Entry process. |
| `task_processor.py`, `task_validator.py`, `task/` | Nhận & xử lý task quét. |
| `flow_executor.py` | Điều phối luồng quét 1 site. |
| `browser/`, `selenium_cdp.py`, `browser_setup.py` | Điều khiển Chromium (CDP). |
| `http_brand_detector.py` | Phát hiện brand qua HTTP (nhanh, không mở browser). |
| `lite-scan-executor.py`, `lightweight_checker.py` | Lite scan (gọi từ N8N). |
| `publisher_intelligence.py` | Phân tích publisher site. |
| `geo_check_browser.py` | Geo check qua browser. |
| `platform_adapters/`, `storage/`, `routes/`, `config.py` | Hạ tầng. |
| `server/routes/` | Flask routes: `status.py`, `dashboard.py`, `manual.py`, `settings.py`, `browser_test.py`. |

`n8n_workflows/` — JSON workflow CPD nằm cùng repo worker (vd `cpd_smart_batch_lite.json`).

**Deploy:** `bash platforms/linux-docker/build-push-slim.sh test` (staging) / `... latest` (prod). Flow: feature branch → `test` → `main`. Dockerfile `platforms/linux-docker/Dockerfile.slim`.

> Logic CPD: `is_auto=1` = TẮT quét. Brand whitelist = cột `brand` trong `cpd_checker_tool_rows` (is_auto=0/NULL). Xem [[CPD Auto-Detect System]] — KHÔNG sửa nhầm.

## services/worker-lite — Realtime GP/TL QC
Python/FastAPI, port 5005. Multi-tier AI: Tier1 HTML regex → T2/T3 Ollama (do-136) → T4 Claude fallback. Xem [[Worker-Lite QC System]]. Deploy qua `platforms/linux-docker/`.

## services/geoblock — Domain geo accessibility
Python (FastAPI + SeleniumBase) + `frontend/` (React/Vite trong `frontend/src`). Kiểm tra 1 domain có bị chặn theo quốc gia. `static/dist/` build output, `data/` kết quả.

## services/ncc-worker — NCC price worker
Python, port 5010 (submodule, hiện rỗng trên máy này — chưa checkout). Lấy giá NCC.

## Skill routing khi sửa
File `.py` ở worker/geoblock → load skill `task9-python` (Flask/FastAPI + SeleniumBase + Docker). Workflow `.json` → skill `task9-n8n`.
