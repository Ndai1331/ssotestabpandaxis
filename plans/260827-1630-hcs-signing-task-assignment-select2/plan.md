---
title: Tạo task từ Trình ký và sửa Select2
status: completed
priority: P1
effort: medium
branch: main
tags: [task-assignment, select2, signing]
created: 2026-08-27
---

# Tạo task từ Trình ký và sửa Select2

## Mục tiêu

- [x] Tạo task liên kết văn bản từ Trình ký mà không bắt buộc chọn người ở tab Thông tin chung.
- [x] Phân công user qua tab Phân công bằng UserSelect2 hiện có.
- [x] Không để lộ `SelectedText`, GUID hoặc placeholder rỗng; dùng bản dịch tiếng Việt phù hợp.
- [x] Giữ Select2 user single-select cùng chiều cao input và không hiển thị layout hai dòng trong ô đã chọn.

## Phạm vi triển khai

- [x] `ProjectTaskCreateModal.razor`: bỏ picker/validation assignment ở tab đầu; tạo task rồi chuyển tab Phân công; báo lỗi khi bấm Phân công thiếu user và reset lựa chọn sau khi gán.
- [x] `UserSelect2.razor` và `CatalogSelect2.razor`: truyền placeholder đúng binding; thêm fallback localization cho picker dùng chung.
- [x] `hcs-catalog-select2.js` và `hcs-components.css`: tách template dropdown/selected và chuẩn hóa chiều cao single-select user.
- [x] `vi.json`/`en.json`: thêm key placeholder dùng chung.

## API và rủi ro

- Không đổi contract/API: `POST /api/project-tasks`, `POST /api/project-tasks/{id}/documents`, `POST /api/project-tasks/{id}/assignments` đã có sẵn.
- Task có thể được tạo trước khi phân công nếu người dùng đóng modal; đây là chủ đích của tab mới và backend cho phép trạng thái này.
- Worktree đang có thay đổi khác ở CSS/localization; chỉ thêm patch cục bộ, không reset hoặc format toàn file.

## Kiểm tra

- [x] `dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore`
- [x] `dotnet build services/work-management/HCS.WorkManagementService/HCS.WorkManagementService.csproj --no-restore`
- [x] `dotnet test services/work-management/HCS.WorkManagementService.Tests/HCS.WorkManagementService.Tests.csproj --no-build`
- [x] `git diff --check`
- [x] Review đã pass.
- [x] Static smoke review: Trình ký → preview → Giao việc → Lưu → tab Phân công → tìm/chọn user → Phân công; kiểm tra placeholder và chiều cao từ diff/build/test. Browser runtime smoke chưa chạy trong lượt này.
