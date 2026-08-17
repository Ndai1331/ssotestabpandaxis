using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenSigningMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserSignature_DigitalCredential",
                table: "UserSignatures");

            migrationBuilder.AddColumn<Guid>(
                name: "SignatureSettingId",
                table: "UserSignatures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_SignatureSettingId",
                table: "UserSignatures",
                column: "SignatureSettingId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserSignature_DigitalCredential",
                table: "UserSignatures",
                sql: "\"SignatureType\" <> 1 OR NOT \"IsActive\" OR (\"TokenReference\" IS NOT NULL AND length(trim(\"TokenReference\")) > 0 AND \"ProtectedSecret\" IS NOT NULL AND length(trim(\"ProtectedSecret\")) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserSignature_ElectronicCredential",
                table: "UserSignatures",
                sql: "\"SignatureType\" <> 0 OR (\"TokenReference\" IS NULL AND \"ProtectedSecret\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSignatures_SignatureSettings_SignatureSettingId",
                table: "UserSignatures",
                column: "SignatureSettingId",
                principalTable: "SignatureSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSignatures_SignatureSettings_SignatureSettingId",
                table: "UserSignatures");

            migrationBuilder.DropIndex(
                name: "IX_UserSignatures_SignatureSettingId",
                table: "UserSignatures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserSignature_DigitalCredential",
                table: "UserSignatures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserSignature_ElectronicCredential",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "SignatureSettingId",
                table: "UserSignatures");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserSignature_DigitalCredential",
                table: "UserSignatures",
                sql: "\"SignatureType\" <> 1 OR (\"TokenReference\" IS NOT NULL AND length(trim(\"TokenReference\")) > 0 AND \"ProtectedSecret\" IS NOT NULL AND length(trim(\"ProtectedSecret\")) > 0)");
        }
    }
}
