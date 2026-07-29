using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenWorkflowRuntimeConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowInstance_ExtensionCounters",
                table: "DocumentWorkflowInstances",
                sql: "\"ExtensionCount\" >= 0 AND \"TotalExtensionBusinessDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowInstance_FinishedShape",
                table: "DocumentWorkflowInstances",
                sql: "((\"Status\" IN (0, 1, 2) AND \"FinishedAtUtc\" IS NULL) OR (\"Status\" IN (3, 4, 5, 6) AND \"FinishedAtUtc\" IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowInstance_OverdueShape",
                table: "DocumentWorkflowInstances",
                sql: "((\"Status\" = 2 AND \"OverdueAtUtc\" IS NOT NULL) OR (\"Status\" <> 2 AND \"OverdueAtUtc\" IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentAssignment_CurrentPending",
                table: "DocumentAssignments",
                sql: "NOT \"IsCurrent\" OR \"Status\" = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentAssignment_ProcessedShape",
                table: "DocumentAssignments",
                sql: "((\"Status\" = 0 AND \"ProcessedAtUtc\" IS NULL) OR (\"Status\" IN (1, 2, 3) AND \"ProcessedAtUtc\" IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowInstance_ExtensionCounters",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowInstance_FinishedShape",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowInstance_OverdueShape",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentAssignment_CurrentPending",
                table: "DocumentAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentAssignment_ProcessedShape",
                table: "DocumentAssignments");
        }
    }
}
