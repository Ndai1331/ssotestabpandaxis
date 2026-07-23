---
type: concept
title: "Self-Learning Memory System"
created: 2026-06-27
updated: 2026-06-27
tags:
  - memory
  - ai
  - dx
status: developing
complexity: intermediate
related:
  - "[[Task9 Platform Overview]]"
sources:
  - "[[Journal Self-Learning System Design]]"
---

# Self-Learning Memory System

Hệ thống memory 3-layer cho AI agents trong task9 workspace. Designed 2026-05-25.

## Vấn đề Giải quyết

Memory cũ có 3 gaps:
1. **Không có root cause** — chỉ ghi "đừng làm X", AI không thể generalize
2. **Reactive-only** — chỉ tạo SAU KHI lỗi xảy ra
3. **Không prune** — memory stale không được dọn dẹp

## Kiến trúc 3 Layer

| Layer | Tên | Cơ chế |
|-------|-----|--------|
| 1 | **Capture** | Template chuẩn với Root Cause + Prevention Rule + Related links |
| 2 | **Reinforce** | `UserPromptSubmit` hook — scan keyword nguy hiểm → in checklist |
| 3 | **Prune** | Audit script + checklist thủ công mỗi 2-4 tuần |

## Memory Prefix Taxonomy

- `feedback-*` — lỗi behavior từ trải nghiệm (AI đã làm sai)
- `constraint-*` — ràng buộc cứng hệ thống (prod DB, server access)
- `pattern-*` — best practice đã validate nhiều lần

## Hook Keywords (Tier 2 — Reinforce)

Hook detect các phrase nguy hiểm và hiển thị checklist:
- `merge main`, `push origin main`
- `ALTER TABLE`, `DROP TABLE`
- `docker push`
- `git reset --hard`

## Prune Strategy

- Không xóa file ngay — chỉ remove khỏi MEMORY.md index
- Giữ file 1 sprint để có thể rollback
- `project-*` entries stale nhanh nhất → ưu tiên review

## Status

Plan: `plans/260525-1431-task9-self-learning-system/` — chờ implement đầy đủ.
