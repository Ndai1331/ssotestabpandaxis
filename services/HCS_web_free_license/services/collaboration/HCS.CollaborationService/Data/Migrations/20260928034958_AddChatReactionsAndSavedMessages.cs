using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReactionsAndSavedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollaborationChatMessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationChatMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationChatMessageReactions_CollaborationMessages_Mes~",
                        column: x => x.MessageId,
                        principalTable: "CollaborationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationChatSavedMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationChatSavedMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationChatSavedMessages_CollaborationMessages_Messag~",
                        column: x => x.MessageId,
                        principalTable: "CollaborationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationChatMessageReactions_MessageId_UserId",
                table: "CollaborationChatMessageReactions",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationChatSavedMessages_MessageId",
                table: "CollaborationChatSavedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationChatSavedMessages_UserId_CreationTime",
                table: "CollaborationChatSavedMessages",
                columns: new[] { "UserId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationChatSavedMessages_UserId_MessageId",
                table: "CollaborationChatSavedMessages",
                columns: new[] { "UserId", "MessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaborationChatMessageReactions");

            migrationBuilder.DropTable(
                name: "CollaborationChatSavedMessages");
        }
    }
}
