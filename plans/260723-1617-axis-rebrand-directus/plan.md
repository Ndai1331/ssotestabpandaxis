---
title: Axis rebrand — table prefix + display text
status: completed
blockedBy: []
blocks: []
created: 2026-07-23
---

# Plan: Rebrand Directus → Axis (`services/directus-main`)

## Goal

White-label fork trong lab:

1. **DB / collection prefix:** `directus_*` → `axis_*`
2. **Text hiển thị:** `Directus` → `Axis`, và chuỗi có tiền tố `directus_` → `axis_`

## Scope estimate

| Metric | Khoảng |
|--------|--------|
| Files chứa `directus_` | ~580 |
| Occurrences `directus_` | ~5.000 |
| Word `Directus` (TS/Vue/JS) | ~275 |
| Magic `substring(9)` (`"directus_".length`) | 6 files / ~10 chỗ |

## IN SCOPE (làm)

| Layer | Việc |
|-------|------|
| `packages/system-data` | YAML collections/fields/relations/permissions + `isSystemCollection` prefix |
| `api/src/database/seeds` | Tất cả `table: directus_*` → `axis_*` |
| `api/src/database/migrations` | Mọi `directus_*` table/collection refs |
| `api` / `app` / `sdk` / tests | Hardcoded collection names, `startsWith('directus_')` |
| Magic length | `substring(9)` → `substring(5)` **hoặc** helper `stripSystemPrefix()` + constant `SYSTEM_COLLECTION_PREFIX = 'axis_'` |
| UI / i18n | `app/src/lang/**`, titles, alt text, welcome strings: Directus → Axis |
| BD lab | `bd-lab-extensions/**` user-facing messages |

## OUT OF SCOPE (giữ nguyên — trừ khi user bảo đổi)

| Mục | Lý do |
|-----|--------|
| npm scope `@directus/*` | Đổi = rename cả monorepo packages + imports |
| Env `DIRECTUS_*` | Runtime env convention upstream |
| Docker path `/directus/...`, volume `bd_directus_*` | Infra path, không phải bảng/UI |
| URL `directus.com` | Link docs/license upstream |
| Binary/CLI name `directus` | Package entry |
| TS types `DirectusUser`, `DirectusError`… | Public SDK API; đổi = breaking riêng |

## Approach

1. Thêm constant prefix trong `packages/system-data` (SoT):
   - `SYSTEM_COLLECTION_PREFIX = 'axis_'`
   - `stripSystemCollectionPrefix(name)` thay `substring(9)`
2. Bulk replace có kiểm soát (script):
   - `directus_` → `axis_` trên source (ts/js/vue/yaml/yml/json/sql) — **không** đụng `node_modules`, lockfiles
   - `\bDirectus\b` → `Axis` trong UI/lang + user-facing strings (app + api messages + bd-lab)
3. Fix residual: `startsWith('axis_')`, i18n keys `directus_collection` → `axis_collection` (đồng bộ với note `$t:...`)
4. **DB lab:** wipe volume Postgres Directus (schema cũ vẫn `directus_*`) rồi bootstrap lại
5. Smoke: start API, confirm tables `axis_*`, UI title/login hiện Axis

## Risks

- DB hiện có **không** migrate tự động → bắt buộc recreate volume lab
- Extensions / snapshot / schema dump ngoài repo vẫn trỏ `directus_*`
- Test blackbox/e2e phải chạy lại sau rename
- Theme keys `theme_directus_*` → `theme_axis_*` nếu đổi i18n keys

## Phases

### Phase 1 — Prefix constant + system-data + seeds/migrations
SoT tên bảng; DB bootstrap tạo `axis_*`.

### Phase 2 — Bulk code/SDK/app references + strip-prefix helper
Mọi service/controller/test trỏ collection mới.

### Phase 3 — Display / i18n / BD lab messages
UI không còn chữ Directus (trong scope).

### Phase 4 — Lab reset + smoke
`docker compose` down -v (pg Directus) → up → verify.

## Acceptance

- [x] `isSystemCollection('axis_users') === true` (source)
- [x] Seeds/migrations/system-data tạo/tham chiếu `axis_*`
- [x] App title / welcome / powered-by (en-US) hiện **Axis**
- [x] Wipe volume lab + restart compose + re-bootstrap KC
- [x] Không đổi `@directus/*` package names
- [x] **Runtime docker image** build local `bd-axis:local` → DB có **33** bảng `axis_*`, **0** `directus_*`

## Notes (2026-07-23 implement)

- Bulk: 592 files, 5094× `directus_` → `axis_`
- Added `SYSTEM_COLLECTION_PREFIX` + `stripSystemCollectionPrefix` in `@directus/system-data`
- Fixed `substring(9)` → `substring(5)` / strip helper
- Display: en-US + hardcoded UI/API fallbacks; vi-VN không có chữ "Directus" sẵn
- Scripts: `scripts/bd-rebrand-axis-prefix.mjs`, `scripts/bd-rebrand-axis-display.mjs`
- Volumes compose: `bd_axis_*` (đã wipe `bd_directus_*`)
- **Follow-up:** build Dockerfile local để container dùng schema `axis_*` + UI Axis
