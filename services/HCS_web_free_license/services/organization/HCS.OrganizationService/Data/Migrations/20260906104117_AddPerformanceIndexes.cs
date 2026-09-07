using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.OrganizationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Code_Trgm",
                schema: "hcs_organization",
                table: "Units",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name_Trgm",
                schema: "hcs_organization",
                table: "Units",
                column: "Name")
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
                name: "IX_Provinces_Name_Trgm",
                schema: "hcs_organization",
                table: "Provinces",
                column: "Name")
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
                name: "IX_Positions_Name_Trgm",
                schema: "hcs_organization",
                table: "Positions",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterDataItems_Code_Trgm",
                schema: "hcs_organization",
                table: "MasterDataItems",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterDataItems_Name_Trgm",
                schema: "hcs_organization",
                table: "MasterDataItems",
                column: "Name")
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
                name: "IX_Icd10_Name_Trgm",
                schema: "hcs_organization",
                table: "Icd10",
                column: "Name")
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
                name: "IX_Departments_Name_Trgm",
                schema: "hcs_organization",
                table: "Departments",
                column: "Name")
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
                name: "IX_Countries_Name_Trgm",
                schema: "hcs_organization",
                table: "Countries",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Communes_Name_Trgm",
                schema: "hcs_organization",
                table: "Communes",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BmiRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BmiRanges",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BmiRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BmiRanges",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodPressureRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BloodPressureRanges",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodPressureRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BloodPressureRanges",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodGlucoseRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BloodGlucoseRanges",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodGlucoseRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BloodGlucoseRanges",
                column: "Title")
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
                name: "IX_Units_Name_Trgm",
                schema: "hcs_organization",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Provinces_Code_Trgm",
                schema: "hcs_organization",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Provinces_Name_Trgm",
                schema: "hcs_organization",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Positions_Code_Trgm",
                schema: "hcs_organization",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_Name_Trgm",
                schema: "hcs_organization",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_MasterDataItems_Code_Trgm",
                schema: "hcs_organization",
                table: "MasterDataItems");

            migrationBuilder.DropIndex(
                name: "IX_MasterDataItems_Name_Trgm",
                schema: "hcs_organization",
                table: "MasterDataItems");

            migrationBuilder.DropIndex(
                name: "IX_Icd10_Code_Trgm",
                schema: "hcs_organization",
                table: "Icd10");

            migrationBuilder.DropIndex(
                name: "IX_Icd10_Name_Trgm",
                schema: "hcs_organization",
                table: "Icd10");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code_Trgm",
                schema: "hcs_organization",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Name_Trgm",
                schema: "hcs_organization",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Code_Trgm",
                schema: "hcs_organization",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Name_Trgm",
                schema: "hcs_organization",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Communes_Code_Trgm",
                schema: "hcs_organization",
                table: "Communes");

            migrationBuilder.DropIndex(
                name: "IX_Communes_Name_Trgm",
                schema: "hcs_organization",
                table: "Communes");

            migrationBuilder.DropIndex(
                name: "IX_BmiRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BmiRanges");

            migrationBuilder.DropIndex(
                name: "IX_BmiRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BmiRanges");

            migrationBuilder.DropIndex(
                name: "IX_BloodPressureRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BloodPressureRanges");

            migrationBuilder.DropIndex(
                name: "IX_BloodPressureRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BloodPressureRanges");

            migrationBuilder.DropIndex(
                name: "IX_BloodGlucoseRanges_Description_Trgm",
                schema: "hcs_organization",
                table: "BloodGlucoseRanges");

            migrationBuilder.DropIndex(
                name: "IX_BloodGlucoseRanges_Title_Trgm",
                schema: "hcs_organization",
                table: "BloodGlucoseRanges");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

        }
    }
}
