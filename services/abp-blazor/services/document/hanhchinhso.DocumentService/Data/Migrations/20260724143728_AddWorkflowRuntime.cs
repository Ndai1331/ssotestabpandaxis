using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentUsers_ConfigurationId",
                table: "WorkflowStepAssignmentUsers");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentUsers_TenantId_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_ConfigurationId",
                table: "WorkflowStepAssignmentOrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_TenantId_Configurat~",
                table: "WorkflowStepAssignmentOrganizationUnits");

            migrationBuilder.CreateTable(
                name: "DocumentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommittedStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DocumentFileResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_DocumentFiles_DocumentFileResultId",
                        column: x => x.DocumentFileResultId,
                        principalTable: "DocumentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAssignments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentHistories_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowCommittedReceivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommittedStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ProvenanceOrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvenanceRoleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowCommittedReceivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowCommittedSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AllowReturn = table.Column<bool>(type: "boolean", nullable: false),
                    SlaDays = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowCommittedSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowCommittedSteps_WorkflowStepTemplates_Templa~",
                        column: x => x.TemplateStepId,
                        principalTable: "WorkflowStepTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowCommittedViewScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommittedStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowCommittedViewScopes", x => x.Id);
                    table.CheckConstraint("CK_CommittedViewScope_OneTarget", "(\"OrganizationUnitId\" IS NOT NULL AND \"UserId\" IS NULL) OR (\"OrganizationUnitId\" IS NULL AND \"UserId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowCommittedViewScopes_DocumentWorkflowCommitt~",
                        column: x => x.CommittedStepId,
                        principalTable: "DocumentWorkflowCommittedSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentCommittedStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeadlineAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OverdueAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PreviousInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtensionCount = table.Column<int>(type: "integer", nullable: false),
                    TotalExtensionBusinessDays = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstances_DocumentWorkflowCommittedSteps_Cu~",
                        column: x => x.CurrentCommittedStepId,
                        principalTable: "DocumentWorkflowCommittedSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstances_DocumentWorkflowInstances_Previou~",
                        column: x => x.PreviousInstanceId,
                        principalTable: "DocumentWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstances_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstances_WorkflowTemplates_WorkflowTemplat~",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstances_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentWorkflowInstanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentWorkflowInstanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstanceLogs_DocumentAssignments_Assignment~",
                        column: x => x.AssignmentId,
                        principalTable: "DocumentAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentWorkflowInstanceLogs_DocumentWorkflowInstances_Inst~",
                        column: x => x.InstanceId,
                        principalTable: "DocumentWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers",
                columns: new[] { "ConfigurationId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_TenantId_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers",
                columns: new[] { "TenantId", "ConfigurationId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_ConfigurationId_Org~",
                table: "WorkflowStepAssignmentOrganizationUnits",
                columns: new[] { "ConfigurationId", "OrganizationUnitId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_TenantId_Configurat~",
                table: "WorkflowStepAssignmentOrganizationUnits",
                columns: new[] { "TenantId", "ConfigurationId", "OrganizationUnitId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_CommittedStepId",
                table: "DocumentAssignments",
                column: "CommittedStepId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_DocumentFileResultId",
                table: "DocumentAssignments",
                column: "DocumentFileResultId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_DocumentId",
                table: "DocumentAssignments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_InstanceId_CommittedStepId",
                table: "DocumentAssignments",
                columns: new[] { "InstanceId", "CommittedStepId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_TenantId_InstanceId_CommittedStepId",
                table: "DocumentAssignments",
                columns: new[] { "TenantId", "InstanceId", "CommittedStepId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAssignments_TenantId_ReceiverUserId_Status_IsCurrent",
                table: "DocumentAssignments",
                columns: new[] { "TenantId", "ReceiverUserId", "Status", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistories_DocumentId",
                table: "DocumentHistories",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistories_InstanceId",
                table: "DocumentHistories",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHistories_TenantId_DocumentId_OccurredAtUtc",
                table: "DocumentHistories",
                columns: new[] { "TenantId", "DocumentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedReceivers_CommittedStepId_UserId",
                table: "DocumentWorkflowCommittedReceivers",
                columns: new[] { "CommittedStepId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedReceivers_TenantId_CommittedStepId~",
                table: "DocumentWorkflowCommittedReceivers",
                columns: new[] { "TenantId", "CommittedStepId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedSteps_InstanceId_Order",
                table: "DocumentWorkflowCommittedSteps",
                columns: new[] { "InstanceId", "Order" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedSteps_InstanceId_TemplateStepId",
                table: "DocumentWorkflowCommittedSteps",
                columns: new[] { "InstanceId", "TemplateStepId" },
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedSteps_TemplateStepId",
                table: "DocumentWorkflowCommittedSteps",
                column: "TemplateStepId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedSteps_TenantId_InstanceId_Order",
                table: "DocumentWorkflowCommittedSteps",
                columns: new[] { "TenantId", "InstanceId", "Order" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedSteps_TenantId_InstanceId_Template~",
                table: "DocumentWorkflowCommittedSteps",
                columns: new[] { "TenantId", "InstanceId", "TemplateStepId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedViewScopes_CommittedStepId_Organiz~",
                table: "DocumentWorkflowCommittedViewScopes",
                columns: new[] { "CommittedStepId", "OrganizationUnitId" },
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"OrganizationUnitId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedViewScopes_CommittedStepId_UserId",
                table: "DocumentWorkflowCommittedViewScopes",
                columns: new[] { "CommittedStepId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedViewScopes_TenantId_CommittedStep~1",
                table: "DocumentWorkflowCommittedViewScopes",
                columns: new[] { "TenantId", "CommittedStepId", "UserId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowCommittedViewScopes_TenantId_CommittedStepI~",
                table: "DocumentWorkflowCommittedViewScopes",
                columns: new[] { "TenantId", "CommittedStepId", "OrganizationUnitId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"OrganizationUnitId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstanceLogs_AssignmentId",
                table: "DocumentWorkflowInstanceLogs",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstanceLogs_InstanceId",
                table: "DocumentWorkflowInstanceLogs",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstanceLogs_TenantId_InstanceId_OccurredAt~",
                table: "DocumentWorkflowInstanceLogs",
                columns: new[] { "TenantId", "InstanceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_CurrentCommittedStepId",
                table: "DocumentWorkflowInstances",
                column: "CurrentCommittedStepId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_DocumentId",
                table: "DocumentWorkflowInstances",
                column: "DocumentId",
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_PreviousInstanceId",
                table: "DocumentWorkflowInstances",
                column: "PreviousInstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_TenantId_DocumentId",
                table: "DocumentWorkflowInstances",
                columns: new[] { "TenantId", "DocumentId" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_TenantId_DocumentId_Status",
                table: "DocumentWorkflowInstances",
                columns: new[] { "TenantId", "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_WorkflowId",
                table: "DocumentWorkflowInstances",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_WorkflowTemplateId",
                table: "DocumentWorkflowInstances",
                column: "WorkflowTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAssignments_DocumentWorkflowCommittedSteps_Committe~",
                table: "DocumentAssignments",
                column: "CommittedStepId",
                principalTable: "DocumentWorkflowCommittedSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAssignments_DocumentWorkflowInstances_InstanceId",
                table: "DocumentAssignments",
                column: "InstanceId",
                principalTable: "DocumentWorkflowInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentHistories_DocumentWorkflowInstances_InstanceId",
                table: "DocumentHistories",
                column: "InstanceId",
                principalTable: "DocumentWorkflowInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentWorkflowCommittedReceivers_DocumentWorkflowCommitte~",
                table: "DocumentWorkflowCommittedReceivers",
                column: "CommittedStepId",
                principalTable: "DocumentWorkflowCommittedSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentWorkflowCommittedSteps_DocumentWorkflowInstances_In~",
                table: "DocumentWorkflowCommittedSteps",
                column: "InstanceId",
                principalTable: "DocumentWorkflowInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentWorkflowInstances_DocumentWorkflowCommittedSteps_Cu~",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropTable(
                name: "DocumentHistories");

            migrationBuilder.DropTable(
                name: "DocumentWorkflowCommittedReceivers");

            migrationBuilder.DropTable(
                name: "DocumentWorkflowCommittedViewScopes");

            migrationBuilder.DropTable(
                name: "DocumentWorkflowInstanceLogs");

            migrationBuilder.DropTable(
                name: "DocumentAssignments");

            migrationBuilder.DropTable(
                name: "DocumentWorkflowCommittedSteps");

            migrationBuilder.DropTable(
                name: "DocumentWorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentUsers_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentUsers_TenantId_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_ConfigurationId_Org~",
                table: "WorkflowStepAssignmentOrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_TenantId_Configurat~",
                table: "WorkflowStepAssignmentOrganizationUnits");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_ConfigurationId",
                table: "WorkflowStepAssignmentUsers",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentUsers_TenantId_ConfigurationId_UserId",
                table: "WorkflowStepAssignmentUsers",
                columns: new[] { "TenantId", "ConfigurationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_ConfigurationId",
                table: "WorkflowStepAssignmentOrganizationUnits",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepAssignmentOrganizationUnits_TenantId_Configurat~",
                table: "WorkflowStepAssignmentOrganizationUnits",
                columns: new[] { "TenantId", "ConfigurationId", "OrganizationUnitId" },
                unique: true);
        }
    }
}
