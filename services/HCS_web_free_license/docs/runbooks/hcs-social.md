# HCS internal social smoke runbook

This runbook covers the minimal social network slice in the free-license HCS runtime.

## Prerequisites

- Start the local Compose stack and open `https://hcs.localhost`.
- Sign in with an account that has `Collaboration.Social`; sign out/in after changing permissions so the BFF session receives the new claim.
- Collaboration, PostgreSQL and MinIO must be healthy. The Collaboration service applies its EF migration on startup.

## Browser acceptance checklist

1. Open `/social` and confirm the Mạng xã hội menu item is shown only with the social permission.
2. Create a text-only `Công khai` post and confirm it appears in the feed.
3. Publish a post containing an `https://` link. Confirm the URL remains visible and the preview card shows the site, title/description and image when the remote page exposes metadata; a metadata failure must not block posting.
4. Upload one image and one video, publish a post, refresh, and confirm both media render through `/api/social/media/{id}`.
5. Search by keyword, from/to date and hashtag. Confirm the result count/list updates and the filters remain in the URL when switching between `Công khai` and `Nội bộ`.
6. Create a top-level comment and a reply. Refresh and confirm the reply remains indented and attached to the parent id.
7. Use each reaction on a post and a comment. Confirm one reaction per user, clicking the selected reaction removes it, choosing another replaces it, and counts/current-user state update without a full page refresh.
8. Share a public post and an internal post. Confirm the share count increments once per user, native share or clipboard feedback appears, and the returned permalink opens/highlights the exact post (internal uses the author's profile scope).
9. Create an `Nội bộ` post. Confirm it is absent from `/social` after switching to another user, while it remains visible at `/social/profile` for its author.
10. On `/social`, click `Công khai` and `Nội bộ`; confirm the active state and list reload, with `Nội bộ` opening `/social/profile?visibility=internal`.
11. On `/social/profile`, click `Tất cả`, `Công khai` and `Nội bộ`; confirm each selection reloads the matching list and preserves the filter in the URL.
12. Verify newest posts appear first, load-more works, and text/media-only validation prevents an empty post.
13. Try an unsupported file type or a file over 25 MB and confirm the UI shows a recoverable error without submitting a post.
14. Repeat composer, media, comments, reactions and share actions at mobile width and with keyboard focus. No horizontal scroll or icon-only unlabeled action should be present.
15. Sign in as a second user in another browser/tab. Add a comment, reply, post reaction and comment reaction from that user; confirm the post owner/comment owner sees a realtime badge and toast without refreshing.
16. Click a social notification toast or notification-panel item. Confirm it marks the notification read and opens/highlights the exact public post; a lost SignalR connection must recover the unread count through fallback sync.

## Targeted verification commands

Run from `services/HCS_web_free_license`:

```bash
dotnet test services/collaboration/HCS.CollaborationService.slnx --no-build
dotnet build services/collaboration/HCS.CollaborationService.slnx --no-restore
dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore
```

If the permission was newly added to a role, sign out/in before testing the route. Do not test `Internal` privacy only through UI filtering; verify the API returns `403`/no data for another authenticated user.
