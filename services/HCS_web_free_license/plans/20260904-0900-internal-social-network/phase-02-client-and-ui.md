# Phase 02 — BFF client and Blazor UI

## Context links

- `gateways/web/HCS.WebGateway/appsettings.json`
- `src/HCS.Blazor.Client/Collaboration/CollaborationClient.cs`
- `src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs`
- `src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor`
- `src/HCS.Blazor.Client/Pages/AccountManagement.razor`
- `src/HCS.Blazor.Client/wwwroot/hcs-tokens.css`

## Overview

- Priority: P0
- Status: Complete
- Add two deep-linkable pages: `/social` for the public feed and `/social/profile` for the current user's posts.

## UI requirements

- Composer has visible body label, Public/Internal select, media picker, upload progress and disabled submit state.
- Scope tabs reload the current view when switching `Public`/`Internal`; the feed keeps public posts, while the personal route supports `All`, `Public` and `Internal` query filters.
- Feed cards show author snapshot, relative/absolute time, visibility badge, body, image/video media, comment count/list and reply action.
- Comments render in creation order with a clear reply indentation; reply composer is scoped to a comment and can be cancelled.
- Profile page reuses the feed card and shows both visibility values with an explicit empty state.
- Link posts render a URL plus a best-effort Open Graph/Twitter preview card; the URL remains available when remote metadata is unavailable.
- Search controls cover keyword, date range and hashtag and are encoded in the route so visibility changes do not lose the active filter.
- Post/comment reactions expose labeled Like/Love/Haha/Wow/Sad/Angry actions; share uses Web Share or copy-link fallback and returns a deep permalink.
- Loading, retry, permission/error, upload failure and empty states are accessible and do not rely on color alone.
- Mobile layout is single-column, touch targets are at least 44px, media reserves aspect-ratio space, and no horizontal scroll is introduced.

## Files

- Add `SocialClient.cs`, shared social DTOs, and a focused `SocialWorkspace.razor` page with `/social` and `/social/profile` routes plus scoped CSS.
- Add a Collaboration route to `HCS.WebGateway/appsettings.json`.
- Add a `Social` menu item and optional header shortcut protected by `Collaboration.Social`.
- Add Vietnamese and English localization keys for menu, composer, visibility, comments, replies, errors and empty states.
- Keep the existing Chat route and current uncommitted UI changes intact.

## Data flow

1. Page loads feed/profile through `SocialClient`.
2. Browser uploads selected image/video to `/api/social/uploads` through `HCS.Bff`.
3. Client sends returned media ids with post creation.
4. Post and comments refresh through the BFF after mutation; no realtime channel is added in this phase.
5. Route and query changes are observed explicitly so switching between `/social`, `/social/profile` and visibility filters reloads the correct API query.

## Success criteria

- Both URLs work after sign-in and direct navigation.
- Public feed never displays internal posts from another user even if the page is manipulated.
- Profile shows the current user's public and internal posts.
- Clicking a visibility tab reloads the matching post list without requiring a browser refresh.
- Search, reaction and share actions update visible state without requiring a browser refresh; shared links reopen the exact post.
- Comment and reply actions update the visible thread without duplicate submissions.
