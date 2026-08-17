---
phase: 4
title: Project service
status: pending
effort: 2-3w
dependsOn: [3]
---

# Phase 04 — Project service

## Goal

`hanhchinhso.ProjectService` (:44381, DB `hanhchinhso_Project`) parity Projects / Tasks / Members / Assignments / TaskDocuments.

## Source (HCS)

`Projects`, `ProjectMembers`, `ProjectTasks`, `ProjectTaskAssignments`, `ProjectTaskDocuments`

## Steps

1. Scaffold từ LanguageService pattern + wire gateway/OpenIddict/run profile
2. Port entities + AppServices + permissions
3. Link document: store DocumentId refs; resolve via DocumentService client khi cần attach
4. MudBlazor pages (project board/list + task)
5. Parity checklist rows Projects = done

## Success criteria

- [ ] CRUD project/task/member qua UI
- [ ] Attach document ref hoạt động (không duplicate file binary)
- [ ] Gateway auth OK

## Risks

- TaskDocuments coupling DocumentService — contract rõ, tránh sync binary
- Có thể bắt đầu sau Phase 3a nếu chỉ cần DocumentId tồn tại — **default: after Phase 3 full** để ổn định ID scheme
