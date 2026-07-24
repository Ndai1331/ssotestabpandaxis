using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hanhchinhso.AIManagementService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbpEventInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    EventName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventData = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandledTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEventInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpEventOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    EventName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventData = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEventOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementApplicationAIProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationName = table.Column<string>(type: "text", nullable: false),
                    SupportedProviders = table.Column<string>(type: "text", nullable: false),
                    SupportedEmbedderProviders = table.Column<string>(type: "text", nullable: false),
                    SupportedVectorStoreProviders = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementApplicationAIProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementDocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementDocumentChunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementMcpServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TransportType = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Command = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Arguments = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    WorkingDirectory = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    EnvironmentVariables = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Headers = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    AuthType = table.Column<int>(type: "integer", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CustomAuthHeaderName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CustomAuthHeaderValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementMcpServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementWorkspaceDataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BlobName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastErrorTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementWorkspaceDataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationName = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: true),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideSystemConfiguration = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredPermissionName = table.Column<string>(type: "text", nullable: true),
                    EmbedderProvider = table.Column<string>(type: "text", nullable: true),
                    EmbedderModelName = table.Column<string>(type: "text", nullable: true),
                    EmbedderApiKey = table.Column<string>(type: "text", nullable: true),
                    EmbedderApiBaseUrl = table.Column<string>(type: "text", nullable: true),
                    VectorStoreProvider = table.Column<string>(type: "text", nullable: true),
                    VectorStoreSettings = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIManagementWorkspaceMcpServers",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    McpServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIManagementWorkspaceMcpServers", x => new { x.WorkspaceId, x.McpServerId });
                    table.ForeignKey(
                        name: "FK_AIManagementWorkspaceMcpServers_AIManagementWorkspaces_Work~",
                        column: x => x.WorkspaceId,
                        principalTable: "AIManagementWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEventInbox_MessageId",
                table: "AbpEventInbox",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpEventInbox_Status_CreationTime",
                table: "AbpEventInbox",
                columns: new[] { "Status", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEventOutbox_CreationTime",
                table: "AbpEventOutbox",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementDocumentChunks_DataSourceId",
                table: "AIManagementDocumentChunks",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementDocumentChunks_WorkspaceId",
                table: "AIManagementDocumentChunks",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementMcpServers_Name",
                table: "AIManagementMcpServers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementWorkspaceDataSources_IsProcessed",
                table: "AIManagementWorkspaceDataSources",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementWorkspaceDataSources_WorkspaceId",
                table: "AIManagementWorkspaceDataSources",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementWorkspaceMcpServers_McpServerId",
                table: "AIManagementWorkspaceMcpServers",
                column: "McpServerId");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementWorkspaceMcpServers_WorkspaceId",
                table: "AIManagementWorkspaceMcpServers",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIManagementWorkspaces_Name",
                table: "AIManagementWorkspaces",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbpEventInbox");

            migrationBuilder.DropTable(
                name: "AbpEventOutbox");

            migrationBuilder.DropTable(
                name: "AIManagementApplicationAIProviders");

            migrationBuilder.DropTable(
                name: "AIManagementDocumentChunks");

            migrationBuilder.DropTable(
                name: "AIManagementMcpServers");

            migrationBuilder.DropTable(
                name: "AIManagementWorkspaceDataSources");

            migrationBuilder.DropTable(
                name: "AIManagementWorkspaceMcpServers");

            migrationBuilder.DropTable(
                name: "AIManagementWorkspaces");
        }
    }
}
