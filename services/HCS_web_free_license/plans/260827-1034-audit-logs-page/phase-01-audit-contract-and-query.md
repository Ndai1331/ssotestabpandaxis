# Phase 01 — Contract và read query

## Mục tiêu

Chuẩn hóa contract và query server-side cho một read model audit tập trung. Phase này chịu trách nhiệm bảo đảm dữ liệu đủ để tra cứu, an toàn khi hiển thị và không tạo thêm đường truy cập trực tiếp tới database/service.

## Context đã xác minh

- Contract: src/HCS.Application.Contracts/Auditing/AuditLogDtos.cs
- App service: src/HCS.Application/Auditing/AuditViewerAppService.cs
- Projection: src/HCS.Domain/Auditing/AuditRecordProjection.cs
- EF mapping/index: src/HCS.EntityFrameworkCore/EntityFrameworkCore/Auditing/HcsAuditProjectionModelBuilderExtensions.cs
- HTTP API: src/HCS.HttpApi/Controllers/Auditing/AuditViewerController.cs
- Gateway route: gateways/web/HCS.WebGateway/appsettings.json
- Capture event: building-blocks/HCS.IntegrationEvents/AuditEvents.cs

## Việc cần làm

1. Mở rộng GetAuditLogsInput theo hướng tương thích ngược:

   - keyword tổng hợp cho user/display name, user id, IP, browser, action, URL/path, service, application và correlation ID;
   - exact hoặc allow-list filter cho user id, status, HTTP method, IP, source service, application và has-exception;
   - thời gian UTC với quy ước start inclusive/end exclusive;
   - page size mặc định 20, giới hạn tuyệt đối 100; trim và giới hạn độ dài chuỗi.

2. Bổ sung ApplicationName vào row DTO hoặc một field hiển thị tương đương. User display name dùng giá trị có sẵn trước, fallback theo thứ tự UserName/UserId/—; không đổi ý nghĩa dữ liệu lịch sử âm thầm.

3. Chuẩn hóa sort bằng allow-list (ExecutionTime, HttpStatusCode, ExecutionDuration, UserName, SourceService, ApplicationName) và luôn thêm Id làm tie-breaker. Không nối trực tiếp tên cột từ request vào LINQ/SQL.

4. Triển khai query trên HcsAuditRecordProjections với AsNoTracking, projection chỉ lấy các field cần cho row, count trước page, và tránh load JSON action/entity ở list. Bảo đảm empty page sau khi xóa/lọc vẫn trả response hợp lệ.

5. Tối ưu index nếu đo đạc cho thấy cần: execution time, correlation ID, user ID và composite source service/action đã có; cân nhắc index cho status/IP/application chỉ sau khi kiểm tra execution plan và volume thực tế. Tạo migration/snapshot nếu schema thay đổi.

6. Detail phải deserialize ActionsJson/EntityChangesJson theo kiểu an toàn: malformed JSON trả danh sách rỗng và log server-side có kiểm soát, không trả raw JsonException cho người dùng. Giữ sanitizer cho exception; tuyệt đối không thêm request/response body, access token, cookie hoặc secret vào DTO.

7. Xác nhận permission HCS.AuditViewer tại application service là authority duy nhất. Kiểm tra route /api/audit-logs không bị audit lại thành vòng lặp hoặc tạo dữ liệu rác.

## Coverage và dữ liệu cần chốt

MVP đọc projection hiện tại, bao phủ các event từ Organization, Document, Work Management và Collaboration. Platform/AuthServer hiện chỉ ghi AbpAuditLogs, nên các dòng AuthServer như screenshot chưa tự động xuất hiện ở endpoint này. Nếu acceptance bắt buộc bao phủ cả Platform/AuthServer, tạo follow-up để phát event cùng schema và đưa vào projection; không query nhiều database hoặc ghép dữ liệu trong browser.

Kiểm tra RemoteIpAddress sau Caddy/YARP/forwarded headers. Chỉ tin proxy đã cấu hình; không dùng header client tùy ý làm IP xác thực. Smoke test phải xác nhận UserName là họ tên mong muốn, không phải subject/username. Nếu không đạt, bổ sung resolver claim/display name ở capture boundary và test backward compatibility.

## File dự kiến

- Sửa: src/HCS.Application.Contracts/Auditing/AuditLogDtos.cs
- Sửa: src/HCS.Application/Auditing/AuditViewerAppService.cs
- Có điều kiện: src/HCS.Domain/Auditing/AuditRecordProjection.cs
- Có điều kiện: src/HCS.EntityFrameworkCore/EntityFrameworkCore/Auditing/HcsAuditProjectionModelBuilderExtensions.cs
- Có điều kiện: migration và model snapshot tương ứng
- Test: test/HCS.EntityFrameworkCore.Tests/EntityFrameworkCore/PlatformFeatureTests.cs hoặc test project phù hợp
- Chỉ kiểm tra, không sửa nếu không cần: controller và gateway route

## Acceptance

- Filter kết hợp không làm mất điều kiện; page/sort ổn định và không vượt 100 row.
- Tìm được theo user, IP, API, action, service, application, status, method, correlation và exception state.
- Date range, null field, malformed detail JSON và record không có user đều có hành vi xác định.
- Unauthorized/forbidden bị chặn ở server; không lộ secret hoặc raw exception.
- Test projection/event deduplication và query/index pass.

## Rủi ro

Eventual consistency khiến record mới chưa xuất hiện ngay; UI phải thể hiện thời điểm refresh. Contains trên nhiều field có thể chậm khi volume lớn; cần đo trước khi thêm index hoặc full-text. Dữ liệu cũ có thể thiếu browser/display name/entity detail; hiển thị fallback thay vì bịa dữ liệu.
