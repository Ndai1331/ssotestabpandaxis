---
title: "HCS OU department catalog"
date: 2026-09-12
scope: "services/HCS_web_free_license"
---

# HCS OU department catalog — 2026-09-12

## Context

Trang `/departments` của `HCS_web_free_license` cần quản lý phòng ban theo OU giống màn hình tham chiếu của `HCS_web_with_license`: cây phòng ban, danh sách user theo node và menu thao tác trên từng node.

## Đã thực hiện

- Dùng `AbpOrganizationUnits` và `AbpUserOrganizationUnits` của ABP Identity làm nguồn dữ liệu canonical.
- Thêm Platform API để đọc cây OU, đọc/thêm/xóa thành viên và thực hiện tạo, sửa, di chuyển, xóa OU, di chuyển toàn bộ user.
- Thay riêng trang `/departments` bằng UI hai cột: cây OU đệ quy, chọn node xem thành viên trực tiếp, lọc/phân trang, thêm và gỡ thành viên, cùng các dialog/menu theo ảnh.
- Giữ nguyên catalog legacy `units`, `positions` và mapping `UserDepartments`; không migration ngầm dữ liệu cũ.
- Bổ sung localization en/vi, permission checks, kiểm tra OU không tự di chuyển vào chính nó hoặc hậu duệ, và giới hạn kích thước input/page.

## Reflection

OU đã có sẵn domain, repository, manager và bảng membership trong Identity nên không cần tạo aggregate hoặc migration riêng. Ranh giới này đáp ứng yêu cầu hiện tại mà không làm thay đổi các màn hình legacy còn phụ thuộc `DepartmentId`.

## Quyết định

- “Thêm thành viên” thao tác trực tiếp trên membership OU; user chỉ có mapping legacy sẽ chưa xuất hiện cho đến khi được gán vào OU.
- Tab “Vai trò” giữ dạng disabled với số lượng 0 vì yêu cầu hiện tại chỉ cần thành viên.
- Route API dùng namespace `api/identity/organization-units`, được gateway hiện tại route sẵn qua `platform-identity`.

## Kiểm chứng

- Build các project affected: pass, 0 warning/0 error.
- Application contract tests: 7/7 pass.
- Gateway tests: 141/141 pass; còn một cảnh báo xUnit2031 đã tồn tại ở `SigningCredentialUploadTests.cs`.
- Compose runtime: `platform`, `blazor`, `web-gateway` healthy; endpoint API trả 401 khi chưa đăng nhập và `/departments` redirect 302 về login.
- Repository-wide audit script đã thử chạy nhưng bị dừng vì tự sinh process audit lồng nhau không có output; bounded scan trên toàn bộ file OU mới pass.

## Next

- Sau khi đăng nhập local, kiểm tra thủ công dữ liệu OU thực tế, quyền CRUD và luồng thêm/gỡ user trên trình duyệt.
- Chưa tạo commit; các thay đổi khác đang tồn tại trong working tree được giữ nguyên.
