using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.OrganizationService.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePrimaryDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserDepartments_TenantId_UserId",
                table: "OrganizationUserDepartments",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsPrimary\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserDepartments_UserId",
                table: "OrganizationUserDepartments",
                column: "UserId",
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsPrimary\" = true AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserDepartments_TenantId_UserId",
                table: "OrganizationUserDepartments");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUserDepartments_UserId",
                table: "OrganizationUserDepartments");
        }
    }
}
