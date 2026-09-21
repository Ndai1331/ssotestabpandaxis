# 00 — Đăng nhập, header và quy ước

Mọi endpoint nghiệp vụ trong bộ docs này đi qua Gateway. Mobile **không** dùng cookie `.HCS.Bff` và **không** gọi `/bff/login`.

## 1. Web vs mobile

| | Web Blazor | Native mobile |
|---|---|---|
| Client OpenIddict | `HCS_App` (confidential, secret trên Gateway) | `hcs-mobile` (public, không secret) |
| Grant | Authorization Code + PKCE, session cookie | Authorization Code + PKCE, token trên máy |
| Gọi API | Cookie + `X-XSRF-TOKEN` | `Authorization: Bearer {access_token}` |
| Token lưu ở | Redis / Gateway | Keychain / Keystore |
| SignalR | Cookie + antiforgery handler | `access_token` query trên handshake |

Không nhúng client secret `HCS_App` vào app mobile. Không dùng password grant.

## 2. Client `hcs-mobile`

Seed khi `OpenIddict:Applications:HCS_Mobile:ClientId` được cấu hình. Local/Docker mặc định:

```text
HCS_MOBILE_CLIENT_ID=hcs-mobile
HCS_MOBILE_REDIRECT_URI=com.htltech.hcs:/oauth/callback
HCS_MOBILE_POST_LOGOUT_REDIRECT_URI=com.htltech.hcs:/oauth/logout
```

- Grant: `authorization_code`, `refresh_token`
- PKCE bắt buộc, `S256`
- Scopes tối thiểu: `openid profile email roles HCS offline_access`
- Audience JWT Gateway: `HCS`

Discovery:

```http
GET {auth_server}/.well-known/openid-configuration
```

Dùng `authorization_endpoint`, `token_endpoint`, `issuer`; logout/revoke nếu có `end_session_endpoint` / `revocation_endpoint`.

## 3. Luồng PKCE

1. Tạo `code_verifier`, `code_challenge = BASE64URL(SHA256(code_verifier))`.
2. Mở browser hệ thống:

```text
{authorization_endpoint}
  ?client_id=hcs-mobile
  &response_type=code
  &redirect_uri={registered_redirect_uri}
  &scope=openid%20profile%20email%20roles%20HCS%20offline_access
  &state={random_state}
  &code_challenge={code_challenge}
  &code_challenge_method=S256
```

3. Callback: kiểm tra `state`, đổi code:

```http
POST {token_endpoint}
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&client_id=hcs-mobile&redirect_uri={registered_redirect_uri}&code={code}&code_verifier={code_verifier}
```

Response:

```json
{
  "access_token": "ey...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "...",
  "scope": "openid profile email roles HCS offline_access"
}
```

Refresh:

```http
POST {token_endpoint}
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&client_id=hcs-mobile&refresh_token={refresh_token}
```

Nếu refresh thất bại: xóa credential, đưa user về login. Không refresh vòng lặp.

Đăng xuất: xóa token local; gọi `end_session_endpoint` / revoke nếu discovery có.

## 4. Gọi API

```http
GET {gateway}/api/account/my-profile
Authorization: Bearer {access_token}
Accept: application/json
```

JSON body thêm `Content-Type: application/json`. Upload dùng `multipart/form-data`. Download đọc `Content-Type` / `Content-Disposition`.

`GET /bff/user`, `GET /bff/antiforgery`, `GET /bff/login`, `POST /bff/logout` là contract browser. Mobile PKCE không gọi.

SignalR:

```text
{gateway}/hubs/chat?access_token={access_token}
```

## 5. Bootstrap ABP

Sau login, Web (và mobile nên) tải cấu hình user + permission + localization:

```http
GET /api/abp/application-configuration
GET /api/abp/application-localization?cultureName=vi
GET /api/language-management/languages/enabled
```

Hai endpoint ABP Gateway cho phép anonymous ở mức proxy (để Web khởi tạo). Mobile đã login thì vẫn gửi Bearer. Dùng `auth.grantedPolicies` để ẩn/hiện action; vẫn xử lý HTTP `403`.

`GET /api/hcs/system-branding/public` và `/api/hcs/system-branding/assets/{slot}` là branding public — không bắt buộc cho các module user-facing trong bộ docs này.

## 6. Phân trang, thời gian, enum

Hai kiểu query:

| Nhóm | Query | Ví dụ |
|---|---|---|
| Identity / OU / employee-directory | `skipCount`, `maxResultCount` | max 100 |
| Document / Work / Chat / Social | `skip`, `take` | max 100 (social feed max 50) |

Response paged chuẩn:

```json
{ "totalCount": 42, "items": [] }
```

Ngoại lệ: `GET /api/calendar` trả **mảng**; `GET /api/signing/queue` trả **mảng**; nhiều list khảo sát trả **mảng**.

Ngày giờ request: UTC ISO-8601 (`2026-09-21T08:00:00.0000000Z`). UI hiển thị theo timezone thiết bị. Social filter `from`/`to` dùng `yyyy-MM-dd` (DateOnly).

Enum trong JSON body Web thường serialize **số** (`status: 0`, `visibility: 1`). Query string có thể là tên (`status=Draft`, `group=Internal`). Dùng đúng ví dụ từng module.

## 7. Status code

| Status | Xử lý |
|---|---|
| `200` | JSON hoặc binary |
| `201` | Tạo thành công |
| `204` | Thành công, không body |
| `400` | Validation; đọc `error.validationErrors` |
| `401` | Refresh một lần; nếu vẫn lỗi thì login |
| `403` | Thiếu permission — **không** đẩy về login |
| `404` | Resource không có / empty |
| `409` | Conflict (ví dụ `Work:EmployeeRatingAlreadySubmitted`) |
| `413` | File quá lớn |
| `429` | Backoff |
| `5xx` | Retry GET có giới hạn; POST chỉ retry khi có `idempotencyKey` |

Lỗi ABP:

```json
{
  "error": {
    "code": "Work:EmployeeRatingAlreadySubmitted",
    "message": "...",
    "details": null,
    "validationErrors": []
  }
}
```

## 8. Idempotency và file

Các thao tác start workflow, decision, signing attempt **phải** gửi UUID `idempotencyKey` ổn định. Timeout thì retry cùng UUID.

Giới hạn Web đang dùng:

| Loại | Max |
|---|---|
| Avatar, chữ ký, seal, CSV attendee | 2 MB |
| Layout image credential | 3 MB |
| Chat / social / event / survey file | 25 MB |
| File văn bản / template workflow | 50 MB |

## 9. Permission

Tên policy trên page Web (ABP `grantedPolicies`):

| Policy | Dùng cho |
|---|---|
| `WorkManagement.Dashboard` | Workspace |
| `Documents.View` / `Update` / `Assign` / `ManageFiles` | Văn bản |
| `Documents.Signing.Execute` / `Configure` | Ký số |
| `Documents.Workflow.View` / `Manage` / `Decide` | Quy trình |
| `WorkManagement.Projects` / `ProjectTasks` | Dự án, task |
| `WorkManagement.Calendar` | Lịch |
| `WorkManagement.Events` | Sự kiện |
| `WorkManagement.Surveys` / `SurveyManagement` | Khảo sát |
| `WorkManagement.EmployeeRatings` | Đánh giá trên social |
| `Collaboration.Chat` / `Notifications` / `Realtime` / `Social` | Chat, thông báo, MXH |

Sau khi admin đổi quyền, user cần login lại để nhận claim mới.
