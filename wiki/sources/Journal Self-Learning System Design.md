---
type: source
title: "Journal Self-Learning System Design"
created: 2026-06-27
updated: 2026-06-27
tags:
  - source
  - journal
  - memory
  - dx
status: mature
source_type: journal
source_path: "docs/journals/260525-task9-self-learning-system-design.md"
date_published: 2026-05-25
confidence: high
key_claims:
  - "Memory system cũ có 3 gaps: không root cause, reactive-only, không prune"
  - "Giải pháp 3-layer: Capture template + UserPromptSubmit hook reinforce + prune audit thủ công"
  - "Memory taxonomy: feedback-* / constraint-* / pattern-*"
related:
  - "[[Self-Learning Memory System]]"
---

# Source: Journal — Self-Learning System Design

**2026-05-25** — Design session cho memory system của AI agent trong workspace.

## Problem

Memory cũ (13 entries) có 3 structural gaps:
1. **Không root cause** — chỉ ghi "đừng làm X" → không generalize được.
2. **Reactive-only** — memory tạo SAU lỗi, không phòng ngừa TRƯỚC.
3. **Không prune** — memory stale (vd do-16 đã xóa) không được review.

## Decision: 3-Layer Architecture

| Tầng | Tên | Cơ chế |
|------|-----|--------|
| 1 | Capture | Template chuẩn — thêm Root Cause + Prevention Rule + Related links |
| 2 | Reinforce | `UserPromptSubmit` hook — scan keyword nguy hiểm → in checklist (zero-friction) |
| 3 | Prune | Audit script + checklist thủ công mỗi 2–4 tuần |

## Key design choices

- **Taxonomy:** `feedback-*` (lỗi behavior), `constraint-*` (ràng buộc cứng hệ thống), `pattern-*` (best practice đã validate).
- **Hook scope:** detect exact phrases (`merge main`, `push origin main`, `ALTER TABLE`, `DROP TABLE`, `docker push`, `git reset --hard`) — tránh false positive.
- **Prune conservative:** không xóa file ngay, chỉ remove khỏi index MEMORY.md, giữ 1 sprint để rollback. `project-*` stale nhanh nhất.
- Không dùng automated cron prune — "memory nào còn valid" cần human judgment.

> Page khái niệm: [[Self-Learning Memory System]]. Plan: `plans/260525-1431-task9-self-learning-system`.
