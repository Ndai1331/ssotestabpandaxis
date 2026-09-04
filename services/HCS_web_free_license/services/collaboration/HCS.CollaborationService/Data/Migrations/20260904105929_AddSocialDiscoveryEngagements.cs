using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialDiscoveryEngagements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hashtags",
                table: "CollaborationSocialPosts",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkDescription",
                table: "CollaborationSocialPosts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkImageUrl",
                table: "CollaborationSocialPosts",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkSiteName",
                table: "CollaborationSocialPosts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkTitle",
                table: "CollaborationSocialPosts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "CollaborationSocialPosts",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "CollaborationSocialPosts" AS p
                SET "Hashtags" = COALESCE((
                    SELECT string_agg('|' || tags.tag || '|', '')
                    FROM (
                        SELECT DISTINCT lower(matches[2]) AS tag
                        FROM regexp_matches(p."Text", '(^|[^[:alnum:]_])#([[:alpha:][:digit:]_-]{1,64})', 'gi') AS matches
                    ) AS tags
                ), '');
                """);

            migrationBuilder.CreateTable(
                name: "CollaborationSocialCommentReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionType = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialCommentReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialCommentReactions_CollaborationSocialPost~",
                        column: x => x.CommentId,
                        principalTable: "CollaborationSocialPostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationSocialPostReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionType = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialPostReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialPostReactions_CollaborationSocialPosts_P~",
                        column: x => x.PostId,
                        principalTable: "CollaborationSocialPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationSocialPostShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialPostShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialPostShares_CollaborationSocialPosts_Post~",
                        column: x => x.PostId,
                        principalTable: "CollaborationSocialPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_Hashtags",
                table: "CollaborationSocialPosts",
                column: "Hashtags");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialCommentReactions_CommentId_ReactionType",
                table: "CollaborationSocialCommentReactions",
                columns: new[] { "CommentId", "ReactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialCommentReactions_CommentId_UserId",
                table: "CollaborationSocialCommentReactions",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostReactions_PostId_ReactionType",
                table: "CollaborationSocialPostReactions",
                columns: new[] { "PostId", "ReactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostReactions_PostId_UserId",
                table: "CollaborationSocialPostReactions",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostShares_PostId",
                table: "CollaborationSocialPostShares",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostShares_PostId_UserId",
                table: "CollaborationSocialPostShares",
                columns: new[] { "PostId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaborationSocialCommentReactions");

            migrationBuilder.DropTable(
                name: "CollaborationSocialPostReactions");

            migrationBuilder.DropTable(
                name: "CollaborationSocialPostShares");

            migrationBuilder.DropIndex(
                name: "IX_CollaborationSocialPosts_Hashtags",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "Hashtags",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "LinkDescription",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "LinkImageUrl",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "LinkSiteName",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "LinkTitle",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "CollaborationSocialPosts");
        }
    }
}
