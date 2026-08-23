# Phase 3 — Avatar API + topbar (MinIO)

## Context

Chưa có ProfilePicture. Topbar/chat dùng initials. `ChatContactDto.AvatarUrl` đã có field nhưng luôn `null` trong `ChatContactsController`. Platform chưa cấu hình MinIO — **bắt buộc thêm** (user rule: mọi ảnh lưu MinIO, không lưu DB).

Mirror pattern Document `UserSignature` + `SigningBlobContainer` / `hcs-signing`.

## Overview

- Priority: P1
- Status: pending
- Estimate: 1.5–2 days
- Goal: upload/xóa/xem avatar self + xem avatar user khác (authenticated); bytes trên MinIO; topbar hiện `<img>` khi có.

## Requirements

1. **Storage rule:** binary ảnh chỉ trên MinIO. DB chỉ metadata. Không `bytea`, không `AbpBlobStoring.Database` cho avatar.
2. Entity `UserAvatar` (1 row / user) trong Identity/`HCSDbContext`: `UserId` (unique), `FileName`, `ContentType`, `BlobName`, `Size`, `LastModificationTime` — **không** cột Content.
3. Blob:
   - Container class `[BlobContainerName("hcs-avatars")]` `AvatarBlobContainer`
   - Name policy: `avatars/{userId:N}` (một object / user; overwrite khi re-upload)
4. Platform module:
   - Package `Volo.Abp.BlobStoring.Minio`
   - `DependsOn(AbpBlobStoringMinioModule)`
   - `ConfigureContainer<AvatarBlobContainer>` giống Document (`Minio:EndPoint/AccessKey/SecretKey/WithSSL/CreateBucketIfNotExists`)
   - `appsettings` Platform + compose/env lab: cùng MinIO endpoint với Document
5. API (Platform, `[Authorize]` authenticated):
   - `GET /api/identity/profile/avatar` — stream từ MinIO (404 nếu chưa có)
   - `PUT` hoặc `POST /api/identity/profile/avatar` — multipart, max **2MB**, `image/jpeg|png|webp|gif`; `SaveAsync` rồi upsert metadata; rollback blob nếu DB fail
   - `DELETE /api/identity/profile/avatar` — xóa DB rồi `DeleteAsync` blob
   - `GET /api/identity/users/{userId}/avatar` — stream (authenticated)
6. Không trả MinIO/presigned public URL thô; client dùng same-origin BFF path.
7. Blazor: upload trên `/account`; cache-bust `?v={ticks}` sau upload.
8. `HCSMainLayout`: `<img>` khi có avatar, else initials; `onerror` → initials.
9. `ChatContactsController`: set `AvatarUrl` = `/api/identity/users/{id}/avatar` khi có metadata (hoặc luôn set URL + consumer fallback 404).

## Related files

- `src/HCS.EntityFrameworkCore/…` — entity + migration metadata only
- `services/platform/HCS.PlatformService/HCSPlatformServiceModule.cs` — MinIO blob config
- `services/platform/HCS.PlatformService/HCS.PlatformService.csproj` — package Minio
- `services/platform/HCS.PlatformService/appsettings.json` (+ Development) — `Minio` section
- Controller/service avatar mới dưới Platform
- Mirror reference: `services/document/.../Storage/BlobContainers.cs`, `SigningAppService` upload/delete
- `src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor`
- `src/HCS.Blazor.Client/Pages/AccountManagement.razor`
- `ChatContactsController.cs`

## Implementation steps

1. Add MinIO blob wiring trên Platform (packages + module + appsettings).
2. Entity metadata + migration (no binary column).
3. AppService: Save/Get/Delete blob + metadata với rollback.
4. Controller endpoints.
5. Blazor client + account upload UI + topbar img.
6. Wire `AvatarUrl` contacts.

## Success criteria

- Object xuất hiện trong bucket MinIO `hcs-avatars`; DB không chứa bytes ảnh.
- Upload → page + topbar thấy ảnh; delete → object MinIO + row metadata sạch.
- File >2MB / không phải image → 400.
- Anonymous không GET được avatar.

## Risks

- Platform trước đây chưa có MinIO: quên env/compose → runtime fail khi upload — verify lab config cùng Document.
- Overwrite cùng `BlobName`: dùng `overrideExisting: true` khi re-upload.
- Không leak AccessKey/presigned URL ra Blazor.
