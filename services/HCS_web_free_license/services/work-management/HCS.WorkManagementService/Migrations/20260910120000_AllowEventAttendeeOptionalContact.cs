using Microsoft.EntityFrameworkCore.Migrations;
using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace HCS.WorkManagementService.Migrations;

[DbContext(typeof(WorkManagementDbContext))]
[Migration("20260910120000_AllowEventAttendeeOptionalContact")]
public partial class AllowEventAttendeeOptionalContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            schema: "hcs_work",
            table: "EventAttendees",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            schema: "hcs_work",
            table: "EventAttendees",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(256)",
            oldMaxLength: 256);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            schema: "hcs_work",
            table: "EventAttendees",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            schema: "hcs_work",
            table: "EventAttendees",
            type: "character varying(256)",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(256)",
            oldMaxLength: 256,
            oldNullable: true);
    }
}
