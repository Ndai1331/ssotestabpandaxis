using HCS.OrganizationService.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HCS.OrganizationService.Data.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(OrganizationDbContext))]
[Migration("20260924100000_UseIdentityDepartmentsForUserMappings")]
public partial class UseIdentityDepartmentsForUserMappings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UserOrganizationMappings_Departments_DepartmentId",
            schema: "hcs_organization", table: "UserOrganizationMappings");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_UserOrganizationMappings_Departments_DepartmentId",
            schema: "hcs_organization", table: "UserOrganizationMappings", column: "DepartmentId",
            principalSchema: "hcs_organization", principalTable: "Departments",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
}
