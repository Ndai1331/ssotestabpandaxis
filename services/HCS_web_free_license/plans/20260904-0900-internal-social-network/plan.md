# Internal social network MVP

## Objective

Add a minimal HCS internal social area with a newest-first public feed, text/image/video posts, comments and replies, plus a personal page showing the current user's public and internal posts.

## Product decisions

- `Public` means visible to every authenticated HCS user and appears in `/social`.
- `Internal` is private to the author in this MVP; it appears only in `/social/profile`.
- No likes, follows, shares, external audience, moderation workflow, or notifications in this phase.
- Collaboration owns the social aggregate and MinIO media; the browser accesses it only through the BFF.

## Phases

1. [x] [Phase 01](phase-01-domain-and-api.md) — contracts, entities, persistence, API and secure media.
2. [x] [Phase 02](phase-02-client-and-ui.md) — BFF route/client, navigation, feed and personal profile UI.
3. [x] [Phase 03](phase-03-tests-and-verification.md) — contract/domain tests, build, smoke checklist and docs.

## Dependencies

- Existing authenticated BFF session and the dedicated `Collaboration.Social` permission are reused as the collaboration boundary.
- Existing Collaboration MinIO configuration is reused with a separate `hcs-social` blob container.
- EF Core migration must be generated only for Collaboration; unrelated dirty worktree changes remain untouched.

## Definition of done

- An authenticated user can create a text-only or media post with either visibility.
- The public page lists only public posts in descending creation order.
- The personal page lists both visibility values for the current user.
- A visible post can display comments and accept a top-level comment or one-level/nested reply.
- Media downloads are authorized by the post visibility/ownership rule.
- API/domain tests and the Collaboration solution build pass; docs include a local smoke path.
