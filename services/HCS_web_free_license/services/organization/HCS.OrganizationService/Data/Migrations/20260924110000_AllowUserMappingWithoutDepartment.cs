using HCS.OrganizationService.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HCS.OrganizationService.Data.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(OrganizationDbContext))]
[Migration("20260924110000_AllowUserMappingWithoutDepartment")]
public partial class AllowUserMappingWithoutDepartment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "DepartmentId",
            schema: "hcs_organization",
            table: "UserOrganizationMappings",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "DepartmentId",
            schema: "hcs_organization",
            table: "UserOrganizationMappings",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
