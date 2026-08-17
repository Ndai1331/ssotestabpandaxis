# SKILLS.md — BD Workspace

Quy trình vận hành cho **bd-workspace** (Bình Dương SSO Lab).  
Chi tiết kiến trúc: `docs/workspace-architecture.md`. Diagram: `system-sso-guideline.png`.

---

## Giai đoạn hiện tại

| Mục | Trạng thái |
|-----|------------|
| GitHub remote / GHA | **Chưa setup** |
| Deploy test/staging/prod | **Không áp dụng** |
| Chạy thử | **Local only** |
| Services | `directus-main`, `abp-blazor` (+ Keycloak Docker) |

Skill / script copy từ Task9 (deploy do-122, `[WEB]`/`[API]`, CPD, SEO…) → **ignore**.

---

## Skill routing (BD)

| Đang làm | Skill kích hoạt | Ghi chú |
|----------|-----------------|---------|
| Plan / architecture SSO | `ck-plan`, `brainstorm`, `research` | Bám `system-sso-guideline.png` |
| Implement theo plan | `cook` (`--auto`) | Bắt buộc khi có PLAN.md |
| Bug local | `fix`, `ck-debug` | Reproduce trước khi patch |
| Directus OIDC / extensions | `backend-development`, `docs-seeker` | Docs Directus Auth OpenID |
| ABP OIDC / Blazor | `backend-development` | External provider → Keycloak |
| Docs / wiki update | `docs` | Đồng bộ `docs/` + `wiki/` |
| Git commit/PR (khi có remote) | `git` | Chỉ khi user yêu cầu |

### Skills Task9 — không dùng

`start-local` (UI:5053/API:7093 Task9), `task9-*`, CPD/N8N/Metabase Task9 skills nếu còn trong `.claude/skills` từ bản copy.

---

## Local lab — checklist SSO

### 1. Keycloak lên

```bash
cd services/directus-main
docker compose up -d keycloak
# http://localhost:5110  — admin / secret
```

### 2. Realm & clients

Trong Keycloak:

1. Tạo Realm (vd: `bd` / `benhvien`)
2. User Federation → LDAP (Zimbra) khi có endpoint; lab có thể dùng user local trước
3. Client `directus` (OIDC, confidential hoặc public theo Directus docs)
4. Client `abp` / `abp-blazor` (OIDC — redirect URIs khớp AuthServer/Blazor)
5. Mappers: email, name, groups/roles

### 3. Directus → Keycloak

- Cấu hình Auth provider OpenID trong Directus (env / settings)
- Issuer = Keycloak realm URL
- Map role Keycloak → Directus roles/policies

### 4. ABP → Keycloak

- AuthServer (`apps/auth-server`) hoặc Blazor: thêm external OpenID Connect provider trỏ Keycloak
- Map claims → ABP roles / Identity users
- Infra deps: `services/abp-blazor/etc/docker` (xem README ABP)

### 5. Verify end-to-end

1. Mở Directus → redirect Keycloak → login (Zimbra hoặc user KC) → về Directus có session  
2. Mở ABP → cùng flow → session ABP  
3. SSO: login một app rồi mở app kia (cùng realm) không hỏi password lần nữa (nếu session KC còn)

---

## Git (khi user yêu cầu commit)

```bash
# Conventional commits — không prefix [WEB]/[API]
git add <files>
git commit -m "$(cat <<'EOF'
docs: describe change why

EOF
)"
```

- Không push nếu chưa có remote / user chưa bảo.
- Không commit `.env`, secrets, `*.pfx` private keys.

---

## Docs bắt buộc giữ đồng bộ

Sau thay đổi kiến trúc / SSO:

1. `docs/workspace-architecture.md`
2. `wiki/hot.md` + `wiki/index.md` (+ page mới nếu cần)
3. `README.md` / `CLAUDE.md` nếu đổi cấu trúc thư mục hoặc port

---

## Tham chiếu nhanh

| File | Nội dung |
|------|----------|
| `README.md` | Quick start + cấu trúc |
| `CLAUDE.md` | Rules agent BD |
| `docs/workspace-architecture.md` | SoT SSO |
| `system-sso-guideline.png` | Diagram gốc |
| `services/directus-main/readme.md` | Upstream Directus |
| `services/abp-blazor/README.md` | Upstream ABP template |
