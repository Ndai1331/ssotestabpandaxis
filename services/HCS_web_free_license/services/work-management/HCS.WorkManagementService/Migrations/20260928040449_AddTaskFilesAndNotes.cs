using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskFilesAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "hcs_work",
                table: "ProjectTaskDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "hcs_work",
                table: "ProjectTaskDocuments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "REFERENCE");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "hcs_work",
                table: "ProjectTaskAssignments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectTaskAttachments",
                schema: "hcs_work",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTaskAttachments_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalSchema: "hcs_work",
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAttachments_BlobName",
                schema: "hcs_work",
                table: "ProjectTaskAttachments",
                column: "BlobName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAttachments_ProjectTaskId_CreationTime",
                schema: "hcs_work",
                table: "ProjectTaskAttachments",
                columns: new[] { "ProjectTaskId", "CreationTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTaskAttachments",
                schema: "hcs_work");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "hcs_work",
                table: "ProjectTaskDocuments");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "hcs_work",
                table: "ProjectTaskDocuments");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "hcs_work",
                table: "ProjectTaskAssignments");
        }
    }
}
