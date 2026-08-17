using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.OrganizationService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomDepartmentsUseAbpOrganizationUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationUserDepartments");

            migrationBuilder.DropTable(
                name: "OrganizationDepartments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ParentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUserDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUserDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUserDepartments_OrganizationDepartments_Departm~",
                        column: x => x.DepartmentId,
                        principalTable: "OrganizationDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDepartments_Code",
                table: "OrganizationDepartments",
                column: "Code",
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDepartments_TenantId_Code",
                table: "OrganizationDepartments",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationDepartments_TenantId_ParentId",
                table: "OrganizationDepartments",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserDepartments_DepartmentId_UserId",
                table: "OrganizationUserDepartments",
                columns: new[] { "DepartmentId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUserDepartments_TenantId_DepartmentId_UserId",
                table: "OrganizationUserDepartments",
                columns: new[] { "TenantId", "DepartmentId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");

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
    }
}
