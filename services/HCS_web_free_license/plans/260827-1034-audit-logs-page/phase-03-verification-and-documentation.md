# Phase 03 — Verification và documentation

## Mục tiêu

Chứng minh page tra cứu đúng dữ liệu, đúng quyền, không lộ thông tin nhạy cảm và không làm hỏng các thay đổi đang có trong working tree.

## Automated checks

Chạy theo thứ tự phù hợp với dependency và ghi lại kết quả:

1. License hygiene:

   ./scripts/audit-license-clean.sh

2. Build các project bị ảnh hưởng bằng no-restore sau khi dependency đã sẵn sàng; sau đó build solution nếu thời gian/môi trường cho phép.

3. Test solution bằng no-build hoặc test project bị ảnh hưởng. Không bỏ qua test vì có thay đổi contract/migration.

4. Kiểm tra whitespace/diff:

   git diff --check

Không reset, stash hoặc ghi đè các thay đổi không thuộc feature audit logs.

## Test matrix backend

- Default page size, page size 20/50/100 và giá trị vượt 100.
- Keyword ở user, user id, IP, browser, URL, action, service, application và correlation.
- Filter exact status/method/service/application/exception; nhiều filter kết hợp.
- Start inclusive/end exclusive, UTC conversion, start lớn hơn end và date null.
- Sort tăng/giảm ở các field allow-list; tie-break bằng Id; page boundary không trùng/mất record.
- Empty result và page vượt tổng số.
- User có display name, chỉ có username, chỉ có user id và anonymous request.
- Detail có action/entity JSON hợp lệ, JSON rỗng và JSON malformed.
- Exception được sanitize; không trả token/cookie/body/header nhạy cảm.
- 401/403, correlation id không hợp lệ, chuỗi quá dài, ký tự đặc biệt và retry không gây lỗi query.
- Event deduplication và eventual consistency của outbox/projection.
- Gateway route /api/audit-logs và /api/audit-logs/{id} giữ đúng upstream.

Ưu tiên unit/integration test cho input normalization, query predicate, mapping, detail fallback và permission; bổ sung migration/model test nếu index/schema thay đổi.

## Runtime smoke test

Qua https://hcs.localhost với account được cấp HCS.AuditViewer:

1. Mở Administration > Audit Logs, refresh, đổi page size, sort và mở detail.
2. Tạo hoặc thực hiện một request thành công và một request lỗi trong các service đã capture; chờ outbox/event handler xử lý rồi xác nhận row có user, IP, method, URL, service, application, status, duration và correlation.
3. Tìm lần lượt theo keyword, IP, API, action, service, status, method, date range và exception; kiểm tra Reset.
4. Đăng nhập account admin không có HCS.AuditViewer và account không phải admin; xác nhận menu/page/403 theo quyết định quyền.
5. Kiểm tra service unavailable/5xx, Retry, stale result khi submit liên tiếp và logout/401.
6. Kiểm tra 375/768/1440px, keyboard-only, focus modal, Escape, screen-reader label cơ bản và prefers-reduced-motion.
7. Đối chiếu timezone hiển thị với timezone đã thống nhất. Kiểm tra IP sau proxy không bị ghi thành IP proxy hoặc header giả mạo.

Ghi rõ nếu AuthServer/Platform không có row. Đây là giới hạn coverage đã biết của projection hiện tại, không được “sửa” bằng fake data.

## Documentation handoff

Nếu triển khai thật, tạo hoặc cập nhật runbook tại docs/runbooks/hcs-admin-audit-logs.md với:

- route, permission và các service đang được capture;
- định nghĩa từng cột/filter, timezone và eventual-consistency delay;
- fallback khi user/browser/IP thiếu;
- cách kiểm tra outbox/projection, 401/403 và gateway;
- retention/PII guidance và các field tuyệt đối không hiển thị;
- backlog bao phủ Platform/AuthServer, export, retention và entity before/after nếu cần.

Cập nhật roadmap/changelog chỉ khi repository đã có tài liệu tương ứng; không tạo tài liệu trùng lặp.

## Acceptance

- Automated checks, build, test và license audit có kết quả ghi nhận.
- Runtime smoke pass cho quyền, filter/paging/sort/detail, lỗi, responsive và accessibility.
- Có evidence về dữ liệu thực từ event/projection; không dùng mock để che coverage gap.
- Working tree vẫn bảo toàn các thay đổi trước đó; report nêu rõ unresolved questions.
