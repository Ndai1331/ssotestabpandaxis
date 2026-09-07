using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.WorkManagementService.Migrations
{
    /// <inheritdoc />
    public partial class FinalizePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_Code_Trgm",
                schema: "hcs_work",
                table: "ProjectTasks",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_Title_Trgm",
                schema: "hcs_work",
                table: "ProjectTasks",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code_Trgm",
                schema: "hcs_work",
                table: "Projects",
                column: "Code")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name_Trgm",
                schema: "hcs_work",
                table: "Projects",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_Code_Trgm",
                schema: "hcs_work",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_Title_Trgm",
                schema: "hcs_work",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Code_Trgm",
                schema: "hcs_work",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Name_Trgm",
                schema: "hcs_work",
                table: "Projects");
        }
    }
}
