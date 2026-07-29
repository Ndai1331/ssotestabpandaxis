using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowAssignmentConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowStepAssignmentConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowStepTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeType = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepAssignmentConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepAssignmentConfigurations_WorkflowStepTemplates_~",
                        column: x => x.WorkflowStepTemplateId,
                        principalTable: "WorkflowStepTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepAssignmentOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepAssignmentOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepAssignmentOrganizationUnits_WorkflowStepAssignm~",
                        column: x => x.ConfigurationId,
                        principalTable: "WorkflowStepAssignmentConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepAssignmentUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepAssignmentUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepAssignmentUsers_WorkflowStepAssignmentConfigura~",
                        column: x => x.ConfigurationId,
                        principalTable: "WorkflowStepAssignmentConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentConfigurations_TenantId_WorkflowStep~1",
                table: "WorkflowStepAssignmentConfigurations",
                columns: new[] { "TenantId", "WorkflowStepTemplateId", "IsPrimary" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsPrimary\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentConfigurations_TenantId_WorkflowStepT~",
                table: "WorkflowStepAssignmentConfigurations",
                columns: new[] { "TenantId", "WorkflowStepTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentConfigurations_WorkflowStepTemplateId~",
                table: "WorkflowStepAssignmentConfigurations",
                columns: new[] { "WorkflowStepTemplateId", "IsPrimary" },
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsPrimary\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_ConfigurationId",
                table: "WorkflowStepAssignmentOrganizationUnits",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_TenantId_Configurat~",
                table: "WorkflowStepAssignmentOrganizationUnits",
                columns: new[] { "TenantId", "ConfigurationId", "OrganizationUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_ConfigurationId",
                table: "WorkflowStepAssignmentUsers",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_TenantId_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers",
                columns: new[] { "TenantId", "ConfigurationId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowStepAssignmentOrganizationUnits");

            migrationBuilder.DropTable(
                name: "WorkflowStepAssignmentUsers");

            migrationBuilder.DropTable(
                name: "WorkflowStepAssignmentConfigurations");
        }
    }
}
