using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialCommentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkDescription",
                table: "CollaborationSocialPostComments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkImageUrl",
                table: "CollaborationSocialPostComments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkSiteName",
                table: "CollaborationSocialPostComments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkTitle",
                table: "CollaborationSocialPostComments",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "CollaborationSocialPostComments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollaborationSocialCommentAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialCommentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialCommentAttachments_CollaborationSocialPo~",
                        column: x => x.CommentId,
                        principalTable: "CollaborationSocialPostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialCommentAttachments_CommentId",
                table: "CollaborationSocialCommentAttachments",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialCommentAttachments_UploadedByUserId_Comm~",
                table: "CollaborationSocialCommentAttachments",
                columns: new[] { "UploadedByUserId", "CommentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaborationSocialCommentAttachments");

            migrationBuilder.DropColumn(
                name: "LinkDescription",
                table: "CollaborationSocialPostComments");

            migrationBuilder.DropColumn(
                name: "LinkImageUrl",
                table: "CollaborationSocialPostComments");

            migrationBuilder.DropColumn(
                name: "LinkSiteName",
                table: "CollaborationSocialPostComments");

            migrationBuilder.DropColumn(
                name: "LinkTitle",
                table: "CollaborationSocialPostComments");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "CollaborationSocialPostComments");
        }
    }
}
