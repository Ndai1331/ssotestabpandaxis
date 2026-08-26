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

## Follow-up: PDF preview fallback

### Diagnosis

- `DocumentSigning.LoadSignPdfAsync` catches every preview exception and sets `signPdfUrl = null`, which renders the generic `Work:PreviewPdfOnly` fallback.
- Every preview path requests `/watermarked-content`; the document service then runs PdfSharp `PdfReader.Open(... Modify)` and creates an `XFont("Arial", ...)` without a portable font resolver.
- PDFsharp Core requires a resolvable font on Linux/Docker, so watermark generation can fail before the browser receives PDF bytes.

### Fix scope

- Configure a small embedded PDFsharp fallback font resolver at document-service startup.
- Keep the existing endpoint, authorization, watermark flow, API shape, routing and authentication unchanged.
- Surface a meaningful client-side error when a preview request really fails instead of silently rendering the Word-conversion hint.

### Verification

- [x] Document Service build passed with 0 warnings / 0 errors.
- [x] Blazor Client build passed with 0 warnings / 0 errors.
- [x] Embedded font resolver test passed (1/1).
- [x] No API, routing, authentication or business-flow changes introduced.

## Follow-up: signing action visibility parity

### Diagnosis

- `HCS_web_with_license` exposes processing actions only when the current user has a current PENDING assignment (`CanAct` + assignment id), while read-only workflow access opens a view-only modal.
- `HCS_web_free_license` builds the queue from all pending PROCESS/SIGN tasks visible to the user, but `CanActOn` currently returns true for elevated users, unassigned tasks, or the assigned user.
- The free modal footer renders Return/Extend/Reject/Approve whenever the modal is open, and the row always renders the signature action; this lets a viewer enter the action modal and see decision buttons.

### Fix scope

- Match the with-license UI policy: only the assigned current user may see/open action controls; viewers keep information/PDF preview only.
- Distinguish PROCESS and SIGN actions in the reusable signing queue UI; only SIGN shows signing-specific fields and the “Ký duyệt” action label.
- Keep the free-license API, backend authorization, routing, authentication and decision endpoints unchanged.

### Verification

- [x] Blazor Client build passed with 0 warnings / 0 errors.
- [x] Existing workflow tests passed (17/17), including SIGN/VIEW step behavior.
- [x] Relevant diff check passed for `DocumentSigning.razor`.
- [x] No API, routing, authentication or backend decision logic changed.
