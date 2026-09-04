# HCS internal social smoke runbook

This runbook covers the minimal social network slice in the free-license HCS runtime.

## Prerequisites

- Start the local Compose stack and open `https://hcs.localhost`.
- Sign in with an account that has `Collaboration.Social`; sign out/in after changing permissions so the BFF session receives the new claim.
- Collaboration, PostgreSQL and MinIO must be healthy. The Collaboration service applies its EF migration on startup.

## Browser acceptance checklist

1. Open `/social` and confirm the Mạng xã hội menu item is shown only with the social permission.
2. Create a text-only `Công khai` post and confirm it appears in the feed.
3. Upload one image and one video, publish a post, refresh, and confirm both media render through `/api/social/media/{id}`.
4. Create a top-level comment and a reply. Refresh and confirm the reply remains indented and attached to the parent id.
5. Create an `Nội bộ` post. Confirm it is absent from `/social` after switching to another user, while it remains visible at `/social/profile` for its author.
6. Verify newest posts appear first, load-more works, and text/media-only validation prevents an empty post.
7. Try an unsupported file type or a file over 25 MB and confirm the UI shows a recoverable error without submitting a post.
8. Repeat composer, media, comments and reply actions at mobile width and with keyboard focus. No horizontal scroll or icon-only unlabeled action should be present.

## Targeted verification commands

Run from `services/HCS_web_free_license`:

```bash
dotnet test services/collaboration/HCS.CollaborationService.slnx --no-build
dotnet build services/collaboration/HCS.CollaborationService.slnx --no-restore
dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore
```

If the permission was newly added to a role, sign out/in before testing the route. Do not test `Internal` privacy only through UI filtering; verify the API returns `403`/no data for another authenticated user.
