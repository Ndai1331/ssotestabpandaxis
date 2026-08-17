namespace hanhchinhso.DocumentService.Data.Migrations;

/// <summary>
/// Repair script for DocumentWorkflowInstances.SourceFileId lineage, shared by
/// the migrations that apply it. It fails fast when the earliest signing
/// attempt of an instance points at a missing file or a file owned by another
/// document, rewrites SourceFileId from the earliest valid signing attempt (or
/// from the root of the signed-file lineage when no attempt exists), then
/// re-validates that every instance points at a file of its own document.
/// Already-applied migrations reference this text, so treat it as frozen:
/// changes here silently alter what those migrations do on databases that have
/// not run them yet. Add a new migration instead.
/// </summary>
internal static class WorkflowSourceFileLineageSql
{
    public static string Repair(string migrationName) =>
        $"""
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM "DocumentWorkflowInstances" AS instance
                CROSS JOIN LATERAL (
                    SELECT attempt."SourceFileId"
                    FROM "SigningAttempts" AS attempt
                    WHERE attempt."WorkflowInstanceId" =
                        instance."Id"
                    ORDER BY attempt."CreationTime",
                             attempt."Id"
                    LIMIT 1
                ) AS first_attempt
                LEFT JOIN "DocumentFiles" AS file
                  ON file."Id" = first_attempt."SourceFileId"
                 AND file."DocumentId" = instance."DocumentId"
                WHERE file."Id" IS NULL)
            THEN
                RAISE EXCEPTION
                    'Earliest SigningAttempt SourceFileId is missing or belongs to a different document. Correct invalid signing attempts before retrying {migrationName}.';
            END IF;
        END $$;

        UPDATE "DocumentWorkflowInstances" AS instance
        SET "SourceFileId" = (
            SELECT attempt."SourceFileId"
            FROM "SigningAttempts" AS attempt
            JOIN "DocumentFiles" AS file
              ON file."Id" = attempt."SourceFileId"
             AND file."DocumentId" = instance."DocumentId"
            WHERE attempt."WorkflowInstanceId" = instance."Id"
            ORDER BY attempt."CreationTime", attempt."Id"
            LIMIT 1)
        WHERE EXISTS (
            SELECT 1
            FROM "SigningAttempts" AS attempt
            JOIN "DocumentFiles" AS file
              ON file."Id" = attempt."SourceFileId"
             AND file."DocumentId" = instance."DocumentId"
            WHERE attempt."WorkflowInstanceId" = instance."Id");

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
        WHERE instance."CurrentSignedFileId" IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM "SigningAttempts" AS attempt
              WHERE attempt."WorkflowInstanceId" = instance."Id");

        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM "DocumentWorkflowInstances" AS instance
                LEFT JOIN "DocumentFiles" AS file
                  ON file."Id" = instance."SourceFileId"
                 AND file."DocumentId" = instance."DocumentId"
                WHERE file."Id" IS NULL)
            THEN
                RAISE EXCEPTION
                    'Workflow SourceFileId lineage validation failed after {migrationName}. Every instance SourceFileId must reference a DocumentFile on the same document.';
            END IF;
        END $$;
        """;
}
