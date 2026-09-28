using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingMethodId",
                schema: "document",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProcessingMethodId",
                schema: "document",
                table: "Documents",
                column: "ProcessingMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_ProcessingMethodId",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingMethodId",
                schema: "document",
                table: "Documents");
        }
    }
}
