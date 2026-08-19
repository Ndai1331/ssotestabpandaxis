# HCS menu / route parity matrix

Source of truth: read-only `services/HCS_web_with_license/src/HC.Blazor/Menus/HCMenuContributor.cs` and existing Razor routes. Status remains `Pending` until the Community UI and gateway contract tests prove the route.

| Area | Route | Permission family | Owner | Status |
|---|---|---|---|---|
| Dashboard / Workspace | `/` | authenticated | Blazor | Pending |
| Văn bản lưu trữ | `/manage-documents?sourceType=0` | Documents | Document | Pending |
| Văn bản cá nhân | `/manage-documents?sourceType=1` | Documents | Document | Pending |
| Văn bản gửi đến | `/manage-documents?sourceType=2` | Documents + Assignments | Document | Pending |
| Ký văn bản | `/document-signing` | Documents.SubmitForSigning | Document | Pending |
| Định nghĩa quy trình | `/workflow-definitions` | WorkflowDefinitions | Document | Pending |
| Danh sách quy trình | `/workflow-lists` | Workflows | Document | Pending |
| Dự án | `/projects` | Projects | WorkManagement | Pending |
| Tasks | `/tasks` | Tasks | WorkManagement | Pending |
| Calendar | `/calendar-events` | CalendarEvents | WorkManagement | Pending |
| Survey | `/survey-results` | SurveyResults | WorkManagement | Pending |
| Loại văn bản | `/document-types` | MasterDatas.DocumentType | Organization | Pending |
| Lĩnh vực | `/sectors` | MasterDatas.Sector | Organization | Pending |
| Độ khẩn | `/urgency-levels` | MasterDatas.UrgencyLevel | Organization | Pending |
| Độ mật | `/confidentiality-levels` | MasterDatas.ConfidentialityLevel | Organization | Pending |
| Phương thức xử lý | `/processing-methods` | MasterDatas.ProcessingMethod | Organization | Pending |
| Trạng thái văn bản | `/document-status` | MasterDatas.DocumentStatus | Organization | Pending |
| Phương thức ký | `/signing-methods` | MasterDatas.SigningMethod | Organization | Pending |
| Loại sự kiện | `/even-types` | MasterDatas.EventType | Organization | Pending; preserve existing typo for compatibility |
| Đơn vị | `/unit-lists` | MasterDatas.Unit | Organization | Pending |
| Chức vụ | `/positions` | MasterDatas.Position | Organization | Pending |
| Địa điểm khảo sát | `/survey-locations` | MasterDatas.SurveyLocation | Organization | Pending |
| Tiêu chí khảo sát | `/survey-criterias` | MasterDatas.SurveyCriteria | Organization | Pending |
| Cấu hình ký | `/signature-settings` | MasterDatas.SignatureSettings | Document | Pending |
| Cấu hình báo cáo | `/reports` | MasterDatas.Reports | WorkManagement | Pending |
| Báo cáo động | `/report-web-frame?reportId={id}` | Reports | WorkManagement | Pending |
| Báo cáo KPI ký | `/signing-kpi-report` | SigningKpiReport | Document | Pending |
| Hồ sơ cá nhân | `/my-profile` | authenticated | Platform | Pending |
| Notifications | `/notification-receivers` | authenticated | Collaboration | Pending |
| Chat | `/chat` | authenticated | Collaboration | Pending |
| Users | `/identity/users-management` | Identity.Users | Platform | Pending |
| Roles | ABP OSS Identity route | Identity.Roles | Platform | Pending |
| Languages | custom route | Languages | Platform | Pending |
| Language Texts | custom route | LanguageTexts | Platform | Pending |
| Audit Logs | custom route | AuditLogs | Platform | Pending |
| Settings | ABP OSS route | SettingManagement | Platform | Pending |

## Explicit removals

The Community UI must not register menus/routes for SaaS, tenant management, GDPR, Text Templates, File Management, Forms, OpenIddict Applications/Scopes, Identity Pro claim types, Organization Units, or Security Logs. OpenIddict server endpoints remain available through `HCS.AuthServer`; only its Pro administration UI is excluded.
