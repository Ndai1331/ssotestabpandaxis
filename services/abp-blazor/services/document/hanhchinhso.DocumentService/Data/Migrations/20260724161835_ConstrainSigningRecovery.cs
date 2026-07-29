using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainSigningRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_SigningAttempt_PendingResultPair",
                table: "SigningAttempts",
                sql: "(\"PendingResultFileId\" IS NULL) = (\"PendingResultBlobName\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SigningAttempt_PendingResultPair",
                table: "SigningAttempts");
        }
    }
}
