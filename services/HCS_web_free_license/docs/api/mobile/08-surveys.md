# 08 — Khảo sát

Policy quản trị tiêu chí/địa điểm: `WorkManagement.SurveyManagement`. Session/kết quả: `WorkManagement.Surveys`. Thu thập public: **anonymous**.

## Pages

| Route | Permission |
|---|---|
| `/survey-locations` | `WorkManagement.SurveyManagement` |
| `/survey-criterias` | `WorkManagement.SurveyManagement` |
| `/survey-sessions` | `WorkManagement.Surveys` |
| `/survey-results` | `WorkManagement.Surveys` |
| `/survey-collections/{locationId}` | Public |

## Màn hình → API

### Địa điểm

`GET/POST /api/surveys/locations`, `PUT/DELETE /api/surveys/locations/{id}`.  
Web create/update gửi `organizationUnitId: null`.

### Tiêu chí

`GET /api/surveys/criteria`, `GET /api/surveys/locations` (dropdown), CRUD criteria, `POST /api/surveys/criteria/{id}/image` multipart max 25 MB.

### Phiên

`GET /api/surveys/sessions`, locations, create/update, `POST .../status`, delete.

### Kết quả

| Hành động | Method | Path |
|---|---|---|
| Locations | `GET` | `/api/surveys/locations` |
| Thống kê | `GET` | `/api/surveys/results/statistics?locationId=` |
| Paged tóm tắt | `GET` | `/api/surveys/results/summaries?locationId=&skip=&take=` |
| Chi tiết phiên | `GET` | `/api/surveys/results/{sessionId}/details?locationId=` |
| Xử lý | `PUT` | `/api/surveys/results/{sessionId}/handling` |
| Xóa phiên kết quả | `DELETE` | `/api/surveys/results/{sessionId}` |

### Public collection

| Hành động | Method | Path |
|---|---|---|
| Location | `GET` | `/api/surveys/public/locations/{locationId}` |
| Criteria | `GET` | `/api/surveys/public/criteria?locationId=` |
| Tạo session | `POST` | `/api/surveys/public/sessions` |
| File tùy chọn | `POST` | `/api/surveys/public/sessions/{sessionId}/files` |
| Nộp điểm | `POST` | `/api/surveys/public/sessions/{sessionId}/results` |

## DTO quản trị

Location: `id`, `code`, `name`, `organizationUnitId`, `isActive`, `description`.  
POST `{ "code", "name", "organizationUnitId", "description", "isActive": true }`.  
PUT `{ "name", "organizationUnitId", "isActive", "description" }`.

Criteria: `id`, `code`, `name`, `sortOrder`, `isActive`, `locationId`, `image`.  
POST `{ "code", "name", "sortOrder", "locationId", "image", "isActive": true }`.  
PUT `{ "name", "sortOrder", "isActive", "locationId", "image" }`.

Session: `id`, `code`, `name`, `startsAt`, `endsAt`, `status`, `locationId`, plus public fields `fullName`, `phoneNumber`, `patientCode`, `surveyTime`, `deviceType`, `note`, `sessionDisplay`.

POST session:

```json
{
  "code": "S01",
  "name": "Đợt 1",
  "startsAt": "2026-09-21T00:00:00Z",
  "endsAt": "2026-09-30T00:00:00Z",
  "locationId": "guid"
}
```

`POST /api/surveys/sessions/{id}/status` `{ "status": "Closed" }`.

List locations/criteria/sessions Web: **mảng**, không paged.

## Kết quả

Statistics: `{ totalReviews, scoreDistribution: { "1": n, ... }, criteriaAverageScores: { "tên tiêu chí": 4.2 } }`.

Summary: `surveySessionId`, `surveyTime`, `locationName`, `score`, `handlingStatus`, `handlingNote`, `fullName`, `phoneNumber`, `patientCode`, `note`.

`PUT .../handling`:

```json
{ "handlingStatus": "Handled", "handlingNote": "Đã liên hệ" }
```

Detail: `surveyResultId`, `surveySessionId`, `criteriaId`, `surveyCriteriaName`, `score`, `comment`.

Các API session results/files (`GET/POST /api/surveys/sessions/{id}/results|files`) có trên client; **trang results dùng `/api/surveys/results/*`**, không gọi session files trên page này.

## Public

POST session:

```json
{
  "locationId": "guid",
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0900000000",
  "patientCode": null,
  "surveyTime": "2026-09-21T10:00:00Z",
  "deviceType": "mobile",
  "note": null,
  "sessionDisplay": null
}
```

Giữ `session.id` cho file + results.

POST results: **mảng**

```json
[
  { "criteriaId": "guid", "respondentUserId": null, "score": 5, "comment": null }
]
```

File multipart `file`, max 25 MB.
