using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HCS.DocumentService.Migrations;

[DbContext(typeof(DocumentServiceDbContext))]
[Migration("20260917010000_TrackWorkflowFile")]
public partial class TrackWorkflowFile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<Guid>(name: "WorkflowFileId", schema: "document",
            table: "Documents", type: "uuid", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "WorkflowFileId", schema: "document", table: "Documents");
}
