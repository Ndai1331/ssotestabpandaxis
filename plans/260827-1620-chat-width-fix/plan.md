# Chat page width fix

## Root cause

In the current working tree, `HCSMainLayout.razor.css` gives
`.hcs-main-content` the shared bounded width
(`max-width: var(--hcs-content-max-width)`) and centers it. The chat page is a
full-height workspace, so at wide viewports that parent stops growing while
the page and grid correctly use `width: 100%` of the smaller parent. Existing
chat overrides in global styles are duplicated and depend on cascade order.
The general `margin: 0 auto` change is pre-existing dirty work and is left
untouched; this fix is scoped to the chat route.

## Implementation

1. Add a layout-scoped `.hcs-main-content:has(.hcs-chat-page)` override with
   `margin: 0`, `max-width: none`, and `width: 100%` so the chat route owns the
   full available content width.
2. Keep the existing page gutter and full-height flex chain unchanged.
3. Preserve the current `<= 860px` single-panel behavior and the existing
   sidebar/thread/info grid breakpoints.

## Verification

- Run `dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore`.
- Run `git diff --check`.
- Confirm the resulting CSS contains the chat-specific width reset and that
  no unrelated dirty files are modified.

## Verification result

- Debug and Release builds of `src/HCS.Blazor/HCS.Blazor.csproj` passed with
  0 warnings and 0 errors.
- The generated scoped bundle contains the chat rule with `margin: 0`,
  `max-width: none`, and `width: 100%`.
- The chat-filtered test run exited successfully; Collaboration tests passed.
- `git diff --check` passed.
- No architecture or API documentation update is needed for this CSS-only fix.
- No commit was created; the worktree contains unrelated user changes.
