---
status: completed
---

# Phase 01 — Progress/status synchronization

## Requirements

- If a task is changed to Completed, persist progress 100.
- If progress reaches 100, persist status Completed.
- Resolve conflicting input deterministically and keep the existing API shape.
- Keep the form controls synchronized before save where practical.

## Files

- services/HCS_web_free_license/services/work-management/HCS.WorkManagementService/Domain/WorkEntities.cs
- services/HCS_web_free_license/src/HCS.Blazor.Client/Components/ProjectTaskViewModal.razor
- services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/ProjectTaskDetail.razor
- services/HCS_web_free_license/services/work-management/HCS.WorkManagementService.Tests/*

## Success criteria

- Domain tests cover both directions and conflicting values.
- Work service and client compile.
