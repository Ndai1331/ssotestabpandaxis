# Prototype CSS (ported from `docs/hcs-hanh-chinh-so`)

| File | Source | Role |
|------|--------|------|
| `../hcs-tokens.css` | `designsystem/tokens.css` + Blazor aliases | Design tokens |
| `base.css` | Buttons/forms/card from `css/app.css` | Shared primitives |
| `pages.css` | Extract of `css/app.css` (list/docs/process) | Shared page chrome |
| `chat.css` | Full chat module from `css/app.css` | Chat (`.chat-*`, `.msg`) |
| `social.css` | `css/social.css` | Social / bang-tin |
| `events.css` | `css/events.css` | Events / check-in / calendar extras |
| `host.css` | Blazor host adapters | Override leftover shell rules for chat/social |
| `messages.css` | ABP/Blazorise message + snackbar | Notification dialogs (16px, typed icons, pill buttons) |

Load order (see `App.razor`): tokens → main → components → base → pages → chat → social → events → bridge → host → messages.
