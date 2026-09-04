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

Tách `Public` và `Internal` ngay từ truy vấn backend giúp UI không trở thành lớp bảo mật duy nhất. Phạm vi vẫn không có follow, moderation hay realtime.

## Decisions

- `Public`: mọi user HCS đã xác thực có quyền `Collaboration.Social` đều xem được.
- `Internal`: chỉ tác giả xem được trong `/social/profile` ở MVP.
- Bài viết được lưu author display-name snapshot; media giới hạn 10 file, 25 MB/file và chỉ image/video MIME allow-list.
- Link trong nội dung được nhận diện và lưu metadata preview theo kiểu best-effort; URL vẫn được hiển thị nếu trang đích không cho preview.
- Tìm kiếm chạy tại Collaboration service theo keyword, ngày bắt đầu/kết thúc và hashtag đã chuẩn hóa; feed/profile giữ đúng visibility boundary.
- Reaction dùng một bản ghi duy nhất cho mỗi user trên post/comment, hỗ trợ Like/Love/Haha/Wow/Sad/Angry và toggle/replace.
- Share ghi nhận duy nhất mỗi user, trả permalink tới đúng post; trình duyệt dùng Web Share API hoặc fallback copy link.

## Visibility switching fix

Bổ sung scope tabs cho `/social` và `/social/profile`. Thay đổi `Công khai`/`Nội bộ` giờ cập nhật URL, gọi lại API theo visibility và reset danh sách/bình luận đang mở. Component lắng nghe `Navigation.LocationChanged`, nên chuyển route hoặc đổi query không còn giữ dữ liệu của view trước.

## Next

Nếu cần chia sẻ `Internal` theo phòng ban/nhóm hoặc mở profile của user khác, bổ sung audience model và policy riêng thay vì nới query hiện tại. Smoke test cần chạy với hai user thật trên local stack.

## Discovery and engagement update

Đã bổ sung metadata link preview, chỉ mục hashtag và ba bảng reaction/share cùng unique constraint theo user. Link preview có giới hạn kích thước, timeout, tối đa redirect và chặn địa chỉ nội bộ/loopback trước khi gọi remote. Permalink có query `post` để feed/profile lọc đúng bài và UI tự cuộn highlight sau khi tải.

## Verification

- License/secret audit: passed.
- `dotnet build HCS.slnx --no-restore`: passed, 2 warning xUnit1051 có sẵn ở test stream.
- `dotnet test HCS.slnx --no-restore --logger "console;verbosity=minimal"`: passed, 377 tests, 0 failed.
- `dotnet build src/HCS.Blazor/HCS.Blazor.csproj --no-restore`: passed, 0 warning/error.
- Docker local: migration `20260904105929_AddSocialDiscoveryEngagements` applied; Collaboration, Blazor và Web Gateway running, db-migrator exited 0, restart count 0.
- Unauthenticated route probe: feed/profile/reaction/share trả `401`; `js/hcs-social.js` trả `200`.
