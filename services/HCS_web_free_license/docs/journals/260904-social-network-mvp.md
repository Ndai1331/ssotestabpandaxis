---
date: 2026-09-04
topic: internal-social-network-mvp
---

# Social network MVP

## Context

HCS cần một khu vực mạng xã hội nội bộ tối giản, dùng chung phiên đăng nhập BFF và service Collaboration hiện có.

## What happened

Đã thêm feed bài viết public sắp xếp mới nhất trước, bài viết text/ảnh/video, trang cá nhân, bình luận và reply. Media được lưu trong MinIO container riêng `hcs-social`; API kiểm tra quyền ở server và BFF route giữ cùng boundary xác thực.

## Reflection

Tách `Public` và `Internal` ngay từ truy vấn backend giúp UI không trở thành lớp bảo mật duy nhất. MVP giữ phạm vi nhỏ: chưa có like, follow, share, moderation hay realtime.

## Decisions

- `Public`: mọi user HCS đã xác thực có quyền `Collaboration.Social` đều xem được.
- `Internal`: chỉ tác giả xem được trong `/social/profile` ở MVP.
- Bài viết được lưu author display-name snapshot; media giới hạn 10 file, 25 MB/file và chỉ image/video MIME allow-list.

## Next

Nếu cần chia sẻ `Internal` theo phòng ban/nhóm hoặc mở profile của user khác, bổ sung audience model và policy riêng thay vì nới query hiện tại. Smoke test cần chạy với hai user thật trên local stack.

## Verification

- License/secret audit: passed.
- `dotnet build HCS.slnx --no-restore`: passed, 2 warning xUnit1051 có sẵn ở test stream.
- `dotnet test HCS.slnx --no-build --verbosity minimal`: passed, 375 tests; TestBase không có test.
