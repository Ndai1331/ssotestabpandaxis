---
title: "Add Ahrefs keyword search volume to search-keyword-realtime"
description: "Integrate Ahrefs Keywords Explorer API to display keyword search volume, difficulty, and CPC data in the search-keyword-realtime UI page"
status: pending
priority: P1
branch: "feat/ahrefs-keyword-volume"
tags: ["ahrefs", "keyword-research", "ui-enhancement", "cross-service"]
blockedBy: []
blocks: []
created: "2026-05-08T06:17:09.656Z"
createdBy: "ck:plan"
source: skill
---

# Add Ahrefs keyword search volume to search-keyword-realtime

## Overview

Tích hợp Ahrefs Keywords Explorer API để hiển thị keyword search volume, difficulty, CPC trong UI page `search-keyword-realtime`. Hiện tại page này chỉ có SERP analysis, cần thêm keyword metrics tương tự SEMrush.

**Scope:**
- Extend `ahrefs-mcp` service với Keywords Explorer endpoints
- Add `KeywordsController` trong `task9-api` 
- Update UI để call API và display data
- Đồng bộ branch `feat/ahrefs-keyword-volume` trên cả 3 services

**Research Report:** `/plans/reports/researcher-260508-1314-ahrefs-keyword-volume.md`

## Phases

| Phase | Name | Status | Effort | Priority |
|-------|------|--------|--------|----------|
| 1 | [Research](./phase-01-research.md) | Pending | 1h | P1 |
| 2 | [Extend ahrefs-mcp](./phase-02-extend-ahrefs-mcp.md) | Pending | 4h | P1 |
| 3 | [Integrate API](./phase-03-integrate-api.md) | Pending | 3h | P1 |
| 4 | [Update UI](./phase-04-update-ui.md) | Pending | 3h | P2 |

**Total Effort:** ~11h

## Dependencies

**External:**
- Ahrefs MCP server phải có `keywords-explorer-overview` tool
- Ahrefs API quota đủ cho testing

**Internal:**
- Phase 2 blocks Phase 3 (cần ahrefs-mcp endpoint trước)
- Phase 3 blocks Phase 4 (cần task9-api endpoint trước)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ UI: search-keyword-realtime (Blazor)                        │
│ services/ui/.../SearchKeywordRealtime.razor                 │
└────────────────┬────────────────────────────────────────────┘
                 │ POST { keyword, country }
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ task9-api: KeywordsController                               │
│ POST /api/keywords/search-volume                            │
└────────────────┬────────────────────────────────────────────┘
                 │ HttpClientService
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ ahrefs-mcp: AnalyzeController                               │
│ POST /keywords-explorer/overview                            │
└────────────────┬────────────────────────────────────────────┘
                 │ MCP JSON-RPC
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Ahrefs MCP Server                                           │
│ https://api.ahrefs.com/mcp                                  │
└─────────────────────────────────────────────────────────────┘
```

## Success Criteria

- [ ] Ahrefs keyword volume data hiển thị trong UI
- [ ] Response time < 3s cho single keyword
- [ ] Error handling đầy đủ (API fail, quota exceeded)
- [ ] Branch `feat/ahrefs-keyword-volume` đồng bộ trên 3 services
- [ ] Docker image ahrefs-mcp được build và push
- [ ] All tests pass

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Ahrefs MCP tool name không đúng | High | Verify bằng list-tools trước khi implement |
| Response format khác research | Medium | Add flexible parsing logic |
| API quota limit | Medium | Add caching (TTL 24h) |
| Docker build fail | Low | Test local build trước |

## Notes

- **Branch naming:** `feat/ahrefs-keyword-volume` cho cả 3 services
- **Deploy order:** ahrefs-mcp → task9-api → ui
- **Testing:** Test với keyword "b52 club" (volume: 301K)
