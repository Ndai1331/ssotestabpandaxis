using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenSigningExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SigningAttempt_StateShape",
                table: "SigningAttempts");

            migrationBuilder.AddColumn<string>(
                name: "PendingResultBlobName",
                table: "SigningAttempts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingResultFileId",
                table: "SigningAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSignedFileId",
                table: "DocumentWorkflowInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_Status_PendingResultFileId_StartedAtUtc",
                table: "SigningAttempts",
                columns: new[] { "Status", "PendingResultFileId", "StartedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SigningAttempt_StateShape",
                table: "SigningAttempts",
                sql: "(\"Status\" = 0 AND \"AttemptCount\" = 0 AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"PendingResultFileId\" IS NULL AND \"PendingResultBlobName\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 1 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 2 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NOT NULL AND \"PendingResultFileId\" IS NULL AND \"PendingResultBlobName\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 3 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NOT NULL) OR (\"Status\" = 4 AND \"FinishedAtUtc\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_CurrentSignedFileId",
                table: "DocumentWorkflowInstances",
                column: "CurrentSignedFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentWorkflowInstances_DocumentFiles_CurrentSignedFileId",
                table: "DocumentWorkflowInstances",
                column: "CurrentSignedFileId",
                principalTable: "DocumentFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentWorkflowInstances_DocumentFiles_CurrentSignedFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_SigningAttempts_Status_PendingResultFileId_StartedAtUtc",
                table: "SigningAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SigningAttempt_StateShape",
                table: "SigningAttempts");

            migrationBuilder.DropIndex(
                name: "IX_DocumentWorkflowInstances_CurrentSignedFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropColumn(
                name: "PendingResultBlobName",
                table: "SigningAttempts");

            migrationBuilder.DropColumn(
                name: "PendingResultFileId",
                table: "SigningAttempts");

            migrationBuilder.DropColumn(
                name: "CurrentSignedFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SigningAttempt_StateShape",
                table: "SigningAttempts",
                sql: "(\"Status\" = 0 AND \"AttemptCount\" = 0 AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 1 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 2 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NOT NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 3 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NOT NULL) OR (\"Status\" = 4 AND \"FinishedAtUtc\" IS NOT NULL)");
        }
    }
}
