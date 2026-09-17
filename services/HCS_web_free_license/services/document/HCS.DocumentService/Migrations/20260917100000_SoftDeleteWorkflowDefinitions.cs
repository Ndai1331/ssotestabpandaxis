using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations;

[DbContext(typeof(DocumentServiceDbContext))]
[Migration("20260917100000_SoftDeleteWorkflowDefinitions")]
public sealed partial class SoftDeleteWorkflowDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            schema: "document",
            table: "WorkflowDefinitions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.DropIndex(
            name: "IX_WorkflowDefinitions_Code",
            schema: "document",
            table: "WorkflowDefinitions");

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowDefinitions_Code",
            schema: "document",
            table: "WorkflowDefinitions",
            column: "Code",
            unique: true,
            filter: "\"IsDeleted\" = FALSE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_WorkflowDefinitions_Code",
            schema: "document",
            table: "WorkflowDefinitions");

        migrationBuilder.DropColumn(
            name: "IsDeleted",
            schema: "document",
            table: "WorkflowDefinitions");

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowDefinitions_Code",
            schema: "document",
            table: "WorkflowDefinitions",
            column: "Code",
            unique: true);
    }
}
