# Phase 04 — Gateway (optional) + solution wiring (abpsln + run profile)

**Goal:** Đăng ký WorkflowService vào solution để ABP Studio nhận diện + chạy đúng thứ tự; (optional) route qua WebGateway.

**Depends on:** phase-01.
**Owns files:**
- `services/abp-blazor/hanhchinhso.abpsln`
- `services/abp-blazor/etc/abp-studio/run-profiles/Default.abprun.json`
- `services/abp-blazor/gateways/web/hanhchinhso.WebGateway/appsettings.json` (optional)

## A. abpsln — đăng ký module
File: `hanhchinhso.abpsln` → `modules`:
- [ ] Thêm:
```json
"hanhchinhso.WorkflowService": {
  "path": "services/workflow-service/hanhchinhso.WorkflowService.abpmdl",
  "folder": "services"
}
```
- [ ] (Optional, deploy sau này) thêm helm chart entry `workflow` giống `language` — **BỎ QUA** cho lab (YAGNI).

## B. Run profile — thứ tự chạy
File: `etc/abp-studio/run-profiles/Default.abprun.json` → `applications`:
- [ ] Thêm:
```json
"hanhchinhso.WorkflowService": {
  "type": "dotnet-project",
  "path": "../../../services/workflow-service/hanhchinhso.WorkflowService/hanhchinhso.WorkflowService.csproj",
  "launchUrl": "http://localhost:44395",
  "folder": "services",
  "kubernetesService": ".*-workflow$",
  "healthCheckEndpoint": "/health-status",
  "healthUiEndpoint": "/health-ui",
  "execution": { "order": 4 }
}
```
> `services` folder execution order = 2 (chạy sau apps=0, gateways=1). `order:4` đặt sau LanguageService(3). Studio app (phase-05) đăng ký riêng nếu muốn Studio chạy chung profile.

## C. (Optional) YARP route qua WebGateway :44398
File: `gateways/web/hanhchinhso.WebGateway/appsettings.json`
- [ ] `ReverseProxy.Routes` thêm:
```json
"Workflow": {
  "ClusterId": "Workflow",
  "Match": { "Path": "/elsa/{**catch-all}" }
},
"WorkflowSwagger": {
  "ClusterId": "Workflow",
  "Match": { "Path": "/swagger-json/Workflow/swagger/v1/swagger.json" },
  "Transforms": [ { "PathRemovePrefix": "/swagger-json/Workflow" } ]
}
```
- [ ] `ReverseProxy.Clusters` thêm:
```json
"Workflow": {
  "Destinations": { "Workflow": { "Address": "http://localhost:44395/" } }
}
```
- [ ] Nếu route qua gateway: Studio `RemoteUrl` trỏ `http://localhost:44398/elsa` thay vì `:44395`. **Quyết định ở unresolved Q3.** Mặc định lab: Studio gọi thẳng `:44395`, KHÔNG cần route này.

## Verify
- [ ] Mở ABP Studio → thấy module `hanhchinhso.WorkflowService` trong nhóm `services`.
- [ ] Run profile Default khởi động WorkflowService cùng cả solution, đúng thứ tự (không lỗi port).
- [ ] (Nếu bật route) `GET http://localhost:44398/elsa/api/...` proxy tới `:44395`.

## Rollback
- Xóa block WorkflowService khỏi `hanhchinhso.abpsln`, `Default.abprun.json`, và (nếu thêm) route/cluster `Workflow` trong WebGateway appsettings.
