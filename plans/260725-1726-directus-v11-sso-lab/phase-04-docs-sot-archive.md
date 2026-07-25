---
phase: 4
title: "Docs SoT + archive v12"
status: completed
effort: 1.5h
dependsOn: [3]
---

# Phase 04 — Docs SoT + archive v12

## Context

- After smoke green, all agent/human docs must point to `services/directus-main-v11`
- Do **not** delete `services/directus-main` — mark archive in README/docs only
- Wiki already has draft note in `hot.md` / `index.md` from brainstorm — finalize paths

## Overview

Update runbook, architecture, handoff, CLAUDE/AGENTS, Directus README notes, wiki. Add short ARCHIVE note on v12 tree.

## Files to update (minimum)

| Path | Change |
|------|--------|
| `docs/runbooks/local-sso-lab.md` | `cd services/directus-main-v11`; compose commands; drop `BD_LAB_ALLOW_SSO` notes if any |
| `docs/workspace-architecture.md` | Path § Directus → v11; env example path |
| `docs/handoff/phase1-sso-context.md` | Compose/extension paths → v11 |
| `docs/codebase-summary.md` | Table row Directus path |
| `CLAUDE.md` / `AGENTS.md` | Navigation table: lab Directus = v11 |
| `wiki/hot.md` | Confirm SoT switch **done** (not pending) |
| `wiki/index.md` | Codebase — Directus line final |
| `wiki/log.md` | Append cook complete entry |
| `services/directus-main/README.md` or short `ARCHIVE.md` | “Archive — lab SoT is directus-main-v11” |
| `services/directus-main-v11/readme.md` or lab note | Point to `docker-compose.bd-lab.yml` + runbook |

Optional if still wrong: `docs/runbooks/deploy-abp-production.md` Keycloak path footnote; `docs/system-architecture.md`.

## Implementation steps

1. Global search for lab paths: `directus-main/docker-compose.bd-lab`, `BD_LAB_ALLOW_SSO`, `.env.sso.example` under docs/wiki/CLAUDE — update to v11 where lab-relevant.
2. Rewrite runbook § Directus to v11-only commands.
3. Add archive banner on v12 (README/ARCHIVE.md): keep for reference/MSCL comparison; do not run for SSO lab.
4. Update wiki hot/index/log status → switch complete + plan dir link.
5. Journal cook completion (or leave for `/ck:journal` after cook).

## Todo

- [x] Runbook + architecture + handoff
- [x] CLAUDE/AGENTS navigation
- [x] Wiki finalize
- [x] v12 archive banner
- [x] v11 lab quickstart pointer

## Success criteria

- [x] New agent reading CLAUDE + runbook starts Directus from **v11** only
- [x] No lab instruction still saying “must set BD_LAB_ALLOW_SSO”
- [x] Plan marked completed after this phase

## Risks

| Risk | Mitigation |
|------|------------|
| Stale absolute paths in old journals | Leave historical journals; only evergreen + hot/handoff |
| Someone still `up` v12 | Archive banner + runbook warn port conflict |

## Next

Mark plan `status: completed`. Optional: `/ck:cook` already done — user may archive plan later via `/ck:plan archive`.
