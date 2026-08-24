# Phase 02 — Queue projection and UI

Status: `pending`  
Priority: `P1`  
Depends on: Phase 01 contract verified  
Owner: Blazor client

## Implementation steps

1. Trong existing `DocumentModels.cs`, thêm pure/internal helper lấy submitter ID: với `SourceType.Workflow`, chọn `History` action `Created` actor trước, rồi fallback `FromUserId`; không dùng `Document.OrganizationUnitId` làm submitter department.
2. Trong existing `DocumentClient.cs`, thêm method gọi batch lookup với ID distinct, query encoding chuẩn và bounded/chunked theo giới hạn Phase 01. Deserialize candidate list thành dictionary theo `UserId`; không log token, tên hoặc raw response.
3. Trong `DocumentSigning.razor`, sau khi queue dựng xong, collect submitter IDs và thực hiện lookup một lần (queue hiện tại tối đa 100 rows). Tách contacts, departments, signatures thành các try/catch độc lập để lỗi Chat không che lookup department/identity.
4. Dùng resolved candidate cho `SubmitterName`, department ID/name, `IsSubmittedBy`, department filter và `ExportAsync`. Fallback tên theo thứ tự `DisplayName -> UserName -> contact cache -> —`; department không tìm thấy hoặc lỗi lookup hiển thị `—`, không hiển thị GUID.
5. Clear/rebuild dictionary khi reload; tránh stale result ghi đè queue mới nếu user refresh nhanh. Không thay đổi signing-provider fields đang dirty trong component/models.

## Data flow and compatibility

Existing `/api/documents` và `/api/workflows/instances` vẫn là nguồn queue. Chỉ bổ sung enrichment client-side cho các row đang hiển thị; document cũ không cần backfill. Queue vẫn render nếu batch hoặc department request thất bại. Với legacy workflow thiếu cả history actor và `FromUserId`, tên/department là `—` nhưng row không mất.

## Failure modes / risks

| Failure | L | I | Mitigation |
|---|---:|---:|---|
| Chọn sender thay vì workflow submitter | M | H | helper deterministic: `Created` actor trước; test cả hai ID khác nhau |
| Dùng recipient OU làm department | M | H | department chỉ lấy candidate OU; không đọc `Document.OrganizationUnitId` cho workflow |
| Chat/Organization 403 làm blank toàn page | M | M | requests độc lập; identity/department failure chỉ làm `—` |
| Queue > batch cap | L | M | chunk theo 100 hoặc giữ explicit bounded failure; không gọi all-users |
| Refresh race giữ dictionary cũ | M | M | generation/cancellation hoặc clear trước load; chỉ publish map cùng queue snapshot |

## Success criteria

- Row có submitter không nằm trong 50 contacts đầu vẫn hiện đúng display name.
- Current-user submitter vẫn hiện đúng tên dù Chat contacts loại user đó.
- Department hiển thị theo primary OU của submitter, khác recipient OU nếu fixture có hai OU.
- Submitter/department filter và CSV dùng cùng projection với cell, không còn OR-match sai theo `FromUserId` trên workflow.
- Một queue load hiện tại phát sinh tối đa một identity batch request và không có request từng row.

## Rollback

Disable enrichment call và revert helper/client/UI hunks theo file; giữ nguyên queue API và dirty signing-provider behavior. Không cần sửa dữ liệu hoặc chạy migration ngược.
