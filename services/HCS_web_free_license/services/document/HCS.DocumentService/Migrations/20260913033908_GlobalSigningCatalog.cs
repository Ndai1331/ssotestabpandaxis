using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.DocumentService.Migrations
{
    /// <inheritdoc />
    public partial class GlobalSigningCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SigningCredentials_UserId_Kind",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "document",
                table: "SigningCredentials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "DefaultSignType",
                schema: "document",
                table: "SigningCredentials",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableSignLog",
                schema: "document",
                table: "SigningCredentials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "document",
                table: "SigningCredentials",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "document",
                table: "SigningCredentials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "KeepOriginalFile",
                schema: "document",
                table: "SigningCredentials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegacyLayoutImagePath",
                schema: "document",
                table: "SigningCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverwriteSignedFile",
                schema: "document",
                table: "SigningCredentials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProviderType",
                schema: "document",
                table: "SigningCredentials",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignedFileSuffix",
                schema: "document",
                table: "SigningCredentials",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "signed");

            migrationBuilder.CreateIndex(
                name: "IX_SigningCredentials_Kind_ProviderCode",
                schema: "document",
                table: "SigningCredentials",
                columns: new[] { "Kind", "ProviderCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SigningCredentials_Kind_ProviderCode",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "DefaultSignType",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "EnableSignLog",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "KeepOriginalFile",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "LegacyLayoutImagePath",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "OverwriteSignedFile",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.DropColumn(
                name: "SignedFileSuffix",
                schema: "document",
                table: "SigningCredentials");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "document",
                table: "SigningCredentials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SigningCredentials_UserId_Kind",
                schema: "document",
                table: "SigningCredentials",
                columns: new[] { "UserId", "Kind" },
                unique: true);
        }
    }
}
