# Cook Completed — Directus Lab SoT Migrated to v11

**Date**: 2026-07-25 18:09  
**Severity**: Medium (milestone — no blockers)  
**Component**: Directus v11 lab, Keycloak SSO integration, BD axis stack  
**Status**: Resolved ✓  

---

## What Happened

Hoàn thành cook plan **plans/260725-1726-directus-v11-sso-lab/** — tất cả 4 phases done. Directus lab SoT chính thức chuyển từ kiến trúc ad-hoc sang v11 production-ready, fully containerized, with Keycloak federation + OIDC + RBAC mapping.

Stack hiện tại:
- **bd-axis-v11:local** chạy trên `:8055` (Directus)
- **Keycloak** trên `:5110` (IdP)
- Named volumes: `bd_axis_v11_postgres`, `bd_axis_v11_redis`, `bd_axis_v11_elasticsearch` (persist data across restarts)
- **Extension bd-app-gate** loaded; gate deny rules verified via logs
- **ROLE_MAPPING** (UUID → local roles):
  - Admin: `56cb789a-...`
  - BacSi: `4183c3a9-...`
  - LanhDao: `ca499eb5-...`
  - NhanVien: `41457ca6-...`

Docs SoT updated; v12 moved to `ARCHIVE.md`.

---

## The Brutal Truth

Chúng ta finally có một **consistent, reproducible, testable** Directus lab environment. Không còn "it works on my machine" hay random config drifts. Điều này là relief — nhưng nó cũng exposes cái mà trước giấu được.

Phát hiện một số gotchas trong quá trình cook mà sẽ ảnh hưởng tới implementation phases tới:

1. **BD_LAB_ALLOW_SSO flag không có trên v11** — Directus SSO behavior khác v12. Mình chưa fully scope impact của điều này. Đó là **technical debt** — cần document behavior differences cụ thể.

2. **Edge case: /users/me endpoint** — SSO users không có field permissions cho `role.name` sẽ return `null` thay vì role name. Đây là security-by-default (không expose role names nếu user không có permission), nhưng nó có thể confuse clients. Phải thêm fallback logic khi call `/users/me`.

3. **Role UUID mapping phức tạp hơn dự kiến** — 4 UUIDs trong ROLE_MAPPING, và mỗi UUID phải mismatch chính xác giữa Keycloak → Directus DB. Nếu admin tạo new role nhưng forget update UUID, SSO sẽ silently fail (user authenticate nhưng role mapping bị bỏ qua). **Silent failures rất nguy hiểm**.

---

## Technical Details

### Extension Status
```
Extension bd-app-gate loaded successfully
Gate deny rules:
  - Route: /auth/oauth2/callback → deny if !allowed_origin
  - Logs show: "Gate: deny [reason]" on failed requests
```

### Smoke Test Results
- ✓ Keycloak login → OIDC token obtained
- ✓ Token redirect → Directus accepts & creates session
- ✓ Role mapping works for known UUIDs (4 roles tested)
- ⚠ `/users/me?fields=role.name` returns null if user lacks field perm (expected behavior, but clients need to handle gracefully)
- ✓ Admin users list shows role IDs correctly

### Volumes Persist
- Restarted services 3 times; data remained intact
- No data loss; no state drift

---

## What We Tried

1. **Phase 1: Scaffold workflow** — docker-compose.bd-lab.yml structure + named volumes → ✓
2. **Phase 2: Boot roles & mapping** — Keycloak realm + user federation (LDAP mock) + client scopes → ✓
3. **Phase 3: Smoke SSO gate** — bd-app-gate extension + OIDC callback validation → ✓
4. **Phase 4: Docs SoT** — Workspace architecture updated; v12 archived → ✓

No major rework needed; plan phases tracked well.

---

## Root Cause Analysis (What We Learned)

### 1. **Why we didn't catch the BD_LAB_ALLOW_SSO issue earlier**
- **Root**: Directus v11 docs vs v12 differ on SSO flags. We copied v12 assumptions into lab without checking v11 changelog.
- **Lesson**: Always **diff CHANGELOG / Release Notes** between major versions before porting config. Don't assume parity.

### 2. **Why role mapping is a silent-failure mine**
- **Root**: UUID mappings live in Directus DB + Keycloak Realm both. If they desync, there's no loud error — just "user authenticated, but no role".
- **Lesson**: Need **migration tooling** to validate UUID parity across Keycloak & Directus on startup. Add a health check endpoint that compares them.

### 3. **Why /users/me endpoint returns null for role.name**
- **Root**: Directus respects field-level permissions. If SSO user role doesn't include "read role.name", response omits it (null).
- **Lesson**: **Clients must handle optional fields gracefully**. Document this in API contract. Add fallback: if role.name is null, use role UUID as identifier.

---

## Decisions Made (and Not Made)

| Decision | Rationale |
|----------|-----------|
| **v11 as lab SoT** | Stable, feature-complete, smaller attack surface than v12 beta. |
| **Skip BD_LAB_ALLOW_SSO flag** | Not applicable to v11 flow; rely on extension + OIDC instead. |
| **4 roles in initial mapping** | Matches Bình Dương organizational structure (Admin, BacSi, LanhDao, NhanVien). Extensible if more roles added. |
| **Named volumes for state** | Persistent, testable, easy to backup/reset. No ephemeral containers. |
| **No external LDAP yet** | Lab uses mock (Keycloak embedded user list). Real Zimbra LDAP bridges in next phase. |

---

## Lessons Extracted

### For Future Cook / Implementation

1. **Version Parity Testing**: Before migrating stack between major versions, create a **feature matrix** (v11 vs v12 vs later). Check each integration point.

2. **Role UUID Auditing**: Add a **pre-startup validation script** that compares:
   - Keycloak realm roles (via API) 
   - Directus directus_roles table
   - Alerts if UUIDs mismatch or missing

3. **API Contract Clarity**: Document which fields can be `null` due to permissions, and provide fallbacks in client code. Example:
   ```
   GET /users/me
   {
     "id": "uuid",
     "email": "user@example.com",
     "role": {
       "id": "uuid",
       "name": null  // ← nullable if user lacks read:role.name permission
     }
   }
   ```

4. **Silent Failure Patterns**: Whenever config (role UUIDs, OIDC secrets, gateway routes) is deployed, add **smoke tests** that verify the config is live and working. Don't wait for users to report "login broken".

5. **Extension Visibility**: Log extension status on startup. Include `bd-app-gate` version + rules in health check endpoint.

---

## Next Steps

### Immediate (within 1 week)
- [ ] Document Directus v11 vs v12 SSO differences in `docs/directus-sso-versions.md`
- [ ] Add startup validation script for role UUID parity (Keycloak ↔ Directus DB)
- [ ] Update API docs to clarify nullable fields on `/users/me` (role.name with permission check)

### Short-term (before production)
- [ ] Integrate real Zimbra LDAP (not mock) into Keycloak federation
- [ ] Add ABP side SSO integration (test OIDC token exchange with ABP AuthServer)
- [ ] Performance test: Keycloak + Directus under 100 concurrent SSO logins

### Deferred (post-lab)
- [ ] Replace BD_LAB_ALLOW_SSO investigation with v11-specific findings
- [ ] Build admin dashboard for role UUID mapping management (UI to view/edit Keycloak ↔ Directus mappings)

---

## Emotional Reality

**Relief.** Chúng ta có một **solid foundation** để làm việc từ bây giờ. Không còn ad-hoc, không còn guessing. Test được reproduce, stack persist được, logs clear.

Nhưng cũng **nervous**. Silent failure modes (role mapping misync, null field returns) mà ta vừa phát hiện chứng tỏ rằng complexity layer này có nhiều trap. Next cook phases (ABP integration, real LDAP) sẽ thêm friction. Cần phải careful, không vội.

---

**Owner**: @claude  
**Reviewed**: ✗ (self-doc)  
**Archive**: Will link in wiki/hot.md & wiki/index.md  
