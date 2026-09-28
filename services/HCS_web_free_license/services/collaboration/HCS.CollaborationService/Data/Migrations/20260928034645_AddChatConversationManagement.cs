using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCS.CollaborationService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatConversationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecalledAt",
                table: "CollaborationMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarBlobName",
                table: "CollaborationConversations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "CollaborationConversations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAt",
                table: "CollaborationConversationMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HistoryClearedAt",
                table: "CollaborationConversationMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "CollaborationConversationMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecalledAt",
                table: "CollaborationMessages");

            migrationBuilder.DropColumn(
                name: "AvatarBlobName",
                table: "CollaborationConversations");

            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "CollaborationConversations");

            migrationBuilder.DropColumn(
                name: "HiddenAt",
                table: "CollaborationConversationMembers");

            migrationBuilder.DropColumn(
                name: "HistoryClearedAt",
                table: "CollaborationConversationMembers");

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "CollaborationConversationMembers");
        }
    }
}
