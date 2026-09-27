# 00 — Đăng nhập, header và quy ước

Mọi endpoint nghiệp vụ trong bộ docs này đi qua Gateway. Mobile **không** dùng cookie `.HCS.Bff` và **không** gọi `/bff/login`.

## Production — copy

Bệnh viện 199. Mobile gọi Auth để lấy token, gọi Gateway cho mọi `/api/*`. Host Web là trang Blazor; app native không gọi API ở đó.

Host lấy từ env bệnh viện (`HCS_AUTH_PUBLIC_HOST`, `HCS_API_PUBLIC_HOST`). Ví dụ BV199:

```text
AUTH=https://auth.benhvien199.vn
API=https://api-hcs.benhvien199.vn
HCS_MOBILE_CLIENT_ID=hcs-mobile
```

| Việc | URL |
|---|---|
| Discovery | `{AUTH}/.well-known/openid-configuration` |
| Token / refresh / password login | `{AUTH}/connect/token` |
| Profile | `{API}/api/account/my-profile` |
| Bootstrap ABP | `{API}/api/abp/application-configuration` |
| Localization | `{API}/api/abp/application-localization?cultureName=vi` |
| Ngôn ngữ | `{API}/api/language-management/languages/enabled` |
| SignalR chat | `{API}/hubs/chat?access_token={access_token}` |

Local: `https://localhost:44401` (auth), `https://localhost:44402` (gateway).

## 1. Web vs mobile

| | Web Blazor | Native mobile |
|---|---|---|
| Client OpenIddict | `HCS_App` (confidential, secret trên Gateway) | `hcs-mobile` (public, không secret) |
| Grant | Authorization Code + PKCE, session cookie | **Password grant** (form trong app, như ABP Commercial). Không mở browser |
| Gọi API | Cookie + `X-XSRF-TOKEN` | `Authorization: Bearer {access_token}` |
| Token lưu ở | Redis / Gateway | Keychain / Keystore |
| SignalR | Cookie + antiforgery handler | `access_token` query trên handshake |

Không nhúng client secret `HCS_App` vào app mobile. PKCE/browser chỉ dùng nếu sau này cần SSO web-view; mặc định mobile **không** mở browser.

## 2. Client `hcs-mobile`

Seed khi `OpenIddict:Applications:HCS_Mobile:ClientId` được cấu hình. Local/Docker mặc định:

```text
HCS_MOBILE_CLIENT_ID=hcs-mobile
HCS_MOBILE_REDIRECT_URI=com.htltech.hcs:/oauth/callback
HCS_MOBILE_POST_LOGOUT_REDIRECT_URI=com.htltech.hcs:/oauth/logout
```

- Grant: `password`, `refresh_token` (in-app). `authorization_code` + PKCE chỉ khi cần SSO browser
- Public client, không secret
- Scopes: `openid profile email roles HCS offline_access`
- Audience JWT: `HCS`

Discovery:

```http
GET {auth_server}/.well-known/openid-configuration
```

Dùng `token_endpoint`, `issuer`; logout/revoke nếu có `end_session_endpoint` / `revocation_endpoint`.

## 3. Login trong app (password grant)

Form username/password **trong app**. POST thẳng Auth — giống ABP Commercial / MAUI.

```http
POST {AUTH}/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=hcs-mobile
&username={username}
&password={password}
&scope=openid profile email roles HCS offline_access
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

`GET /bff/user`, `GET /bff/antiforgery`, `GET /bff/login`, `POST /bff/logout` là contract browser. Mobile không gọi.

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
| `401` | Refresh một lần; nếu vẫn lỗi thì login. Gateway cũ reject `typ: at+jwt` — xem mục 7.1 |
| `403` | Thiếu permission — **không** đẩy về login |
| `404` | Resource không có / empty |
| `409` | Conflict (ví dụ `Work:EmployeeRatingAlreadySubmitted`) |
| `413` | File quá lớn |
| `429` | Backoff |
| `5xx` | Retry GET có giới hạn; POST chỉ retry khi có `idempotencyKey` |

### 7.1 Gateway 401 `invalid_token` (ID2004) với token PKCE hợp lệ

Triệu chứng (issuer / API host lấy từ env `HCS_AUTH_PUBLIC_HOST` / `HCS_API_PUBLIC_HOST` của từng bệnh viện):

- PKCE client `hcs-mobile` lấy được `access_token` (`typ: at+jwt`, `aud: ["HCS","HCS.BusManagementService"]`).
- `GET /connect/userinfo` trên Auth Server → `200`.
- Cùng Bearer gọi Gateway `/api/*` → `401`, `www-authenticate: Bearer error="invalid_token"`.
- Web cookie `.HCS.Bff` (client `HCS_App`) vẫn `200` — Web không đi JwtBearer.

Nguyên nhân: Gateway `HCS.Mobile.Bearer` dùng Microsoft JwtBearer, mặc định chỉ chấp `typ: JWT`. OpenIddict phát hành access token `typ: at+jwt`. Userinfo validate local nên không dính rule này.

Fix backend (cần **rebuild + redeploy Gateway**): `ValidTypes = at+jwt + JWT`, `ValidIssuers` lấy từ `Authentication__Authority` (có/không trailing slash), `Authentication__BearerMetadataAddress` trỏ discovery nội bộ (`http://auth-server:8080/.well-known/openid-configuration`). Collaboration JwtBearer cũng nhận `at+jwt` cho chat/social.

Issuer công khai không hardcode bệnh viện — compose set `Authentication__Authority=https://${HCS_AUTH_PUBLIC_HOST}`.

Sau deploy, kiểm tra lại (`{HCS_API_PUBLIC_HOST}` theo env bệnh viện):

```http
GET https://{HCS_API_PUBLIC_HOST}/api/account/my-profile
Authorization: Bearer {access_token}
```

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
