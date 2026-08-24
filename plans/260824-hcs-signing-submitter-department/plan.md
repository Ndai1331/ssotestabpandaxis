# Sửa người trình và phòng ban trong danh sách trình ký

## Mục tiêu

Hiển thị đúng người trình và phòng ban của người trình trong bảng trình ký của `HCS_web_free_license`, đồng thời giữ tương thích với các workflow đã có metadata.

## Chẩn đoán

- `DocumentSigning.razor` đã có hai cột `Người trình` và `Phòng ban`.
- Các cột dựa vào `DocumentDto.FromUserId` và `DocumentDto.OrganizationUnitId`.
- Workflow được tạo bằng `DuplicateAsWorkflow` hoặc từ template nhưng chưa gán `FromUserId`, nên tên người trình phải fallback qua lịch sử tạo; `OrganizationUnitId` của document lại là đơn vị nhận trong luồng gửi, không phải phòng ban người trình.
- Tải contacts, departments và signatures trong cùng một `try/catch`, khiến một lỗi phụ có thể làm thiếu lookup còn lại.

## Phạm vi thực hiện

1. Thêm thao tác gán người trình workflow trong aggregate.
2. Gán người trình khi start workflow, giữ nguyên nghĩa đơn vị nhận của document.
3. Thêm API đọc mapping phòng ban HCS theo user và tách các lookup khởi tạo ở trang trình ký thành các khối độc lập.
4. Bổ sung test hồi quy cho metadata người trình.
5. Build/test các project liên quan và rà soát diff, không commit.

## Tiêu chí hoàn thành

- Workflow mới có `FromUserId`; phòng ban hiển thị được tra từ mapping HCS của người trình.
- Bảng trình ký hiển thị được tên người trình và phòng ban khi lookup có dữ liệu.
- Không làm hỏng workflow cũ hoặc luồng resubmit khi metadata đã tồn tại.
- Test và build liên quan pass.
