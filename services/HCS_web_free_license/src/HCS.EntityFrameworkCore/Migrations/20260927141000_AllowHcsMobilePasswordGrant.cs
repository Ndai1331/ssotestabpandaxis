using HCS.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.Migrations;

/// <summary>
/// Adds OpenIddict <c>gt:password</c> to the existing native <c>hcs-mobile</c> client
/// so in-app login (ABP commerce style) works without wiping extra redirect URIs.
/// </summary>
[DbContext(typeof(HCSDbContext))]
[Migration("20260927141000_AllowHcsMobilePasswordGrant")]
public class AllowHcsMobilePasswordGrant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "OpenIddictApplications"
            SET
                "Permissions" = (
                    SELECT COALESCE(jsonb_agg(to_jsonb(value)), '[]'::jsonb)::text
                    FROM (
                        SELECT value
                        FROM jsonb_array_elements_text(COALESCE(NULLIF("Permissions", ''), '[]')::jsonb) AS t(value)
                        UNION
                        SELECT 'gt:password'
                    ) granted
                ),
                "LastModificationTime" = NOW()
            WHERE "ClientId" = 'hcs-mobile'
              AND "IsDeleted" = FALSE
              AND NOT (COALESCE(NULLIF("Permissions", ''), '[]')::jsonb ? 'gt:password');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "OpenIddictApplications"
            SET
                "Permissions" = (
                    SELECT COALESCE(jsonb_agg(to_jsonb(value)), '[]'::jsonb)::text
                    FROM jsonb_array_elements_text(COALESCE(NULLIF("Permissions", ''), '[]')::jsonb) AS t(value)
                    WHERE value <> 'gt:password'
                ),
                "LastModificationTime" = NOW()
            WHERE "ClientId" = 'hcs-mobile'
              AND "IsDeleted" = FALSE
              AND (COALESCE(NULLIF("Permissions", ''), '[]')::jsonb ? 'gt:password');
            """);
    }
}
