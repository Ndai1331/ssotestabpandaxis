using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class BindWorkflowSourceFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceFileId",
                table: "DocumentWorkflowInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "DocumentWorkflowInstances" AS instance
                SET "SourceFileId" = (
                    SELECT attempt."SourceFileId"
                    FROM "SigningAttempts" AS attempt
                    WHERE attempt."WorkflowInstanceId" = instance."Id"
                      AND attempt."SourceFileId" IN (
                          SELECT file."Id"
                          FROM "DocumentFiles" AS file
                          WHERE file."DocumentId" = instance."DocumentId")
                    ORDER BY attempt."CreationTime", attempt."Id"
                    LIMIT 1)
                WHERE instance."SourceFileId" IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "SigningAttempts" AS attempt
                      WHERE attempt."WorkflowInstanceId" = instance."Id"
                        AND attempt."SourceFileId" IN (
                            SELECT file."Id"
                            FROM "DocumentFiles" AS file
                            WHERE file."DocumentId" =
                                instance."DocumentId"));

                UPDATE "DocumentWorkflowInstances" AS instance
                SET "SourceFileId" = (
                    WITH RECURSIVE lineage AS (
                        SELECT file."Id", file."SourceFileId",
                               file."DocumentId", 0 AS depth
                        FROM "DocumentFiles" AS file
                        WHERE file."Id" = instance."CurrentSignedFileId"
                          AND file."DocumentId" = instance."DocumentId"
                        UNION ALL
                        SELECT parent."Id", parent."SourceFileId",
                               parent."DocumentId", child.depth + 1
                        FROM "DocumentFiles" AS parent
                        JOIN lineage AS child
                          ON parent."Id" = child."SourceFileId"
                        WHERE parent."DocumentId" =
                            instance."DocumentId"
                          AND child.depth < 100
                    )
                    SELECT lineage."Id"
                    FROM lineage
                    WHERE lineage."SourceFileId" IS NULL
                    ORDER BY lineage.depth DESC
                    LIMIT 1)
                WHERE instance."SourceFileId" IS NULL
                  AND instance."CurrentSignedFileId" IS NOT NULL;

                UPDATE "DocumentWorkflowInstances" AS instance
                SET "SourceFileId" = (
                    SELECT file."Id"
                    FROM "DocumentFiles" AS file
                    WHERE file."DocumentId" = instance."DocumentId"
                      AND NOT file."BlobDeletionPending"
                    LIMIT 1)
                WHERE instance."SourceFileId" IS NULL
                  AND (
                      SELECT COUNT(*)
                      FROM "DocumentFiles" AS file
                      WHERE file."DocumentId" = instance."DocumentId"
                        AND NOT file."BlobDeletionPending") = 1;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "DocumentWorkflowInstances"
                        WHERE "SourceFileId" IS NULL)
                    THEN
                        RAISE EXCEPTION
                            'Cannot infer an immutable source file for every workflow instance. Backfill SourceFileId explicitly before retrying BindWorkflowSourceFile.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceFileId",
                table: "DocumentWorkflowInstances",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_SourceFileId",
                table: "DocumentWorkflowInstances",
                column: "SourceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentWorkflowInstances_TenantId_SourceFileId",
                table: "DocumentWorkflowInstances",
                columns: new[] { "TenantId", "SourceFileId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentWorkflowInstances_DocumentFiles_SourceFileId",
                table: "DocumentWorkflowInstances",
                column: "SourceFileId",
                principalTable: "DocumentFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentWorkflowInstances_DocumentFiles_SourceFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_DocumentWorkflowInstances_SourceFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_DocumentWorkflowInstances_TenantId_SourceFileId",
                table: "DocumentWorkflowInstances");

            migrationBuilder.DropColumn(
                name: "SourceFileId",
                table: "DocumentWorkflowInstances");
        }
    }
}
