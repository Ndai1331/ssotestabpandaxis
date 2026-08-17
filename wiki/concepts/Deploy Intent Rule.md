---
type: concept
title: "Deploy Intent Rule"
created: 2026-07-13
updated: 2026-07-13
tags:
  - git-flow
  - ci-cd
  - rule
status: active
related:
  - "[[Git Flow 3 Tầng]]"
  - "[[CI/CD Pipeline]]"
---

# Deploy Intent Rule

> Quy định thêm 2026-07-03 (commit `d866633`). Áp dụng cho MỌI commit có prefix `[WEB]`/`[API]`.

## Luật

- **Agent CHỈ được tạo commit mang prefix `[WEB]`/`[API]` (hoặc merge/push lên `test`/`staging`/`main`) KHI user nói rõ "deploy"** (hoặc "OK deploy", "lên test/staging/prod"). Không có chỉ thị deploy = KHÔNG kích build.
- **Mọi commit thường ngày** (code trên branch feature, docs, config, refactor chưa deploy) → message **KHÔNG** chứa `[WEB]`/`[API]`. Dùng conventional commit trơn (`docs:`, `feat:`, `fix:`, `chore:`...).
- **Commit chỉ đổi docs/markdown/config** → KHÔNG BAO GIỜ mang prefix build, kể cả khi user nói deploy (không có gì để build).

## Lý do

Workflow GHA chỉ trigger `on push` với `main`/`test`/`staging`; prefix quyết định có build image hay không. Đặt prefix bừa = build/deploy ngoài ý muốn, tốn tài nguyên CI.

## Tham chiếu

- `CLAUDE.md` § "🔴 BẮT BUỤC: CHỈ kích build GHA khi user nói deploy"
- Xem thêm: [[Git Flow 3 Tầng]], [[CI/CD Pipeline]]