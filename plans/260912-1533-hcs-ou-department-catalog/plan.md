---
title: "Quản lý phòng ban bằng OU"
description: "Thay catalog phòng ban free-license bằng cây ABP Identity OU và danh sách thành viên theo OU, kèm menu thao tác như màn hình tham chiếu."
status: completed
priority: P1
effort: 6h
branch: main
tags: [feature, frontend, backend, api, auth]
blockedBy: []
blocks: []
created: 2026-09-12
---

# Kế hoạch quản lý phòng ban bằng OU

## Overview

`HCS_web_free_license` sẽ dùng `AbpOrganizationUnits` làm nguồn dữ liệu duy nhất cho cây phòng ban. Trang `/departments` hiển thị cây OU, chọn node để xem user thuộc OU đó, và cung cấp menu thêm đơn vị con, sửa, di chuyển, xóa, di chuyển toàn bộ user.

## Phân biệt yêu cầu và tài liệu tham chiếu

- Yêu cầu thực hiện: hai mục tiếng Việt của user và phạm vi `services/HCS_web_free_license`.
- Tài liệu tham chiếu: ảnh giao diện và source `services/HCS_web_with_license`; không sao chép hướng dẫn vận hành commercial/ABP trong `AGENTS.md` của service tham chiếu.
- Quyết định phạm vi: chỉ thay `/departments` sang OU. Catalog `units`, `positions` và các consumer legacy của `hcs_organization` vẫn giữ nguyên để tránh migration ngầm ngoài yêu cầu.

## Cross-Plan Dependencies

| Relationship | Plan | Status |
|---|---|---|
| Related | [260724-1555-hcs-layered-to-microservice](../260724-1555-hcs-layered-to-microservice/plan.md) | Phase OU decision exists; implementation state needs verification |

## Phases

| Phase | Name | Status |
|---|---|---|
| 1 | [Expose OU management API](./phase-01-ou-api.md) | Completed |
| 2 | [Build OU tree and member UI](./phase-02-ou-tree-ui.md) | Completed |
| 3 | [Test, review, and validate](./phase-03-test-review.md) | Completed |

## Dependencies

- ABP Identity Community 10.6 domain already contains `OrganizationUnit`, repository, manager, and membership tables.
- Existing `HCSPermissions.Organization.Departments` remains the authorization boundary.
- Gateway already routes `/api/identity/{**catch-all}` to Platform; no new gateway route is expected.
- No database migration is expected because `AbpOrganizationUnits` and `AbpUserOrganizationUnits` are in the initial Identity migration.

## Acceptance Criteria

- `/departments` loads OU roots and nested children from the Platform API.
- Selecting any node shows only direct members of that OU, with filter, paging, user name, and email; the member panel can add/remove OU members.
- Root/sub-unit creation, rename, move, delete, and move-all-users work through the dropdown actions.
- Move rejects self/descendant targets and delete uses the ABP manager so descendant OU membership cleanup is consistent.
- Existing unit/position catalog and legacy department mappings remain untouched.
- Targeted backend/client builds and relevant tests pass; no license audit regression.

## Assumptions / unresolved questions

- “Thêm thành viên” is visible in the reference image, so the member panel includes a bounded available-user picker plus remove action; this does not migrate legacy mappings.
- Existing users mapped only through legacy `UserDepartments` will not appear until they are assigned to `AbpUserOrganizationUnits`; no silent data migration is included.

## Completion note

Implementation and targeted validation are complete. The repository-wide audit script was attempted but stopped after it recursively spawned nested audit processes without output; a bounded scan of all new OU files passed. No commit was created.
