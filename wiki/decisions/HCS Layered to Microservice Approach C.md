---
type: decision
title: HCS Layered to Microservice Approach C
updated: 2026-07-24
---

# HCS Layered to Microservice Approach C

**Status:** Approved 2026-07-24

## Decision

Migrate `services/HCS_web` (ABP layered + Blazorise) → `services/abp-blazor` (microservice + MudBlazor) bằng **Approach C: fat-core rồi peel**.

- Rewrite module-by-module; HCS_web sống đến parity (strangler nhẹ).
- **document-service** gộp Documents + Workflow + Signing (+ Mobile / REMOTE_CA) trước khi peel.
- Shared-schema multi-tenant trong từng service (`TenantId` + ABP data filter); mỗi MS một database Postgres. Không dùng database-per-tenant trong roadmap này.
- Keycloak: tiến dần; giữ AuthServer federation (Approach A) ngắn hạn.
- UI: port MudBlazor theo từng phase domain.
- Permission mới dùng `HanhChinhSo.{Module}.{Action}`; không tiếp tục namespace `HC.*`.

## Boundaries

- `services/document/` (`:44380`) sở hữu workflow nghiệp vụ tài liệu HCS.
- `services/workflow-service/` (`:44395`) thuộc Elsa và không phải đích port workflow HCS.
- Mỗi service chỉ truy cập database của chính nó; tích hợp chéo service đi qua HTTP contract hoặc distributed event.

## Target services

| Service | Port | Database |
|---|---:|---|
| OrganizationService | 44370 | `hanhchinhso_Organization` |
| DocumentService | 44380 | `hanhchinhso_Document` |
| ProjectService | 44381 | `hanhchinhso_Project` |
| CalendarService | 44382 | `hanhchinhso_Calendar` |
| SurveyService | 44383 | `hanhchinhso_Survey` |
| CollaborationService | 44384 | `hanhchinhso_Collaboration` |
| ReportingService | 44385 | `hanhchinhso_Reporting` |

## Why

Core HCS gắn chặt; tách sớm phá parity ký số/mobile. Big-bang cutover rủi ro cao khi src cũ đã chạy.

## Report

`plans/reports/brainstorm-260724-1549-hcs-layered-to-microservice.md`

## Related

- [[Codebase — ABP Blazor]]
- [[SSO Phase 1 Approach A]]
- Source: `services/HCS_web`
