# HCS Free — PDF, document-detail, signing UI — 2026-08-20

**Phạm vi:** `services/HCS_web_free_license`  
**Ranh giới:** LICENSE chỉ đọc. PdfViewer NuGet 2.3.0 được phép; không copy `HC.DocumentPdfViewer` / `RichTextEdit`.

## Đã giao

1. **Modal Thêm bước** — `UserSelect2` khi người cụ thể; `CatalogSelect2` khi vai trò. SLA và «Cho phép trả lại» tách hàng; radio có label «Cách gán».
2. **PDF** — `HcsPdfFrame` dùng `PdfViewerContainer` + `PdfViewer` (Blazorise.PdfViewer 2.3.0). CSS/JS `_content/Blazorise.PdfViewer`. `PreferPdf` không fallback DOCX.
3. **Upload DOCX** — `TryNormalizeContentType` theo đuôi khi MIME trống/`octet-stream`. Convert LibreOffice best-effort (fail không xóa Word). Client stream 50MB.
4. **document-detail** — một Card 6/6 (form+file trái, PDF phải); nút Quay lại/Hủy là Blazorise `Button` + `inline-flex`.
5. **Nút action văn bản** — Edit cũng `Button`; border = màu icon (edit/preview/send/sign).
6. **document-signing** — filter 3 hàng như LICENSE; modal trình ký Alert+card template; modal ký ExtraLarge 50/50 + PDF; modal xem dùng PdfViewer `Height.Rem(35)`.

## Quyết định

- PdfViewer 2.3 không có `AddBlazorisePdfViewer()` — chỉ package + static assets.
- CSS runtime: `main.css` (host) **và** `hcs-catalog.css`.

## Tests / build

- DocumentService.Tests: 52 passed
- HCS.Blazor.Client + HCS.Blazor: build sạch
- Không gắn `<script src=".../pdfviewer.js">` (ES module; Blazorise tự import). CSS `pdf_viewer.min.css` trên host `App.razor`.
- `PreferPdf` không fallback DOCX; không có PDF → empty state `Work:NoPdfAvailable`.

## Manual

Rebuild container `blazor` (+ `document` nếu chưa). Hard refresh `/document-detail`, `/workflow-detail`, `/manage-documents`, `/document-signing`.
