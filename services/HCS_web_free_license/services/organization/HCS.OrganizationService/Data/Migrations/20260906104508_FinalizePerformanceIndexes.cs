using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.OrganizationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalizePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_Code_Trgm",
                schema: "hcs_organization",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Provinces_Code_Trgm",
                schema: "hcs_organization",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Positions_Code_Trgm",
                schema: "hcs_organization",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Icd10_Code_Trgm",
                schema: "hcs_organization",
                table: "Icd10");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code_Trgm",
                schema: "hcs_organization",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Code_Trgm",
                schema: "hcs_organization",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Code_Trgm",
                schema: "hcs_organization",
                table: "Units",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Code_Trgm",
                schema: "hcs_organization",
                table: "Provinces",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Code_Trgm",
                schema: "hcs_organization",
                table: "Positions",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Icd10_Code_Trgm",
                schema: "hcs_organization",
                table: "Icd10",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code_Trgm",
                schema: "hcs_organization",
                table: "Departments",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code_Trgm",
                schema: "hcs_organization",
                table: "Countries",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_Code_Trgm",
                schema: "hcs_organization",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Provinces_Code_Trgm",
                schema: "hcs_organization",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Positions_Code_Trgm",
                schema: "hcs_organization",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Icd10_Code_Trgm",
                schema: "hcs_organization",
                table: "Icd10");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code_Trgm",
                schema: "hcs_organization",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Code_Trgm",
                schema: "hcs_organization",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Code_Trgm",
                schema: "hcs_organization",
                table: "Units",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Code_Trgm",
                schema: "hcs_organization",
                table: "Provinces",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Code_Trgm",
                schema: "hcs_organization",
                table: "Positions",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Icd10_Code_Trgm",
                schema: "hcs_organization",
                table: "Icd10",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code_Trgm",
                schema: "hcs_organization",
                table: "Departments",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code_Trgm",
                schema: "hcs_organization",
                table: "Countries",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }
    }
}
