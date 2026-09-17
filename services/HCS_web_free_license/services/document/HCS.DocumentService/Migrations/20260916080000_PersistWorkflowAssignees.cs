using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations;

[DbContext(typeof(DocumentServiceDbContext))]
[Migration("20260916080000_PersistWorkflowAssignees")]
public sealed partial class PersistWorkflowAssignees : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "AssigneeOverridesJson", schema: "document", table: "WorkflowInstances",
            type: "jsonb", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "AssigneeOverridesJson", schema: "document", table: "WorkflowInstances");
}
