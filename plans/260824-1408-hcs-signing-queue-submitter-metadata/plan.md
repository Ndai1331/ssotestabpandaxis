---
title: "Hiển thị người trình và phòng ban trong hàng đợi ký"
description: "Bổ sung projection submitter/department cho signing queue của HCS_web_free_license mà không đổi schema."
status: pending
priority: P2
effort: 7h
branch: main
tags: [bugfix, hcs, signing, workflow, blazor, api]
blockedBy: []
blocks: []
created: 2026-08-24
---

# Hiển thị người trình và phòng ban trong hàng đợi ký

## Outcome

Hai cột đã tồn tại trong `DocumentSigning.razor` nhưng thường rỗng: workflow document không bảo đảm `FromUserId`/`OrganizationUnitId`, còn cache contacts chỉ lấy tối đa 50 user và loại current user. Kết quả cần đạt: mỗi hàng workflow dùng đúng người khởi tạo workflow, department hiện tại của người đó, và cùng một dữ liệu cho cột, filter, CSV.

## Quyết định kiến trúc

Không migration và không ghi metadata mới vào aggregate trong scope này. `OrganizationUnitId` hiện mang nghĩa đơn vị nhận của luồng gửi; dùng lại cho submitter sẽ gây sai dữ liệu cũ. Với workflow, canonical submitter là actor của history `Created`, fallback `FromUserId` chỉ cho dữ liệu legacy thiếu history. Bổ sung một lookup batch identity additive, giới hạn theo các user ID thật sự có trong queue.

## Data flow

```text
/api/documents?sourceType=3 + /api/workflows/instances
  -> DocumentDto (History.Created actor, FromUserId)
  -> SubmitterUserId helper
  -> BFF /api/identity/workflow-assignees/lookup?userId=... (batch)
  -> active identity: DisplayName/UserName + primary OrganizationUnitId
  -> existing organization department lookup: department name
  -> queue row, submitter/department filters, CSV export
```

## Phases

| Phase | Nội dung | Depends | Status |
|---|---|---|---|
| [01](phase-01-directory-contract.md) | Identity batch lookup, auth, bounds | — | pending |
| [02](phase-02-queue-projection-ui.md) | Client projection, UI/filter/export/fallback | 01 | pending |
| [03](phase-03-regression-validation.md) | Unit, contract, E2E/manual regression và gates | 02 | pending |

## Ownership / dirty-worktree guard

| Phase | Files được phép chạm |
|---|---|
| 01 | Existing Platform `WorkflowAssigneeCandidatesController.cs` và contract cần thiết |
| 02 | Existing `DocumentModels.cs`, `DocumentClient.cs`, `DocumentSigning.razor` |
| 03 | Existing gateway/document test files; test artifact/log tạm ngoài source |

Không phase nào reset, checkout, format hoặc staging file. Các file trên đều đang dirty ở mức khác nhau; implementation phải merge additive vào diff hiện tại, đặc biệt signing-provider changes trong `DocumentSigning.razor`, `DocumentModels.cs` và workflow resolver changes. Plan `plans/260824-hcs-signing-submitter-department/` là bản nháp user-owned cho cùng chủ đề, chỉ tham chiếu, không ghi đè.

## Compatibility, rollback, success

- Backward-compatible: `DocumentDto` và DB giữ nguyên; workflow cũ dùng history/fallback; endpoint mới không đổi auth của endpoint exact-user hiện tại; BFF đã wildcard-route `/api/identity/{**catch-all}`.
- Failure behavior: lookup identity/department lỗi thì queue vẫn render, hiển thị `—`, không lộ GUID và không làm hỏng contacts/signatures; các lookup khởi tạo độc lập.
- Rollback: bỏ route/client projection theo từng phase; không cần data rollback hoặc migration rollback.
- Done đo được: workflow submitter ngoài 50 contacts và current user đều hiện tên; department đúng primary mapping; filter/CSV khớp row; không N+1; targeted build/test pass; diff chỉ nằm trong ownership matrix.

## Risks / unresolved questions

Risks chính: nhầm actor với sender (M×H), nhầm recipient OU với submitter OU (M×H), PII enumeration qua batch endpoint (M×H), thiếu quyền department lookup (M×M), và xung đột dirty diff (H×H). Mitigation chi tiết ở từng phase.

Unresolved: queue role hiện có chắc chắn được `OrganizationPermissions.Departments` hay không; nếu không, cần quyết định thêm read-only policy/endpoint tối thiểu trước Phase 02. Cần fixture/runtime account để xác nhận screenshot E2E sau khi code được triển khai.
