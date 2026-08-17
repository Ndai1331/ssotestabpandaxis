using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSigningAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "UserSignatures")
                       OR EXISTS (
                           SELECT 1 FROM "SignatureSettings"
                           WHERE "LayoutImageBlobName" IS NOT NULL)
                    THEN
                        RAISE EXCEPTION
                            'Signing image paths must be migrated to SigningAssets before applying AddSigningAssets.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "SealImageBlobName",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "SignatureImageBlobName",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "LayoutImageBlobName",
                table: "SignatureSettings");

            migrationBuilder.AddColumn<Guid>(
                name: "SealAssetId",
                table: "UserSignatures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignatureAssetId",
                table: "UserSignatures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LayoutAssetId",
                table: "SignatureSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SigningAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningAssets", x => x.Id);
                    table.CheckConstraint("CK_SigningAsset_KindOwner", "\"Kind\" IN (0, 1, 2) AND \"Size\" > 0 AND (\"Kind\" = 2 OR \"OwnerUserId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "SigningBlobCleanups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningBlobCleanups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_SealAssetId",
                table: "UserSignatures",
                column: "SealAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_SignatureAssetId",
                table: "UserSignatures",
                column: "SignatureAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureSettings_LayoutAssetId",
                table: "SignatureSettings",
                column: "LayoutAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAssets_BlobName",
                table: "SigningAssets",
                column: "BlobName",
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAssets_TenantId_BlobName",
                table: "SigningAssets",
                columns: new[] { "TenantId", "BlobName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAssets_TenantId_OwnerUserId_Kind",
                table: "SigningAssets",
                columns: new[] { "TenantId", "OwnerUserId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_SigningBlobCleanups_BlobName",
                table: "SigningBlobCleanups",
                column: "BlobName",
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SigningBlobCleanups_CreationTime",
                table: "SigningBlobCleanups",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_SigningBlobCleanups_TenantId_BlobName",
                table: "SigningBlobCleanups",
                columns: new[] { "TenantId", "BlobName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SignatureSettings_SigningAssets_LayoutAssetId",
                table: "SignatureSettings",
                column: "LayoutAssetId",
                principalTable: "SigningAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSignatures_SigningAssets_SealAssetId",
                table: "UserSignatures",
                column: "SealAssetId",
                principalTable: "SigningAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSignatures_SigningAssets_SignatureAssetId",
                table: "UserSignatures",
                column: "SignatureAssetId",
                principalTable: "SigningAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SignatureSettings_SigningAssets_LayoutAssetId",
                table: "SignatureSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSignatures_SigningAssets_SealAssetId",
                table: "UserSignatures");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSignatures_SigningAssets_SignatureAssetId",
                table: "UserSignatures");

            migrationBuilder.DropTable(
                name: "SigningAssets");

            migrationBuilder.DropTable(
                name: "SigningBlobCleanups");

            migrationBuilder.DropIndex(
                name: "IX_UserSignatures_SealAssetId",
                table: "UserSignatures");

            migrationBuilder.DropIndex(
                name: "IX_UserSignatures_SignatureAssetId",
                table: "UserSignatures");

            migrationBuilder.DropIndex(
                name: "IX_SignatureSettings_LayoutAssetId",
                table: "SignatureSettings");

            migrationBuilder.DropColumn(
                name: "SealAssetId",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "SignatureAssetId",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "LayoutAssetId",
                table: "SignatureSettings");

            migrationBuilder.AddColumn<string>(
                name: "SealImageBlobName",
                table: "UserSignatures",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImageBlobName",
                table: "UserSignatures",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LayoutImageBlobName",
                table: "SignatureSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
