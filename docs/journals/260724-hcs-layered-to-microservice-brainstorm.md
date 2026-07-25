---
title: Journal — HCS layered to microservice brainstorm
date: 2026-07-24
tags: [brainstorm, migration, abp, microservice, hcs]
---

# Journal — HCS → microservice brainstorm

## Context

User muốn chuyển `HCS_web` (layered + Blazorise) sang `abp-blazor` (MS + MudBlazor). Phase 1 SSO đã xong trên target.

## What happened

- Scout song song 2 codebase: HCS giàu domain (~45 entity, Doc+WF+Sign coupling cao); abp-blazor gần như template + KC.
- User chốt: rewrite toàn bộ, timeline dài OK, shared DB, tiến Keycloak, mobile/REMOTE_CA parity sớm.
- Duyệt **Approach C**: fat-core rồi peel; HCS_web freeze feature đến khi parity.

## Decisions

- Document+Workflow+Signing = 1 service ban đầu.
- Roadmap Phase 0→7; UI Mud theo phase domain.
- Auth giữ Approach A ngắn hạn.

## Next

- Chờ user: `/ck:plan` Phase 0+1 (khuyến nghị) vs plan full roadmap.
- Report: `plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`
