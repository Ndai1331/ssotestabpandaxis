using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollaborationSocialPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationSocialPostComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialPostComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialPostComments_CollaborationSocialPosts_Po~",
                        column: x => x.PostId,
                        principalTable: "CollaborationSocialPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationSocialPostMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationSocialPostMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationSocialPostMedia_CollaborationSocialPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CollaborationSocialPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostComments_ParentCommentId",
                table: "CollaborationSocialPostComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostComments_PostId_CreationTime_Id",
                table: "CollaborationSocialPostComments",
                columns: new[] { "PostId", "CreationTime", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostMedia_PostId",
                table: "CollaborationSocialPostMedia",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPostMedia_UploadedByUserId_PostId",
                table: "CollaborationSocialPostMedia",
                columns: new[] { "UploadedByUserId", "PostId" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_AuthorUserId",
                table: "CollaborationSocialPosts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_Visibility_CreationTime_Id",
                table: "CollaborationSocialPosts",
                columns: new[] { "Visibility", "CreationTime", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaborationSocialPostComments");

            migrationBuilder.DropTable(
                name: "CollaborationSocialPostMedia");

            migrationBuilder.DropTable(
                name: "CollaborationSocialPosts");
        }
    }
}
