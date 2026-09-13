using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentCode",
                schema: "document",
                table: "Documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentCode_Trgm",
                schema: "document",
                table: "Documents",
                column: "DocumentCode")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentCode_Trgm",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentCode",
                schema: "document",
                table: "Documents");
        }
    }
}
