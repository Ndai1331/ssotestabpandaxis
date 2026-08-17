---
type: source
title: "Plan seo-agent-ops-center"
created: 2026-06-27
updated: 2026-06-27
tags:
  - source
  - plan
  - ai-ops
  - ui
  - agent
status: mature
source_type: plan
source_path: "plans/260607-0923-seo-agent-ops-center/"
date_published: 2026-06-07
confidence: high
key_claims:
  - "Admin-only main menu/page cho SEO report AI ops; pinned bottom-left sidebar"
  - "Runtime trên do-122 dùng Ollama local gateway 127.0.0.1:11434 (Ollama Pro cloud models)"
  - "KHÔNG chạy ops trên do-187 prod; AI không có quyền destructive trong MVP"
  - "Status: implemented-mvp (branch codex_feat/seo-agent-ops-center)"
related:
  - "[[Task9 Platform Overview]]"
  - "[[do-122]]"
  - "[[Blazor Page Creation Checklist]]"
---

# Source: Plan SEO Agent Ops Center

**2026-06-07** · Status **implemented-mvp** · branch `codex_feat/seo-agent-ops-center` · P1.

## Goal

Tính năng **Admin-only main menu** cho AI-powered SEO operations — không chỉ widget "Hỏi AI" nổi, mà là ops entry point riêng:
- Nút pinned **bottom-left sidebar**, chỉ role `ADMIN`.
- Page chính: SEO Agent Ops Center. Context đầu tiên: `/report-seo-performance`.
- Runtime: [[do-122]] làm closed ops control-plane, Ollama local `127.0.0.1:11434` cho Ollama Pro cloud models.

## Non-Goals

KHÔNG chạy ops trên `do-187` prod · KHÔNG expose LLM public không auth · KHÔNG cho AI quyền destructive trong MVP · KHÔNG thay thế SEO reports hiện có · Admin-only trước (chưa multi-role).

## Architecture

```
Admin UI (sidebar Ops button → /admin/seo-agent-ops → capture /report-seo-performance context)
  → task9-agent on do-122 (SEO ops routes, playbook runner, audit log, Ollama gateway /v1)
  → Ollama Pro cloud models
  → Task9 data/API (task9-api on do-187, keyword-ranking endpoints, optional Metabase MCP)
```

## Files chính

- UI report: `services/ui/.../Components/Task9/BaoCaoSEOTong.razor(.cs)`
- Menu: `PageLayout.razor.cs` · Auth: `UrlAuthorizationService.cs` (xem [[Blazor Page Creation Checklist]])
- Phases 00–06 + `seo-agent-ops-runbook.md` trong plan folder.
