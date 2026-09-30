# Gán chức vụ cho toàn bộ user trên prod

Chức vụ hiển thị lấy từ `hcs_organization.hcs_organization."UserOrganizationMappings"."PositionId"`, nối sang `hcs_organization."Positions"`.

Nguồn chữ đang có sẵn trên user: `hcs_identity.public."AbpUsers"."ExtraProperties"` → `PositionId_Text`, dạng `CODE - Tên` (ví dụ `DOCTOR - Bác sĩ`).

Container Postgres prod: `d12eab8ee303` (`postgres:18-alpine`, cổng `5432`).

Script ghi dữ liệu mở session ở `hcs_identity`, rồi ghi sang `hcs_organization` qua `dblink` (cùng server, user `postgres`, socket local, không ghi password vào file).

## Việc script làm

1. Bỏ khóa ngoại `DepartmentId` của mapping và của `Units` sang `hcs_organization."Departments"` nếu còn. Khoa thật nằm ở `hcs_identity.public."AbpOrganizationUnits"`.
2. Cho phép `UserOrganizationMappings."DepartmentId"` null.
3. Ghi ba migration tương ứng vào `public."__EFMigrationsHistory"` nếu chưa có:
   - `20260923120000_UseIdentityDepartmentsForUnits`
   - `20260924100000_UseIdentityDepartmentsForUserMappings`
   - `20260924110000_AllowUserMappingWithoutDepartment`
4. Với mỗi user chưa xóa và có `PositionId_Text` khớp danh mục:
   - Đã có mapping `IsPrimary = true`: chỉ cập nhật `PositionId`.
   - Chưa có: thêm một dòng `IsPrimary = true`, `DepartmentId` là organization unit sớm nhất của user.

User không có `PositionId_Text`, hoặc chữ không khớp `Positions."Code"` / `"Name"`, bị bỏ qua.

Chạy lại script không tạo thêm dòng chính: user đã có `IsPrimary` chỉ được cập nhật `PositionId`.

## 1. Backup

Trên máy đang thấy container:

```bash
docker exec d12eab8ee303 \
  pg_dump -U postgres -d hcs_organization \
  --schema=hcs_organization \
  --table='hcs_organization."UserOrganizationMappings"' \
  --file=/tmp/user-org-mappings.sql

docker cp d12eab8ee303:/tmp/user-org-mappings.sql ./user-org-mappings.sql
```

Giữ file `user-org-mappings.sql` ngoài server trước khi chạy bước 3.

## 2. Xem trước (không ghi)

```bash
docker exec -i d12eab8ee303 \
  psql -U postgres -d hcs_identity <<'SQL'
CREATE EXTENSION IF NOT EXISTS dblink;
SELECT dblink_connect('org', 'dbname=hcs_organization user=postgres');

SELECT *
FROM dblink('org', $$
  SELECT
    (SELECT count(*) FROM hcs_organization."Positions") AS positions,
    (SELECT count(*) FROM hcs_organization."UserOrganizationMappings") AS mappings,
    (SELECT count("PositionId") FROM hcs_organization."UserOrganizationMappings") AS mappings_with_position
$$) AS t(positions bigint, mappings bigint, mappings_with_position bigint);

CREATE TEMP TABLE positions AS
SELECT * FROM dblink('org', $$
  SELECT "Id", "Code", "Name" FROM hcs_organization."Positions"
$$) AS t(id uuid, code text, name text);

CREATE TEMP TABLE parsed AS
SELECT
  u."Id" AS user_id,
  btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text') AS position_text,
  split_part(btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text'), ' - ', 1) AS code,
  btrim(substring(
    btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text')
    FROM position(' - ' IN btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text')) + 3
  )) AS name
FROM public."AbpUsers" u
WHERE COALESCE(u."IsDeleted", false) = false
  AND COALESCE(btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text'), '') <> '';

SELECT
  (SELECT count(*) FROM public."AbpUsers" WHERE COALESCE("IsDeleted", false) = false) AS active_users,
  (SELECT count(*) FROM parsed) AS with_position_text,
  (SELECT count(*) FROM (
     SELECT DISTINCT p.user_id
     FROM parsed p
     JOIN positions pos
       ON pos.code = p.code
       OR (pos.name = p.name AND pos.code LIKE p.code || '-%')
   ) m) AS matched;

SELECT p.position_text, count(*) AS users
FROM parsed p
WHERE NOT EXISTS (
  SELECT 1 FROM positions pos
  WHERE pos.code = p.code
     OR (pos.name = p.name AND pos.code LIKE p.code || '-%')
)
GROUP BY p.position_text
ORDER BY users DESC;

SELECT u."UserName"
FROM public."AbpUsers" u
WHERE COALESCE(u."IsDeleted", false) = false
  AND COALESCE(btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text'), '') = ''
ORDER BY u."UserName";

SELECT dblink_disconnect('org');
SQL
```

Chỉ chạy bước 3 khi `matched` gần bằng `with_position_text`. Danh sách `position_text` không khớp cần xử lý danh mục trước. User không có chữ chức vụ sẽ không được gán.

Nếu `dblink_connect` báo sai mật khẩu, thêm `password=...` vào chuỗi kết nối. Không commit chuỗi đó.

## 3. Ghi mapping

```bash
docker exec -i d12eab8ee303 \
  psql -U postgres -d hcs_identity -v ON_ERROR_STOP=1 <<'SQL'
CREATE EXTENSION IF NOT EXISTS dblink;
SELECT dblink_connect('org', 'dbname=hcs_organization user=postgres');
SELECT dblink_exec('org', 'BEGIN');

DO $$
DECLARE
  chunk text;
  summary record;
BEGIN
  PERFORM dblink_exec('org', $remote$
    ALTER TABLE hcs_organization."Units"
      DROP CONSTRAINT IF EXISTS "FK_Units_Departments_DepartmentId";

    ALTER TABLE hcs_organization."UserOrganizationMappings"
      DROP CONSTRAINT IF EXISTS "FK_UserOrganizationMappings_Departments_DepartmentId";

    ALTER TABLE hcs_organization."UserOrganizationMappings"
      ALTER COLUMN "DepartmentId" DROP NOT NULL;

    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES
      ('20260923120000_UseIdentityDepartmentsForUnits', '10.0.9'),
      ('20260924100000_UseIdentityDepartmentsForUserMappings', '10.0.9'),
      ('20260924110000_AllowUserMappingWithoutDepartment', '10.0.9')
    ON CONFLICT ("MigrationId") DO NOTHING;

    CREATE TEMP TABLE incoming (
      user_id uuid,
      position_id uuid,
      department_id uuid
    );
  $remote$);

  CREATE TEMP TABLE positions AS
  SELECT * FROM dblink('org', $$
    SELECT "Id", "Code", "Name" FROM hcs_organization."Positions"
  $$) AS t(id uuid, code text, name text);

  CREATE TEMP TABLE departments AS
  SELECT DISTINCT ON (uo."UserId")
    uo."UserId" AS user_id,
    uo."OrganizationUnitId" AS department_id
  FROM public."AbpUserOrganizationUnits" uo
  ORDER BY uo."UserId", uo."CreationTime", uo."OrganizationUnitId";

  CREATE TEMP TABLE matched AS
  WITH parsed AS (
    SELECT
      u."Id" AS user_id,
      split_part(btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text'), ' - ', 1) AS code,
      btrim(substring(
        btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text')
        FROM position(' - ' IN btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text')) + 3
      )) AS name
    FROM public."AbpUsers" u
    WHERE COALESCE(u."IsDeleted", false) = false
      AND COALESCE(btrim(u."ExtraProperties"::jsonb ->> 'PositionId_Text'), '') <> ''
  )
  SELECT DISTINCT ON (p.user_id)
    p.user_id,
    pos.id AS position_id,
    d.department_id
  FROM parsed p
  JOIN positions pos
    ON pos.code = p.code
    OR (pos.name = p.name AND pos.code LIKE p.code || '-%')
  LEFT JOIN departments d ON d.user_id = p.user_id
  ORDER BY p.user_id, CASE WHEN pos.code = p.code THEN 0 ELSE 1 END, pos.code;

  IF (SELECT count(*) FROM matched) = 0 THEN
    RAISE EXCEPTION 'No user matched a position. Nothing was written.';
  END IF;

  FOR chunk IN
    SELECT string_agg(val, ',') AS vals
    FROM (
      SELECT
        format(
          '(%L::uuid,%L::uuid,%s)',
          user_id,
          position_id,
          COALESCE(format('%L::uuid', department_id), 'NULL::uuid')
        ) AS val,
        (row_number() OVER (ORDER BY user_id) - 1) / 200 AS grp
      FROM matched
    ) rows
    GROUP BY grp
  LOOP
    PERFORM dblink_exec(
      'org',
      'INSERT INTO incoming (user_id, position_id, department_id) VALUES ' || chunk);
  END LOOP;

  PERFORM dblink_exec('org', $remote$
    UPDATE hcs_organization."UserOrganizationMappings" m
    SET "PositionId" = i.position_id,
        "LastModificationTime" = now()
    FROM incoming i
    WHERE m."UserId" = i.user_id
      AND m."PositionId" IS DISTINCT FROM i.position_id
      AND NOT EXISTS (
        SELECT 1
        FROM hcs_organization."UserOrganizationMappings" other
        WHERE other."Id" <> m."Id"
          AND other."UserId" = m."UserId"
          AND other."DepartmentId" IS NOT DISTINCT FROM m."DepartmentId"
          AND other."UnitId" IS NOT DISTINCT FROM m."UnitId"
          AND other."PositionId" = i.position_id
      );

    INSERT INTO hcs_organization."UserOrganizationMappings" (
      "Id", "UserId", "DepartmentId", "UnitId", "PositionId", "IsPrimary",
      "ExtraProperties", "ConcurrencyStamp", "CreationTime"
    )
    SELECT
      gen_random_uuid(),
      i.user_id,
      i.department_id,
      NULL,
      i.position_id,
      true,
      '{}',
      substr(replace(gen_random_uuid()::text, '-', ''), 1, 32),
      now()
    FROM incoming i
    WHERE NOT EXISTS (
      SELECT 1
      FROM hcs_organization."UserOrganizationMappings" existing
      WHERE existing."UserId" = i.user_id
        AND existing."IsPrimary" = true
    );
  $remote$);

  SELECT *
  INTO summary
  FROM dblink('org', $$
    SELECT
      count(*) AS mapping_rows,
      count("PositionId") AS with_position,
      count(DISTINCT "UserId") FILTER (WHERE "PositionId" IS NOT NULL) AS users_with_position
    FROM hcs_organization."UserOrganizationMappings"
  $$) AS t(mapping_rows bigint, with_position bigint, users_with_position bigint);

  RAISE NOTICE 'mapping_rows=%, with_position=%, users_with_position=%',
    summary.mapping_rows, summary.with_position, summary.users_with_position;

  PERFORM dblink_exec('org', 'COMMIT');
EXCEPTION WHEN OTHERS THEN
  PERFORM dblink_exec('org', 'ROLLBACK');
  RAISE;
END $$;

SELECT dblink_disconnect('org');
SQL
```

`users_with_position` phải bằng số `matched` ở bước 2. User đã có mapping nhưng không có chữ chức vụ giữ nguyên `PositionId` null.

## 4. Kiểm tra lại

```bash
docker exec -i d12eab8ee303 \
  psql -U postgres -d hcs_identity <<'SQL'
CREATE EXTENSION IF NOT EXISTS dblink;
SELECT dblink_connect('org', 'dbname=hcs_organization user=postgres');

SELECT code, name, users
FROM dblink('org', $$
  SELECT p."Code", p."Name", count(*) AS users
  FROM hcs_organization."UserOrganizationMappings" m
  JOIN hcs_organization."Positions" p ON p."Id" = m."PositionId"
  WHERE m."IsPrimary" = true
  GROUP BY p."Code", p."Name"
  ORDER BY users DESC
$$) AS t(code text, name text, users bigint);

SELECT dblink_disconnect('org');
SQL
```

Mở lại màn danh sách user và tải lại trang. Cột chức vụ đọc `PositionId` vừa ghi.
