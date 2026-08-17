using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndexPendingDocumentFileCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_TenantId_BlobDeletionPending_LastModification~",
                table: "DocumentFiles",
                columns: new[] { "TenantId", "BlobDeletionPending", "LastModificationTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentFiles_TenantId_BlobDeletionPending_LastModification~",
                table: "DocumentFiles");
        }
    }
}
