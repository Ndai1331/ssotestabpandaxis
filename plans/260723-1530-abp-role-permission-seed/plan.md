# Plan — ABP sample role permissions (lab)

## Goal
Seed permission grants for `bacsi` / `lanhdao` / `nhanvien` (admin already full), analogous to Directus role scopes.

## Matrix (lab sample)

| Role | Intent | Permissions |
|------|--------|-------------|
| `admin` | Full | keep existing seed (all) |
| `lanhdao` | Read / oversight | Dashboard, Users view+details, Roles view, SecurityLogs, Sessions, AuditLogs |
| `bacsi` | Scoped ops | Dashboard, UserLookup, Users.ViewDetails, AIManagement Workspaces (read/playground) |
| `nhanvien` | Basic | Dashboard only |

## Change
- `AdministrationServiceDataSeeder.cs` — after admin seed, call `IPermissionDataSeeder` per role
- Restart AdministrationService to apply (runtime migrator only seeds on migrate; extend SeedAsync so restart re-runs seed — PermissionDataSeeder is idempotent)

## Verify
- SQL: `AbpPermissionGrants` has ProviderKey bacsi/lanhdao/nhanvien
- Login KC user each role → menus differ
