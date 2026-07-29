using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSigningExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BlobDeletionPending",
                table: "SigningAssets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceFileId",
                table: "DocumentFiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SigningAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSignatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignatureType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserSignatureConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningAttempts", x => x.Id);
                    table.CheckConstraint("CK_SigningAttempt_SignatureType", "\"SignatureType\" IN (0, 1)");
                    table.CheckConstraint("CK_SigningAttempt_StateShape", "(\"Status\" = 0 AND \"AttemptCount\" = 0 AND \"StartedAtUtc\" IS NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 1 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 2 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NOT NULL AND \"FailureCode\" IS NULL) OR (\"Status\" = 3 AND \"AttemptCount\" > 0 AND \"StartedAtUtc\" IS NOT NULL AND \"FinishedAtUtc\" IS NOT NULL AND \"ResultFileId\" IS NULL AND \"FailureCode\" IS NOT NULL) OR (\"Status\" = 4 AND \"FinishedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_SigningAttempt_Status", "\"Status\" IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_SigningAttempts_DocumentAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "DocumentAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAttempts_DocumentFiles_ResultFileId",
                        column: x => x.ResultFileId,
                        principalTable: "DocumentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAttempts_DocumentFiles_SourceFileId",
                        column: x => x.SourceFileId,
                        principalTable: "DocumentFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAttempts_DocumentWorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "DocumentWorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SigningAttempts_UserSignatures_UserSignatureId",
                        column: x => x.UserSignatureId,
                        principalTable: "UserSignatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_SourceFileId",
                table: "DocumentFiles",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_AssignmentId",
                table: "SigningAttempts",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_IdempotencyKey",
                table: "SigningAttempts",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"TenantId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_ResultFileId",
                table: "SigningAttempts",
                column: "ResultFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_SourceFileId",
                table: "SigningAttempts",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_TenantId_AssignmentId_Status",
                table: "SigningAttempts",
                columns: new[] { "TenantId", "AssignmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_TenantId_IdempotencyKey",
                table: "SigningAttempts",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_UserSignatureId",
                table: "SigningAttempts",
                column: "UserSignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningAttempts_WorkflowInstanceId",
                table: "SigningAttempts",
                column: "WorkflowInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentFiles_SourceFileId",
                table: "DocumentFiles",
                column: "SourceFileId",
                principalTable: "DocumentFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentFiles_SourceFileId",
                table: "DocumentFiles");

            migrationBuilder.DropTable(
                name: "SigningAttempts");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFiles_SourceFileId",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "BlobDeletionPending",
                table: "SigningAssets");

            migrationBuilder.DropColumn(
                name: "SourceFileId",
                table: "DocumentFiles");
        }
    }
}
