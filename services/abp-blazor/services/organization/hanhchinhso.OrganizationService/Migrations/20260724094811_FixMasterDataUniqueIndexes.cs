using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.OrganizationService.Migrations
{
    /// <inheritdoc />
    public partial class FixMasterDataUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMasterData_TenantId_Type_Code",
                table: "OrganizationMasterData");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMasterData_TenantId_Type_Code",
                table: "OrganizationMasterData",
                columns: new[] { "TenantId", "Type", "Code" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMasterData_Type_Code",
                table: "OrganizationMasterData",
                columns: new[] { "Type", "Code" },
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationMasterData_TenantId_Type_Code",
                table: "OrganizationMasterData");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMasterData_Type_Code",
                table: "OrganizationMasterData");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMasterData_TenantId_Type_Code",
                table: "OrganizationMasterData",
                columns: new[] { "TenantId", "Type", "Code" },
                unique: true);
        }
    }
}
