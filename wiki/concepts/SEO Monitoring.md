---
type: concept
title: "SEO Monitoring"
created: 2026-07-13
updated: 2026-07-13
tags:
  - seo
  - feature
  - ui
  - api
status: developing
related:
  - "[[Codebase — task9-ui]]"
  - "[[Codebase — task9-api]]"
sources:
  - "[[Handoff SEO Monitoring 2026-06-30]]"
---

# SEO Monitoring

Trang `/seo-monitoring` — tính năng monitoring backlink SEO với realtime GP/TL check, run history, CSV export.

## Kiến trúc

- **UI:** `Components/Task9/SeoBacklink/` — `SeoBacklinkList.razor` (danh sách + filter + pagination), `SeoBacklinkJobHistoryTab.razor` (lịch sử chạy), `SeoMonitoringPage.razor` (parent), `RealtimeGptlPage` (realtime GP/TL)
- **API:** `SeoBacklinkController.cs` — CRUD + run + delete-all endpoints
- **Service:** `SeoBacklinkCsvExport.cs` — static helper export CSV (list + batch), JS interop `window.downloadCsv` qua `PageLayout.razor`
- **Repo:** `SeoBacklinkRepository.UpsertCheckAsync` — upsert với `CreatedAt` update

## Tính năng đã ship (2026-06-30)

- Bulk delete, phân trang 50 dòng, cột Anchor 1 + Link 1
- Filter search + status, nút "Chạy tất cả đã lọc"
- Tab "Lịch sử chạy" + auto-reload status
- Progress bar khi `_isRunning`
- Nút "Xóa tất cả đã lưu + lịch sử" (`DELETE /api/seo-backlink/delete-all`)
- Nút "Hiện tất cả" / "Chỉ của tôi" (admin)
- Thanh phân trang số (`1 2 3 ...`)
- Nút "Xuất CSV" trong tab Realtime GP/TL
- `runByUserId` param cho manual run (gán check cho người chạy)

## SystemAiSettings Recovery (2026-06-29)

- **Vấn đề:** Toast "AI detect không khả dụng" trên `/seo-monitoring`
- **Root cause:** Commit recovery `f6b0065` khôi phục code `system_ai_settings` NHƯNG bỏ sót config section `SystemAiSettings:SystemApiKey` trong appsettings → 401 trên server-to-server call
- **Fix:** Thêm section `SystemAiSettings:SystemApiKey` vào appsettings (API + UI), set env `SystemAiSettings__SystemApiKey` trên cả 2 container prod
- **Chi tiết:** `plans/reports/fix-260629-1758-seo-monitoring-ai-detect.md`

## DB

- Bảng: `seo_backlink`, `seo_backlink_check` trong `qcadmin` (operational CRUD)
- Delete-all: xóa `seo_backlink_check` trước rồi `seo_backlink` (constraint order)

## Deploy state (2026-06-30)

| Service | Test | Staging | Main | Tag |
|---|---|---|---|---|
| UI | `1cbcb04` ✅ | `eceacca` ✅ | `b436fb1` ✅ | `v20260630-1` |
| API | `6f56473` ✅ | `329df4c` ✅ | `223a616` ✅ | `v20260630-1` |