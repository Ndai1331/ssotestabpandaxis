---
phase: 6
title: Collaboration Chat Notify Push
status: pending
effort: 3-4w
dependsOn: [2]
---

# Phase 06 — Collaboration (Chat / Notifications / Push)

## Goal

`hanhchinhso.CollaborationService` (:44384, DB `hanhchinhso_Collaboration`): Chat (SignalR), Notifications, Push worker (Firebase).

## Source (HCS)

- `Chat` entities + ChatHub
- `Notifications`, `NotificationReceivers`
- `HC.PushNotificationWorker` (FirebaseAdmin)
- RabbitMQ event patterns nếu có

## Steps

1. Scaffold service + SignalR hub hosting (gateway WebSocket sticky / direct port — chốt lab: hub qua Gateway hoặc `:44384`)
2. Port chat + notification AppServices
3. Worker project hoặc hosted service Firebase (port secrets qua env)
4. Mud chat UI tối thiểu + notification bell
5. Mobile device token API (hoàn thiện stub Phase 3)
6. Parity checklist

## Success criteria

- [ ] 2 user chat realtime lab
- [ ] In-app notification create/receive
- [ ] Push smoke (hoặc documented skip nếu thiếu Firebase creds lab)

## Risks

- SignalR + YARP config phức tạp — test sớm
- Firebase credentials — không commit
