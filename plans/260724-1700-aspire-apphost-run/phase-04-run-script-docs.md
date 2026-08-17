# Phase 04 — run.sh + docs

## Context

- Parent: [plan.md](./plan.md)
- Depends: Phase 03
- Docker: `services/abp-blazor/etc/docker/up.ps1` + `containers/*.yml`
- Workspace skill: `.agents/skills/start-local/SKILL.md` / `.claude/skills/start-local/SKILL.md`

## Overview

| | |
|--|--|
| Priority | P2 |
| Status | Pending |
| Effort | ~30–45m |
| Goal | Một lệnh `./aspire/run.sh light|full` = ensure infra + AppHost |

## Requirements

**Script:** `services/abp-blazor/aspire/run.sh`

Behavior:

1. Arg1 = `light` (default) | `full`
2. Ensure docker network + containers:
   - **light:** `postgresql.yml`, `redis.yml`, `rabbitmq.yml` (compose `-f` từ `etc/docker`)
   - **full:** gọi `pwsh ./up.ps1` nếu có; fallback compose từng file như `up.ps1`
3. `dotnet run --project "$(dirname)/hanhchinhso.AppHost" -- --profile "$PROFILE"`
4. In ra reminder Keycloak: `services/directus-main` → `:5110` (không auto-start trừ flag optional `--with-keycloak` — **YAGNI**: chỉ print URL/hướng dẫn)
5. `chmod +x run.sh`

**Docs:**

- `services/abp-blazor/aspire/README.md` — how to run, profile table, port map, Keycloak note, pin-port warning
- Update ngắn `services/abp-blazor/README.md` (section Running) → link aspire README
- Update `start-local` skill: ABP = `./aspire/run.sh light` thay vì chỉ nhắc Studio

## Related files

**Create:**

- `aspire/run.sh`
- `aspire/README.md`

**Modify:**

- `services/abp-blazor/README.md` (short link)
- `.claude/skills/start-local/SKILL.md` và/hoặc `.agents/skills/start-local/SKILL.md` (đồng bộ nếu cả hai tồn tại)

## Implementation steps

1. Viết `run.sh` (bash, macOS-friendly): `set -euo pipefail`, resolve ROOT.
2. Function `ensure_infra_light` / `ensure_infra_full`.
3. Pass profile vào AppHost.
4. README tiếng Anh kỹ thuật OK; user-facing notes có thể bilingual ngắn.
5. Patch start-local skill 5–10 dòng.

## Todo

- [x] `run.sh` light/full + infra ensure
- [x] `aspire/README.md`
- [x] Link từ abp-blazor README
- [ ] Update start-local skill

## Success criteria

- [x] `./aspire/run.sh` (no args) = light + AppHost
- [x] `./aspire/run.sh full` = full infra path + `--profile full`
- [x] Skill/docs không còn bảo “chỉ Studio” là cách duy nhất

## Risks

| Risk | Mitigation |
|------|------------|
| `pwsh` missing on Mac | Fallback `docker compose -f` list; document |
| Containers already running | `up -d` idempotent |

## Security

- Không embed credentials trong script; dùng compose defaults lab hiện có.

## Next

→ [Phase 05 — Smoke](./phase-05-smoke-verify.md)
