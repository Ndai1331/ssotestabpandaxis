---
title: "Approval modal preview, workflow history and side tabs"
description: "Sửa preview tệp, tách lịch sử trình ký khỏi lịch sử văn bản và đưa lịch sử/tệp đính kèm sang cột phải của modal phê duyệt."
status: completed
priority: P1
effort: 1-2d
branch: main
tags: [hcs, blazor, approval, workflow, preview, localization]
created: 2026-08-26
createdBy: Codex
---

# Approval modal fixes

## Scope

- Sửa preview PDF khi bấm icon mắt, không lồng modal bên trong modal.
- Hiển thị lịch sử của workflow instance/tasks: người trình, giao bước, người phê duyệt/từ chối/trả lại và thời điểm.
- Dịch nhãn/sự kiện bằng localization hiện có và bổ sung key cần thiết cho vi/en.
- Đưa `Lịch sử` và `Tệp đính kèm` thành tabs ở cột phải của approval modal.
- Không đổi quyền, routing, authentication, nghiệp vụ quyết định, API endpoint hoặc cách upload/download.

## Diagnosis

- `WorkflowInfoModal` đang render `document.History`, là lịch sử văn bản/tệp, và còn render `DocumentPdfPreviewModal` lồng bên trong modal.
- `DocumentSigning` có cột phải preview nhưng không có layout height ổn định; phần lịch sử/tệp đang ở cột trái.
- Domain `ApprovalTask` có `CreationTime`, nhưng `ApprovalTaskDto` chưa expose field này.

## Implementation phases

1. [x] Giữ nguyên workflow contract/API; dùng các timestamp đã có trên workflow instance/task.
2. [x] Tạo timeline từ workflow instance, task decision, assignee và submitter; không dùng `Document.History` cho workflow history.
3. [x] Chuyển history/files sang tabs ở cột phải approval modal; giữ form và action buttons bên trái.
4. [x] Dùng preview host ở parent hoặc inline right pane; đảm bảo PDF frame có chiều cao và trạng thái loading/error rõ ràng.
5. [x] Bổ sung localization vi/en cho các sự kiện/nhãn, giữ fallback an toàn cho step code.
6. [x] Build/test, diff review, kiểm tra responsive và không lẫn thay đổi dirty worktree.

## Acceptance criteria

- Icon mắt hiển thị được PDF trong preview host; non-PDF vẫn có thông báo/tải xuống phù hợp.
- Modal phê duyệt không còn blank right pane khi có PDF hoặc khi đang tải; trạng thái thiếu PDF hiển thị rõ.
- History tab không hiển thị `FileAdded`/`Created` của document như workflow event; hiển thị submit/assign/decide theo task timeline.
- History và attachments nằm ở cột phải; responsive mobile xếp dưới phần phê duyệt.
- API, auth, routing, decision flow và file persistence không đổi.
- `dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore` đạt 0 warning / 0 error.
- `dotnet build services/document/HCS.DocumentService/HCS.DocumentService.csproj --no-restore` đạt 0 warning / 0 error.
