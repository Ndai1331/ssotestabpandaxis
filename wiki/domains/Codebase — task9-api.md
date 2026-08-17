---
type: domain
title: "Codebase — task9-api"
created: 2026-06-28
updated: 2026-06-28
tags:
  - task9/api
  - codebase
  - clean-architecture
status: mature
related:
  - "[[Task9 Platform Overview]]"
  - "[[Database Architecture]]"
  - "[[Codebase — task9-ui]]"
  - "[[CI/CD Pipeline]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Codebase — task9-api

Backend .NET REST (port 7093, prod `api.task9.pro`). Repo `services/api`, solution `QCTool.sln`. Theo **Clean Architecture + vertical slice** (mỗi feature có folder cùng tên ở mọi layer).

## 5 Projects (.csproj)

| Project | Vai trò | Phụ thuộc |
|---------|---------|-----------|
| **Domain** | Entities, business rules thuần. `BaseEntity.cs` là gốc. | (không) |
| **Application** | Service + DTO + interface mỗi feature. `ApplicationModule.cs` DI, `AutoMapperProfile.cs` mapping. | Domain, Contract |
| **Contract** | DTO request/response chia sẻ (cho client). 1 folder / feature. | Domain |
| **Core** | Cross-cutting: `Const/`, `Enum/`, `Exceptions/`, `Extension/`, `Helper/`. | (không) |
| **WebApi** | Controllers, Middlewares, Authorization, HostedServices, `Program.cs`, `MyModule.cs` (DI root), Dockerfile. | tất cả |
| **Application.Tests** | Unit test cho Application. | Application |

`SqlServ4r` = project SQL Server hỗ trợ.

## Quy mô (2026-06)
- **~55 feature module** (folder cùng tên trải Domain/Application/Contract).
- **63 Controllers** trong `WebApi/Controllers/`.

## Anatomy của 1 feature (vd `DomainPriceEvals`)
```
Domain/DomainPriceEvals/        → DomainPriceEvalSession.cs, DomainPriceEvalItem.cs (entities)
Application/DomainPriceEvals/    → IDomainPriceEvalSessionService.cs, ...Service.cs, Dto/
Contract/DomainPriceEvals/       → request/response DTOs
WebApi/Controllers/              → DomainPriceEvalController.cs
```

## Thêm 1 feature mới — checklist
1. Entity → `Domain/<Feature>/`. Kế thừa `BaseEntity`.
2. DTO contract → `Contract/<Feature>/`.
3. `I<Feature>Service.cs` + `<Feature>Service.cs` → `Application/<Feature>/`, đăng ký DI ở `ApplicationModule.cs`.
4. Mapping → `AutoMapperProfile.cs`.
5. Controller → `WebApi/Controllers/<Feature>Controller.cs`.
6. **⚠️ Chọn DB:** table mới luôn vào `qcadmin` (xem [[DB Separation Rule]]). KHÔNG tạo operational table trong `seo_data`.
7. Migration nếu cần.

## Core building blocks đáng nhớ
- **Enums** (`Core/Enum/`): `ApprovalStatus`, `SeoPaymentTicketApprovalStep`, `NotificationType`, `RoleClaimEnum`, `EntityEnum`, `ScheduleStatus`... — luồng payment & notification phụ thuộc nặng các enum này.
- **Const** (`Core/Const/`): `ClaimNameTypes`, `ChatConst`, `HttpMessage`, `NotificationTemplateSymbol`.
- `WebApi/HostedServices/` — background jobs.
- `WebApi/Middlewares/`, `WebApi/Authorization/` — JWT + auth pipeline.
- `N8nWebhookController.cs` — điểm vào cho N8N gọi ngược API.

## Deploy
Commit PHẢI có prefix `[API]` → CI build. `test`→tag `test` (staging), `main`→tag `net9` (prod). Dùng `git merge --squash` (merge `--no-ff` KHÔNG có prefix → CI không chạy). Chi tiết: `agent.md` trong repo, [[CI/CD Pipeline]].
