# HCS Mobile: hướng dẫn đăng nhập, API và parity với Web

> Cập nhật theo mã nguồn hiện tại ngày 2026-09-09. Tài liệu này là bản đồ triển khai native mobile để mobile làm được các chức năng đang có trên Web. Các endpoint bên dưới đi qua HCS Gateway/BFF; không gọi trực tiếp các service nội bộ.

## Mục lục

1. [Tóm tắt kiến trúc và giới hạn hiện tại](#1-tóm-tắt-kiến-trúc-và-giới-hạn-hiện-tại)
2. [Môi trường và URL](#2-môi-trường-và-url)
3. [Đăng nhập native mobile](#3-đăng-nhập-native-mobile)
4. [Quy ước gọi API chung](#4-quy-ước-gọi-api-chung)
5. [Ma trận page Web → API mobile](#5-ma-trận-page-web--api-mobile)
6. [Danh mục API chi tiết](#6-danh-mục-api-chi-tiết)
7. [Thứ tự triển khai và tiêu chí nghiệm thu](#7-thứ-tự-triển-khai-và-tiêu-chí-nghiệm-thu)
8. [Việc backend cần chốt trước khi mobile tích hợp](#8-việc-backend-cần-chốt-trước-khi-mobile-tích-hợp)

## 1. Tóm tắt kiến trúc và giới hạn hiện tại

### 1.1. Web và mobile dùng hai mô hình session khác nhau

Web hiện tại là mô hình BFF:

1. Browser vào `/bff/login` của Gateway.
2. Gateway đăng nhập OIDC với Auth Server, sau đó tạo session cookie `.HCS.Bff`.
3. Access token và refresh token nằm ở Gateway/Redis, không đưa xuống JavaScript của browser.
4. Khi Web gọi `/api/*`, Gateway lấy access token trong cookie rồi forward sang service bằng `Authorization: Bearer`.

Native mobile nên dùng Authorization Code + PKCE:

1. Mobile mở system browser hoặc thư viện OAuth native.
2. Browser đăng nhập tại Auth Server.
3. Auth Server trả authorization code về custom scheme hoặc universal/app link của mobile.
4. Mobile đổi code lấy access token và refresh token bằng `code_verifier`.
5. Mobile gọi Gateway bằng `Authorization: Bearer <access_token>`.

Không dùng cookie `.HCS.Bff`, không nhúng `HCS_App` client secret vào ứng dụng mobile, và không dùng password grant. `HCS_App` hiện là confidential client dành cho Gateway Web.

### 1.2. Giới hạn quan trọng trong mã nguồn hiện tại

Gateway hiện chỉ cấu hình cookie authentication làm scheme mặc định. Middleware proxy yêu cầu session cookie trước khi forward `/api/*`; chỉ gửi Bearer token vào Gateway hiện chưa đủ để đăng nhập qua Gateway. Gateway cũng đang áp dụng antiforgery cho các request unsafe của BFF.

Vì vậy, tài liệu này mô tả contract mobile mục tiêu, nhưng mobile chỉ có thể gọi API bảo vệ được sau khi backend hoàn thành các mục ở [phần 8](#8-việc-backend-cần-chốt-trước-khi-mobile-tích-hợp):

- đăng ký public mobile client với PKCE tại Auth Server;
- cho Gateway xác thực Bearer token cho mobile, hoặc cung cấp một API ingress riêng có cùng route contract;
- xác định chính sách antiforgery cho Bearer request (thường bỏ yêu cầu CSRF khi request đã được xác thực bằng Bearer, còn cookie BFF vẫn giữ CSRF);
- công bố redirect URI chính thức cho Android và iOS.

## 2. Môi trường và URL

| Môi trường | Auth Server | Gateway/BFF/API | Web |
|---|---|---|---|
| Local | `https://localhost:44401` | `https://localhost:44402` | `https://hcs.localhost` |
| Production | `https://auth-hcs.htltech.vn` | `https://api-hcs.htltech.vn` | `https://hanhchinhso.htltech.vn` |

Mobile dùng `Gateway/BFF/API` làm `baseUrl`. Ví dụ:

```text
Local:       https://localhost:44402/api/projects
Production:  https://api-hcs.htltech.vn/api/projects
```

Các port service `44411`–`44415` là port nội bộ/local của Platform, Organization, Document, Work Management và Collaboration. Không hard-code các port này trong mobile.

Trong local, thiết bị thật cần truy cập được máy dev và tin cậy certificate HTTPS phù hợp. Không tắt kiểm tra certificate ở production.

## 3. Đăng nhập native mobile

### 3.1. Discovery và client mobile

Mobile lấy endpoint từ discovery document thay vì hard-code toàn bộ URL:

```http
GET https://auth-hcs.htltech.vn/.well-known/openid-configuration
```

Discovery phải cung cấp tối thiểu `authorization_endpoint`, `token_endpoint`, `issuer`; nếu hỗ trợ logout/revoke thì dùng thêm `end_session_endpoint` hoặc `revocation_endpoint`.

Backend cần đăng ký một public client, ví dụ `hcs-mobile`, với:

- grant: `authorization_code` và `refresh_token`;
- PKCE: bắt buộc, `S256`;
- không có client secret;
- redirect URI chính xác, ví dụ `com.htltech.hcs:/oauth/callback` hoặc universal/app link đã được duyệt;
- scopes tối thiểu: `openid profile email roles HCS offline_access`.

Tên client và redirect URI ở trên chỉ là mẫu, chưa phải giá trị đã được seed trong repo.

### 3.2. Luồng Authorization Code + PKCE

Mobile tạo `code_verifier` ngẫu nhiên, tính `code_challenge = BASE64URL(SHA256(code_verifier))`, rồi mở authorization endpoint:

```text
{authorization_endpoint}
  ?client_id={mobile_client_id}
  &response_type=code
  &redirect_uri={registered_redirect_uri}
  &scope=openid%20profile%20email%20roles%20HCS%20offline_access
  &state={random_state}
  &code_challenge={code_challenge}
  &code_challenge_method=S256
```

Khi callback nhận `code`, mobile phải kiểm tra `state`, sau đó POST form-urlencoded tới `token_endpoint`:

```http
POST {token_endpoint}
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&client_id={mobile_client_id}&redirect_uri={registered_redirect_uri}&code={code}&code_verifier={code_verifier}
```

Token response chuẩn có dạng:

```json
{
  "access_token": "ey...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "...",
  "scope": "openid profile email roles HCS offline_access"
}
```

Lưu refresh token trong secure storage của hệ điều hành (Keychain/Keystore). Access token chỉ lưu trong memory hoặc secure storage ngắn hạn. Không log token, code verifier, hoặc response chứa secret.

### 3.3. Refresh token

Khi access token hết hạn, mobile gọi:

```http
POST {token_endpoint}
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&client_id={mobile_client_id}&refresh_token={refresh_token}
```

Nếu refresh thất bại do token bị revoke/expired, xóa credential cục bộ và đưa người dùng về login. Không lặp refresh vô hạn.

### 3.4. Gọi API sau khi đăng nhập

```http
GET {gateway_base_url}/api/account/my-profile
Authorization: Bearer {access_token}
Accept: application/json
```

Response `401` là access token thiếu/hết hạn/không được Gateway chấp nhận. `403` là đã đăng nhập nhưng thiếu permission. Mobile không được biến `403` thành màn hình login; hãy hiển thị access denied hoặc ẩn action tương ứng.

`GET /bff/user`, `GET /bff/antiforgery` và `POST /bff/logout` là contract cho browser BFF, không phải API login native. Mobile không cần gọi chúng trong luồng PKCE riêng.

### 3.5. Đăng xuất

Mobile xóa access token và refresh token khỏi secure storage. Nếu backend công bố `end_session_endpoint` hoặc revocation endpoint trong discovery thì gọi theo chuẩn OIDC/OAuth; sau đó xóa session cục bộ dù endpoint remote có lỗi.

## 4. Quy ước gọi API chung

### 4.1. Header và content type

Request JSON thông thường:

```http
Authorization: Bearer {access_token}
Accept: application/json
Content-Type: application/json
```

Upload dùng `multipart/form-data`. Download file dùng response binary và nên đọc `Content-Type`, `Content-Disposition`, `Content-Length` nếu có.

Các enum trong contract C# hiện được serialize dạng số trong nhiều request JSON của Web, ví dụ `status: 0`, `visibility: 1`, `kind: 0`. Với query string, binder thường nhận được cả tên enum lẫn số; để nhất quán mobile nên dùng đúng ví dụ/enum mapping trong các bảng dưới đây.

### 4.2. Bootstrap ABP, quyền và ngôn ngữ

Sau khi có access token, mobile có thể tải application configuration để biết current user, permission và setting mà Web đang dùng:

```text
GET /abp/application-configuration
GET /abp/application-localization?cultureName=vi
```

Hai endpoint bootstrap/localization được Gateway cho phép anonymous ở mức proxy để Web khởi tạo; với mobile, gửi Bearer nếu đã login và coi response là dữ liệu cấu hình có thể thay đổi. Không hard-code toàn bộ permission từ UI: dùng permission claims/configuration để quyết định page/action, sau đó vẫn xử lý `403` từ API.

### 4.3. Phân trang, ngày giờ và filter

Có hai kiểu phân trang đang tồn tại:

- ABP/Identity/Organization/Audit: `skipCount`, `maxResultCount`.
- Document/Work/Collaboration: `skip`, `take`.

Response phân trang chuẩn:

```json
{
  "totalCount": 42,
  "items": []
}
```

Ngày giờ gửi theo UTC ISO-8601, ví dụ `2026-09-09T08:00:00Z`. Với filter `from`/`to`, mobile nên convert từ giờ địa phương sang UTC trước khi gửi. UI có thể hiển thị lại theo timezone của thiết bị.

### 4.4. Status code và retry

| Status | Ý nghĩa và cách xử lý |
|---|---|
| `200` | Đọc JSON hoặc binary theo `Content-Type`. |
| `201` | Tạo resource thành công; đọc body. |
| `204` | Thành công, không có body. |
| `400` | Payload/query sai; đọc validation errors và hiển thị tại form. |
| `401` | Refresh một lần, nếu vẫn lỗi thì login lại. |
| `403` | Thiếu permission; giữ user ở page và ẩn/khóa action. |
| `404` | Resource không tồn tại hoặc conversation/project chưa có; xử lý empty/not found. |
| `409` | Conflict/idempotency/duplicate; không tự retry mù. Employee rating trùng ngày trả mã `Work:EmployeeRatingAlreadySubmitted`. |
| `413` | File quá kích thước. |
| `429` | Backoff tăng dần, có giới hạn. |
| `5xx`/timeout | Retry có giới hạn cho GET; POST chỉ retry khi có idempotency key hoặc client biết request chưa được xử lý. |

Lỗi ABP thường có dạng `error.code`, `error.message`, `error.details`, `error.validationErrors`. Mobile nên giữ cả `code` và message để map lỗi nghiệp vụ.

### 4.5. Idempotency và cache

Các thao tác tạo workflow instance, workflow decision, signing attempt và những thao tác có field `idempotencyKey` phải gửi một UUID ổn định cho mỗi thao tác nghiệp vụ. Khi timeout, retry cùng UUID; không tạo UUID mới.

Có thể cache read-only catalog và profile ngắn hạn. Sau create/update/delete, invalidate cache của resource đó. Không cache permission lâu vì permission claims được cập nhật sau khi user đăng nhập lại.

## 5. Ma trận page Web → API mobile

Bảng này là checklist parity. Các API chi tiết và field chính nằm ở [phần 6](#6-danh-mục-api-chi-tiết).

### 5.1. Tài khoản, dashboard và cộng tác

| Page Web | Auth/permission | API và hành vi mobile |
|---|---|---|
| `/`, `/login` | Public → login | Public welcome; user đã đăng nhập chuyển `/workspace`. Native dùng PKCE, không mở `/bff/login`. |
| `/account` | Authenticated | `GET/PUT /api/account/my-profile`; `POST .../change-password`; avatar qua `GET/POST/DELETE /api/identity/profile/avatar`. |
| `/workspace` | `WorkManagement.Dashboard` | `GET /api/dashboard`, đồng thời tải calendar, projects, tasks, documents, workflow instances và notifications để dựng các card/list. |
| `/notifications` | `Collaboration.Notifications` | `GET /api/notifications`, `GET /unread-count` hoặc `/count`; mark one/all read. Đăng ký push bằng `POST /api/notifications/devices`. |
| `/chat`, `/chat1`, `/chat/{conversationId}`, `/chat1/{conversationId}` | `Collaboration.Chat` | Contacts → conversations → permissions → messages; gửi/forward/delete/pin/read; upload attachment; SignalR `/hubs/chat` và REST fallback. |
| `/social`, `/social/profile` | `Collaboration.Social` | Feed/profile → people/profile → comments; create/update/delete post/comment; reaction/share; upload media qua `/api/social/uploads` và comment uploads. |
| `/reports`, `/report-web-frame` | `WorkManagement.Reports` nếu gọi được | `GET /api/reports?dimension=...`. `report-web-frame` là generic frame; kiểm tra dimension/contract trước khi làm màn hình native. |
| `/notification-receivers` | Placeholder hiện tại | Feature catalog trỏ `/api/notifications` nhưng `CanLoad=false`; Web chưa có contract CRUD thực tế cho page này. Mobile chỉ triển khai khi backend chốt API nghiệp vụ. |

### 5.2. Document, workflow và ký số

| Page Web | Auth/permission | API và hành vi mobile |
|---|---|---|
| `/manage-documents`, `/my-documents`, `/document-assignments`, `/document-files`, `/document-histories` | `Documents.View`; action cần thêm quyền | `GET /api/documents` với filter/status/source; mở detail; assign/send; upload/download/delete file theo quyền. |
| `/document-detail`, `/document-detail/{id}`, `/view-document-detail/{id}` | `Documents.View` | `GET /api/documents/{id}`; edit bằng `PUT`; submit/send/revoke; assignment; file content/watermarked content. |
| `/document-signing`, `/document-signing/{relatedId}` | `Documents.Signing.Execute` | Queue → workflow/task detail → decision/extend → signing attempt. Có thể cần `/api/identity/workflow-assignees/lookup`. |
| `/signature-settings` | `Documents.Signing.Configure` | Provider definitions → current credentials; save JSON hoặc multipart khi có layout image. |
| `/user-signatures` | Authenticated; write theo signing policy | List signature → upload/update/default/delete → download content. |
| `/signing-kpi-report` | `Documents.Signing.Report` | `GET /api/signing/reports/documents/{documentId}` và `GET /api/reports?dimension=signing`. |
| `/workflow-definitions` | `Documents.Workflow.View/Manage` | Kinds/definitions list; create/update/delete definition. |
| `/workflow-lists` | `Documents.Workflow.View/Manage` | Definitions + templates list; create/delete workflow definition; template metadata. |
| `/workflow-detail`, `/workflow-detail/{id}` | `Documents.Workflow.View/Manage` | Definition detail; assignee candidates; templates CRUD/active/file upload/download. |
| `/workflow-instances`, `/document-workflow-instances`, `/document-workflow-instances/{id}` | `Documents.Workflow.View` + decision/start khi thao tác | List/detail instances; start instance; task decision/extend; resubmit. |

### 5.3. Tổ chức và danh mục tham chiếu

| Page Web | Auth/permission | API và hành vi mobile |
|---|---|---|
| `/departments` | `HCS.Organization.Departments` | CRUD `/api/organization/departments`; hỗ trợ `parentId`. |
| `/unit-lists` | `HCS.Organization.Units` | CRUD `/api/organization/units`; chọn department trước khi tạo/sửa unit. |
| `/positions` | `HCS.Organization.Positions` | CRUD `/api/organization/positions`. |
| `/master-datas`, `/document-types`, `/sectors`, `/urgency-levels`, `/confidentiality-levels`, `/processing-methods`, `/document-status`, `/signing-methods`, `/event-types`, `/even-types` | `HCS.Organization.MasterData` | CRUD `/api/organization/master-data?type=...`; lưu `type` dạng enum/string theo contract. `/even-types` là alias typo đang có trong Web. |
| `/icd10` | `HCS.Catalogs.ICD10` | CRUD `/api/organization/icd10`. |
| `/blood-pressure` | `HCS.Catalogs.BloodPressure` | CRUD `/api/organization/blood-pressure`. |
| `/blood-glucose` | `HCS.Catalogs.BloodGlucose` | CRUD `/api/organization/blood-glucose`. |
| `/bmi` | `HCS.Catalogs.BMI` | CRUD `/api/organization/bmi`. |
| `/countries` | `HCS.Catalogs.Countries` | CRUD `/api/organization/countries`. |
| `/provinces` | `HCS.Catalogs.Provinces` | CRUD `/api/organization/provinces`; filter theo `countryId` nếu cần. |
| `/communes` | `HCS.Catalogs.Communes` | CRUD `/api/organization/communes`; filter theo `provinceId` nếu cần. |

### 5.4. Quản trị

| Page Web | Auth/permission | API và hành vi mobile |
|---|---|---|
| `/administration`, `/users`, `/identity/users-management` | Role `admin` | Users CRUD; assignable roles; user roles; organization mappings; tab cấu hình signing credential nếu cần. |
| `/administration/roles`, `/roles` | Role `admin` | Roles CRUD; permissions đọc từ `/api/permission-management/permissions`; cập nhật qua `/api/admin/roles/{roleName}/permissions`. Role `admin` bất biến. |
| `/administration/languages` | Role `admin` | Languages list/detail/create/update/delete. |
| `/administration/language-texts` | Role `admin` | Language texts theo `resourceName=HCS`, `cultureName`, filter; CRUD text. |
| `/administration/audit-logs` | `HCS.AuditViewer` | Paged audit log filter; mở detail để xem exception, action và entity changes. |
| `/administration/employee-ratings`, `/administration/employee-ratings/{userId}` | `WorkManagement.EmployeeRatings.Management` | Directory → summaries → employee detail theo kỳ. |
| `/administration/employee-ratings-dashboard` | `WorkManagement.EmployeeRatings.Dashboard` | `GET /api/employee-ratings/dashboard`. |

### 5.5. Công việc, lịch, sự kiện, khảo sát và đánh giá

| Page Web | Auth/permission | API và hành vi mobile |
|---|---|---|
| `/projects` | `WorkManagement.Projects` | List/filter projects; create/update/delete; mở project detail; tạo conversation project nếu cần. |
| `/project-detail`, `/project-detail/{id}` | `WorkManagement.Projects` | Project detail + members/tasks; add/remove member; `POST /api/projects/{id}/chat-access`. |
| `/tasks` | `WorkManagement.ProjectTasks` | List theo project/status; update/delete task. |
| `/project-task-detail/{id}` | `WorkManagement.ProjectTasks` | Task detail; assignment add/remove; document reference add/remove. |
| `/calendar-events` | `WorkManagement.Calendar` | List theo khoảng UTC; create/update/delete event; project/task links từ API tương ứng. |
| `/calendar-event-detail/{id}` | `WorkManagement.Calendar` | Detail + update/delete. |
| `/events` | `WorkManagement.Events` | Event list; create/update/delete. |
| `/events/{id}` | `WorkManagement.Events` | Event detail/dashboard attendance; attendee CRUD/status/bulk delete/import; QR and attachments. |
| `/event-dashboard` | `WorkManagement.Events` | `GET /api/events/dashboard?from&to`. |
| `/event-check-in/{code}` | Public | Public event lookup và check-in, chỉ cần `code` + `token`; không yêu cầu bearer. |
| `/survey-locations` | `WorkManagement.SurveyManagement` | Locations CRUD. |
| `/survey-criterias` | `WorkManagement.SurveyManagement` | Criteria CRUD, load locations. |
| `/survey-sessions` | `WorkManagement.Surveys`/management action | Sessions theo location; create/update/status/delete; xem results/files. |
| `/survey-results` | `WorkManagement.Surveys` | Statistics, paged summaries, details theo session/location. |
| `/survey-collections/{locationId}` | Public | Location + criteria → create anonymous session → submit results → optional file upload. |
| `/employee-ratings` | `WorkManagement.EmployeeRatings` | Directory/search employee → `POST /api/employee-ratings` score 1–5; duplicate same day xử lý mã conflict. |

## 6. Danh mục API chi tiết

Tất cả endpoint dưới đây có prefix `{gateway}/api`, trừ khi ghi rõ là Auth Server, SignalR hoặc public route. `204` nghĩa là không có response body.

### 6.1. Account, Identity, Language và Audit

#### Account

| Method | Endpoint | Body/query | Response chính |
|---|---|---|---|
| `GET` | `/account/my-profile` | — | `{userName,email,name,surname,phoneNumber,isExternal,hasPassword,concurrencyStamp}` |
| `PUT` | `/account/my-profile` | `{userName,email,name?,surname?,phoneNumber?,concurrencyStamp}` | Account profile mới |
| `POST` | `/account/my-profile/change-password` | `{currentPassword,newPassword}` | `204` |
| `POST` | `/identity/profile/avatar` | multipart `file`, tối đa 2 MB | `204` |
| `DELETE` | `/identity/profile/avatar` | — | `204` |
| `GET` | `/identity/profile/avatar` | — | Binary avatar; `404` nếu chưa có |
| `GET` | `/identity/users/{userId}/avatar` | — | Binary avatar của user |

Khi update profile, phải gửi `concurrencyStamp` mới nhất để tránh ghi đè dữ liệu người khác. Avatar nên render từ byte response hoặc URL Gateway có kèm Bearer, không lưu URL MinIO nội bộ.

#### User, role và permission administration

| Method | Endpoint | Body/query | Response chính |
|---|---|---|---|
| `GET` | `/identity/users` | `filter,skipCount,maxResultCount` | `{totalCount,items:[User]}` |
| `POST` | `/identity/users` | `{userName,password,surname,name,email,phoneNumber?,isActive,lockoutEnabled,roleNames[]}` | `User` |
| `PUT` | `/identity/users/{id}` | Payload create + `password?`, `concurrencyStamp` | `User` |
| `DELETE` | `/identity/users/{id}` | — | `204` |
| `GET` | `/identity/roles` | `filter,skipCount,maxResultCount` | `{totalCount,items:[Role]}` |
| `POST` | `/identity/roles` | `{name,isDefault,isPublic}` | `Role` |
| `DELETE` | `/identity/roles/{id}` | — | `204` |
| `GET` | `/identity/users/assignable-roles` | — | `{items:[Role]}` |
| `GET` | `/identity/users/{userId}/roles` | — | `{items:[Role]}` |
| `PUT` | `/identity/users/{userId}/roles` | `{roleNames:[]}` | `204` |
| `GET` | `/permission-management/permissions` | `providerName=R&providerKey={roleName}` | Permission groups và `isGranted` |
| `PUT` | `/admin/roles/{roleName}/permissions` | `{permissions:[{name,isGranted}]}` | `204` |

User DTO chính có `id,userName,name,surname,email,emailConfirmed,phoneNumber,isActive,lockoutEnabled,twoFactorEnabled,accessFailedCount,creationTime,lastModificationTime,concurrencyStamp`. Role DTO có `id,name,isDefault,isStatic,isPublic`.

Organization mapping trong tab user:

```http
GET    /organization/user-mappings?userId={id}&skipCount=0&maxResultCount=100
POST   /organization/user-mappings
PUT    /organization/user-mappings/{mappingId}
DELETE /organization/user-mappings/{mappingId}
```

Body create/update:

```json
{
  "userId": "guid",
  "departmentId": "guid",
  "unitId": null,
  "positionId": "guid",
  "isPrimary": true
}
```

#### Languages và language texts

Languages:

```text
GET    /language-management/languages?filter&isEnabled&skipCount&maxResultCount
GET    /language-management/languages/enabled
GET    /language-management/languages/{id}
POST   /language-management/languages
PUT    /language-management/languages/{id}
DELETE /language-management/languages/{id}
```

Create body: `{cultureName,displayName,isEnabled,isDefault}`. Update body: `{displayName,isEnabled,isDefault}`. `GET /enabled` trả `[{cultureName,displayName,isDefault}]` và là endpoint anonymous để mobile lấy danh sách ngôn ngữ trước login nếu cần.

Language texts:

```text
GET    /language-management/language-texts?resourceName=HCS&cultureName=vi&filter&skipCount&maxResultCount
GET    /language-management/language-texts/{id}
POST   /language-management/language-texts
PUT    /language-management/language-texts/{id}
DELETE /language-management/language-texts/{id}
```

Body create: `{resourceName,cultureName,name,value}`; update: `{value}`. Repo cũng có alias `/api/hcs/languages` và `/api/hcs/language-texts`, nhưng Web đang dùng `/api/language-management/...`; mobile nên dùng cùng path với Web.

#### Audit logs

```http
GET /audit-logs?skipCount=0&maxResultCount=20&sorting=ExecutionTime%20desc
```

Filter hỗ trợ `filter,userId,userName,startTime,endTime,endTimeExclusive,httpStatusCode,httpMethod,clientIpAddress,browserInfo,sourceService,applicationName,hasException,correlationId,action,url`. Response `{totalCount,items}`. Mở chi tiết:

```http
GET /audit-logs/{id}
```

Detail có thể gồm `BrowserInfo`, `Exceptions`, `Comments`, `Actions` và `EntityChanges`. Page Web dùng page size mặc định 20, tối đa 100 và thời gian UTC.

### 6.2. Organization và reference catalogs

Các list endpoint sau dùng query chung `filter,isActive,skipCount,maxResultCount` và response `{totalCount,items}`. Mỗi resource có `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`; POST/PUT nhận các field của DTO tương ứng. List page Web mặc định 20 và giới hạn 100.

| Resource | Endpoint | Field chính |
|---|---|---|
| Department | `/organization/departments` | `id,code,name,parentId,sortOrder,isActive` |
| Unit | `/organization/units` | `id,departmentId,code,name,sortOrder,isActive` |
| Position | `/organization/positions` | `id,code,name,signOrder,sortOrder,isActive` |
| ICD-10 | `/organization/icd10` | `id,code,name,diseaseGroup,isChronic,sortOrder` |
| Blood pressure | `/organization/blood-pressure` | `id,HATTMin,HATTMax,HATTrMin,HATTrMax,title,description,sortOrder` |
| Blood glucose | `/organization/blood-glucose` | `id,title,minValue,maxValue,description,beforeMeal,sortOrder` |
| BMI | `/organization/bmi` | `id,title,gender,minValue,maxValue,description,sortOrder` |
| Country | `/organization/countries` | `id,code,name,countryCode,sortOrder` |
| Province | `/organization/provinces` | `id,code,name,countryId,countryCode,sortOrder` |
| Commune | `/organization/communes` | `id,code,name,provinceId,provinceCode,sortOrder` |

Master data dùng một endpoint:

```text
GET    /organization/master-data?type=DocumentType&filter&isActive&skipCount&maxResultCount
GET    /organization/master-data/{id}
POST   /organization/master-data
PUT    /organization/master-data/{id}
DELETE /organization/master-data/{id}
```

Body/DTO: `{id?,type,code,name,sortOrder,isActive}`. Các `type` đang dùng: `DocumentType`, `Sector`, `UrgencyLevel`, `ConfidentialityLevel`, `ProcessingMethod`, `DocumentStatus`, `SigningMethod`, `EventType`.

Lookup đặc biệt:

```http
GET /organization/departments
GET /organization/user-departments?userIds={guid}&userIds={guid}
```

`user-departments` trả `userId,departmentId,departmentName,positionId,positionName` và được dùng khi chọn người nhận, người ký hoặc người hiển thị trên social. Cascading reference UI là country → province → commune.

### 6.3. Documents, workflows và signing

#### Documents

```http
GET /documents?filter&status&mine=false&skip=0&take=20&sourceType&documentTypeId&sectorId&urgencyId&confidentialityId&from&to
POST /documents
GET /documents/{id}
PUT /documents/{id}
```

Document chính có `id,number,title,description,status,documentTypeId,sectorId,urgencyId,confidentialityId,files,assignments,history,creationTime,sourceType,parentDocumentId,fromUserId,organizationUnitId`. `DocumentStatus`: `Draft=0`, `Submitted=1`, `InReview=2`, `Approved=3`, `Rejected=4`, `Archived=5`. `DocumentSourceType`: `Archive=0`, `Personal=1`, `SentToMe=2`, `Workflow=3`.

Create body:

```json
{
  "number": null,
  "title": "Tên văn bản",
  "description": "Mô tả",
  "documentTypeId": "guid",
  "sectorId": "guid",
  "urgencyId": "guid",
  "confidentialityId": "guid",
  "sourceType": 0
}
```

Các thao tác:

| Method | Endpoint | Body | Quyền |
|---|---|---|---|
| `PUT` | `/documents/{id}` | `{title,description?,documentTypeId?,sectorId?,urgencyId?,confidentialityId?}` | `Documents.Update` |
| `POST` | `/documents/{id}/assignments` | `{assigneeUserId,responsibility}` | `Documents.Assign` |
| `POST` | `/documents/{id}/submit` | `{}` | `Documents.Update` |
| `POST` | `/documents/{id}/send` | `{receiverUserId?,organizationUnitId?}` | `Documents.Assign` |
| `POST` | `/documents/{id}/revoke` | `{}` | `Documents.Assign` |
| `POST` | `/documents/{id}/files` | multipart `file`, tối đa 50 MB | `Documents.ManageFiles` |
| `GET` | `/documents/{id}/files/{fileId}/content` | — | `Documents.View` |
| `GET` | `/documents/{id}/files/{fileId}/watermarked-content` | — | `Documents.View` |
| `DELETE` | `/documents/{id}/files/{fileId}` | — | `Documents.ManageFiles` |

File DTO có `id,fileName,contentType,size,sha256,creationTime,pairedFileId?`. Khi download, lấy tên file từ `Content-Disposition` nếu có; không suy ra URL storage trực tiếp.

#### Workflow

Kinds:

```text
GET    /workflows/kinds
GET    /workflows/kinds/{id}
POST   /workflows/kinds
PUT    /workflows/kinds/{id}
DELETE /workflows/kinds/{id}
```

Create body `{code,name,description?,isActive}`; update body `{name,description?,isActive}`. Definition:

```text
GET    /workflows/definitions
GET    /workflows/definitions/{id}
POST   /workflows/definitions
PUT    /workflows/definitions/{id}
DELETE /workflows/definitions/{id}
GET    /workflows/definitions/{definitionId}/assignee-candidates
```

Definition create body tối thiểu:

```json
{
  "code": "APPROVAL_01",
  "name": "Quy trình duyệt",
  "kindId": "guid",
  "description": "...",
  "isActive": true,
  "signMode": "SEQUENTIAL",
  "steps": [
    {
      "code": "REVIEW",
      "name": "Kiểm tra",
      "order": 1,
      "requiredPermission": "Documents.Workflow.Decide",
      "type": "PROCESS",
      "assigneeType": "SpecificUser",
      "assigneeUserId": "guid",
      "slaDays": 2,
      "allowReturn": true
    }
  ]
}
```

Template:

```text
GET  /workflows/templates
POST /workflows/templates
PUT  /workflows/templates/{id}
POST /workflows/templates/{id}/active       body: true|false
POST /workflows/templates/{id}/files?kind=word|pdf   multipart file, tối đa 50 MB
GET  /workflows/templates/{id}/files/{kind}/content
```

Template DTO có `id,code,name,definitionId,version,isActive,wordFileId,wordFileName,pdfFileId,pdfFileName,templateJson,outputFormat,creationTime`. Output format mặc định `PDF`.

Instances/tasks:

```text
GET  /workflows/instances?documentId&status
GET  /workflows/instances/{id}
POST /workflows/instances
POST /workflows/tasks/{taskId}/decision
POST /workflows/tasks/{taskId}/extend
POST /workflows/instances/{instanceId}/resubmit
GET  /identity/workflow-assignees/lookup?userIds={guid}&userIds={guid}
```

Start instance body gồm `{documentId?,definitionId,idempotencyKey,signers?,viewScopes?,useTemplateFile,useWorkflowTemplateFile,signingContent?}`. Theo rule hiện tại phải chọn đúng nguồn file: `useWorkflowTemplateFile` hoặc `documentId`.

Decision body:

```json
{
  "approve": true,
  "comment": "Đồng ý",
  "idempotencyKey": "uuid",
  "return": false,
  "signingAttemptId": null,
  "signingFileId": null
}
```

`WorkflowInstanceStatus`: `Running=0`, `Completed=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`. `ApprovalTaskStatus`: `Pending=0`, `Approved=1`, `Rejected=2`, `Cancelled=3`, `Returned=4`. Resubmit nhận raw JSON string idempotency key, ví dụ body là `"uuid"`.

#### Signing

Provider và credential:

```text
GET /signing/provider-definitions
GET /signing/credentials/current?userId={id?}
PUT /signing/credentials/current?userId={id?}
PUT /signing/credentials/current/upload?userId={id?}   multipart
GET /signing/queue
```

JSON credential body: `{kind,providerCode,endpoint,secret,layoutImageBase64?,apiTimeoutSeconds,signWidth,signHeight,allowElectronicSign,allowDigitalSign,requireOtp}`. Multipart có thêm field `layoutImage`; secret không được log. `SigningKind`: `Electronic=0`, `RemoteCa=1`, `Hsm=2`, `UsbToken=3`.

Create signing attempt:

```http
POST /signing/attempts
Content-Type: application/json

{
  "documentId": "guid",
  "fileId": "guid",
  "kind": 0,
  "idempotencyKey": "uuid",
  "signatureId": null,
  "placeholder": null,
  "signerName": "Nguyễn Văn A",
  "note": null
}
```

Response có `id,documentId,fileId,kind,status,inputSha256,outputSha256,error,creationTime,completedAt`. `SigningStatus`: `Pending=0`, `Completed=1`, `Failed=2`.

User signatures:

```text
GET    /signing/signatures?userId={id?}
POST   /signing/signatures?userId={id?}                 multipart file
PUT    /signing/signatures/{id}?userId={id?}            multipart, file optional
PUT    /signing/signatures/{id}/default?userId={id?}
DELETE /signing/signatures/{id}?userId={id?}
GET    /signing/signatures/{id}/content?userId={id?}
```

Signature upload nhận `file`, `signatureType`, `providerCode?`, `tokenRef?`, `secret?`, `sealImage?`, `validFrom?`, `validTo?`, `isActive`; file/seal tối đa 2 MB, ảnh hỗ trợ JPEG/PNG/WebP/GIF. `UserSignatureType`: `Electronic=0`, `Digital=1`.

### 6.4. Projects, tasks, calendar và events

#### Projects và tasks

```text
GET    /projects?filter&status&skip&take
GET    /projects/{id}
POST   /projects
PUT    /projects/{id}
DELETE /projects/{id}
POST   /projects/{id}/members
DELETE /projects/{id}/members/{memberId}
POST   /projects/{id}/chat-access
```

Project list có `id,code,name,description,startDate,endDate,status,ownerDepartmentId,ownerUserId,memberCount,taskCount`. Create body `{code,name,description?,startDate,endDate,status,ownerDepartmentId?}`; update bỏ `code` và `owner`.

Task:

```text
GET    /project-tasks?projectId&filter&status&skip&take
GET    /project-tasks/{id}
POST   /project-tasks
PUT    /project-tasks/{id}
DELETE /project-tasks/{id}
POST   /project-tasks/{id}/assignments
DELETE /project-tasks/{id}/assignments/{assignmentId}
POST   /project-tasks/{id}/documents
DELETE /project-tasks/{id}/documents/{referenceId}
```

Task body create `{projectId,parentTaskId?,code,title,description?,startDate,dueDate,priority,status,progressPercent}`; update `{title,description?,startDate,dueDate,priority,status,progressPercent}`. Detail trả `{task,assignments:[{id,projectTaskId,userId,assignmentType}],documents:[{id,projectTaskId,documentId,documentCode?}]}`.

Member body `{userId,role}`. Assignment body `{userId,assignmentType}`. Task-document body `{documentId,documentCode?}`.

#### Calendar

```text
GET    /calendar?from={utc}&to={utc}
GET    /calendar/{id}
POST   /calendar
PUT    /calendar/{id}
DELETE /calendar/{id}
```

Event body: `{title,description?,startTime,endTime,allDay,eventType,location?,relatedType,relatedId?,visibility,participantUserIds[]}`. List trả array, không phải `{totalCount,items}`.

#### Events và attendance

```text
GET    /events?filter&group&status&skip&take
GET    /events/dashboard?from={utc}&to={utc}
GET    /events/{id}
POST   /events
PUT    /events/{id}
DELETE /events/{id}
GET    /events/{eventId}/attendees?filter&registrationStatus&checkInStatus&skip&take
POST   /events/{eventId}/attendees
PUT    /events/attendees/{attendeeId}
POST   /events/attendees/{attendeeId}/status
DELETE /events/attendees/{attendeeId}
POST   /events/{eventId}/attendees/delete-bulk
POST   /events/{eventId}/attendees/import       multipart CSV, tối đa 2 MB
GET    /events/{id}/qr                           image/png
POST   /events/{eventId}/attachments             multipart, tối đa 25 MB
GET    /events/attachments/{fileId}
DELETE /events/attachments/{fileId}
```

Create/update event body `{group,name,content?,description?,location?,startTime,endTime,status}`. Detail có `attachments` và attendance counts `{total,confirmed,unconfirmed,declined,checkedIn,notCheckedIn}`.

Attendee body có `userId?,username?,surname?,name?,fullName,cccd?,phoneNumber?,email,address?,registrationStatus,checkInStatus,note?`. Update không cần `userId`. Bulk delete nhận raw JSON array GUID; response là số lượng đã xóa. Import trả `{imported,skipped}`.

Public event check-in không cần bearer:

```text
GET  /events/public/{code}?token={qr_token}
POST /events/public/{code}/check-in?token={qr_token}
```

Check-in body `{fullName?,cccd?,phoneNumber?,email?}`, response `{fullName,checkedInAt}`. Không đưa endpoint public này vào luồng admin attendance.

### 6.5. Surveys, employee ratings, dashboard và reports

#### Survey public collection

Các endpoint sau anonymous:

```text
GET  /surveys/public/locations/{locationId}
GET  /surveys/public/criteria?locationId={id}
POST /surveys/public/sessions
POST /surveys/public/sessions/{sessionId}/results
POST /surveys/public/sessions/{sessionId}/files        multipart, tối đa 25 MB
```

Create session body `{locationId,fullName,phoneNumber,patientCode?,surveyTime,deviceType?,note?,sessionDisplay?}`. Results là array các item `{criteriaId,respondentUserId?,score,comment?}`. Public app nên giữ `sessionId` response và dùng đúng session đó cho results/files.

#### Survey management và results

```text
GET    /surveys/locations
POST   /surveys/locations
PUT    /surveys/locations/{id}
DELETE /surveys/locations/{id}

GET    /surveys/criteria
POST   /surveys/criteria
PUT    /surveys/criteria/{id}
DELETE /surveys/criteria/{id}

GET    /surveys/sessions?locationId={id}
POST   /surveys/sessions
PUT    /surveys/sessions/{id}
POST   /surveys/sessions/{id}/status
DELETE /surveys/sessions/{id}
GET    /surveys/sessions/{id}/results
POST   /surveys/sessions/{id}/results
GET    /surveys/sessions/{id}/files
POST   /surveys/sessions/{id}/files          multipart
GET    /surveys/files/{fileId}/content       binary

GET    /surveys/results/statistics?locationId={id}
GET    /surveys/results/summaries?locationId={id}&skip={n}&take={n}
GET    /surveys/results/{sessionId}/details?locationId={id}
```

Location có `id,code,name,organizationUnitId,isActive,description`. Criteria có `id,code,name,sortOrder,isActive,locationId,image`. Statistics trả `totalReviews,scoreDistribution,criteriaAverageScores`. Summary trả survey result/session/criteria, score và thông tin respondent; details thêm `comment`.

#### Employee ratings

Directory:

```http
GET /identity/employee-directory?filter={text}&skipCount=0&maxResultCount=50
```

Response `{items:[{userId,userName,displayName,avatarUrl?}],hasMore}`. Summary:

```text
GET  /employee-ratings/summary?from={utc}&to={utc}&skip&take
POST /employee-ratings
GET  /employee-ratings/{userId}/detail?period&from&to
GET  /employee-ratings/dashboard?from&to
```

Submit body `{targetUserId,score}` với score 1–5. Cùng một người đánh giá cùng target trong ngày trả `409` với code `Work:EmployeeRatingAlreadySubmitted`. Summary có `targetUserId,averageScore,reviewCount,scoreDistribution,myTodayScore,latestRatingAt`; detail có thêm trend; dashboard có `evaluatedEmployeeCount,companyAverageScore,distinctVoterCount,scoreDistribution,employeeSummaries`.

#### Dashboard và reports

```text
GET /dashboard
GET /reports?dimension=signing
```

Dashboard trả `activeProjects,openTasks,overdueTasks,activeSurveys,calculatedAt`. Report item có `dimension,key,label,value,refreshedAt`. `dimension` hiện chắc chắn được dùng là `signing`; các dimension khác cần backend công bố trước khi mobile dựng chart.

### 6.6. Chat, notifications, social và realtime

#### Chat REST

```text
GET    /chat/contacts?search&take
POST   /chat/conversations
GET    /chat/conversations?type&pinnedOnly&skip&take
GET    /chat/conversations/{id}
GET    /chat/conversations/by-project/{projectId}
GET    /chat/conversations/{id}/permissions
GET    /chat/conversations/{id}/messages?keyword&skip&take&pinnedOnly
GET    /chat/conversations/{id}/messages/{messageId}/context?before&after
POST   /chat/messages
POST   /chat/messages/{messageId}/forward
DELETE /chat/messages/{messageId}
PUT    /chat/messages/{messageId}/pin              body: {pinned}
POST   /chat/conversations/{id}/read
GET    /chat/unread-count
PUT    /chat/conversations/{id}/pin                body: {pinned}
PUT    /chat/conversations/{id}/name                body: {name}
POST   /chat/conversations/{id}/members             body: {userIds[]}
PUT    /chat/conversations/{id}/members/{userId}/role body: {role}
DELETE /chat/conversations/{id}/members/{userId}
POST   /chat/conversations/{id}/leave               body: {transferAdminTo?}
POST   /chat/conversations/{id}/attachments         multipart, tối đa 25 MB
GET    /chat/attachments/{attachmentId}             binary
DELETE /chat/attachments/{attachmentId}
```

Conversation type: `User=0`, `Group=1`, `Project=2`, `Task=3`; member role: `Member=0`, `Admin=1`. Message body:

```json
{
  "conversationId": "guid",
  "text": "Nội dung tối đa 4000 ký tự",
  "clientMessageId": "uuid",
  "replyToMessageId": null,
  "attachmentIds": []
}
```

Message DTO có `id,conversationId,senderUserId,text,createdAt,replyToMessageId,forwardedFromMessageId,isPinned,isDeleted,attachments`. Conversation permissions trả `canSend,canManageMembers,canRename,canLeave,canModerateMessages`; mobile dùng các cờ này để quyết định action.

#### Chat SignalR

Hub qua Gateway:

```text
https://api-hcs.htltech.vn/hubs/chat
```

Kết nối bằng Bearer token theo cơ chế SignalR native của platform. Các server event:

| Event | Payload |
|---|---|
| `ReceiveMessage` | `ChatMessageDto` |
| `MessageDeleted` | `{conversationId,messageId}` |
| `NotificationReceived` | `NotificationDto` |
| `PresenceChanged` | `{userId,isOnline}` |

Client có thể invoke `GetOnlineUserIds` để nhận danh sách GUID online. Luôn giữ REST làm nguồn đồng bộ: sau reconnect hoặc khi bỏ lỡ event, gọi lại conversation/messages. Tên event và auth policy phải được test trên SDK SignalR native của Android/iOS.

#### Notifications

```text
GET  /notifications?unreadOnly&skip&take
GET  /notifications/unread-count
GET  /notifications/count?unreadOnly
POST /notifications/read-all
POST /notifications/{notificationId}/read
POST /notifications/devices                  body: {token,platform}
```

Admin broadcast:

```http
POST /notifications
{
  "userIds": ["guid"],
  "title": "Tiêu đề",
  "body": "Nội dung",
  "link": "/document-detail/guid"
}
```

Notification có `id,userId,title,body,link,isRead,createdAt`. `link` là route Web; mobile cần map sang deep link nội bộ, không mở nguyên văn nếu không có allow-list.

#### Social REST

```text
GET    /social/feed?skip&take&keyword&from&to&hashtag&postId
GET    /social/profile/posts?skip&take&visibility&keyword&from&to&hashtag&postId
GET    /identity/social-people?search&take
GET    /identity/social-profile
GET    /social/posts/{postId}/comments?skip&take
POST   /social/posts
PUT    /social/posts/{postId}
DELETE /social/posts/{postId}
POST   /social/posts/{postId}/comments
DELETE /social/comments/{commentId}
POST   /social/posts/{postId}/reactions
POST   /social/comments/{commentId}/reactions
POST   /social/posts/{postId}/shares
POST   /social/uploads                         multipart, tối đa 25 MB
GET    /social/media/{mediaId}                 binary
DELETE /social/media/{mediaId}
POST   /social/comment-uploads                  multipart, tối đa 25 MB
GET    /social/comment-media/{mediaId}          binary
DELETE /social/comment-media/{mediaId}
```

Post body `{text?,visibility,mediaIds[]}`; comment body `{text?,parentCommentId?,attachmentIds[]}`. Visibility: `Public=0`, `Internal=1`; reaction: `Like=0`, `Love=1`, `Haha=2`, `Wow=3`, `Sad=4`, `Angry=5`.

Rule validation hiện tại: post tối đa 4000 ký tự và 10 media; comment tối đa 2000 ký tự và 10 attachment; post/comment phải có text hoặc media/attachment. `POST reaction` nhận `{reactionType,remove}` và trả aggregate counts/current user reaction. `POST share` trả `{postId,shareUrl,shareCount,alreadyShared}`.

`GET /identity/social-people` khi search rỗng có thể trả `[]`. Social feed hiện newest-first; public post hiển thị cho mọi HCS user đã đăng nhập, Internal chỉ hiển thị cho tác giả trên trang cá nhân theo behavior hiện tại của Web.

## 7. Thứ tự triển khai và tiêu chí nghiệm thu

### 7.1. Thứ tự khuyến nghị

1. Auth bootstrap: discovery, PKCE, refresh, logout, secure storage.
2. API client chung: base URL, bearer interceptor, retry, error decoder, UTC, pagination, upload/download.
3. Account + language + permission gate.
4. Workspace/dashboard và notifications.
5. Organization catalogs dùng cho các form.
6. Documents → workflows → signing.
7. Projects → tasks → calendar → events.
8. Surveys và employee ratings.
9. Chat REST → attachment → SignalR reconnect/fallback.
10. Social và media.
11. Admin pages nếu mobile có scope quản trị.

### 7.2. Definition of Done cho mỗi page

- Page có loading, empty, error, unauthorized và not-found state.
- List có filter, pagination và pull-to-refresh; không giả định response luôn có item.
- Detail reload được bằng deep link hoặc notification link.
- Create/update/delete cập nhật UI sau response thành công, không chỉ optimistic state.
- File có progress, giới hạn kích thước trước upload, cancel và retry có kiểm soát.
- Date hiển thị theo timezone thiết bị nhưng request filter gửi UTC.
- Permission lấy từ server claims/API và kiểm tra cả UI lẫn response `403`.
- Khi token expired: refresh đúng một lần rồi replay request an toàn; không replay POST không có idempotency.
- Các thao tác ký số, workflow decision, check-in và rating có confirmation phù hợp và chống double submit.

### 7.3. Smoke test xuyên page

1. Login → profile → đổi ngôn ngữ → refresh app.
2. Mở workspace → project/task/calendar → notification deep link.
3. Tạo document → upload file → assign/send → start workflow → decision → signing.
4. Tạo event → import attendee → QR public check-in → xem checked-in count.
5. Tạo survey public session → submit results → admin xem statistics/details.
6. Mở chat → gửi text/file → tắt mạng/bật lại → REST resync và nhận SignalR.
7. Tạo social post/photo → comment/reaction/share → xóa media/post.
8. Với user không có quyền, xác nhận `403` hiển thị access denied chứ không redirect login vòng lặp.

## 8. Việc backend cần chốt trước khi mobile tích hợp

Đây là các mục bắt buộc để contract trong tài liệu chạy được end-to-end:

1. **Public client**: seed `hcs-mobile` hoặc tên chính thức, grant code + refresh, PKCE S256, redirect URI Android/iOS, scopes và refresh-token lifetime.
2. **Gateway Bearer**: thêm JWT Bearer authentication/selector cho request có `Authorization: Bearer`, hoặc đưa các route `/api/*` và `/hubs/*` vào ingress mobile xác thực JWT rồi forward nội bộ.
3. **Antiforgery**: quyết định rõ Bearer request có được miễn BFF cookie/CSRF check hay không. Native mobile không có browser BFF cookie; nếu vẫn bắt `X-XSRF-TOKEN`, cần công bố flow native tương ứng.
4. **CORS/network policy**: native không bị CORS như browser, nhưng cần allow HTTPS host, WebSocket `/hubs/chat`, upload size và timeout trên reverse proxy.
5. **Discovery và certificate**: công bố issuer/metadata production, redirect URI và certificate chain cho Android/iOS.
6. **Permission contract**: cung cấp role/permission matrix cho mobile; sau khi admin đổi quyền, user cần login lại để nhận claims mới nếu Gateway dùng claims trong session/token.
7. **Notification push**: chốt `platform` accepted values, token lifecycle, deep-link allow-list và cơ chế refresh token push.
8. **API versioning**: khi đổi DTO/enum/validation, giữ backward compatibility hoặc công bố version/path mới; mobile không thể cập nhật đồng thời với Web.
9. **Undocumented placeholders**: chốt contract thật cho `/notification-receivers` và các report dimension ngoài `signing` trước khi triển khai native tương ứng.

### Nguồn kiểm tra trong repo

- Route/page: `src/HCS.Blazor.Client/Pages/*.razor`.
- Feature → endpoint mapping: `src/HCS.Blazor.Client/Pages/FeatureCatalog.cs`.
- Typed clients và request DTO: `src/HCS.Blazor.Client/Api/`, `src/HCS.Blazor.Client/Account/`.
- Gateway auth/proxy/BFF: `gateways/web/HCS.WebGateway/`.
- Auth client seed: `src/HCS.Domain/OpenIddict/OpenIddictDataSeedContributor.cs`.
- API contracts: `services/*/*.Contracts/` và controllers tương ứng.
