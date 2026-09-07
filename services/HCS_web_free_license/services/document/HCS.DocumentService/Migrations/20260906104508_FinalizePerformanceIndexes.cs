using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations
{
    /// <inheritdoc />
    public partial class FinalizePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_Number_Trgm",
                schema: "document",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Number",
                schema: "document",
                table: "Documents",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Number_Trgm",
                schema: "document",
                table: "Documents",
                column: "Number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_Number",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Number_Trgm",
                schema: "document",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Number_Trgm",
                schema: "document",
                table: "Documents",
                column: "Number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }
    }
}
