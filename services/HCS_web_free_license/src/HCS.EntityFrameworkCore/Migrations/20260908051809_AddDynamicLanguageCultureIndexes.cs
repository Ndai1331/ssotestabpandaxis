using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicLanguageCultureIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HcsLanguages_CultureName",
                table: "HcsLanguages");

            migrationBuilder.DropIndex(
                name: "IX_HcsLanguages_IsDefault",
                table: "HcsLanguages");

            migrationBuilder.DropIndex(
                name: "IX_HcsLanguageTexts_ResourceName_CultureName_Name",
                table: "HcsLanguageTexts");

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguages_CultureName",
                table: "HcsLanguages",
                column: "CultureName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguages_IsDefault",
                table: "HcsLanguages",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguageTexts_ResourceName_CultureName_Name",
                table: "HcsLanguageTexts",
                columns: new[] { "ResourceName", "CultureName", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HcsLanguages_CultureName",
                table: "HcsLanguages");

            migrationBuilder.DropIndex(
                name: "IX_HcsLanguages_IsDefault",
                table: "HcsLanguages");

            migrationBuilder.DropIndex(
                name: "IX_HcsLanguageTexts_ResourceName_CultureName_Name",
                table: "HcsLanguageTexts");

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguages_CultureName",
                table: "HcsLanguages",
                column: "CultureName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguages_IsDefault",
                table: "HcsLanguages",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_HcsLanguageTexts_ResourceName_CultureName_Name",
                table: "HcsLanguageTexts",
                columns: new[] { "ResourceName", "CultureName", "Name" },
                unique: true);
        }
    }
}
