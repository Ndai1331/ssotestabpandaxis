---
phase: 7
title: Reporting and Signing KPI
status: pending
effort: 1-2w
dependsOn: [3]
---

# Phase 07 — Reporting + Signing KPI

## Goal

`hanhchinhso.ReportingService` (:44385, DB `hanhchinhso_Reporting`): dynamic report menu (iframe) + Signing KPI. Optional legacy SQL Server ETL.

## Source (HCS)

- `Reports` (host-level menu?)
- `SigningKpiReports`
- `tools/legacy-signature-etl` + `LegacySigningReport`

## Steps

1. Scaffold ReportingService
2. Port report definitions + Mud host page (iframe allowlist)
3. Port Signing KPI queries — đọc từ Document DB qua **read replica / integration events / ETL table** — **không** cross-DB join ad-hoc từ Document; prefer materialized KPI trong Reporting DB fed by events/jobs
4. Legacy ETL: chỉ nếu còn nhu cầu LVT — feature-flag
5. Parity checklist

## Success criteria

- [ ] Ít nhất 1 report menu + 1 KPI dashboard lab
- [ ] Không phá DocumentService schema

## Risks

- Cross-service reporting anti-pattern — bắt buộc sync/ETL rõ
- Legacy SQL Server dependency — optional
