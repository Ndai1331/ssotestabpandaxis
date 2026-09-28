using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillChatCreationTimes : Migration
    {
        // ABP SequentialAsString GUIDs keep DateTime.UtcNow.Ticks / 10000 (48 bits) in the first 12 hex chars.
        // 62135596800000 = milliseconds between 0001-01-01 and the Unix epoch.
        private const string GuidTime =
            "to_timestamp((('x' || lpad(left(replace(\"Id\"::text, '-', ''), 12), 16, '0'))::bit(64)::bigint - 62135596800000) / 1000.0)";

        private static readonly string[] Tables =
        [
            "CollaborationConversations",
            "CollaborationConversationMembers",
            "CollaborationMessages",
            "CollaborationAttachments"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "CreationTime" = {GuidTime}
                    WHERE "CreationTime" < TIMESTAMPTZ '2000-01-01 00:00:00+00'
                      AND {GuidTime} BETWEEN TIMESTAMPTZ '2020-01-01 00:00:00+00' AND now() + INTERVAL '1 day';
                    """);
            }

            migrationBuilder.Sql("""
                UPDATE "CollaborationConversationMembers" m
                SET "CreationTime" = c."CreationTime"
                FROM "CollaborationConversations" c
                WHERE m."ConversationId" = c."Id"
                  AND m."CreationTime" < TIMESTAMPTZ '2000-01-01 00:00:00+00'
                  AND c."CreationTime" >= TIMESTAMPTZ '2000-01-01 00:00:00+00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
