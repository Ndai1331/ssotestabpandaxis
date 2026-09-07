using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_AuthorName_Trgm",
                table: "CollaborationSocialPosts",
                column: "AuthorName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_Hashtags_Trgm",
                table: "CollaborationSocialPosts",
                column: "Hashtags")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationSocialPosts_Text_Trgm",
                table: "CollaborationSocialPosts",
                column: "Text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationMessages_Text_Trgm",
                table: "CollaborationMessages",
                column: "Text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollaborationSocialPosts_AuthorName_Trgm",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropIndex(
                name: "IX_CollaborationSocialPosts_Hashtags_Trgm",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropIndex(
                name: "IX_CollaborationSocialPosts_Text_Trgm",
                table: "CollaborationSocialPosts");

            migrationBuilder.DropIndex(
                name: "IX_CollaborationMessages_Text_Trgm",
                table: "CollaborationMessages");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

        }
    }
}
