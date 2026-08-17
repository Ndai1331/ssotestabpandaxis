# HCS feature parity checklist

Nguồn chuẩn: `services/HCS_web/scripts/feature-inventory/build_hc_feature_inventory.mjs`.

Trạng thái: `Pending` → `API` → `UI` → `Verified`. Chỉ dùng `Verified` sau khi API, permission, UI và flow liên quan đã được kiểm thử trên target.

| ID | Chức năng | Owner | Status | Nguồn |
|---|---|---|---|---|
| M01 | Trang chủ | Platform/Blazor | Pending | `Index/TenantDashboard` |
| M02 | Quản lý tài liệu | DocumentService (P3) | Pending | `Documents.razor` |
| M03 | Tài liệu của tôi | DocumentService (P3) | Pending | `Documents.Extended.razor.cs` |
| M04 | Tài liệu gửi tới tôi | DocumentService (P3) | Pending | `Documents.razor` |
| M05 | Chi tiết tài liệu | DocumentService (P3) | Pending | `DocumentDetail.razor` |
| M06 | Xem chi tiết tài liệu | DocumentService (P3) | Pending | `ViewDocumentDetail.razor` |
| M07 | Trình ký | DocumentService (P3) | Pending | `DocumentSigning.razor` |
| M08 | File tài liệu | DocumentService (P3) | Pending | `DocumentFiles.razor` |
| M09 | Phân công tài liệu | DocumentService (P3) | Pending | `DocumentAssignments.razor` |
| M10 | Quy trình văn bản | DocumentService (P3) | Pending | `DocumentWorkflowInstances.razor` |
| M11 | Lịch sử tài liệu | DocumentService (P3) | Pending | `DocumentHistories.razor` |
| M12 | Loại quy trình | DocumentService (P3) | Pending | `WorkflowDefinitions.razor` |
| M13 | Quy trình | DocumentService (P3) | Pending | `Workflows.razor` |
| M14 | Chi tiết quy trình | DocumentService (P3) | Pending | `WorkflowDetail.razor` |
| M15 | Mẫu quy trình/trình ký | DocumentService (P3) | Pending | `WorkflowDetail.razor` |
| M16 | Bước quy trình/trình ký | DocumentService (P3) | Pending | `WorkflowDetail.razor` |
| M17 | Người thực hiện bước | DocumentService (P3) | Pending | `WorkflowDetail.razor` |
| M18 | Danh sách dự án | ProjectService (P4) | Pending | `Projects.razor` |
| M19 | Chi tiết dự án | ProjectService (P4) | Pending | `ProjectDetail.razor` |
| M20 | Công việc | ProjectService (P4) | Pending | `ProjectTasks.razor` |
| M21 | Chi tiết công việc | ProjectService (P4) | Pending | `ProjectTasksDetail.razor` |
| M22 | Gán người thực hiện công việc | ProjectService (P4) | Pending | `ProjectTaskCreateModal` |
| M23 | Văn bản của công việc | ProjectService (P4) | Pending | `ProjectTaskCreateModal` |
| M24 | Lịch & Sự kiện | CalendarService (P5) | Pending | `CalendarEvents.razor` |
| M25 | Chi tiết sự kiện | CalendarService (P5) | Pending | `CalendarEventDetail.razor` |
| M26 | Người tham gia sự kiện | CalendarService (P5) | Pending | `CalendarEventParticipants` |
| M27 | Khảo sát hài lòng | SurveyService (P5) | Pending | `SurveyResults.razor` |
| M28 | Phiên khảo sát | SurveyService (P5) | Pending | `SurveySessions.razor` |
| M29 | Tệp khảo sát | SurveyService (P5) | Pending | `SurveyFiles controllers` |
| M30 | Biểu mẫu khảo sát công khai | SurveyService (P5) | Pending | `SurveyCollections.razor` |
| M31 | Loại văn bản | DocumentService (P3) | Pending | `DocumentTypes.razor` |
| M32 | Lĩnh vực | DocumentService (P3) | Pending | `Sectors.razor` |
| M33 | Mức độ cấp bách | DocumentService (P3) | Pending | `UrgencyLevels.razor` |
| M34 | Mức độ bí mật | DocumentService (P3) | Pending | `ConfidentialityLevels.razor` |
| M35 | Phương pháp xử lý | DocumentService (P3) | Pending | `ProcessingMethods.razor` |
| M36 | Trạng thái văn bản | DocumentService (P3) | Pending | `DocumentStatus.razor` |
| M37 | Phương pháp ký | DocumentService (P3) | Pending | `SigningMethods.razor` |
| M38 | Loại sự kiện | CalendarService (P5) | Pending | `EventTypes.razor` |
| M39 | Đơn vị | OrganizationService (P2) | Pending | `Units.razor` |
| M40 | Chức vụ | OrganizationService (P2) | Pending | `Positions.razor` |
| M41 | Vị trí khảo sát | SurveyService (P5) | Pending | `SurveyLocations.razor` |
| M42 | Tiêu chí khảo sát | SurveyService (P5) | Pending | `SurveyCriterias.razor` |
| M43 | Cấu hình chữ ký | DocumentService (P3) | Pending | `SignatureSettings.razor` |
| M44 | Cấu hình báo cáo | ReportingService (P7) | Pending | `Reports.razor` |
| M45 | Phòng ban | Platform/Identity (ABP OU, P2) | Implemented | ABP Identity Organization Units |
| M46 | Phòng ban của người dùng | Platform/Identity (ABP OU members, P2) | Implemented | ABP Identity Organization Unit members |
| M47 | Báo cáo động | ReportingService (P7) | Pending | `ReportMenuDataProvider.cs` |
| M48 | Báo cáo nghiệp vụ | ReportingService (P7) | Pending | `HCPermissionDefinitionProvider.cs` |
| M49 | Hồ sơ của tôi | Platform/Identity | Pending | `MyProfile.razor` |
| M50 | Quản lý files | Platform/FileManagement | Pending | ABP File Management |
| M51 | Thông báo | CollaborationService (P6) | Pending | `NotificationReceivers.razor` |
| M52 | Chat | CollaborationService (P6) | Pending | `Chat1.razor` |
| M53 | Đăng xuất | Platform/Auth | Pending | `HCMenuContributor.cs` |
| M54 | Chữ ký người dùng | DocumentService (P3) | Pending | `UserSignatures.razor` |
| M55 | Người dùng | Platform/Identity | Pending | ABP Identity |
| M56 | Vai trò & quyền | Platform/Identity | Pending | ABP Identity Pro |
| M57 | OpenIddict | Platform/AuthServer | Pending | ABP OpenIddict Pro |
| M58 | Language Management | Platform/Language | Pending | ABP Language Management |
| M59 | Text Template Management | Platform/TextTemplate | Pending | ABP Text Template Management |
| M60 | Audit Logs | Platform/AuditLogging | Pending | ABP Audit Logging |
| M61 | Settings | Platform/Administration | Pending | ABP Setting Management |
| M62 | SaaS | Platform/SaaS | Pending | ABP SaaS |
| M63 | Dashboard Host/Tenant | Platform/Blazor | Pending | `HostDashboard/TenantDashboard` |
| M64 | Thông báo realtime/push | CollaborationService (P6) | Pending | `PushNotificationWorker, NotificationHub` |
| M65 | Chat realtime | CollaborationService (P6) | Pending | `ChatHub` |
| M66 | Chuyển đổi DOCX/PDF | DocumentService (P3) | Pending | `DocxToPdfConverter` |
| M67 | Ký số Remote CA | DocumentService (P3) | Pending | signing services |

## Quy tắc ownership

- Platform modules đã có trên target không được port lại vào domain service.
- Master data đi theo service sử dụng chính; dữ liệu dùng chéo qua contract/event.
- Reporting chỉ đọc projection/ETL/event, không join trực tiếp database service khác.
