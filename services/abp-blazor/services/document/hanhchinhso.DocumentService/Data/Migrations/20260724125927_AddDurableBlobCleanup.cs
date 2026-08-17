using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableBlobCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentBlobCleanups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentBlobCleanups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlobCleanups_CreationTime",
                table: "DocumentBlobCleanups",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBlobCleanups_TenantId_BlobName",
                table: "DocumentBlobCleanups",
                columns: new[] { "TenantId", "BlobName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentBlobCleanups");
        }
    }
}
