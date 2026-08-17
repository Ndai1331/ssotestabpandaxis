---
phase: 2
title: Organization service
status: in_progress
effort: 1-2w
dependsOn: [1]
---

# Phase 02 — Organization service

## Goal

`hanhchinhso.OrganizationService` (:44370, DB `hanhchinhso_Organization`) quản lý
`Units`, `Positions`, `MasterData`. Cơ cấu phòng ban và thành viên phòng ban dùng
**ABP Identity Organization Units (OU)**, không tạo aggregate/bảng/API trùng lặp.

## Source (HCS)

- Domains nguồn: `Departments`, `Units`, `Positions`, `UserDepartments`, `MasterDatas`
- Mapping đích: `Departments` → ABP OU; `UserDepartments` → ABP OU members
- AppServices: `*AppService` / Extended tương ứng
- Path gốc: `services/HCS_web/src/HC.*`

## Target

```
services/abp-blazor/services/organization/
  hanhchinhso.OrganizationService/
  hanhchinhso.OrganizationService.Contracts/
  hanhchinhso.OrganizationService.Tests/
```

## Steps

1. Clone LanguageService → rename Organization (port **44370**, audience `OrganizationService`)
2. Wire `.abpsln`, gateway routes `/api/organization-management/**`, `Default.abprun.json`
3. OpenIddict: API scope/resource + add scope to Blazor + Swagger clients (serialize vs Elsa seeder edits)
4. Port `Units`, `Positions`, `MasterData`; reuse ABP OU cho phòng ban + membership
5. EF migration **mới** + runtime migrator
6. Blazor Client: Contracts ref + Mud pages (list/create/edit) theo blazorise-mud-map
7. Menu contributor + permission seed cho roles lab
8. Optional ETL: copy master data từ HCS DB lab

> Quyết định local lab 2026-07-24: schema custom Department/UserDepartment chưa
> phát hành/deploy. Migration chuyển đổi chủ đích drop hai bảng thử nghiệm và dữ
> liệu lab; không dùng migration này cho môi trường có dữ liệu thật. Khi có môi
> trường ngoài local phải có ETL/reconciliation sang ABP OU trước khi drop.

## Success criteria

- [ ] CRUD Units, Positions, MasterData qua UI Mud
- [ ] CRUD phòng ban + phân công user qua ABP Identity Organization Units
- [ ] API qua Gateway authenticated
- [ ] Parity checklist rows Org/MasterData = done
- [ ] Tests smoke/integration tối thiểu

## Risks

- OU và user membership thuộc IdentityService; không copy Identity DB và không tạo
  `OrganizationDepartments` / `OrganizationUserDepartments`.
- MasterData polymorphic types — giữ enum/string type như HCS

## Depends

Phase 1 conventions. IdentityService đã chạy (SSO Phase 1).
