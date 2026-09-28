---
title: "Bổ sung API còn thiếu cho mobile (chat, push, ký số, dự án)"
description: "Làm các API mobile đề xuất trong new-api-request.md mục 1–4 mà source HCS_web_free_license chưa có; sửa bug thời gian chat; chốt docs mobile."
status: completed
priority: P1
effort: 8-10d
branch: main
tags: [feature, backend, api, mobile, bugfix]
blockedBy: []
blocks: []
created: 2026-09-27
---

# Kế hoạch bổ sung API mobile còn thiếu

## Overview

App mobile đã chuyển sang contract `docs/api/mobile/`. Đối chiếu source ngày 2026-09-27 cho thấy phần lớn đề xuất mục 1–4 chưa có backend. Plan này làm các mục **chưa có** hoặc **có một phần**, theo thứ tự ưu tiên P1 → P3.

Kết quả đối chiếu từng mục và thay đổi bắt buộc phía mobile: [`docs/api/mobile/12-api-request-status.md`](../../services/HCS_web_free_license/docs/api/mobile/12-api-request-status.md).

## Nguyên tắc

- Không phá contract Web đang dùng. Route cũ giữ nguyên; chỉ thêm route/field mới (field mới trong DTO record luôn có default).
- Mỗi thay đổi schema = 1 EF migration trong đúng service (Collaboration / Document / WorkManagement / Platform).
- Route mới đi qua prefix gateway đã có (`/api/chat`, `/api/notifications`, `/api/signing`, `/api/documents`, `/api/workflows`, `/api/projects`, `/api/project-tasks`, `/api/identity`) → không cần sửa YARP, trừ khi ghi rõ.
- Sự kiện realtime mới phát qua `ChatHub` (`/hubs/chat`) cùng pattern `MessageDeleted`.
- Sau mỗi phase: cập nhật `12-api-request-status.md` + file module docs (`09-chat.md`, `03-documents.md`, …) + `hcs-mobile-api.json`.

## Phases

| Phase | Tên | Mục request | Ưu tiên | Effort | Status |
|---|---|---|---|---|---|
| 1 | [Sửa thời gian chat + phân trang tin](./phase-01-chat-timestamps-paging.md) | 1.11, 1.12 | P1 | 1d | Done |
| 2 | [Quản lý hội thoại](./phase-02-chat-conversation-management.md) | 1.1, 1.2, 1.3, 1.8, 1.9 | P1/P2 | 2d | Done |
| 3 | [Tìm kiếm, đính kèm, reaction, lưu tin](./phase-03-chat-search-reactions.md) | 1.4, 1.5, 1.6, 1.7 | P1/P2 | 2d | Done |
| 4 | [Push token, hồ sơ, tắt tự đăng ký](./phase-04-account-push.md) | 2.2, 2.3, 2.5 | P1/P2 | 1d | Done |
| 5 | [Văn bản, ký số, quy trình](./phase-05-documents-signing.md) | 3.2–3.6, 3.8, 3.9 | P1/P2 | 2d | Done |
| 6 | [Dự án / công việc](./phase-06-projects-tasks.md) | 4.1–4.5 | P1/P2 | 1.5d | Done |
| 7 | [Docs, catalog, kiểm thử](./phase-07-docs-tests.md) | tất cả | P1 | 0.5d | Done (còn smoke gateway + chạy migration trên DB thật) |

Phase 1 làm trước vì bug thời gian làm sai cả thứ tự tin nhắn, và các phase chat sau dựa vào `CreationTime` đúng.

## Không làm backend (chỉ chốt docs)

| Mục | Lý do |
|---|---|
| 1.10 | Contract đã có: `{ userIds }`, `{ pinned }` |
| 1.13 | Giữ mảng để không phá Web; tổng lấy qua `/api/notifications/count`, `/api/chat/unread-count` |
| 2.3 (một phần) | `userCode`, `dob`, `gender`: không trả về (quyết định 2026-09-28) |
| 2.1 | Đã có `POST /api/notifications/devices`; mobile bắt buộc chuyển sang |
| 2.4 | Đã có `GET /api/chat/contacts/page?search&skip&take` (50/trang, có `totalCount`) |
| 3.1 | Giữ như hiện tại: bước ký ghi ghi chú lên PDF qua `POST /api/signing/attempts`; bước xử lý chỉ lưu `comment` (không ghi PDF) |
| 3.7 | Chỉ sửa docs; bổ sung `currentStepCode` nằm trong phase 5 |

## Acceptance Criteria

- Tin nhắn/thành viên mới có `createdAt`/`joinedAt` đúng; dữ liệu cũ đã backfill; `GET .../messages` mới nhất trước, ổn định khi trùng thời gian.
- Mọi route mới có test service-level (quyền + happy path) và build pass.
- Web hiện tại không lỗi (route cũ giữ nguyên hành vi).
- Docs mobile phản ánh đúng route đã deploy; route chưa deploy ghi rõ "dự kiến".

## Quyết định (2026-09-28)

| # | Câu hỏi | Quyết định | Ảnh hưởng plan |
|---|---|---|---|
| 2.5 | Cho tự đăng ký? | **Không** | Phase 4: tắt `Abp.Account.IsSelfRegistrationEnabled`, chặn `/Account/Register` + `POST /api/account/register` |
| 1.3 | Giới hạn thời gian thu hồi? | **Không giới hạn** | Phase 2: recall chỉ kiểm người gửi |
| 2.3 | `userCode`, `dob`, `gender`? | **Không cần trả về** | Phase 4: bỏ extra property + migration Platform |
| 1.8 | Mute tắt gì? | **Chỉ tắt thông báo realtime SignalR** (toast/âm thanh trong app) | Phase 2: `ReceiveMessage` vẫn gửi (đồng bộ chat); client không toast cho hội thoại muted; server không phát `NotificationReceived` cho member muted; `/api/notifications`, push, unread giữ nguyên |
| 3.1 | Ghi chú lên PDF? | **Bước ký như hiện tại; bước xử lý chỉ lưu note** | Không có việc backend; bỏ `NoteOnFileId` khỏi phase 5 |

## Unresolved questions

Không còn.
