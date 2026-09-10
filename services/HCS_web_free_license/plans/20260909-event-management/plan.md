# Kế hoạch triển khai: Quản lý sự kiện

## 1. Nguồn yêu cầu và cách diễn giải

- **Yêu cầu trực tiếp của người dùng:** lập plan cho tính năng trong tài liệu đính kèm và triển khai vào codebase hiện tại.
- **Nội dung tài liệu `Quan_ly_Su_kien.pages`:** đặc tả nghiệp vụ và mockup cho Dashboard sự kiện; danh sách/tạo/chỉnh sửa/xóa sự kiện; sinh QR; tài liệu đính kèm; quản lý người tham dự theo từng sự kiện; thêm nhanh, tìm kiếm/lọc, xác nhận/từ chối, check-in, import và xuất dữ liệu.
- **Nguyên tắc áp dụng:** tài liệu là nguồn đặc tả chức năng, còn shell HCS, permission, API conventions và design system hiện có của repo là nguồn chuẩn cho cách tích hợp.

## 2. Phạm vi

### Trong phạm vi

1. Backend Work Management: model, migration, API và authorization riêng cho quản lý sự kiện.
2. Event dashboard với bộ lọc thời gian và các chỉ số tổng quan.
3. CRUD sự kiện với mã tự sinh, nhóm, nội dung/mô tả, địa điểm, thời gian, trạng thái, QR và file đính kèm.
4. Danh sách người tham dự theo sự kiện: thêm nhanh, chọn người dùng đã có, lọc/tìm kiếm, phân trang, trạng thái đăng ký, trạng thái check-in, xóa hàng loạt, import CSV và export CSV.
5. Luồng public check-in tối thiểu từ QR để QR có giá trị sử dụng thực tế.
6. Client pages, navigation, localization, responsive/accessibility theo HCS design system.
7. Gateway route, BFF anonymous policy cho public check-in, migration và automated tests phù hợp.

### Quyết định an toàn dữ liệu

Mockup có trường mật khẩu ở form thêm nhanh. Mật khẩu **không thuộc hồ sơ người tham dự và không được lưu**; tài khoản mới phải được tạo qua Quản trị người dùng. Form sẽ hỗ trợ chọn người dùng hiện có hoặc tạo hồ sơ khách mời độc lập, không nhận/lưu credential.

### Ngoài phạm vi đợt này

- Gửi email/SMS thực tế theo nhà cung cấp bên ngoài; UI chỉ chuẩn bị export và thao tác mở mail client nếu cần.
- Đồng bộ attendee sang `CalendarEvent`; calendar hiện tại là module khác và không bị thay đổi semantics.

## 3. Thiết kế kỹ thuật

### Data/API

- Thêm aggregate `ManagedEvent`, `EventAttendee`, `EventAttachment` trong Work service.
- Status chuẩn: `Preparing`, `Ongoing`, `Completed`, `Cancelled`; registration: `Confirmed`, `Unconfirmed`, `Declined`; check-in: `CheckedIn`, `NotCheckedIn`.
- Mã sự kiện và token QR sinh phía server; thời gian lưu UTC.
- API protected dưới `/api/events`; public check-in dưới `/api/events/public`.
- Attachment dùng blob container hiện hữu, metadata được lưu trong database.

### Client

- Routes: `/event-dashboard`, `/events`, `/events/{id}`, `/event-check-in/{code}`.
- Mở rộng `WorkManagementClient` và models; dùng `CsvDownload` hiện hữu.
- Giữ HCS top navigation hiện tại, bổ sung nhóm Quản lý sự kiện theo permission `WorkManagement.Events`.
- Tạo UI dạng catalog/detail, có empty state, loading state, visible focus, không phụ thuộc màu để biểu đạt status và có responsive table wrapper.

### Kiểm thử/kiểm chứng

- Domain invariants: date range, required fields, status/check-in transitions và duplicate attendees.
- EF model/migration build.
- Build/test backend và client; kiểm tra các route/permission/localization đã nối đủ.

## 4. Trạng thái thực hiện

- [x] Đọc và phân biệt đặc tả trong Pages với yêu cầu trực tiếp.
- [x] Khảo sát kiến trúc và design system hiện tại.
- [x] Lập kế hoạch triển khai.
- [x] Implement backend, migration, gateway và client.
- [x] Build/test và rà soát thay đổi.

## 5. Kết quả kiểm chứng

- `dotnet build HCS.slnx --no-restore`: đạt, 0 lỗi và 0 cảnh báo ở lần kiểm tra follow-up.
- `dotnet test HCS.slnx --no-build`: đạt toàn bộ các test project có test, gồm 403 test; `HCS.TestBase` không chứa test nên được VSTest bỏ qua.
- `git diff --check`: đạt.

## 6. Follow-up fixes theo kiểm thử giao diện

- [x] Căn lại filter Event theo cùng một hàng và cùng pattern với các trang catalog hiện có.
- [x] Thay pager tự dựng bằng Blazorise DataGrid, dùng page size `10/20/50/100` cho danh sách Event và người tham dự.
- [x] Sửa QR/file resource URL để đi qua API gateway production thay vì trỏ tương đối về host Blazor.
- [x] Tải QR qua BFF thành data URL để request ảnh luôn mang phiên xác thực, không phụ thuộc cookie cross-origin của thẻ `<img>`.
- [x] Bổ sung log có cấu trúc ở Work service cho kết quả/lỗi sinh QR (không ghi QR token).
- [x] Chẩn đoán log public check-in: QR sinh thành công, Gateway proxy thành công; `403` phát sinh từ `BusinessException` khi định danh không khớp attendee của event.
- [x] Chuẩn hóa số điện thoại (`+84`, `0084`, dấu cách/gạch), email và CCCD khi đối chiếu; thêm log kết quả check-in an toàn, không ghi PII/token.
- [x] Map mã lỗi attendee về thông báo tiếng Việt/Anh dễ hiểu trên trang public check-in.
- [x] Bỏ yêu cầu BFF antiforgery cho toàn bộ public event check-in request, giữ route public đúng mục đích.
- [x] Cho phép Admin nhận diện theo role ở client/API policy; Auth Server bổ sung các permission definition đang bật khi phát token mới.
- [x] Thu gọn filter trang chi tiết người tham dự thành một hàng trên desktop và responsive wrap trên màn hình nhỏ.
