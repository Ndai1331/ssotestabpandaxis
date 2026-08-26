---
title: "HCS motion, loading and micro-interaction system"
description: "Chuẩn hóa motion/transitions/loading cho HCS Blazor mà không đổi visual system, kiến trúc hoặc hành vi nghiệp vụ."
status: pending
priority: P2
effort: 2-4d
branch: main
tags: [hcs, blazor, bootstrap, blazorise, leptonx, motion, accessibility]
blockedBy: [260826-hcs-free-license-workspace-document-parity]
blocks: []
relatedPlans: [260810-0900-hcs-community-feature-parity, 260814-1000-hcs-blazorise-localization, 260814-1721-hcs-header-chat]
created: 2026-08-26
createdBy: Codex
---

# HCS motion, loading and micro-interaction system

## Outcome

Tạo một lớp motion nhất quán, nhẹ và có thể tắt cho HCS Community free-license runtime tại `services/HCS_web_free_license`. Phạm vi chỉ gồm modal, sidebar/drawer, dropdown, tabs, DataGrid loading, skeleton, toast, button, form validation và dashboard entrance.

Giữ nguyên:

- Visual design system hiện có: token màu, typography, spacing, border-radius, breakpoint, z-index và layout.
- Kiến trúc Blazor + Bootstrap + LeptonX + Blazorise; các package/version hiện tại.
- Business logic, API/BFF contract, polling/auto-dismiss timing hiện có, routing và authentication.
- Semantics/keyboard behavior của modal, dropdown, tabs, focus trap, Escape, click-outside và mobile drawer.

Plan này chỉ tạo kế hoạch; không sửa source code trong giai đoạn lập kế hoạch.

## Findings

- `wwwroot/hcs-tokens.css` đã có `--hcs-motion-fast: 150ms`, `--hcs-motion-normal: 220ms`, `--hcs-motion-ease` và reduced-motion variables. Đây là nền tảng cần tái sử dụng, không tạo motion scale mới nếu chưa cần.
- `wwwroot/hcs-components.css` đã là shared layer cho button, focus, modal, table và reduced-motion, nhưng thiếu primitive thống nhất cho skeleton, DataGrid loading, validation message, toast entry và dashboard entrance.
- `wwwroot/main.css` còn duration hard-code cho boot screen, catalog button và workspace icon action; một số hover/entrance surface chưa dùng motion tokens.
- `Layouts/HCSMainLayout.razor.css` đã có transition cho header controls, nav submenu, user menu và mobile drawer. Cần chuẩn hóa edge cases (`visibility`, `pointer-events`, transform và reduced-motion), không đổi state/event logic trong `.razor`.
- `Components/NotificationToast.razor.css` đã có tokenized list transition nhưng toast/panel xuất hiện đột ngột. Component hiện tự quản lý polling và auto-dismiss; các timing này không thuộc phạm vi.
- Skeleton animation đang bị lặp ở `GatewayDataPanel.razor.css`, `Pages/AccountManagement.razor.css` và `Components/UserSignaturesPanel.razor.css`, với duration/keyframe riêng và reduced-motion chưa hoàn toàn tập trung.
- Các DataGrid đã có `LoadingTemplate`/`hcs-loading` ở các trang `Administration`, `DocumentManagement`, `DocumentSigning`, `Notifications`, `OrganizationCatalog`, `Projects`, `SurveyCriterias`, `SurveyLocations`, `SurveySessions`, `WorkflowDefinitions`, `WorkflowInstances` và `Workflows`; có thể chuẩn hóa từ shared CSS, không thay `ReadData` hoặc data state.
- Các modal/tabs liên quan đã hiện diện trong `Components/Documents/DocumentPdfPreviewModal.razor`, `WorkflowInfoModal.razor`, `ProjectTaskCreateModal.razor`, `ProjectTaskViewModal.razor`, `SubmitWorkflowModal.razor` và các page modal. Markup/functionality của workspace/document đang thuộc plan parity bị chặn trước.
- `prefers-reduced-motion` hiện có ở token/global/layout nhưng một số transition/keyframe hard-code vẫn cần được kiểm tra theo thứ tự bundle và CSS isolation.

## Scope guardrails

### In scope

- Dùng lại motion tokens hiện có; chỉ bổ sung semantic alias tối thiểu nếu một primitive không thể diễn đạt bằng `fast`/`normal`.
- Transition/animation chỉ trên opacity, transform, color, border-color, box-shadow hoặc indicator; không animate layout dimensions gây reflow.
- Loading/skeleton/validation presentation, entry/exit affordance và hover/focus/active feedback.
- Một vài class/attribute trình bày tối thiểu nếu CSS không thể phân biệt state; không thêm state nghiệp vụ, API call, timer hay route.
- Explicit reduced-motion override cho cả CSS isolation và shared styles.

### Out of scope

- Không sửa `services/HCS_web_with_license` hoặc vendor-generated LeptonX/Bootstrap/Blazorise CSS.
- Không nâng package, thêm animation library, thêm JS animation engine hoặc đổi framework architecture.
- Không đổi màu, font, spacing, radius, breakpoint, component hierarchy, page layout hay responsive behavior.
- Không đổi API/BFF/client contract, polling, auto-dismiss duration, auth, routing, authorization, validation rules hoặc DataGrid query/paging.
- Không tạo skeleton data model mới; chỉ trình bày các loading state đã có.

## Proposed file scope

### Primary files to modify during implementation

- `services/HCS_web_free_license/src/HCS.Blazor.Client/wwwroot/hcs-components.css` — shared primitives cho buttons, loading/DataGrid, skeleton, modal, tabs, validation feedback, toast và reduced-motion.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/wwwroot/main.css` — thay duration hard-code ở boot/catalog/workspace bằng tokens; thêm entrance rules cho existing dashboard/feature classes mà không đổi layout.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor.css` — polish header, sidebar/drawer, nav submenu và user dropdown; giữ nguyên state class/event trong `.razor`.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/NotificationToast.razor.css` — toast/panel entry và interaction states; không thay polling hoặc dismiss behavior.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/GatewayDataPanel.razor.css` — chuyển skeleton về shared tokens/primitive, tránh duplicate keyframes.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/AccountManagement.razor.css` — chuẩn hóa custom tabs, skeleton và validation feedback hiện có.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Components/UserSignaturesPanel.razor.css` — chuẩn hóa skeleton/transition và reduced-motion behavior.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/Index.razor.css` — dashboard/landing feature entrance bằng class hiện có.

### Inspect first; modify only if a CSS hook is genuinely missing

- `services/HCS_web_free_license/src/HCS.Blazor.Client/wwwroot/hcs-tokens.css` — ưu tiên giữ nguyên; chỉ thêm alias motion semantic nếu cần, không đổi palette hoặc timing contract.
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor` và `Components/NotificationToast.razor` — chỉ thêm presentation class/ARIA state nếu CSS-only entry state không đủ; không đổi event, timer, focus hoặc data flow.
- Modal components: `Components/Documents/DocumentPdfPreviewModal.razor`, `Components/Documents/WorkflowInfoModal.razor`, `Components/ProjectTaskCreateModal.razor`, `Components/ProjectTaskViewModal.razor`, `Components/SubmitWorkflowModal.razor` — ưu tiên selector/class hiện có; nếu parity plan đổi markup thì motion layer chạy sau đó.
- `Pages/Workspace.razor`, `Pages/Administration.razor`, `Pages/AdministrationRoles.razor`, `Pages/Organization/OrganizationCatalog.razor`, `Pages/Projects.razor` và các page DataGrid — không sửa data/query logic; chỉ thêm hook trình bày khi shared selectors không đủ.
- `Pages/Administration.razor.css`, `Pages/AdministrationRoles.razor.css`, `Pages/SurveyCollections.razor.css`, `Pages/SurveyResults.razor.css` — chỉ touch nếu local specificity/CSS isolation chặn primitive tabs, dropdown hoặc validation.

### Explicitly read-only consumers for the first pass

Các `LoadingTemplate` DataGrid và `hcs-loading` hiện có phải được dùng lại trước khi cân nhắc sửa markup. Không nhân bản motion rule trong từng page. Không chỉnh generated `HCS.Blazor.Client.styles.css` hoặc vendor bundle.

## Implementation phases

### Phase 1 — Establish shared motion contract

1. Xác nhận thứ tự load: generated CSS, `hcs-tokens.css`, `main.css`, `hcs-components.css`; đặt shared override ở layer đang thắng CSS isolation/vendor mà không dùng `!important` tràn lan.
2. Chuẩn hóa các primitive theo token: `hcs-loading`, spinner indicator, skeleton/pulse, button hover/focus/active/loading/disabled, validation feedback, modal/dropdown/tab state và toast entry.
3. Đưa hard-coded durations ở `main.css`, `GatewayDataPanel.razor.css`, `AccountManagement.razor.css`, `UserSignaturesPanel.razor.css` về token; không thay giá trị visual.
4. Tạo một reduced-motion contract duy nhất: duration về `0ms`, animation tắt, transform/entrance không tạo chuyển động, `scroll-behavior: auto`; giữ focus ring và trạng thái disabled/loading.

### Phase 2 — Shell and component surfaces

1. Sidebar/drawer: giữ nguyên `.open`/`hcs-app-shell--nav-open`, backdrop, inert/ARIA và focus return; chỉ làm mượt transform/opacity/visibility và bảo đảm click không lọt khi đang đóng.
2. Dropdown: áp dụng cùng easing/duration cho nav submenu, user menu và các dropdown/action menu hiện có; keyboard, hover/pointer và click-outside không đổi.
3. Modal: áp dụng backdrop/content entrance nhẹ cho Blazorise modal và các HCS modal; không delay focus trap, Escape, close hoặc render body.
4. Tabs: indicator/active state và content entry nhẹ cho custom tabs và Blazorise Tabs; không giữ panel tương tác ở trạng thái ẩn sai hoặc làm thay đổi selection.
5. Toast: entry cho toast và notification panel; nếu không có lifecycle class cho exit thì chỉ làm entry, không trì hoãn removal để “đợi animation”.

### Phase 3 — Loading, forms and dashboard

1. Chuẩn hóa DataGrid `LoadingTemplate` hiện có qua `.hcs-loading`; giữ chiều cao hiện tại và phân biệt loading/empty/error.
2. Chuẩn hóa skeleton ở gateway/account/signature và các loading surface khác; animation chỉ là presentation, không thay state/timer.
3. Áp dụng button micro-interactions cho các action hiện có; disabled/loading không hover-lift, không cho cảm giác double-submit, không thay `Loading`/`disabled` semantics.
4. Áp dụng validation feedback cho `.invalid-feedback`, `.validation-message`, `aria-invalid` và focus state hiện có; không đổi rule, message, submit flow hoặc layout jump đáng kể.
5. Thêm staged entrance nhẹ cho `.hcs-workspace`, KPI/cards/quick actions và `.hcs-feature` bằng selector/class hiện có; không animate khi đang loading và không thay data fetch.

### Phase 4 — Verification and scope audit

1. Chạy license audit, build và test của service; kiểm tra không có dependency/vendor diff.
2. Kiểm tra thủ công ở viewport desktop, breakpoint drawer, tablet và mobile; thử mouse, keyboard-only, Escape, click-outside, focus return và screen-reader-visible labels.
3. Bật OS `prefers-reduced-motion: reduce`; xác nhận không còn spinner/skeleton/entrance/transform chuyển động, nhưng state, focus và loading feedback vẫn rõ.
4. Soát `git diff --name-only`: chỉ CSS và presentation hooks đã được phê duyệt; không có thay đổi contracts, clients, routing, auth, API, timers hoặc business logic.

## Acceptance criteria

- [ ] Modal open/close, backdrop và body loading có transition tokenized, hoàn tất trong motion budget hiện có (`fast`/`normal`), không làm thay đổi focus trap, Escape, close hoặc scroll behavior.
- [ ] Sidebar/drawer mobile, nav submenu và user/action dropdown có state transition nhất quán; `visibility`/`pointer-events` không cho tương tác khi đóng; desktop/mobile breakpoint và keyboard behavior giữ nguyên.
- [ ] Tabs custom và Blazorise có active/indicator/content feedback nhất quán, không đổi tab selection, panel content, URL hoặc data load.
- [ ] Tất cả DataGrid nêu trong scope hiển thị loading theo cùng primitive; `LoadingTemplate`, empty state, error state, `ReadData`, paging và query không đổi.
- [ ] Gateway/account/signature skeleton dùng chung duration/easing hoặc token tương đương; không layout-shift bất ngờ và trở thành static khi reduced motion bật.
- [ ] Toast/panel có entry affordance; polling, auto-dismiss, close, mark-all, view-all và notification semantics không đổi.
- [ ] Buttons có hover/focus/active/loading/disabled feedback nhẹ, focus-visible không bị che, disabled không bị lift/hover; không đổi submit behavior.
- [ ] Form validation feedback có thể nhận biết khi xuất hiện/invalid/focus, không đổi validation rules/messages/ARIA và không gây layout jump đáng kể.
- [ ] Workspace/landing dashboard entrance không chạy khi reduced motion bật hoặc khi đang ở loading state; không đổi thứ tự, kích thước, spacing hoặc API/data behavior.
- [ ] Một reduced-motion audit cho thấy mọi motion mới và mọi hard-coded motion trong scope đều bị vô hiệu hóa hoặc đưa về instant; vendor CSS không bị chỉnh.
- [ ] Visual regression thủ công ở 1440px, 1100px, 768px và 375px xác nhận palette, typography, spacing, radius, breakpoint, z-index và layout không đổi.
- [ ] `./scripts/audit-license-clean.sh`, `dotnet build HCS.slnx --no-restore` và `dotnet test HCS.slnx --no-build` đạt (hoặc failure được ghi rõ nếu do baseline/user changes ngoài scope).

## Risks and mitigations

- CSS isolation có thể thắng shared selector: kiểm tra generated bundle/order trước, tăng specificity cục bộ tối thiểu và chỉ dùng `::deep` khi cần.
- Blazor render/remove DOM quá nhanh cho exit animation: ưu tiên entry/active transition; không thêm delay/timer để phục vụ animation.
- FontAwesome `fa-spin` có thể bypass token: thêm reduced-motion override ở layer HCS sau vendor, không thay package icon.
- Plan parity đang sở hữu nhiều modal/workspace markup: thực hiện motion sau `260826-hcs-free-license-workspace-document-parity`, hoặc rebase selector sau khi markup ổn định.
- Global selector có thể tác động trang ngoài scope: giới hạn dưới `.hcs-app-shell`, `.hcs-feature`, `.hcs-loading`, class HCS cụ thể; tránh selector Bootstrap quá rộng.

## Handoff

Implementation phase có thể chạy sau khi dependency parity hoàn tất với:

`/ck:cook --auto /Users/nguyenlong/Documents/Projects/bd-workspace/plans/260826-hcs-motion-system/plan.md`

Mọi thay đổi source phải giữ đúng file scope, chạy verification ở Phase 4 và báo unresolved questions nếu CSS isolation hoặc markup parity buộc phải mở rộng phạm vi.
