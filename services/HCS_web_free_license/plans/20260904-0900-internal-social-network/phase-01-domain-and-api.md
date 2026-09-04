# Phase 01 — Social domain and API

## Context links

- `services/collaboration/HCS.CollaborationService.Contracts/CollaborationContracts.cs`
- `services/collaboration/HCS.CollaborationService/Domain/CollaborationEntities.cs`
- `services/collaboration/HCS.CollaborationService/Data/CollaborationDbContext.cs`
- `services/collaboration/HCS.CollaborationService/Application/CollaborationAppService.cs`
- `services/collaboration/HCS.CollaborationService/Storage/CollaborationAttachmentStore.cs`

## Overview

- Priority: P0
- Status: Complete
- Add a small social aggregate without coupling posts to chat conversations.

## Requirements

- Post body is optional only when at least one image/video is attached; body max 4,000 chars.
- Visibility is `Public` or `Internal`; public feed is newest-first with stable `(CreationTime, Id)` ordering.
- A post can have up to 10 media items, each limited to the existing 25 MB policy and image/video MIME allow-list.
- Comments are text-only, max 2,000 chars, and retain `ParentCommentId` for replies.
- Visible posts allow comment reads/writes; internal posts are only visible to the author.
- Author display name is stored as a safe snapshot from the authenticated claims at post creation; media URLs remain gateway-relative.

## API shape

- `GET /api/social/feed?skip=0&take=20`
- `GET /api/social/profile/posts?skip=0&take=20`
- `POST /api/social/posts` with `{ text, visibility, mediaIds }`
- `GET /api/social/posts/{postId}/comments`
- `POST /api/social/posts/{postId}/comments` with `{ text, parentCommentId }`
- `POST /api/social/uploads` multipart `file`
- `GET /api/social/media/{mediaId}` authorized stream download

All routes use the dedicated `Collaboration.Social` permission policy. Unsafe browser calls are CSRF-protected once by the BFF.

## Architecture and files

- Add `SocialPost`, `SocialPostMedia`, and `SocialPostComment` in focused domain files; use soft delete only if needed by existing audited base, otherwise omit deletion from this MVP.
- Add immutable DTOs and inputs in a focused social contracts file, plus `CollaborationPermissions.Social`.
- Add `SocialPostAppService` and `SocialCommentAppService`; keep each under the repository's 200-line guideline where practical.
- Add `SocialController` and `SocialMediaStore` with a dedicated `hcs-social` MinIO container.
- Add DbSets, indexes, foreign keys and a generated `AddSocialNetwork` migration/snapshot.

## Security considerations

- Never accept author/user id from the client; use `ICurrentUser`.
- Validate visibility, body/media shape, MIME, file name and size server-side.
- Reject media ids not uploaded by the caller, already attached, or exceeding the post limit.
- Query visibility in the service before returning post, comment, or media data; do not rely on UI filtering.
- Use `AsNoTracking` for feed/profile/comment reads and stable paging.

## Success criteria

- Migration applies from a clean Collaboration database.
- Unauthorized, internal-other-user, invalid-parent, invalid-media and empty-post cases are rejected.
- Public/profile endpoints return deterministic descending post order.
