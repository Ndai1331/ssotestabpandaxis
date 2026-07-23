---
type: concept
title: "Worker-Lite QC System"
created: 2026-06-27
updated: 2026-06-27
tags:
  - worker-lite
  - qc
  - ai
  - guestpost
status: mature
complexity: advanced
related:
  - "[[Task9 Platform Overview]]"
  - "[[Infrastructure & Servers]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Worker-Lite QC System

Realtime Guestpost/Textlink QC — AI-driven multi-tier link verification.

## Tech Stack

- **Path:** `services/worker-lite/`
- **Port:** 5005
- **Tech:** Python/FastAPI + Pydantic + BeautifulSoup
- **Domain:** `wl.task9.pro` / `qcinspect.task9.pro` (do-122)

## Multi-tier AI Validation

| Tier | Công nghệ | Khi nào dùng |
|------|-----------|-------------|
| **Tier 1** | HTML parse (BeautifulSoup) | Nhanh, không tốn token |
| **Tier 2/3** | Ollama (do-136: `llm.task9.pro`) | Khi T1 không đủ tin cậy |
| **Tier 4** | Claude API (arbiter) | Fallback cuối, high-confidence decision |

## Canonical Endpoints

```
POST /api/v1/qc/schema-detections   — AI schema mapping từ TSV
POST /api/v1/qc/jobs                — Tạo batch QC job
GET  /api/v1/qc/jobs/{jobId}        — Job status + progress
GET  /api/v1/qc/jobs/{jobId}/results — Job results
```

## Ollama Connection

Worker-lite kết nối đến do-136 qua:
- Public: `https://llm.task9.pro/v1`
- Internal: `http://152.42.250.136:11434/v1`
- API key: `"ollama"` (any string, không cần auth)

```python
from openai import OpenAI
client = OpenAI(base_url="https://llm.task9.pro/v1", api_key="ollama")
```
