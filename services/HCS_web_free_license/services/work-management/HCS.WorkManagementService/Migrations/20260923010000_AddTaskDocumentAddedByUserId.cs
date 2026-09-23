using System;
using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    [DbContext(typeof(WorkManagementDbContext))]
    [Migration("20260923010000_AddTaskDocumentAddedByUserId")]
    public partial class AddTaskDocumentAddedByUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedByUserId",
                schema: "hcs_work",
                table: "ProjectTaskDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskDocuments_AddedByUserId",
                schema: "hcs_work",
                table: "ProjectTaskDocuments",
                column: "AddedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTaskDocuments_AddedByUserId",
                schema: "hcs_work",
                table: "ProjectTaskDocuments");

            migrationBuilder.DropColumn(
                name: "AddedByUserId",
                schema: "hcs_work",
                table: "ProjectTaskDocuments");
        }
    }
}
