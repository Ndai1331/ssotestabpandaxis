# HCS Collaboration Service

ABP Community 10.6/.NET 10 bounded context for custom Chat/Chat1, notifications and push delivery.

## Runtime contracts

- Database: PostgreSQL `hcs_collaboration` (runtime: `ConnectionStrings__Default`; design-time migration: `HCS_COLLABORATION_CONNECTION_STRING`).
- HTTP: `/api/chat`, `/api/notifications`.
- SignalR: `/hubs/chat`; one connection carries chat and social notification events through separate per-user groups, while chat/presence methods remain Chat-permission protected.
- Attachments: private typed ABP Blob Storing container backed by MinIO. Downloads are membership-authorized streams.
- Messaging: RabbitMQ with durable local outbox and idempotent inbox. Cross-domain payloads contain IDs only.
- Push: Firebase HTTP v1 when runtime credentials are supplied. Failed/unconfigured push retains the in-app notification and retries with backoff.
- Social notifications: comments, replies and newly-added post/comment reactions are persisted with the interaction and delivered after commit through `NotificationReceived`; reconnect/polling recovers missed events.

No ABP Chat, commercial package/feed, tenant column, secret, or licensed migration is referenced. Create and apply fresh migrations from this service only:

```bash
HCS_COLLABORATION_CONNECTION_STRING='...' dotnet ef migrations add InitialCollaboration \
  --project HCS.CollaborationService/HCS.CollaborationService.csproj
```

MinIO bucket `hcs-collaboration` must be provisioned private. Supply runtime values with .NET environment keys such as `Minio__AccessKey`, `Minio__SecretKey`, `RabbitMQ__Connections__Default__Password`, `Firebase__ProjectId` and `Firebase__AccessToken`, or User Secrets; never tracked config.
