using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <summary>
    /// Rewrites workflow source lineage so every instance SourceFileId points
    /// at an unsigned root file of its own document.
    /// </summary>
    public partial class CorrectWorkflowSourceFileLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WorkflowSourceFileLineageSql.Repair(
                nameof(CorrectWorkflowSourceFileLineage)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair cannot be reverted: the previous SourceFileId values
            // are not recorded anywhere.
        }
    }
}
