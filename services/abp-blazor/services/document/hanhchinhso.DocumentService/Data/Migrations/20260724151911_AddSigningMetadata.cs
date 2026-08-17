using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSigningMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignatureSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LayoutImageBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    DefaultSignatureType = table.Column<int>(type: "integer", nullable: false),
                    AllowElectronicSign = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDigitalSign = table.Column<bool>(type: "boolean", nullable: false),
                    RequireOtp = table.Column<bool>(type: "boolean", nullable: false),
                    SignWidth = table.Column<int>(type: "integer", nullable: false),
                    SignHeight = table.Column<int>(type: "integer", nullable: false),
                    SignedFileSuffix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KeepOriginalFile = table.Column<bool>(type: "boolean", nullable: false),
                    OverwriteSignedFile = table.Column<bool>(type: "boolean", nullable: false),
                    EnableSignLog = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SignatureSettings", x => x.Id);
                    table.CheckConstraint("CK_SignatureSetting_DefaultSignatureType", "\"DefaultSignatureType\" IN (0, 1)");
                    table.CheckConstraint("CK_SignatureSetting_ExecutionLimits", "\"ApiTimeoutSeconds\" BETWEEN 1 AND 600 AND \"SignWidth\" BETWEEN 1 AND 2000 AND \"SignHeight\" BETWEEN 1 AND 2000");
                    table.CheckConstraint("CK_SignatureSetting_ProviderType", "\"ProviderType\" IN (0, 1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "UserSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdentityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignatureType = table.Column<int>(type: "integer", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProtectedSecret = table.Column<string>(type: "text", nullable: true),
                    SealImageBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignatureImageBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_UserSignatures", x => x.Id);
                    table.CheckConstraint("CK_UserSignature_DigitalCredential", "\"SignatureType\" <> 1 OR (\"TokenReference\" IS NOT NULL AND length(trim(\"TokenReference\")) > 0 AND \"ProtectedSecret\" IS NOT NULL AND length(trim(\"ProtectedSecret\")) > 0)");
                    table.CheckConstraint("CK_UserSignature_Type", "\"SignatureType\" IN (0, 1)");
                    table.CheckConstraint("CK_UserSignature_Validity", "\"ValidFromUtc\" IS NULL OR \"ValidToUtc\" IS NULL OR \"ValidToUtc\" >= \"ValidFromUtc\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignatureSettings_ProviderCode",
                table: "SignatureSettings",
                column: "ProviderCode",
                unique: true,
                filter: "\"TenantId\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureSettings_TenantId_ProviderCode",
                table: "SignatureSettings",
                columns: new[] { "TenantId", "ProviderCode" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_TenantId_IdentityUserId_SignatureType_IsActi~",
                table: "UserSignatures",
                columns: new[] { "TenantId", "IdentityUserId", "SignatureType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_TenantId_ProviderCode",
                table: "UserSignatures",
                columns: new[] { "TenantId", "ProviderCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignatureSettings");

            migrationBuilder.DropTable(
                name: "UserSignatures");
        }
    }
}
