using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <summary>
    /// Re-audits workflow source lineage after CorrectWorkflowSourceFileLineage
    /// was already applied locally. Fail-fast on invalid earliest signing attempts,
    /// re-apply corrections, then validate final SourceFileId ownership.
    /// </summary>
    public partial class AuditWorkflowSourceFileLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WorkflowSourceFileLineageSql.Repair(
                nameof(AuditWorkflowSourceFileLineage)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair cannot be reverted: the previous SourceFileId values
            // are not recorded anywhere.
        }
    }
}
