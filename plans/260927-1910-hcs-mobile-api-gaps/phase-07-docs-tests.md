# Phase 7 — Docs, catalog, kiểm thử

Chạy cuối mỗi phase (không đợi hết plan).

## Docs

- `docs/api/mobile/12-api-request-status.md`: đổi trạng thái mục từ "Dự kiến" → "Đã có", ghi ngày deploy.
- File module: `09-chat.md` (phase 1–3), `01-account.md` + `09-chat.md` phần push (phase 4), `03-documents.md` + `04-workflows.md` (phase 5), `05-projects-tasks.md` (phase 6).
- `hcs-mobile-api.json`: thêm route mới.

## Kiểm thử

- Test service-level cho từng route mới (quyền + happy path + idempotent).
- `dotnet build` các service đụng tới + chạy test project tương ứng.
- Smoke qua gateway local (`https://localhost:44402`) bằng Bearer token client `hcs-mobile`.
- Web: mở `/chat`, `/document-signing`, `/project-task-detail/{id}` xác nhận không vỡ.

## Deploy

- Migration chạy theo thứ tự service: Collaboration → Document → WorkManagement (Platform không có migration vì không thêm `userCode`/`dob`/`gender`).
  - Collaboration: `BackfillChatCreationTimes`, `AddChatConversationManagement`, `AddChatReactionsAndSavedMessages`.
  - Document: `AddDocumentProcessingMethod`.
  - WorkManagement: `AddTaskFilesAndNotes`.
- Phase 1 có backfill: chạy trên bản sao DB trước, đối chiếu số dòng `CreationTime = '0001-01-01'` trước/sau.

## Kết quả (2026-09-28)

- Docs cập nhật: `12-api-request-status.md`, `01`/`03`/`04`/`05`/`09`/`11`, `README.md`, `hcs-mobile-api.json`.
- Build pass: `HCS.Blazor.Client`, Platform, AuthServer.
- Test: Collaboration 76/76, Organization 35/35, Document 192/194, WorkManagement 135/136. Các test lỗi (`DocumentDeleteTests` ×2, `SurveyResultExcelExportTests`) đã lỗi sẵn trên baseline trước plan.
- Chưa làm được ở local: chạy migration trên DB HCS, smoke gateway `https://localhost:44402`, kiểm `AbpSettings` có override `IsSelfRegistrationEnabled` hay không, mở thử Web.
