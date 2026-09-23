using Microsoft.EntityFrameworkCore.Migrations;

namespace HCS.OrganizationService.Data.Migrations;

public partial class UseIdentityDepartmentsForUnits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Units_Departments_DepartmentId",
            schema: "hcs_organization", table: "Units");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback requires remapping Identity department IDs to legacy departments first.
        migrationBuilder.AddForeignKey(
            name: "FK_Units_Departments_DepartmentId",
            schema: "hcs_organization", table: "Units", column: "DepartmentId",
            principalSchema: "hcs_organization", principalTable: "Departments",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
}
